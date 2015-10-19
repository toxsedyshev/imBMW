#if !MF_FRAMEWORK_VERSION_V4_1

using System;
using imBMW.Tools;

namespace imBMW.iBus
{
    public class MessageParser
    {
        public event MessageReceiver MessageReceived;

        byte[] buffer = null;
        
        protected virtual bool CanStartWith(byte[] data)
        {
            return InternalMessage.CanStartWith(data);
        }

        protected virtual Message TryCreate(byte[] data)
        {
            return InternalMessage.TryCreate(data);
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
                    var tmp = buffer;
                    buffer = null;
                    throw new Exception("Wrong data: " + tmp.ToHex(' '));
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
                try
                {
                    var e = MessageReceived;
                    if (e != null)
                    {
                        e(m);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "processing message by MessageParser");
                }
            }
        }
    }
}

#endif