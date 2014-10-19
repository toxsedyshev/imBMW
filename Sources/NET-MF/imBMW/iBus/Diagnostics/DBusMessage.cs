using System;
using Microsoft.SPOT;
using imBMW.Tools;

namespace imBMW.iBus.Diagnostics
{
    class DBusMessage : Message
    {
        public const int DBusPacketLengthMin = 4;
        public const int DBusPacketLengthMax = 64;

        public DBusMessage(DeviceAddress destination, params byte[] data)
            : base(DeviceAddress.Diagnostic, destination, data)
        {
        }

        public DBusMessage(byte destination, params byte[] data)
            : base((byte)DeviceAddress.Diagnostic, destination, data)
        {
        }

        public static new Message TryCreate(byte[] packet)
        {
            return TryCreate(packet, (byte)packet.Length);
        }

        public static new Message TryCreate(byte[] packet, int length)
        {
            if (!IsValid(packet, length))
            {
                return null;
            }

            return new DBusMessage(packet[0], packet.SkipAndTake(2, packet[1] - 3));
        }

        public static new bool IsValid(byte[] packet)
        {
            return IsValid(packet, (byte)packet.Length);
        }

        public static new bool IsValid(byte[] packet, int length)
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

        public static new bool CanStartWith(byte[] packet, int length)
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

    }
}
