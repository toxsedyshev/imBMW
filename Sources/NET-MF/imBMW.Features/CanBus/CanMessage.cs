using System;
using GHI.IO;
using Microsoft.SPOT;

namespace imBMW.Features.CanBus
{
    public class CanMessage
    {
        ControllerAreaNetwork.Message message;

        public CanMessage(ControllerAreaNetwork.Message message)
        {
            this.message = message;
        }

        public CanMessage(MCP2515.CANMSG message)
            : this(new ControllerAreaNetwork.Message
            {
                ArbitrationId = message.CANID,
                Data = message.data,
                IsRemoteTransmissionRequest = message.IsRemote,
                IsExtendedId = message.IsExtended
            })
        { }

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

        public MCP2515.CANMSG MCP2515Message
        {
            get
            {
                return new MCP2515.CANMSG
                {
                    CANID = ArbitrationId,
                    data = Data,
                    IsRemote = IsRemoteTransmissionRequest,
                    
                };
            }
        }
    }
}
