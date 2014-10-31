using System;
using Microsoft.SPOT;
using System.IO.Ports;
using Microsoft.SPOT.Hardware;
using System.Text;
using imBMW.Tools;

namespace imBMW.Multimedia
{
    public class BluetoothWT32 : AudioPlayerBase
    {
        SerialPortBase port;

        public BluetoothWT32(string port)
        {
            Name = "Bluetooth";

            queue = new QueueThreadWorker(ProcessSendCommand);

            this.port = new SerialInterruptPort(new SerialPortConfiguration(port, BaudRate.Baudrate115200, Parity.None, 8, StopBits.One, true), Cpu.Pin.GPIO_NONE, 0, 16, 10);
            this.port.NewLine = "\n";
            this.port.DataReceived += port_DataReceived;
        }

        QueueThreadWorker queue;
        byte[] btBuffer;
        string lastCommand = "";
        string connectedAddress = "";

        void SendCommand(string command, string param = null)
        {
            lastCommand = command;
            if (param != null)
            {
                command += " " + param;
            }
            queue.Enqueue(command);
        }

        void ProcessSendCommand(object o)
        {
            var s = (string)o;
            port.WriteLine(s);
            Logger.Info(s, "> BT");
        }

        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (port.AvailableBytes == 0)
            {
                return;
            }
            var data = port.ReadAvailable();
            if (btBuffer == null)
            {
                btBuffer = data;
            }
            else
            {
                btBuffer = btBuffer.Combine(data);
            }
            int index;
            while (btBuffer != null && ((index = btBuffer.IndexOf(0x0D)) != -1 || (index = btBuffer.IndexOf(0x0A)) != -1))
            {
                var s = index == 0 ? String.Empty : Encoding.UTF8.GetString(btBuffer.SkipAndTake(0, index));
                if (s != String.Empty)
                {
                    ProcessBTNotification(s);
                }
                var skip = btBuffer[index] == 0x0A ? 1 : 2;
                if (index + skip >= btBuffer.Length)
                {
                    btBuffer = null;
                }
                else
                {
                    btBuffer = btBuffer.Skip(index + skip);
                }
            }
        }

        int initStep = 0;

        private void ProcessBTNotification(string s)
        {
            s = s.Trim();
            Logger.Info(s, "BT <");
            var p = s.IndexOf(' ') > 0 ? s.Split(' ') : null;
            if (p != null)
            {
                s = p[0];
            }
            var plen = p == null ? 0 : p.Length;
            switch (s)
            {
                case "READY.":
                    switch (initStep)
                    {
                        case 0:
                            SendCommand("SET PROFILE A2DP", "SINK");
                            SendCommand("SET PROFILE AVRCP", "CONTROLLER");
                            SendCommand("SET BT CLASS", "240428");
                            SendCommand("RESET");
                            break;
                        case 1:
                            SendCommand("SET BT SSP 3 0");
                            SendCommand("SET BT AUTH * 0000");
                            SendCommand("SET BT NAME imBMW");
                            SendCommand("RESET");
                            break;
                        case 2:
                            SendCommand("SET");
                            break;
                    }
                    initStep++;
                    break;
                case "RING":
                    if (plen == 5)
                    {
                        switch (p[4])
                        {
                            case "A2DP":
                                connectedAddress = p[2];
                                if (p[1] == "1")
                                {
                                    SendCommand("CALL " + connectedAddress + " 17 AVRCP");
                                }
                                break;
                        }
                    }
                    break;
                case "CONNECT":
                    if (plen == 4)
                    {
                        if (p[2] == "AVRCP")
                        {
                            SendCommand("AVRCP PDU 50 0");
                            SendCommand("AVRCP PDU 20 0");
                            SendCommand("AVRCP PDU 10 3");
                            SendCommand("AVRCP PDU 31 1 0d");
                            SendCommand("AVRCP UP");
                        }
                    }
                    break;
                case "AVRCP":
                    if (plen > 5 && p[1] == "GET_ELEMENT_ATTRIBUTES_RSP")
                    {
                        var track = p[5]; // TODO find closing quote
                    }
                    break;
            }
        }

        public override void Next()
        {
            throw new NotImplementedException();
        }

        public override void Prev()
        {
            throw new NotImplementedException();
        }

        public override void MFLRT()
        {
            throw new NotImplementedException();
        }

        public override void VoiceButtonPress()
        {
            throw new NotImplementedException();
        }

        public override void VoiceButtonLongPress()
        {
            throw new NotImplementedException();
        }

        public override bool RandomToggle()
        {
            throw new NotImplementedException();
        }

        public override void VolumeUp()
        {
            throw new NotImplementedException();
        }

        public override void VolumeDown()
        {
            throw new NotImplementedException();
        }

        public override Features.Menu.MenuScreen Menu
        {
            get { throw new NotImplementedException(); }
        }

        public override bool IsPlaying
        {
            get
            {
                return false;  throw new NotImplementedException();
            }
            protected set
            {
                throw new NotImplementedException();
            }
        }
    }
}
