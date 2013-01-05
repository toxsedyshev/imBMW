using System;
using System.Text;
using imBMW.Tools;

namespace imBMW.iBus
{
    class Message
    {
        public const int PacketLengthMin = 5;
        public const int PacketLengthMax = 32;

        byte source;
        byte destination;
        byte[] data;
        byte check;

        byte[] packet;
        byte packetLenght;
        string packetDump;
        string dataDump;
        DeviceAddress sourceDevice = DeviceAddress.Unset;
        DeviceAddress destinationDevice = DeviceAddress.Unset;

        public Message(DeviceAddress source, DeviceAddress destination, params byte[] data)
        {
            if (source == DeviceAddress.Unset || source == DeviceAddress.Unknown)
            {
                throw new ArgumentException("Wrong source device");
            }
            if (destination == DeviceAddress.Unset || destination == DeviceAddress.Unknown)
            {
                throw new ArgumentException("Wrong destination device");
            }
            init((byte)source, (byte)destination, data);
        }

        public Message(byte source, byte destination, byte[] data)
        {
            init(source, destination, data);
        }

        void init(byte source, byte destination, byte[] data) 
        {
            this.source = source;
            this.destination = destination;
            this.data = data;
            packetLenght = (byte)(data.Length + 4); // + source + destination + len + chksum

            byte check = 0x00;
            check ^= source;
            check ^= (byte)(packetLenght - 2);
            check ^= destination;
            foreach (byte b in data)
            {
                check ^= b;
            }
            this.check = check;
        }

        public static Message TryCreate(byte[] packet)
        {
            return TryCreate(packet, (byte)packet.Length);
        }

        public static Message TryCreate(byte[] packet, byte lenght)
        {
            if (!IsValid(packet, lenght))
            {
                return null;
            }

            return new Message(packet[0], packet[2], packet.SkipAndTake(3, packet[1] - 2));
        }

        public static bool IsValid(byte[] packet)
        {
            return IsValid(packet, (byte)packet.Length);
        }

        public static bool IsValid(byte[] packet, byte lenght)
        {
            if (lenght < PacketLengthMin)
            {
                return false;
            }

            byte packetLength = (byte)(packet[1] + 2);
            if (lenght < packetLength)
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

        public byte PacketLenght
        {
            get
            {
                return packetLenght;
            }
        }

        public byte[] Packet
        {
            get
            {
                if (this.packet != null)
                {
                    return this.packet;
                }

                byte[] packet = new byte[PacketLenght];
                packet[0] = source;
                packet[1] = (byte)(PacketLenght - 2);
                packet[2] = destination;
                data.CopyTo(packet, 3);
                packet[PacketLenght - 1] = check;

                this.packet = packet;
                return packet;
            }
        }

        public byte[] Data
        {
            get
            {
                return data;
            }
        }

        public String PacketDump
        {
            get
            {
                if (packetDump == null)
                {
                    packetDump = Packet.ToHex(" ");
                }
                return packetDump;
            }
        }

        public String DataDump
        {
            get
            {
                if (dataDump == null)
                {
                    dataDump = data.ToHex(" ");
                }
                return dataDump;
            }
        }

        public String PrettyDump
        {
            get
            {
                return SourceDevice.ToStringValue() + " > " + DestinationDevice.ToStringValue() + ": " + DataDump;
            }
        }

        public DeviceAddress SourceDevice {
            get
            {
                if (sourceDevice == DeviceAddress.Unset)
                {
                    try
                    {
                        sourceDevice = (DeviceAddress)source;
                    }
                    catch (InvalidCastException)
                    {
                        sourceDevice = DeviceAddress.Unknown;
                    }
                }
                return sourceDevice;
            }
        }


        public DeviceAddress DestinationDevice
        {
            get
            {
                if (destinationDevice == DeviceAddress.Unset)
                {
                    try
                    {
                        destinationDevice = (DeviceAddress)destination;
                    }
                    catch (InvalidCastException)
                    {
                        destinationDevice = DeviceAddress.Unknown;
                    }
                }
                return destinationDevice;
            }
        }
    }
}
