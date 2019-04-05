#if !MF_FRAMEWORK_VERSION_V4_1

using System;
using imBMW.Tools;
using System.Text;

namespace imBMW.iBus
{
    public class InternalMessage : Message
    {
        public new const int PacketLengthMax = 1028;

        string dataString;

        public InternalMessage(DeviceAddress device, string data)
            : this(device, data, data)
        { }

        public InternalMessage(DeviceAddress device, string data, string description)
            : this(device, description, Encoding.UTF8.GetBytes(data))
        { }

        public InternalMessage(DeviceAddress device, params byte[] data)
            : this(device, null, data)
        { }

        public InternalMessage(DeviceAddress device, string description, params byte[] data)
            : base((byte)device, (byte)((data.Length + 2) >> 8), data)
        {
            if (!device.IsInternal())
            {
                throw new Exception("Internal messages are for internal devices only.");
            }

            if (PacketLength > PacketLengthMax)
            {
                throw new Exception("Message packet length exceeds " + PacketLengthMax + " bytes.");
            }
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
                throw new Exception("No destination address in internal message.");
            }
        }

        public string DataString
        {
            get
            {
                if (dataString == null)
                {
                    try
                    {
                        dataString = Encoding.UTF8.GetString(Data);
                    }
                    catch
                    {
                        dataString = DataDump;
                    }
                }
                return dataString;
            }
        }

        public override bool Compare(Message message)
        {
            return SourceDevice == message.SourceDevice && Data.Compare(message.Data);
        }

        public static new Message TryCreate(byte[] buffer, int bufferLength = -1)
        {
            if (bufferLength < 0)
            {
                bufferLength = buffer.Length;
            }
            if (!IsValid(buffer))
            {
                return Message.TryCreate(buffer, bufferLength);
            }

            return new InternalMessage((DeviceAddress)buffer[0], buffer.SkipAndTake(3, ParseDataLength(buffer, bufferLength)));
        }

        public static new bool IsValid(byte[] buffer, int bufferLength = -1)
        {
            if (bufferLength < 0)
            {
                bufferLength = buffer.Length;
            }
            return bufferLength > 0 && buffer[0].IsInternal()
                && ParsePacketLength(buffer, bufferLength) <= PacketLengthMax
                && IsValid(buffer, ParsePacketLength, bufferLength)
                && CheckDeviceAddresses(buffer);
        }

        public static new bool CanStartWith(byte[] buffer, int bufferLength = -1)
        {
            var packetLength = ParsePacketLength(buffer, bufferLength);
            if (packetLength > PacketLengthMax)
            {
                return false;
            }
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

        protected static new bool CheckDeviceAddresses(byte[] buffer, int bufferLength = -1)
        {
            if (bufferLength < 0)
            {
                bufferLength = buffer.Length;
            }
            if (bufferLength >= 1)
            {
                if (!buffer[0].IsInternal() && !Message.CheckDeviceAddresses(buffer, bufferLength))
                {
                    return false;
                }
                if (!CheckSourceDeviceAddress(buffer[0]))
                {
                    return false;
                }
            }
            return true;
        }

        protected static new int ParsePacketLength(byte[] buffer, int bufferLength)
        {
            if (bufferLength < 0)
            {
                bufferLength = buffer.Length;
            }
            if (bufferLength < 3)
            {
                return -1;
            }
            if (buffer[0].IsInternal())
            {
                return (buffer[2] << 8) + buffer[1] + 2;
            }
            else
            {
                return buffer[1] + 2;
            }
        }

        protected static new int ParseDataLength(byte[] buffer, int bufferLength)
        {
            return ParsePacketLength(buffer, bufferLength) - 4;
        }
    }
}

#endif