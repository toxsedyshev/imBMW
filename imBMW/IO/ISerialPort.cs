using System;
using Microsoft.SPOT;
using System.IO.Ports;

namespace System.IO.Ports
{
    public interface ISerialPort
    {
        void Write(params byte[] data);

        void Write(byte[] data, int offset, int length);

        void Write(string text);

        void WriteLine(string text);

        byte[] ReadAvailable();

        byte[] ReadAvailable(int maxCount);

        int Read(byte[] buffer, int offset, int count);

        string ReadLine();

        void Flush();

        event SerialDataReceivedEventHandler DataReceived;
    }
}
