using System;
using Microsoft.SPOT;
using System.IO.Ports;
using Microsoft.SPOT.Hardware;
using imBMW.Tools;

namespace imBMW.Multimedia
{
    public class BluetoothOVC3860 : AudioPlayerBase
    {
        SerialPortBase port;

        public BluetoothOVC3860(string port)
        {
            ShortName = "BlueTooth";
            this.port = new SerialInterruptPort(new SerialPortConfiguration(port, BaudRate.Baudrate115200), Cpu.Pin.GPIO_NONE, 0, 16, 10);
            this.port.DataReceived += port_DataReceived;
        }

        #region IAudioPlayer members

        public override void Next()
        {
            SendCommand(CmdNext);
        }

        public override void Prev()
        {
            SendCommand(CmdPrev);
        }

        public override void MFLRT()
        {
            throw new NotImplementedException();
        }

        public override void MFLDial()
        {
            throw new NotImplementedException();
        }

        public override void MFLDialLong()
        {
            throw new NotImplementedException();
        }

        public override bool RandomToggle()
        {
            throw new NotImplementedException();
        }

        public override void VolumeUp()
        {
            SendCommand(CmdVolumeUp);
        }

        public override void VolumeDown()
        {
            SendCommand(CmdVolumeDown);
        }

        public override bool IsPlaying
        {
            get
            {
                return isPlaying;
            }
            protected set
            {
                if (isPlaying == value)
                {
                    return;
                }
                isPlaying = value;
                // TODO Change this flag only on BT event

                if (IsCurrentPlayer)
                {
                    SendCommand(CmdPlayPause);
                }
                else
                {
                    SendCommand(CmdStop);
                }
            }
        }

        #endregion

        #region OVC3860 members

        const string CmdPlayPause = "MA";
        const string CmdStop = "MC";
        const string CmdNext = "MD";
        const string CmdPrev = "ME";
        const string CmdVolumeUp = "VU";
        const string CmdVolumeDown = "VD";

        void SendCommand(string command, string param = null)
        {
            command = "AT#" + command;
            if (param != null)
            {
                command += param;
            }
            port.WriteLine(command);
            Logger.Info(command, "BT>");
        }

        void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            return;
            var port = (SerialPortBase)sender;
            if (port.AvailableBytes == 0)
            {
                return;
            }
            // TODO Use ReadLine()
            var data = port.ReadAvailable();
            /*while (port.AvailableBytes > 0)
            {
                var data = port.ReadLine();
                if (data == null || data.Length == 0)
                {
                    continue;
                }*/
                // TODO Use port.Encoding
                var chars = new char[data.Length];
                for (int i = 0; i < data.Length; i++)
                {
                    chars[i] = (char)data[i];
                    if (chars[i] == '\r' || chars[i] == '\n')
                    {
                        chars[i] = ' ';
                    }
                }
                Logger.Info(new string(chars), "BT<");
            //}
        }

        #endregion
    }
}
