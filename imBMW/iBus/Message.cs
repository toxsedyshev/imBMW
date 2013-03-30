using System;
using System.Text;
using imBMW.Tools;

namespace imBMW.iBus
{
    public class Message
    {
        public const int PacketLengthMin = 5;
        public const int PacketLengthMax = 64;

        byte source;
        byte destination;
        byte[] data;
        byte check;

        byte[] packet;
        byte packetLength;
        string packetDump;
        string dataDump;
        DeviceAddress sourceDevice = DeviceAddress.Unset;
        DeviceAddress destinationDevice = DeviceAddress.Unset;
        PerformanceInfo performanceInfo;

        public Message(DeviceAddress source, DeviceAddress destination, params byte[] data)
            : this(source, destination, null, data)
        {
        }

        public Message(DeviceAddress source, DeviceAddress destination, string description, params byte[] data)
        {
            if (source == DeviceAddress.Unset || source == DeviceAddress.Unknown)
            {
                throw new ArgumentException("Wrong source device");
            }
            if (destination == DeviceAddress.Unset || destination == DeviceAddress.Unknown)
            {
                throw new ArgumentException("Wrong destination device");
            }
            init((byte)source, (byte)destination, data, description);
            sourceDevice = source;
            destinationDevice = destination;
        }

        public Message(byte source, byte destination, params byte[] data)
            : this(source, destination, null, data)
        {
        }

        public Message(byte source, byte destination, string description, params byte[] data)
        {
            init(source, destination, data, description);
        }

        void init(byte source, byte destination, byte[] data, string description = null) 
        {
            this.source = source;
            this.destination = destination;
            this.data = data;
            this.ReceiverDescription = description;
            packetLength = (byte)(data.Length + 4); // + source + destination + len + chksum

            byte check = 0x00;
            check ^= source;
            check ^= (byte)(packetLength - 2);
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

        public static Message TryCreate(byte[] packet, int length)
        {
            if (!IsValid(packet, length))
            {
                return null;
            }

            return new Message(packet[0], packet[2], packet.SkipAndTake(3, packet[1] - 2));
        }

        public static bool IsValid(byte[] packet)
        {
            return IsValid(packet, (byte)packet.Length);
        }

        public static bool IsValid(byte[] packet, int length)
        {
            if (length < PacketLengthMin)
            {
                return false;
            }

            byte packetLength = (byte)(packet[1] + 2);
            if (length < packetLength)
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

        public static bool CanStartWith(byte[] packet, int length)
        {
            if (length < PacketLengthMin)
            {
                return true;
            }

            byte packetLength = (byte)(packet[1] + 2);
            if (packetLength < PacketLengthMin
                || packetLength > PacketLengthMax)
            {
                return false;
            }

            if (length >= packetLength && !IsValid(packet, length))
            {
                return false;
            }

            return true;
        }

        public byte PacketLength
        {
            get
            {
                return packetLength;
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

                byte[] packet = new byte[PacketLength];
                packet[0] = source;
                packet[1] = (byte)(PacketLength - 2);
                packet[2] = destination;
                data.CopyTo(packet, 3);
                packet[PacketLength - 1] = check;

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
                    packetDump = Packet.ToHex(' ');
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
                    dataDump = data.ToHex(' ');
                }
                return dataDump;
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

        /// <summary>
        /// Description of the message set by a receiver or by MessageRegistry
        /// </summary>
        public string ReceiverDescription { get; set; }

        public PerformanceInfo PerformanceInfo
        {
            get
            {
                if (performanceInfo == null)
                {
                    performanceInfo = new PerformanceInfo();
                }
                return performanceInfo;
            }
        }

        public override string ToString()
        {
            return this.ToPrettyString();
        }
    }

    public class PerformanceInfo 
    {
        /// <summary>
        /// Time when the message was enqueued.
        /// Available only when debugging
        /// </summary>
        public DateTime TimeEnqueued { get; set; }

        /// <summary>
        /// Time when the message was started processing.
        /// Available only when debugging
        /// </summary>
        public DateTime TimeStartedProcessing { get; set; }

        /// <summary>
        /// Time when the message was ended processing.
        /// Available only when debugging
        /// </summary>
        public DateTime TimeEndedProcessing { get; set; }

        public override string ToString()
        {
            // TODO Change to Cpu.SystemClock
            if (TimeStartedProcessing != default(DateTime))
            {
                string s = "";
                if (TimeEndedProcessing != default(DateTime))
                {
                    TimeSpan span = TimeEndedProcessing - TimeStartedProcessing;
                    s = "Processed: " + span.GetTotalSeconds() + "." + span.GetTotalMilliseconds();
                }
                if (TimeEnqueued != default(DateTime))
                {
                    if (s != "")
                    {
                        s += " + ";
                    }
                    TimeSpan span = TimeStartedProcessing - TimeEnqueued;
                    s += "In queue: " + span.GetTotalSeconds() + "." + span.GetTotalMilliseconds();
                }
                return s;
            }
            return String.Empty;
        }

    }
}
