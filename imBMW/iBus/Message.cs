using System;
using imBMW.Tools;

namespace imBMW.iBus
{
    public class Message
    {
        public const int PacketLengthMin = 5;
        public const int PacketLengthMax = 64;

        byte _source;
        byte _destination;
        byte[] _data;
        byte _check;

        byte[] _packet;
        byte _packetLength;
        string _packetDump;
        string _dataDump;
        DeviceAddress _sourceDevice = DeviceAddress.Unset;
        DeviceAddress _destinationDevice = DeviceAddress.Unset;
        PerformanceInfo _performanceInfo;

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
            Init((byte)source, (byte)destination, data, description);
            _sourceDevice = source;
            _destinationDevice = destination;
        }

        public Message(byte source, byte destination, params byte[] data)
            : this(source, destination, null, data)
        {
        }

        public Message(byte source, byte destination, string description, params byte[] data)
        {
            Init(source, destination, data, description);
        }

        void Init(byte source, byte destination, byte[] data, string description = null) 
        {
            _source = source;
            _destination = destination;
            _data = data;
            ReceiverDescription = description;
            _packetLength = (byte)(data.Length + 4); // + source + destination + len + chksum

            byte check = 0x00;
            check ^= source;
            check ^= (byte)(_packetLength - 2);
            check ^= destination;
            foreach (byte b in data)
            {
                check ^= b;
            }
            _check = check;
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

            var packetLength = (byte)(packet[1] + 2);
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

        public static bool CanStartWith(byte[] packet, int length)
        {
            if (length < PacketLengthMin)
            {
                return true;
            }

            var packetLength = (byte)(packet[1] + 2);
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
                return _packetLength;
            }
        }

        public byte[] Packet
        {
            get
            {
                if (_packet != null)
                {
                    return _packet;
                }

                var packet = new byte[PacketLength];
                packet[0] = _source;
                packet[1] = (byte)(PacketLength - 2);
                packet[2] = _destination;
                _data.CopyTo(packet, 3);
                packet[PacketLength - 1] = _check;

                _packet = packet;
                return packet;
            }
        }

        public byte[] Data
        {
            get
            {
                return _data;
            }
        }

        public String PacketDump
        {
            get { return _packetDump ?? (_packetDump = Packet.ToHex(' ')); }
        }

        public String DataDump
        {
            get { return _dataDump ?? (_dataDump = _data.ToHex(' ')); }
        }

        public DeviceAddress SourceDevice {
            get
            {
                if (_sourceDevice == DeviceAddress.Unset)
                {
                    try
                    {
                        _sourceDevice = (DeviceAddress)_source;
                    }
                    catch (InvalidCastException)
                    {
                        _sourceDevice = DeviceAddress.Unknown;
                    }
                }
                return _sourceDevice;
            }
        }


        public DeviceAddress DestinationDevice
        {
            get
            {
                if (_destinationDevice == DeviceAddress.Unset)
                {
                    try
                    {
                        _destinationDevice = (DeviceAddress)_destination;
                    }
                    catch (InvalidCastException)
                    {
                        _destinationDevice = DeviceAddress.Unknown;
                    }
                }
                return _destinationDevice;
            }
        }

        /// <summary>
        /// Description of the message set by a receiver or by MessageRegistry
        /// </summary>
        public string ReceiverDescription { get; set; }

        public PerformanceInfo PerformanceInfo
        {
            get { return _performanceInfo ?? (_performanceInfo = new PerformanceInfo()); }
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
