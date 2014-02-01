using System;
using Microsoft.SPOT;
using System.IO.Ports;
using Microsoft.SPOT.Hardware;
using imBMW.Tools;
using System.Threading;

namespace imBMW.Multimedia
{
    /// <summary>
    /// Bluetooth module Bolutek BLK-MD-SPK-B based on OmniVision OVC3860 that supports A2DP and AVRCP profiles
    /// Communicates via COM port
    /// </summary>
    public class BluetoothOVC3860 : AudioPlayerBase
    {
        SerialPortBase port;
        QueueThreadWorker queue;

        /// <summary>
        /// </summary>
        /// <param name="port">COM port name</param>
        public BluetoothOVC3860(string port)
        {
            Name = "Bluetooth";
            
            this.port = new SerialInterruptPort(new SerialPortConfiguration(port, BaudRate.Baudrate115200), Cpu.Pin.GPIO_NONE, 0, 16, 10);
            this.port.DataReceived += port_DataReceived;

            queue = new QueueThreadWorker(ProcessSendCommand);
        }

        #region Private methods

        protected override void SetPlaying(bool value)
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

        public override void Next()
        {
            SetPlaying(true);
            SendCommand(CmdNext);
            OnStatusChanged(CharIcons.Next + "Bluetooth");
        }

        public override void Prev()
        {
            SetPlaying(true);
            SendCommand(CmdPrev);
            OnStatusChanged(CharIcons.Prev + "Bluetooth");
        }

        public override void MFLRT()
        {
            PlayPauseToggle();
        }

        public override void MFLDial()
        {
            SendCommand(CmdAnswer);
            OnStatusChanged(((char)0xC8) + "AnswerCall");
        }

        public override void MFLDialLong()
        {
            SendCommand(CmdVoiceCall);
            OnStatusChanged(((char)0xC8) + " VoiceCall");
        }

        public override bool RandomToggle()
        {
            SendCommand(CmdEnterPairing);
            OnStatusChanged(((char)0xC9) + "Disconnect");
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
                OnIsPlayingChanged(value);
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

        string btBuffer = "";

        void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (port.AvailableBytes == 0)
            {
                return;
            }
            var data = port.ReadAvailable();
            var s = port.Encoding.GetString(data);
            foreach (var c in s)
            {
                if (c == '\r' || c == '\n')
                {
                    if (btBuffer != "")
                    {
                        ProcessBTNotification(btBuffer);
                    }
                    btBuffer = "";
                }
                else
                {
                    btBuffer += c;
                }
            }
        }

        void ProcessBTNotification(string s)
        {
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
                    OnStatusChanged(((char)0xC9) + " Bluetooth");
                    if (IsPlayerHostActive && IsCurrentPlayer)
                    {
                        Play();
                    }
                    break;
                case "II":
                    IsPlaying = false;
                    Logger.Info("Waiting", "BT");
                    OnStatusChanged(((char)0xC9) + "BT Waiting");
                    break;
                case "IA":
                    IsPlaying = false;
                    Logger.Info("Disconnected", "BT");
                    OnStatusChanged(((char)0xC9) + "Disconnect");
                    break;
                case "IJ2":
                    IsPlaying = false;
                    Logger.Info("Cancel pairing", "BT");
                    OnStatusChanged(((char)0xC9) + " No BT");
                    break;
                default:
                    if (s.IsNumeric())
                    {
                        Logger.Info("Phone call: " + s, "BT");
                        OnStatusChanged(((char)0xC8) + s);
                    }
                    else
                    {
                        Logger.Info(s, "BT<");
                    }
                    break;
            }
        }

        #endregion
    }
}
