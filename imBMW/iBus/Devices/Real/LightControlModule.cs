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
                if ((on & 0x01) != 0)
                {
                    args.ParkingLightsOn = true;
                    on &= ((byte)0x01).Invert();
                    description += "Park ";
                }
                if ((on & 0x02) != 0)
                {
                    args.LowBeamOn = true;
                    on &= ((byte)0x02).Invert();
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
                if ((errorReason & 0x10) != 0)
                {
                    args.ErrorFrontRightsLights = true;
                    errorReason &= ((byte)0x10).Invert();
                    description += "FrontRight ";
                }
                if ((errorReason & 0x20) != 0)
                {
                    args.ErrorFrontLeftLights = true;
                    errorReason &= ((byte)0x20).Invert();
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
