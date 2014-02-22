using Microsoft.SPOT;

namespace imBMW.iBus.Devices.Real
{
    public static class SeatMemory
    {
        #region Private static readonly fields

        private static readonly Message MessageMoveToMemoryPosition1 = new Message(DeviceAddress.Diagnostic, DeviceAddress.SeatMemory, "Move to memory position 1", 0x0C, 0x02, 0x01, 0x00);
        private static readonly Message MessageMoveToMemoryPosition2 = new Message(DeviceAddress.Diagnostic, DeviceAddress.SeatMemory, "Move to memory position 2", 0x0C, 0x02, 0x02, 0x00);
        private static readonly Message MessageMoveToMemoryPosition3 = new Message(DeviceAddress.Diagnostic, DeviceAddress.SeatMemory, "Move to memory position 3", 0x0C, 0x02, 0x04, 0x00);

        #endregion

        #region Public static fields

        public static event SeatMemoryPositionChangedEventHandler SeatMemoryPositionChanged;

        #endregion

        #region Private fields

        private static SeatMemoryPosition _memoryPosition = SeatMemoryPosition.PositionUnknown;
    
        #endregion

        #region Static constructor

        static SeatMemory()
        {
            Manager.AddMessageReceiverForSourceOrDestinationDevice(DeviceAddress.SeatMemory, DeviceAddress.SeatMemory, ProcessMessage);
        }

        #endregion

        #region Public static fields

        public static SeatMemoryPosition SeatPosition
        {
            get { return _memoryPosition; }
            set
            {
                if (_memoryPosition != value)
                {
                    ChangePosition(value);
                    OnSeatMemoryPositionChanged(value);
                }
            }
        }

    #endregion

        #region Private static methods

        private static void ProcessMessage(Message m)
        {
            if (m.SourceDevice == DeviceAddress.SeatMemory)
            {
                if (m.Data[0] == 0x78 && m.Data.Length == 3)
                {
                    var position = SeatMemoryPosition.PositionUnknown;
                    switch (m.Data[1])
                    {
                        case 0x01:
                            position = SeatMemoryPosition.Position1;
                            break;
                        case 0x02:
                            position = SeatMemoryPosition.Position2;
                            break;
                        case 0x04:
                            position = SeatMemoryPosition.Position3;
                            break;
                    }
                    if (position != SeatMemoryPosition.PositionUnknown)
                        OnSeatMemoryPositionChanged(position);
                }
            }
        }

        private static void OnSeatMemoryPositionChanged(SeatMemoryPosition newPosition)
        {
            if (_memoryPosition == newPosition) return;

            _memoryPosition = newPosition;
            var e = SeatMemoryPositionChanged;
            if (e != null)
            {
                e(new SeatMemoryPositionChangedEventArgs { Position = _memoryPosition });
            }
        }

        private static void ChangePosition(SeatMemoryPosition newPosition)
        {
            switch (newPosition)
            {
                case SeatMemoryPosition.Position1:
                    Manager.EnqueueMessage(MessageMoveToMemoryPosition1);
                    break;
                case SeatMemoryPosition.Position2:
                    Manager.EnqueueMessage(MessageMoveToMemoryPosition2);
                    break;
                case SeatMemoryPosition.Position3:
                    Manager.EnqueueMessage(MessageMoveToMemoryPosition3);
                    break;
            }
        }

        #endregion
    }

    public enum SeatMemoryPosition
    {
        PositionUnknown,
        Position1,
        Position2,
        Position3
    }

    public class SeatMemoryPositionChangedEventArgs : EventArgs
    {
        public SeatMemoryPosition Position { get; set; }
    }

    public delegate void SeatMemoryPositionChangedEventHandler(SeatMemoryPositionChangedEventArgs args);
}
