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

        protected virtual int GetPacketLength(Message m)
        {
            return m.PacketLength;
        }

        public void Parse(byte[] data)
        {
            if (data.Length == 0)
            {
                return;
            }
            if (buffer == null)
            {
                buffer = data;
            }
            else
            {
                buffer = buffer.Combine(data);
            }
            var tmp = buffer;
            var skipped = 0;
            while (!CanStartWith(buffer))
            {
                if (buffer.Length > data.Length)
                {
                    skipped++;
                    buffer = buffer.Skip(1);
                }
                else
                {
                    buffer = null;
                    throw new Exception("Wrong data: " + tmp.ToHex(' '));
                }
            }
            if (skipped > 0)
            {
                Logger.Error($"Incoming message data skipped: " + tmp.SkipAndTake(0, skipped).ToHex(' '));
            }
            tmp = null;
            Message m;
            while (buffer != null && (m = TryCreate(buffer)) != null)
            {
                if (GetPacketLength(m) == buffer.Length)
                {
                    buffer = null;
                }
                else
                {
                    buffer = buffer.Skip(GetPacketLength(m));
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