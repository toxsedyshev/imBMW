#if !MF_FRAMEWORK_VERSION_V4_1

using System;
using imBMW.Tools;

namespace imBMW.iBus
{
    public class MessageParser
    {
        public event MessageReceiver MessageReceived;

        byte[] buffer = null;

        object parserLock = new object();
        
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
            lock (parserLock)
            {
                if (data != null && data.Length > 0)
                {
                    if (buffer == null)
                    {
                        buffer = data;
                    }
                    else
                    {
                        buffer = buffer.Combine(data);
                    }
                }
                if (buffer == null)
                {
                    return;
                }
                var tmp = buffer;
                var skipped = 0;
                while (!CanStartWith(buffer))
                {
                    if (buffer.Length > 1)
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
                    Logger.Error("Incoming message data skipped: " + tmp.SkipAndTake(0, skipped).ToHex(' '));
                }
                tmp = null;
                Message m;
                while (buffer != null)
                {
                    m = TryCreate(buffer);
                    if (m == null)
                    {
                        if (skipped > 0 || data != null)
                        {
                            Parse(null);
                        }
                        return;
                    }
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
                //if (buffer != null)
                //{
                //    Logger.Info("Buffer size = " + buffer.Length);
                //}
                //else
                //{
                //    Logger.Info("Buffer empty");
                //}
            }
        }
    }
}

#endif