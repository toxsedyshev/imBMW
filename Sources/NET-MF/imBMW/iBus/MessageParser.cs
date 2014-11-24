using System;
using imBMW.Tools;

namespace imBMW.iBus
{
    public class MessageParser
    {
        public event MessageReceiver MessageReceived;

        byte[] buffer = null;

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
            if (!InternalMessage.CanStartWith(buffer))
            {
                if (InternalMessage.CanStartWith(data))
                {
                    buffer = data;
                }
                else
                {
                    throw new Exception("Wrong data: " + buffer.ToHex(' '));
                }
            }
            Message m;
            while (buffer != null && (m = InternalMessage.TryCreate(buffer)) != null)
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
