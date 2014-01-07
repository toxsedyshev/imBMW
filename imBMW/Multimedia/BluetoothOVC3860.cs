using System;
using Microsoft.SPOT;
using System.IO.Ports;
using Microsoft.SPOT.Hardware;
using imBMW.Tools;
using System.Threading;

namespace imBMW.Multimedia
{
    public class BluetoothOVC3860 : AudioPlayerBase
    {
        SerialPortBase port;
        QueueThreadWorker queue;

        public BluetoothOVC3860(string port)
        {
            ShortName = "BlueTooth";
            
            this.port = new SerialInterruptPort(new SerialPortConfiguration(port, BaudRate.Baudrate115200), Cpu.Pin.GPIO_NONE, 0, 16, 10);
            this.port.DataReceived += port_DataReceived;

            queue = new QueueThreadWorker(ProcessSendCommand);
        }

        #region Private methods

        void SendPlayPause(bool value)
        {
            if (IsCurrentPlayer)
            {
                if (value != IsPlaying)
                {
                    SendCommand(CmdPlayPause);
                }
            }
            else
            {
                SendCommand(CmdStop);
            }
        }

        #endregion

        #region IAudioPlayer members

        public override void Play()
        {
            SendPlayPause(true);
        }

        public override void Pause()
        {
            SendPlayPause(false);
        }

        public override void PlayPauseToggle()
        {
            SendPlayPause(!IsPlaying);
        }

        public override void Next()
        {
            SendPlayPause(true);
            SendCommand(CmdNext);
        }

        public override void Prev()
        {
            SendPlayPause(true);
            SendCommand(CmdPrev);
        }

        public override void MFLRT()
        {
            PlayPauseToggle();
        }

        public override void MFLDial()
        {
            SendCommand(CmdAnswer);
        }

        public override void MFLDialLong()
        {
            SendCommand(CmdVoiceCall);
        }

        public override bool RandomToggle()
        {
            SendCommand(CmdEnterPairing);
            return false;
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
                // TODO Fire event
                Logger.Info(value ? "Playing" : "Paused", "BT");
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
        const string CmdVoiceCall = "CI";
        const string CmdEnterPairing = "CA";
        const string CmdAnswer = "CE";

        void SendCommand(string command, string param = null)
        {
            command = "AT#" + command;
            if (param != null)
            {
                command += param;
            }
            queue.Enqueue(command);
        }

        void ProcessSendCommand(object o)
        {
            var s = (string)o;
            port.WriteLine(s);
            Logger.Info(s, "BT>");
        }

        void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (port.AvailableBytes == 0)
            {
                return;
            }
            var data = port.ReadAvailable();
            var s = port.Encoding.GetString(data);
            var t = "";
            foreach (var c in s)
            {
                if (c == '\r' || c == '\n')
                {
                    if (t != "")
                    {
                        ProcessBTNotification(t);
                    }
                    t = "";
                }
                else
                {
                    t += c;
                }
            }
        }

        void ProcessBTNotification(string s)
        {
            Logger.Info(s, "BT<");
            switch (s)
            {
                case "MR":
                    IsPlaying = true;
                    break;
                case "MP":
                    IsPlaying = false;
                    break;
                case "MX":
                    Logger.Info("Next", "BT");
                    break;
                case "MS":
                    Logger.Info("Prev", "BT");
                    break;
                case "IV":
                    Logger.Info("Connected", "BT");
                    break;
                case "II":
                case "IA":
                    Logger.Info("Disconnected", "BT");
                    break;
                case "IJ2":
                    Logger.Info("Connecting", "BT");
                    break;
            }
        }

        #endregion
    }
}
