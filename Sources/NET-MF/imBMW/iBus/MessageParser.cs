#if !MF_FRAMEWORK_VERSION_V4_1

using System;
using imBMW.Tools;

namespace imBMW.iBus
{
    public class MessageParser
    {
        public event MessageReceiver MessageReceived;

        byte[] buffer = null;

        bool dBus = false;

        public MessageParser(bool dBus = false)
        {
            this.dBus = dBus;
        }

        bool CanStartWith(byte[] data)
        {
            if (dBus)
            {
                return DBusMessage.CanStartWith(data);
            }
            else
            {
                return InternalMessage.CanStartWith(data);
            }
        }

        Message TryCreate(byte[] data)
        {
            if (dBus)
            {
                return DBusMessage.TryCreate(data);
            }
            else
            {
                return InternalMessage.TryCreate(data);
            }
        }

        public void Parse(byte[] data)
        {
            if (buffer == null)
            {
                buffer = data;
            }
            else
            {
                buffer = buffer.Combine(data);
            }
            if (!CanStartWith(buffer))
            {
                if (CanStartWith(data))
                {
                    buffer = data;
                }
                else
                {
                    throw new Exception("Wrong data: " + buffer.ToHex(' '));
                }
            }
            Message m;
            while (buffer != null && (m = TryCreate(buffer)) != null)
            {
                if (m.PacketLength == buffer.Length)
                {
                    buffer = null;
                }
                else
                {
                    buffer = buffer.Skip(m.PacketLength);
                }
                var e = MessageReceived;
                if (e != null)
                {
                    e(m);
                }
            }
        }
    }
}

#endif