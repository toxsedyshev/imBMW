namespace System.IO.Ports
{
    public delegate void BusyChangedEventHandler(bool busy);

    public interface ISerialPort
    {
        void Write(params byte[] data);

        void Write(byte[] data, int offset, int length);

        void Write(string text);

        void WriteLine(string text);

        int AvailableBytes { get; }

        int AfterWriteDelay { get; set; }

        int ReadTimeout { get; set; }

        byte[] ReadAvailable();

        byte[] ReadAvailable(int maxCount);

        int Read(byte[] buffer, int offset, int count);

        string ReadLine();

        void Flush();

        event SerialDataReceivedEventHandler DataReceived;

        event BusyChangedEventHandler BusyChanged; 
    }
}
