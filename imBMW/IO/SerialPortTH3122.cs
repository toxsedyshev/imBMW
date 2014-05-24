using Microsoft.SPOT.Hardware;

namespace System.IO.Ports
{
    public class SerialPortTH3122 : SerialInterruptPort
    {
        public SerialPortTH3122(String port, Cpu.Pin busy, bool fixParity = false) :
            base(new SerialPortConfiguration(port, BaudRate.Baudrate9600, Parity.Even, 8 + (fixParity ? 1 : 0), StopBits.One), busy, 0, imBMW.iBus.Message.PacketLengthMax, 50)
        {
            AfterWriteDelay = 20;
        }
    }
}
