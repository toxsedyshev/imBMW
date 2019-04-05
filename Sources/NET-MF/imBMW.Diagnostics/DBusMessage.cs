using System;
using imBMW.Tools;
using System.Text;
using imBMW.iBus;

namespace imBMW.Diagnostics
{
    /// <summary>
    /// BMW DS2 Diagnostic Bus (DBus) message packet
    /// </summary>
    public class DBusMessage : Message
    {
        byte[] packet;
        byte check;
        int packetLength;

        public static new int PacketLengthMin { get { return 4; } }

        string dataString;
        
        public DBusMessage(DeviceAddress device, params byte[] data)
            : this(device, null, data)
        { }

        public DBusMessage(DeviceAddress device, string description, params byte[] data)
            : base (device, DeviceAddress.Diagnostic, description, data)
        {
            // packet = device + length + data + chksum
            //          |     ===== length =====      |

            var packetLength = data.Length + 3;
            byte check = 0x00;
            check ^= (byte)device;
            check ^= (byte)packetLength;
            foreach (byte b in data)
            {
                check ^= b;
            }

            PacketLength = packetLength;
            CRC = check;
        }

        public DeviceAddress Device
        {
            get
            {
                return SourceDevice;
            }
        }

        public override DeviceAddress DestinationDevice
        {
            get
            {
                return DeviceAddress.Diagnostic;
            }
        }

        public string DataString
        {
            get
            {
                if (dataString == null)
                {
                    dataString = Encoding.UTF8.GetString(Data);
                }
                return dataString;
            }
        }

        public new byte CRC
        {
            get
            {
                return check;
            }
            private set
            {
                check = value;
            }
        }

        public new int PacketLength
        {
            get
            {
                return packetLength;
            }
            private set
            {
                packetLength = value;
            }
        }

        public new byte[] Packet
        {
            get
            {
                if (this.packet != null)
                {
                    return this.packet;
                }

                byte[] packet = new byte[PacketLength];
                packet[0] = (byte)Device;
                packet[1] = (byte)(PacketLength);
                Data.CopyTo(packet, 2);
                packet[PacketLength - 1] = CRC;

                this.packet = packet;
                return packet;
            }
        }

        public bool Compare(DBusMessage message)
        {
            return Device == message.Device && Data.Compare(message.Data);
        }

        public Message ToIBusMessage()
        {
            return new Message(SourceDevice, DestinationDevice, ReceiverDescription, Data);
        }

        public static new Message TryCreate(byte[] buffer, int bufferLength = -1)
        {
            if (bufferLength < 0)
            {
                bufferLength = buffer.Length;
            }
            if (!IsValid(buffer))
            {
                return null;
            }

            return new DBusMessage((DeviceAddress)buffer[0], buffer.SkipAndTake(2, ParseDataLength(buffer, bufferLength)));
        }
        
        public static new bool IsValid(byte[] buffer, int bufferLength = -1)
        {
            return IsValid(buffer, ParsePacketLength, bufferLength);
        }

        protected static new bool IsValid(byte[] buffer, PacketLengthHandler packetLengthCallback, int bufferLength = -1)
        {
            if (bufferLength < 0)
            {
                bufferLength = buffer.Length;
            }
            if (bufferLength < PacketLengthMin)
            {
                return false;
            }

            byte packetLength = (byte)ParsePacketLength(buffer, bufferLength);
            if (bufferLength < packetLength || packetLength < PacketLengthMin)
            {
                return false;
            }

            byte check = 0x00;
            for (byte i = 0; i < packetLength - 1; i++)
            {
                check ^= buffer[i];
            }
            return check == buffer[packetLength - 1];
        }

        public static new bool CanStartWith(byte[] buffer, int bufferLength = -1)
        {
            return CanStartWith(buffer, ParsePacketLength, bufferLength);
        }

        protected static new bool CanStartWith(byte[] buffer, PacketLengthHandler packetLengthCallback, int bufferLength = -1)
        {
            if (bufferLength < 0)
            {
                bufferLength = buffer.Length;
            }

            var packetLength = packetLengthCallback(buffer, bufferLength);
            if (packetLength > -1 && packetLength < PacketLengthMin)
            {
                return false;
            }

            if (bufferLength < PacketLengthMin)
            {
                return true;
            }

            if (bufferLength >= packetLength && !IsValid(buffer, packetLengthCallback, bufferLength))
            {
                return false;
            }

            return true;
        }

        protected static new int ParsePacketLength(byte[] buffer, int bufferLength)
        {
            if (bufferLength < 0)
            {
                bufferLength = buffer.Length;
            }
            if (bufferLength < 2)
            {
                return -1;
            }
            return buffer[1];
        }

        protected static new int ParseDataLength(byte[] buffer, int bufferLength)
        {
            return ParsePacketLength(buffer, bufferLength) - 3;
        }
    }
}