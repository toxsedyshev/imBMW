using System;
using GHI.IO;
using Microsoft.SPOT;
using System.Text;
using imBMW.Tools;

namespace imBMW.Features.CanBus
{
    public class CanMessage
    {
        ControllerAreaNetwork.Message message;

        public CanMessage(uint arbitrationId, byte[] data, int length)
            : this(new ControllerAreaNetwork.Message { ArbitrationId = arbitrationId, Data = PrepareData(data), Length = length })
        { }

        public CanMessage(uint arbitrationId, byte[] data)
            : this(new ControllerAreaNetwork.Message { ArbitrationId = arbitrationId, Data = PrepareData(data), Length = data.Length })
        { }

        public CanMessage(ControllerAreaNetwork.Message message)
        {
            this.message = message;
        }

        //public CanMessage(MCP2515.CANMSG message)
        //    : this(message, DateTime.Now)
        //{ }

        //public CanMessage(MCP2515.CANMSG message, DateTime time)
        //    : this(new ControllerAreaNetwork.Message
        //    {
        //        ArbitrationId = message.CANID,
        //        Data = message.data,
        //        IsRemoteTransmissionRequest = message.IsRemote,
        //        IsExtendedId = message.IsExtended,
        //        TimeStamp = time,
        //        Length = message.data.Length
        //    })
        //{ }

        public override string ToString()
        {
            return ArbitrationIdHex + " > " + DataHex;
        }

        public string ArbitrationIdHex
        {
            get
            {
                return ArbitrationId.ToString("x2");
            }
        }

        public string DataHex
        {
            get
            {
                var sb = new StringBuilder(Length * 3 - 1);
                for (int i = 0; i < Length && i < Data.Length; i++)
                {
                    var b = Data[i];
                    if (sb.Length != 0)
                    {
                        sb.Append(" ");
                    }
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public bool Compare(CanMessage message)
        {
            return message.ArbitrationId == ArbitrationId && message.Data.Compare(Data);
        }

        private static byte[] PrepareData(byte[] data)
        {
            if (data.Length == 8)
            {
                return data;
            }
            if (data.Length > 8)
            {
                throw new CanException("CAN message data length shouldn't be more that 8 bytes.");
            }
            var newData = new byte[8];
            Array.Copy(data, newData, data.Length);
            return newData;
        }

        //
        // Summary:
        //     The message arbitration id.
        public uint ArbitrationId
        {
            get { return message.ArbitrationId; }
            set { message.ArbitrationId = value; }
        }

        //
        // Summary:
        //     The message data. It must be eight bytes.
        public byte[] Data
        {
            get { return message.Data; }
            set { message.Data = value; }
        }

        //
        // Summary:
        //     Whether or not the message uses an extended id.
        public bool IsExtendedId
        {
            get { return message.IsExtendedId; }
            set { message.IsExtendedId = value; }
        }

        //
        // Summary:
        //     Whether or not the message is a remote transmission request.
        public bool IsRemoteTransmissionRequest
        {
            get { return message.IsRemoteTransmissionRequest; }
            set { message.IsRemoteTransmissionRequest = value; }
        }

        //
        // Summary:
        //     The number of bytes in the message.
        public int Length
        {
            get { return message.Length; }
            set { message.Length = value; }
        }

        //
        // Summary:
        //     When the message was received.
        public DateTime TimeStamp
        {
            get { return message.TimeStamp; }
            set { message.TimeStamp = value; }
        }


        public ControllerAreaNetwork.Message NativeMessage
        {
            get
            {
                return message;
            }
        }

        //public MCP2515.CANMSG MCP2515Message
        //{
        //    get
        //    {
        //        return new MCP2515.CANMSG
        //        {
        //            CANID = ArbitrationId,
        //            data = Data,
        //            IsRemote = IsRemoteTransmissionRequest
        //        };
        //    }
        //}
    }
}
