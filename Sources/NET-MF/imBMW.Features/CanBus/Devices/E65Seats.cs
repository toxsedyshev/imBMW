using System;
using Microsoft.SPOT;
using System.Threading;
using imBMW.Features.CanBus.Adapters;
using imBMW.Tools;
using imBMW.iBus.Devices.Real;
using imBMW.iBus;
using imBMW.Features.Menu;

namespace imBMW.Features.CanBus.Devices
{
    #region Seats menu screen

    public class E65SeatsScreen : MenuScreen
    {
        protected static E65SeatsScreen instance;

        MenuItem driverHeater, passengerHeater;

        protected E65SeatsScreen()
        {
            TitleCallback = s => "Seats";

            SetItems();

            E65Seats.DriverSeat.Changed += DriverSeat_Changed;
            E65Seats.PassengerSeat.Changed += PassengerSeat_Changed;
        }

        private void DriverSeat_Changed()
        {
            if (driverHeater != null)
            {
                driverHeater.Refresh();
            }
        }

        private void PassengerSeat_Changed()
        {
            if (passengerHeater != null)
            {
                passengerHeater.Refresh();
            }
        }

        protected virtual void SetItems()
        {
            ClearItems();
            driverHeater = new MenuItem(i => "Driver Heat: [" + GetLevel(E65Seats.DriverSeat.HeaterLevel) + "]",
                i => E65Seats.ButtonHeaterDriverPress(), MenuItemType.Button, MenuItemAction.Refresh);
            passengerHeater = new MenuItem(i => "Pass Heat: [" + GetLevel(E65Seats.PassengerSeat.HeaterLevel) + "]",
                i => E65Seats.ButtonHeaterPassengerPress(), MenuItemType.Button, MenuItemAction.Refresh);
            AddItem(driverHeater);
            AddItem(passengerHeater);
            AddItem(new MenuItem(i => "Driver Front", i => E65Seats.ButtonFrontDriverPress(), MenuItemType.Button));
            AddItem(new MenuItem(i => "Driver Back", i => E65Seats.ButtonBackDriverPress(), MenuItemType.Button));
            AddItem(new MenuItem(i => "Memory", i => E65Seats.ButtonMDriverPress(), MenuItemType.Button));
            AddItem(new MenuItem(i => "Memory 1", i => E65Seats.ButtonM1DriverPress(), MenuItemType.Button));
            AddItem(new MenuItem(i => "Activated", i => E65Seats.EmulatorPaused = !i.IsChecked, MenuItemType.Checkbox)
            {
                IsChecked = !E65Seats.EmulatorPaused
            });
            this.AddBackButton();
        }

        string GetLevel(byte level)
        {
            switch (level)
            {
                case 3:
                    return "XXX";
                case 2:
                    return "XX_";
                case 1:
                    return "X__";
                default:
                    return "___";
            }
        }

        public static E65SeatsScreen Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new E65SeatsScreen();
                }
                return instance;
            }
        }
    }

    #endregion

    #region Seat info

    public class E65SeatInfo
    {
        public event Action Changed;

        private byte heaterLevel;
        private byte ventilationSpeed;
        private bool isMassageActive;

        public byte HeaterLevel
        {
            get { return heaterLevel; }
            set
            {
                if (heaterLevel == value)
                {
                    return;
                }
                heaterLevel = value;
                OnChanged();
            }
        }

        public byte VentilationSpeed
        {
            get { return ventilationSpeed; }
            set
            {
                if (ventilationSpeed == value)
                {
                    return;
                }
                ventilationSpeed = value;
                OnChanged();
            }
        }
        
        public bool IsMassageActive
        {
            get { return isMassageActive; }
            set
            {
                if (isMassageActive == value)
                {
                    return;
                }
                isMassageActive = value;
                OnChanged();
            }
        }

        private void OnChanged()
        {
            if (Changed != null)
            {
                Changed();
            }
        }
    }

    #endregion

    public static class E65Seats
    {
        enum QueueCommand
        {
            ButtonHeaterDriver,
            ButtonHeaterPassenger,
            ButtonMDriver,
            ButtonM1Driver
        }

        static QueueThreadWorker queueWorker;
        static Thread emulatorThread;
        static bool emulatorStarted;
        static bool lastEmulationFailed;
        static object emulatorLock = new object();
        static DateTime lastIBusMessageTime = DateTime.MinValue;
        static byte lastDimmer;

        public static E65SeatInfo DriverSeat { get; private set; }
        public static E65SeatInfo PassengerSeat { get; private set; }

        public static bool EmulatorPaused { get; set; }

        //static CanMessage canEmulationSeatFront = new CanMessage(0x0DA, new byte[] { 0x01, 0x00, 0xC0, 0xFF });
        //static CanMessage canEmulationEngineStop = new CanMessage(0x5A9, new byte[] { 0x30, 0x06, 0x00, 0x70, 0x17, 0xF1, 0x62, 0x03 });
        //static CanMessage canEmulationEngineStart = new CanMessage(0x38E, new byte[] { 0xF4, 0x01 });
        //static CanMessage canEmulationEngineRunning = new CanMessage(0x130, new byte[] { 0x45, 0x40, 0x21, 0x8F, 0xFE });
        //static CanMessage canEmulationTransmissionD = new CanMessage(0x1D2, new byte[] { 0x78, 0x0C, 0x8B, 0x1C, 0xF0 });
        //static CanMessage canEmulationTransmissionP = new CanMessage(0x1D2, new byte[] { 0xE1, 0x0C, 0x8B, 0x1C, 0xF0 });
        //static CanMessage emulateEngineRunning = new CanMessage(0x130, new byte[] { 0x45, 0x40, 0x21, 0x8F, 0xFE });

        static CanMessage messageKeepAlive = new CanMessage(0x130, new byte[] { 0x45, 0xFE, 0xFC, 0xFF, 0xFF });
        // TODO ^^^ should it have first byte = 0x00 when engine stopped?
        static CanMessage messageEngineRunning = new CanMessage(0x130, new byte[] { 0x45, 0x41, 0x39, 0xBF, 0xB0 });
        static CanMessage messageEmulateIHKA = new CanMessage(0x4F8, new byte[] { 0x00, 0x42, 0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }); // Some IHKA message?
        static CanMessage messageLightDimmer = new CanMessage(0x202, new byte[] { 0xFD, 0xFF }); // 00FF=min, FDFF=max, FEFF=off?
        static CanMessage messageEmulationUnknown5 = new CanMessage(0x2CC, new byte[] { 0x80, 0x80, 0xF6 });

        static CanMessage messageButtonHeaterDriverPress = new CanMessage(0x1E7, new byte[] { 0xF1, 0xFF });
        static CanMessage messageButtonHeaterDriverRelease = new CanMessage(0x1E7, new byte[] { 0xF0, 0xFF });
        static CanMessage messageButtonHeaterPassengerPress = new CanMessage(0x1E8, new byte[] { 0xF1, 0xFF });
        static CanMessage messageButtonHeaterPassengerRelease = new CanMessage(0x1E8, new byte[] { 0xF0, 0xFF });

        static CanMessage messageButtonDriverSeatMoveFront = new CanMessage(0xDA, new byte[] { 0x01, 0x00, 0xC0, 0xFF });
        static CanMessage messageButtonDriverSeatMoveRear = new CanMessage(0xDA, new byte[] { 0x02, 0x00, 0xC0, 0xFF });
        static CanMessage messageButtonDriverMPress = new CanMessage(0x1F3, new byte[] { 0xFC, 0xFF });
        static CanMessage messageButtonDriverMRelease = new CanMessage(0x1F3, new byte[] { 0xF8, 0xFF });
        static CanMessage messageButtonDriverM1Press = new CanMessage(0x1F3, new byte[] { 0xF9, 0xFF });

        static E65Seats()
        {
            DriverSeat = new E65SeatInfo();
            PassengerSeat = new E65SeatInfo();

            queueWorker = new QueueThreadWorker(ProcessQueue);
        }
        
        public static void Init()
        {
            CanAdapter.Current.MessageReceived += Can_MessageReceived;

            Manager.AfterMessageReceived += IBusManager_AfterMessageReceived;
        }

        private static void Can_MessageReceived(CanAdapter can, CanMessage message)
        {
            switch (message.ArbitrationId)
            {
                case 0x232:
                    DriverSeat.HeaterLevel = (byte)(message.Data[0] >> 4);
                    break;
                case 0x22A:
                    PassengerSeat.HeaterLevel = (byte)(message.Data[0] >> 4);
                    break;
            }
        }

        private static void ProcessQueue(object item)
        {
            switch ((QueueCommand)item)
            {
                case QueueCommand.ButtonHeaterDriver:
                    CanAdapter.Current.SendMessage(messageButtonHeaterDriverPress);
                    Thread.Sleep(100);
                    //CanAdapter.Current.SendMessage(messageButtonHeaterDriverPress);
                    //Thread.Sleep(200);
                    CanAdapter.Current.SendMessage(messageButtonHeaterDriverRelease);
                    Thread.Sleep(100);
                    break;
                case QueueCommand.ButtonHeaterPassenger:
                    CanAdapter.Current.SendMessage(messageButtonHeaterPassengerPress);
                    Thread.Sleep(100);
                    //CanAdapter.Current.SendMessage(messageButtonHeaterPassengerPress);
                    //Thread.Sleep(200);
                    CanAdapter.Current.SendMessage(messageButtonHeaterPassengerRelease);
                    Thread.Sleep(100);
                    break;
                case QueueCommand.ButtonMDriver:
                    CanAdapter.Current.SendMessage(messageButtonDriverMPress);
                    Thread.Sleep(100);
                    CanAdapter.Current.SendMessage(messageButtonDriverMRelease);
                    Thread.Sleep(100);
                    break;
                case QueueCommand.ButtonM1Driver:
                    CanAdapter.Current.SendMessage(messageButtonDriverM1Press);
                    Thread.Sleep(100);
                    CanAdapter.Current.SendMessage(messageButtonDriverMRelease);
                    Thread.Sleep(100);
                    break;
            }
        }

        public static void ButtonHeaterDriverPress()
        {
            queueWorker.Enqueue(QueueCommand.ButtonHeaterDriver);
        }

        public static void ButtonHeaterPassengerPress()
        {
            queueWorker.Enqueue(QueueCommand.ButtonHeaterPassenger);
        }

        public static void ButtonFrontDriverPress()
        {
            CanAdapter.Current.SendMessage(messageButtonDriverSeatMoveFront);
        }

        public static void ButtonBackDriverPress()
        {
            CanAdapter.Current.SendMessage(messageButtonDriverSeatMoveRear);
        }
        
        public static void ButtonMDriverPress()
        {
            queueWorker.Enqueue(QueueCommand.ButtonMDriver);
        }

        public static void ButtonM1DriverPress()
        {
            queueWorker.Enqueue(QueueCommand.ButtonM1Driver);
        }

        private static void IBusManager_AfterMessageReceived(MessageEventArgs e)
        {
            if (emulatorStarted)
            {
                lastIBusMessageTime = DateTime.Now;
            }
            else
            {
                StartEmulator();
            }
        }

        static void EmulatorWorker()
        {
            int time = 1;
            while (emulatorStarted)
            {
                if (EmulatorPaused)
                {
                    Thread.Sleep(80);
                    continue;
                }

                if (time % 10 == 0 && lastIBusMessageTime != DateTime.MinValue)
                {
                    var fromLastIBusMessage = DateTime.Now - lastIBusMessageTime;
                    // turn off after 5 minutes of inactivity on iBus
                    if (fromLastIBusMessage.GetTotalMinutes() >= 5)
                    {
                        StopEmulator();
                        break;
                    }
                }

                var can = CanAdapter.Current;
                var ign = InstrumentClusterElectronics.CurrentIgnitionState == IgnitionState.Ign;
                messageKeepAlive.Data[0] = ign ? (byte)0x45 : (byte)0x00;
                if (can.SendMessage(messageKeepAlive))
                {
                    var counter = messageEngineRunning.Data[4];
                    counter += 0x11;
                    if (counter == 0xAF)
                    {
                        counter = 0xB0;
                    }
                    messageEngineRunning.Data[0] = messageKeepAlive.Data[0];
                    messageEngineRunning.Data[4] = counter;
                    can.SendMessage(messageEngineRunning);

                    var dimmer = imBMW.Tools.Math.Min(LightControlModule.DimmerRaw, 0xFD);
                    if (InstrumentClusterElectronics.CurrentIgnitionState == IgnitionState.Off)
                    {
                        dimmer = 0xFE;
                    }
                    if (dimmer != lastDimmer || time % 10 == 0)
                    {
                        messageLightDimmer.Data[0] = dimmer;
                        can.SendMessage(messageLightDimmer);
                        lastDimmer = dimmer;
                    }

                    if (time % 100 == 0)
                    {
                        can.SendMessage(messageEmulationUnknown5);
                    }

                    if (time % 7 == 0)
                    {
                        can.SendMessage(messageEmulateIHKA);
                    }

                    lastEmulationFailed = false;
                    time++;
                    Thread.Sleep(80); // 100ms - 20ms for processing
                }
                else
                {
                    Thread.Sleep(lastEmulationFailed ? 1000 : 80); // slow down on second unsuccessful attempt
                    lastEmulationFailed = true;
                }
            }
        }
        
        public static void StartEmulator()
        {
            lock (emulatorLock)
            {
                if (emulatorStarted)
                {
                    return;
                }
                emulatorStarted = true;
                emulatorThread = new Thread(EmulatorWorker);
                emulatorThread.Start();
            }
        }

        public static void StopEmulator()
        {
            lock (emulatorLock)
            {
                if (!emulatorStarted)
                {
                    return;
                }
                emulatorStarted = false;
                if (emulatorThread != null)
                {
                    if (emulatorThread.ThreadState == ThreadState.Running)
                    {
                        emulatorThread.Abort();
                    }
                    emulatorThread = null;
                }
            }
        }
    }
}
