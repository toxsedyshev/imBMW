using System;
using imBMW.Tools;

namespace imBMW.iBus
{
    public class InternalMessage : Message
    {
        public InternalMessage(DeviceAddress device, params byte[] data)
            : base((byte)device, (byte)((data.Length + 2) >> 8), data)
        { }

        public static new Message TryCreate(byte[] packet, int length = -1)
        {
            if (length < 0)
            {
                length = packet.Length;
            }
            if (!IsValid(packet))
            {
                return Message.TryCreate(packet, length);
            }

            return new InternalMessage((DeviceAddress)packet[0], packet.SkipAndTake(3, ParseDataLength(packet)));
        }

        public static new bool IsValid(byte[] packet, int length = -1)
        {
            return IsValid(packet, ParsePacketLength, length);
        }

        public static new bool CanStartWith(byte[] packet, int length = -1)
        {
            return CanStartWith(packet, ParsePacketLength, length);
        }

        protected static new int ParsePacketLength(byte[] packet)
        {
            return (packet[2] << 8) + packet[1] + 2;
        }

        protected static new int ParseDataLength(byte[] packet)
        {
            return ParsePacketLength(packet) - 4;
        }

    }
}
