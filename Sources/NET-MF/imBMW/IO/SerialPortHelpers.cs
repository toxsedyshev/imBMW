using System;
using Microsoft.SPOT;
using System.IO.Ports;

namespace imBMW.IO
{
    public static class SerialPortHelpers
    {
        public static void Write(this SerialPort port, byte[] buffer)
        {
            port.Write(buffer, 0, buffer.Length);
        }
    }
}
