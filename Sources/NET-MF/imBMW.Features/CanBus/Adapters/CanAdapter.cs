using System;
using imBMW.Tools;
using CanMessage = GHI.IO.ControllerAreaNetwork.Message;

namespace imBMW.Features.CanBus.Adapters
{
    public abstract class CanAdapter
    {
        public static CanAdapter Current { get; set; }

        public CanAdapter(CanAdapterSettings settings)
        {
            Settings = settings;
            if (Settings.UseReadQueue)
            {
                ReadQueueWorker = new QueueThreadWorker(ReadQueue);
            }
        }

        public delegate void MessageHandler(CanAdapter can, CanMessage message);

        public delegate void ErrorHandler(CanAdapter can, string message);

        public event MessageHandler MessageReceived;

        public event MessageHandler MessageSent;

        public event ErrorHandler Error;

        public abstract bool SendMessage(CanMessage message);

        public CanAdapterSettings Settings { get; protected set; }

        protected QueueThreadWorker ReadQueueWorker { get; private set; }

        public abstract bool IsEnabled { get; set; }

        protected virtual void ReadQueue(object message)
        {
            OnMessageReceived((CanMessage)message);
        }

        protected virtual void ProcessReceivedMessage(CanMessage message)
        {
            if (Settings.UseReadQueue)
            {
                ReadQueueWorker.Enqueue(message);
            }
            else
            {
                OnMessageReceived(message);
            }
        }

        protected void OnMessageReceived(CanMessage message)
        {
            if (MessageReceived != null)
            {
                MessageReceived(this, message);
            }
        }

        protected void OnMessageSent(CanMessage message, bool sent)
        {
            if (sent)
            {
                if (MessageSent != null)
                {
                    MessageSent(this, message);
                }
            }
            else
            {
                OnError("Can't send: " + message);
            }
        }

        protected void OnError(string error)
        {
            if (Error != null)
            {
                Error(this, error);
            }
        }

        internal void CheckEnabled()
        {
            if (!IsEnabled)
            {
                throw new CanException("CAN adapter is not enabled.");
            }
        }

        #region Packet packer

        //                   0:FF 1:id 5:len 6:data 14:CRC
        public const int PacketLength = 1 + 4 + 1 + 8 + 1;

        /// <summary>
        /// Convert CanMessage to binary packet.
        /// </summary>
        /// <param name="message">Message to convert.</param>
        /// <param name="packet">Packet to fill with data or null to create new one.</param>
        /// <returns>Packet bytes.</returns>
        public byte[] MakePacket(CanMessage message, byte[] packet)
        {
            if (packet == null)
            {
                packet = new byte[PacketLength];
            }
            packet[0] = 0xFF;
            var id = BitConverter.GetBytes(message.ArbitrationId);
            for (var i = 0; i < 4; i++)
            {
                packet[i + 1] = id[3 - i];
            }
            packet[5] = (byte)message.Length;
            Array.Copy(message.Data, 0, packet, 6, message.Data.Length);
            byte crc = 0;
            for (var i = 1; i < packet.Length - 1; i++) // without starting 0xFF byte
            {
                crc ^= packet[i];
            }
            packet[14] = crc;
            return packet;
        }

        #endregion
    }
}
