using System;
using Microsoft.SPOT;
using imBMW.Tools;

namespace imBMW.iBus.Devices.Real
{
    #region Enums, delegales and event args

    public class LightStatusEventArgs
    {
        public bool ParkingLightsOn;
        public bool LowBeamOn;

        public byte ErrorCode;
        public bool ErrorFrontLeftLights;
        public bool ErrorFrontRightsLights;
    }

    public delegate void LightStatusEventHandler(Message message, LightStatusEventArgs args);

    #endregion

    public static class LightControlModule
    {
        static LightControlModule()
        {
            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.LightControlModule, ProcessLCMMessage);
        }

        static void ProcessLCMMessage(Message m)
        {
            if (m.Data.Length == 5 && m.Data[0] == 0x5B)
            {
                OnLightStatusReceived(m);
            }
        }

        static void OnLightStatusReceived(Message m)
        {
            // TODO Hack Data[3] and other bits meaning
            var on = m.Data[1];
            var error = m.Data[2];
            var errorUnk = m.Data[3];
            var errorReason = m.Data[4];

            string description = "";
            var args = new LightStatusEventArgs();
            if (on == 0)
            {
                description = "Lights Off ";
            }
            else
            {
                if (on.HasBit(0))
                {
                    args.ParkingLightsOn = true;
                    on = on.RemoveBit(0);
                    description += "Park ";
                }
                if (on.HasBit(1))
                {
                    args.LowBeamOn = true;
                    on = on.RemoveBit(1);
                    description += "LowBeam ";
                }
                if (on != 0)
                {
                    description += "Unknown=" + on.ToHex() + " ";
                }
            }
            if (error != 0 || errorReason != 0)
            {
                description += "| Errors";
                if (error != 0x01)
                {
                    description += "=" + error.ToHex();
                }
                description += ": ";
            }
            args.ErrorCode = error;
            if (errorReason == 0)
            {
                if (error != 0)
                {
                    description += "Unknown=" + errorUnk.ToHex() + "00";
                }
                else
                {
                    description += "| Lights OK";
                }
            }
            else
            {
                if (errorReason.HasBit(4))
                {
                    args.ErrorFrontRightsLights = true;
                    errorReason = errorReason.RemoveBit(4);
                    description += "FrontRight ";
                }
                if (errorReason.HasBit(5))
                {
                    args.ErrorFrontLeftLights = true;
                    errorReason = errorReason.RemoveBit(5);
                    description += "FrontLeft ";
                }
                if (errorReason != 0)
                {
                    description += "Unknown=" + errorReason.ToHex();
                }
            }
            m.ReceiverDescription = description;

            var e = LightStatusReceived;
            if (e != null)
            {
                e(m, args);
            }
        }

        public static event LightStatusEventHandler LightStatusReceived;
    }
}
