using System;
using System.Text;
using imBMW.Tools;

namespace imBMW.iBus
{
    public class Message
    {
        public static int PacketLengthMin { get { return 5; } }
        public const int PacketLengthMax = 258;

        byte source;
        byte destination;
        byte[] data;
        byte check;

        byte[] packet;
        int packetLength;
        string packetDump;
        string dataDump;
        protected DeviceAddress sourceDevice = DeviceAddress.Unset;
        protected DeviceAddress destinationDevice = DeviceAddress.Unset;
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
            // packet = source + length + destination + data + chksum
            //                            |   ===== length =====    |
            
            byte check = 0x00;
            check ^= source;
            check ^= (byte)(data.Length + 2);
            check ^= destination;
            foreach (byte b in data)
            {
                check ^= b;
            }

            init(source, destination, data, data.Length + 4, check, description);  
        }

        protected void init(byte source, byte destination, byte[] data, int packetLength, byte check, string description = null) 
        {
            if (source.IsInternal() || destination.IsInternal())
            {
                throw new Exception("iBus messages are not for internal devices.");
            }

            this.source = source;
            this.destination = destination;
            Data = data;
            ReceiverDescription = description;
            PacketLength = packetLength;
            CRC = check;
        }

        public static Message TryCreate(byte[] buffer, int bufferLength = -1)
        {
            if (bufferLength < 0)
            {
                bufferLength = buffer.Length;
            }
            if (!IsValid(buffer, bufferLength))
            {
                return null;
            }

            return new Message(buffer[0], buffer[2], buffer.SkipAndTake(3, ParseDataLength(buffer, bufferLength)));
        }

        protected delegate int PacketLengthHandler(byte[] buffer, int bufferLength);

        public static bool IsValid(byte[] buffer, int bufferLength = -1)
        {
            return IsValid(buffer, ParsePacketLength, bufferLength) && CheckDeviceAddresses(buffer, bufferLength);
        }

        protected static bool IsValid(byte[] buffer, PacketLengthHandler packetLengthCallback, int bufferLength = -1)
        {
            if (bufferLength < 0)
            {
                bufferLength = buffer.Length;
            }
            if (bufferLength < PacketLengthMin)
            {
                return false;
            }

            int packetLength = packetLengthCallback(buffer, bufferLength);
            if (bufferLength < packetLength || packetLength < PacketLengthMin)
            {
                return false;
            }

            var packetCheck = buffer[packetLength - 1];
            if (packetCheck == 0x00 && packetLength > 8)
            {
                // find corrupted packet as a part of current packet
                for (int i = packetLength / 2; i < packetLength - 1; i++)
                {
                    var theSame = true;
                    for (int ic = i, io = 0; ic < packetLength - 1 && io < i; ic++, io++)
                    {
                        if (buffer[ic] != buffer[io])
                        {
                            theSame = false;
                            break;
                        }
                    }
                    if (theSame)
                    {
                        return false;
                    }
                }
            }
            byte check = 0x00;
            for (int i = 0; i < packetLength - 1; i++)
            {
                check ^= buffer[i];
            }
            return check == packetCheck;
        }

        public static bool CanStartWith(byte[] buffer, int bufferLength = -1)
        {
            if (!CanStartWith(buffer, ParsePacketLength, bufferLength))
            {
                return false;
            }
            if (!CheckDeviceAddresses(buffer, bufferLength))
            {
                return false;
            }
            return true;
        }

        protected static bool CanStartWith(byte[] buffer, PacketLengthHandler packetLengthCallback, int bufferLength = -1)
        {
            if (bufferLength < 0)
            {
                bufferLength = buffer.Length;
            }

            int packetLength = packetLengthCallback(buffer, bufferLength);
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

        protected static bool CheckDeviceAddresses(byte[] buffer, int bufferLength = -1)
        {
            if (bufferLength < 0)
            {
                bufferLength = buffer.Length;
            }
            if (bufferLength >= 1 && !CheckSourceDeviceAddress(buffer[0]))
            {
                return false;
            }
            if (bufferLength >= 3 && !CheckDestinationDeviceAddress(buffer[2]))
            {
                return false;
            }
            return true;
        }

        protected static bool CheckSourceDeviceAddress(byte address)
        {
            return address != (byte)DeviceAddress.Broadcast && CheckDestinationDeviceAddress(address);
        }

        protected static bool CheckDestinationDeviceAddress(byte address)
        {
#if NETMF
            return ((DeviceAddress)address).ToStringValue(true) != null;
#else
            return Enum.IsDefined(typeof(DeviceAddress), (short)address);
#endif
        }

        protected static int ParsePacketLength(byte[] buffer, int bufferLength)
        {
            if (bufferLength < 0)
            {
                bufferLength = buffer.Length;
            }
            if (bufferLength < 2)
            {
                return -1;
            }
            return buffer[1] + 2;
        }

        protected static int ParseDataLength(byte[] buffer, int bufferLength)
        {
            return ParsePacketLength(buffer, bufferLength) - 4;
        }

        public byte CRC
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

        public int PacketLength
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
            private set
            {
                data = value;
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


        public virtual DeviceAddress DestinationDevice
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

        public virtual bool Compare(Message message)
        {
            if (message == null)
            {
                return false;
            }
            return SourceDevice == message.SourceDevice
                && DestinationDevice == message.DestinationDevice
                && Data.Compare(message.Data);
        }

        public virtual bool Compare(byte[] packet)
        {
            return Packet.Compare(packet);
        }

        /// <summary>
        /// Description of the message set by a receiver or by MessageRegistry
        /// </summary>
        public string ReceiverDescription { get; set; }

        /// <summary>
        /// Custom delay after sending the message in milliseconds. Zero = default (20ms).
        /// </summary>
        public byte AfterSendDelay { get; set; }

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
                    s = "Processed: " + span.GetTotalSeconds() + "." + span.Milliseconds.ToString().PrependToLength(3, '0'); // TODO use string format
                }
                if (TimeEnqueued != default(DateTime))
                {
                    if (s != "")
                    {
                        s += " + ";
                    }
                    TimeSpan span = TimeStartedProcessing - TimeEnqueued;
                    s += "In queue: " + span.GetTotalSeconds() + "." + span.Milliseconds.ToString().PrependToLength(3, '0'); // TODO use string format
                }
                return s;
            }
            return String.Empty;
        }

    }
}
