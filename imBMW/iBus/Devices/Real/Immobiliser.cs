using System;
using Microsoft.SPOT;
using imBMW.Tools;

namespace imBMW.iBus.Devices.Real
{
    #region Enums, delegales and event args

    public class KeyEventArgs : EventArgs
    {
        public byte KeyNumber { get; private set; }

        public KeyEventArgs(byte keyNumber)
        {
            KeyNumber = keyNumber;
        }
    }

    public delegate void KeyInsertedEventHandler(KeyEventArgs e);

    public delegate void KeyRemovedEventHandler(KeyEventArgs e);

    #endregion


    public static class Immobiliser
    {
        public static byte LastKeyInserted { get; private set; }
        public static bool IsKeyInserted { get; private set; }

        static Immobiliser()
        {
            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.Immobiliser, ProcessEWSMessage);
        }

        static void ProcessEWSMessage(Message m)
        {
            if (m.Data.Length == 3 && m.Data[0] == 0x74)
            {
                if (m.Data[1] == 0x04)
                {
                    IsKeyInserted = true;
                    LastKeyInserted = m.Data[2];
                    var e = KeyInserted;
                    if (e != null)
                    {
                        e(new KeyEventArgs(LastKeyInserted));
                    }
                    Logger.Info("Key " + LastKeyInserted + " inserted");
                }
                else if (m.Data[1] == 0x00)
                {
                    IsKeyInserted = false;
                    var e = KeyRemoved;
                    if (e != null)
                    {
                        e(new KeyEventArgs(LastKeyInserted));
                    }
                    Logger.Info("Key " + LastKeyInserted + " removed");
                }
            }
        }

        public static event KeyInsertedEventHandler KeyInserted;

        public static event KeyRemovedEventHandler KeyRemoved;
    }
}
