using System;
using imBMW.iBus;

namespace imBMW.Diagnostics
{
    public class DBusMessageParser : MessageParser
    {
        protected override bool CanStartWith(byte[] data)
        {
            return DBusMessage.CanStartWith(data);
        }

        protected override Message TryCreate(byte[] data)
        {
            return DBusMessage.TryCreate(data);
        }
    }
}
