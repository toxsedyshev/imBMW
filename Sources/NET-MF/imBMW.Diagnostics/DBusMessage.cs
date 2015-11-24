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

            init((byte)device, (byte)DeviceAddress.Diagnostic, data, packetLength, check, description);
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

        public override byte[] Packet
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

        public static new Message TryCreate(byte[] packet, int length = -1)
        {
            if (length < 0)
            {
                length = packet.Length;
            }
            if (!IsValid(packet))
            {
                return null;
            }

            return new DBusMessage((DeviceAddress)packet[0], packet.SkipAndTake(2, ParseDataLength(packet)));
        }

        public static bool IsValid(byte[] packet)
        {
            return IsValid(packet, (byte)packet.Length);
        }

        public static new bool IsValid(byte[] packet, int length)
        {
            if (length < PacketLengthMin)
            {
                return false;
            }

            byte packetLength = (byte)ParsePacketLength(packet);
            if (length < packetLength || packetLength < PacketLengthMin)
            {
                return false;
            }

            byte check = 0x00;
            for (byte i = 0; i < packetLength - 1; i++)
            {
                check ^= packet[i];
            }
            return check == packet[packetLength - 1];
        }

        public static new bool CanStartWith(byte[] packet, int length = -1)
        {
            return CanStartWith(packet, ParsePacketLength, length);
        }

        protected static new bool CanStartWith(byte[] packet, IntFromByteArray packetLengthCallback, int length = -1)
        {
            if (length < 0)
            {
                length = packet.Length;
            }

            if (length < PacketLengthMin)
            {
                return true;
            }

            byte packetLength = (byte)(packet[1] + 2);
            if (packetLength < PacketLengthMin)
            {
                return false;
            }

            if (length >= packetLength && !IsValid(packet, length))
            {
                return false;
            }

            return true;
        }

        protected static new int ParsePacketLength(byte[] packet)
        {
            if (packet.Length < PacketLengthMin)
            {
                return 0;
            }
            return packet[1];
        }

        protected static new int ParseDataLength(byte[] packet)
        {
            if (packet.Length < PacketLengthMin)
            {
                return 0;
            }
            return ParsePacketLength(packet) - 3;
        }
    }
}