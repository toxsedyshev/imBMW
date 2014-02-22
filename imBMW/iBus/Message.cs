using System;
using imBMW.Tools;

namespace imBMW.iBus
{
    public class Message
    {
        #region Public constants
        
        public const int PacketLengthMin = 5;
        public const int PacketLengthMax = 64;

        #endregion

        #region Private fields

        private byte _source;
        private byte _destination;
        private byte _check;

        private byte[] _packet;
        private string _packetDump;
        private string _dataDump;
        private DeviceAddress _sourceDevice = DeviceAddress.Unset;
        private DeviceAddress _destinationDevice = DeviceAddress.Unset;
        private PerformanceInfo _performanceInfo;

        #endregion

        #region Public constructors
        
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

        #endregion

        #region Private methods
        
        private void Init(byte source, byte destination, byte[] data, string description = null)
        {
            _source = source;
            _destination = destination;
            Data = data;
            ReceiverDescription = description;
            PacketLength = (byte)(data.Length + 4); // + source + destination + len + chksum

            byte check = 0x00;
            check ^= source;
            check ^= (byte)(PacketLength - 2);
            check ^= destination;
            foreach (byte b in data)
            {
                check ^= b;
            }
            _check = check;
        }

        #endregion

        #region Public static methods
        
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

            return length < packetLength || IsValid(packet, length);
        }

        #endregion

        #region Public properties

        public byte PacketLength { get; private set; }

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
                Data.CopyTo(packet, 3);
                packet[PacketLength - 1] = _check;

                _packet = packet;
                return packet;
            }
        }

        public byte[] Data { get; private set; }

        public String PacketDump
        {
            get { return _packetDump ?? (_packetDump = Packet.ToHex(' ')); }
        }

        public String DataDump
        {
            get { return _dataDump ?? (_dataDump = Data.ToHex(' ')); }
        }

        public DeviceAddress SourceDevice
        {
            get
            {
                switch (_sourceDevice)
                {
                    case DeviceAddress.Unset:
                        try
                        {
                            _sourceDevice = (DeviceAddress)_source;
                        }
                        catch (InvalidCastException)
                        {
                            _sourceDevice = DeviceAddress.Unknown;
                        }
                        break;
                }
                return _sourceDevice;
            }
        }

        public DeviceAddress DestinationDevice
        {
            get
            {
                switch (_destinationDevice)
                {
                    case DeviceAddress.Unset:
                        try
                        {
                            _destinationDevice = (DeviceAddress)_destination;
                        }
                        catch (InvalidCastException)
                        {
                            _destinationDevice = DeviceAddress.Unknown;
                        }
                        break;
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

        #endregion

        #region Public overriden methods
        
        public override string ToString()
        {
            return this.ToPrettyString();
        }

        #endregion
    }

    public class PerformanceInfo
    {
        /// <summary>
        /// Time when the message was enqueued
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
                    s = "Processed: " + span.GetTotalSeconds() + "." + span.Milliseconds.ToString().PrependToLength('0', 3);
                }
                if (TimeEnqueued != default(DateTime))
                {
                    if (s != "")
                    {
                        s += " + ";
                    }
                    TimeSpan span = TimeStartedProcessing - TimeEnqueued;
                    s += "In queue: " + span.GetTotalSeconds() + "." + span.Milliseconds.ToString().PrependToLength('0', 3);
                }
                return s;
            }
            return string.Empty;
        }
    }
}
