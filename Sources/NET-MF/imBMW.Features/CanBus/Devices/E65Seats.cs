using System;
using System.Threading;
using imBMW.Features.CanBus.Adapters;
using imBMW.Tools;
using imBMW.iBus.Devices.Real;
using imBMW.iBus;
using imBMW.Features.Menu;
using CanMessage = GHI.IO.ControllerAreaNetwork.Message;

namespace imBMW.Features.CanBus.Devices
{
    #region Seats menu screen

    public class E65SeatsMoveScreen : MenuScreen
    {
        public bool IsDriverSide { get; private set; }

        public E65SeatsMoveScreen(bool driver)
        {
            IsDriverSide = driver;

            TitleCallback = s => (IsDriverSide ? "Driver" : "Pass") + " Seat";

            SetItems();
        }

        protected virtual void SetItems()
        {
            ClearItems();


            AddItem(new MenuItem(i => "Front", i => E65Seats.MoveFrontPress(IsDriverSide), MenuItemType.Button));
            AddItem(new MenuItem(i => "Back", i => E65Seats.MoveBackPress(IsDriverSide), MenuItemType.Button));
            AddItem(new MenuItem(i => "Up", i => E65Seats.MoveUpPress(IsDriverSide), MenuItemType.Button));
            AddItem(new MenuItem(i => "Down", i => E65Seats.MoveDownPress(IsDriverSide), MenuItemType.Button));
            AddItem(new MenuItem(i => "Tilt Front", i => E65Seats.TiltFrontPress(IsDriverSide), MenuItemType.Button));
            AddItem(new MenuItem(i => "Tilt Back", i => E65Seats.TiltBackPress(IsDriverSide), MenuItemType.Button));
            AddItem(new MenuItem(i => "Memory", i => E65Seats.ButtonM(IsDriverSide), MenuItemType.Button));
            AddItem(new MenuItem(i => "Memory 1", i => E65Seats.ButtonM1(IsDriverSide), MenuItemType.Button));
            this.AddBackButton();
        }
    }

    public class E65SeatsSettingsScreen : MenuScreen
    {
        public E65SeatsSettingsScreen()
        {
            TitleCallback = s => "Settings";

            SetItems();
        }

        protected virtual void SetItems()
        {
            ClearItems();

            AddItem(new MenuItem(i => "Auto Heating", i => E65Seats.AutoHeater = i.IsChecked, MenuItemType.Checkbox)
            {
                IsChecked = E65Seats.AutoHeater,
                RadioAbbreviation = "Auto Heat"
            });
            AddItem(new MenuItem(i => "Auto Ventilation", i => E65Seats.AutoVentilation = i.IsChecked, MenuItemType.Checkbox)
            {
                IsChecked = E65Seats.AutoVentilation,
                RadioAbbreviation = "Auto Vent"
            });
            AddItem(new MenuItem(i => "Activated", i => E65Seats.EmulatorPaused = !i.IsChecked, MenuItemType.Checkbox)
            {
                IsChecked = !E65Seats.EmulatorPaused
            });
            this.AddBackButton();
        }
    }

    public class E65SeatsScreen : MenuScreen
    {
        protected static E65SeatsScreen instance;

        MenuItem driverHeater, driverVentilation, driverMassage,
            passengerHeater, passengerVentilation, passengerMassage;

        E65SeatsMoveScreen driverMoveScreen = new E65SeatsMoveScreen(true);
        E65SeatsMoveScreen passengerMoveScreen = new E65SeatsMoveScreen(false);
        E65SeatsSettingsScreen settingsScreen = new E65SeatsSettingsScreen();

        protected E65SeatsScreen()
        {
            TitleCallback = s => "Seats";

            SetItems();

            E65Seats.DriverSeat.Changed += DriverSeat_Changed;
            E65Seats.PassengerSeat.Changed += PassengerSeat_Changed;
        }

        private void DriverSeat_Changed()
        {
            driverMassage.IsChecked = E65Seats.DriverSeat.IsMassageActive;

            driverHeater.Refresh();
            driverVentilation.Refresh();
            driverMassage.Refresh();
        }

        private void PassengerSeat_Changed()
        {
            passengerMassage.IsChecked = E65Seats.PassengerSeat.IsMassageActive;

            passengerHeater.Refresh();
            passengerVentilation.Refresh();
            passengerMassage.Refresh();
        }

        protected virtual void SetItems()
        {
            ClearItems();

            driverHeater = new MenuItem(i => "Driver Heat: [" + GetLevel(E65Seats.DriverSeat.HeaterLevel) + "]",
                i => E65Seats.ButtonHeaterDriver(), MenuItemType.Button) { RadioAbbreviation = "Drv Ht" };

            passengerHeater = new MenuItem(i => "Pass Heat: [" + GetLevel(E65Seats.PassengerSeat.HeaterLevel) + "]",
                i => E65Seats.ButtonHeaterPassenger(), MenuItemType.Button) { RadioAbbreviation = "Pas Ht" };

            driverVentilation = new MenuItem(i => "Driver Vent: [" + GetLevel(E65Seats.DriverSeat.VentilationSpeed) + "]",
                i => E65Seats.ButtonVentilationDriver(), MenuItemType.Button) { RadioAbbreviation = "Drv Vt" };

            passengerVentilation = new MenuItem(i => "Pass Vent: [" + GetLevel(E65Seats.PassengerSeat.VentilationSpeed) + "]",
                i => E65Seats.ButtonVentilationPassenger(), MenuItemType.Button) { RadioAbbreviation = "Pas Vt" };

            driverMassage = new MenuItem(i => "Driver Massage", i => E65Seats.ButtonMassageDriver(), MenuItemType.Checkbox, MenuItemAction.PassiveCheckbox)
            { RadioAbbreviation = "Drv Masg", IsChecked = E65Seats.DriverSeat.IsMassageActive };

            passengerMassage = new MenuItem(i => "Pass Massage", i => E65Seats.ButtonMassagePassenger(), MenuItemType.Checkbox, MenuItemAction.PassiveCheckbox)
            { RadioAbbreviation = "Pass Masg", IsChecked = E65Seats.PassengerSeat.IsMassageActive };

            AddItem(driverHeater);
            AddItem(passengerHeater);
            AddItem(driverVentilation);
            AddItem(passengerVentilation);
            AddItem(driverMassage);
            AddItem(passengerMassage);
            AddItem(new MenuItem(i => "Driver Move", MenuItemType.Button, MenuItemAction.GoToScreen) { GoToScreen = driverMoveScreen });
            AddItem(new MenuItem(i => "Passenger Move", MenuItemType.Button, MenuItemAction.GoToScreen) { GoToScreen = passengerMoveScreen, RadioAbbreviation = "Pass. Move" });
            AddItem(new MenuItem(i => "Settings", MenuItemType.Button, MenuItemAction.GoToScreen) { GoToScreen = settingsScreen });
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

        public void SetValues(byte heater, byte ventilation, bool massage)
        {
            if (heater == HeaterLevel && ventilation == VentilationSpeed && massage == IsMassageActive)
            {
                return;
            }
            heaterLevel = heater;
            ventilationSpeed = ventilation;
            isMassageActive = massage;
            OnChanged();
        }

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

        public void ParseStatusMessage(CanMessage message)
        {
            var heater = (byte)(message.Data[0] >> 4);
            var ventilation = (byte)(message.Data[1] & 0xF);
            var massage = (message.Data[0] & 0xF) == 1;
            SetValues(heater, ventilation, massage);
        }
    }

    #endregion

    public static class E65Seats
    {
        enum QueueCommand
        {
            ButtonHeaterDriver,
            ButtonHeaterPassenger,
            ButtonVentilationDriver,
            ButtonVentilationPassenger,
            ButtonMassageDriver,
            ButtonMassagePassenger,
            ButtonMDriver,
            ButtonM1Driver,
            ButtonMPassenger,
            ButtonM1Passenger
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

        public static bool AutoHeater { get; set; }

        public static bool AutoVentilation { get; set; }

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

        static uint buttonClimateDriver = 0x1E7;
        static uint buttonClimatePassenger = 0x1E8;
        static uint buttonMassageDriver = 0x1EB;
        static uint buttonMassagePassenger = 0x1EC;
        static byte[] dataButtonHeaterPress = new byte[] { 0xF1, 0xFF };
        static byte[] dataButtonVentilationPress = new byte[] { 0xF4, 0xFF };
        static byte[] dataButtonClimateRelease = new byte[] { 0xF0, 0xFF };
        static byte[] dataButtonMassagePress = new byte[] { 0xFD, 0xFF };
        static byte[] dataButtonMassageRelease = new byte[] { 0xFC, 0xFF };

        static uint buttonMoveDriver = 0xDA;
        static uint buttonMovePassenger = 0xD2;
        static byte[] dataMoveFront = new byte[] { 0x01, 0x00, 0xC0, 0xFF };
        static byte[] dataMoveBack = new byte[] { 0x02, 0x00, 0xC0, 0xFF };
        static byte[] dataMoveUp = new byte[] { 0x04, 0x00, 0xC0, 0xFF };
        static byte[] dataMoveDown = new byte[] { 0x08, 0x00, 0xC0, 0xFF };
        static byte[] dataTiltBack = new byte[] { 0x20, 0x00, 0xC0, 0xFF };
        static byte[] dataTiltFront = new byte[] { 0x10, 0x00, 0xC0, 0xFF };

        static uint buttonMemoryDriver = 0x1F3;
        static uint buttonMemoryPassenger = 0x1F2;
        static byte[] dataButtonMPress = new byte[] { 0xFC, 0xFF };
        static byte[] dataButtonMRelease = new byte[] { 0xF8, 0xFF };
        static byte[] dataButtonM1Press = new byte[] { 0xF9, 0xFF };

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

            InstrumentClusterElectronics.EngineStarted += InstrumentClusterElectronics_EngineStarted;
        }

        private static void InstrumentClusterElectronics_EngineStarted()
        {
            if (AutoHeater
                && InstrumentClusterElectronics.TemperatureOutside < 10
                && InstrumentClusterElectronics.TemperatureCoolant < 80)
            {
                if (DriverSeat.HeaterLevel == 0)
                {
                    ButtonHeaterDriver();
                }
                if (PassengerSeat.HeaterLevel == 0)
                {
                    ButtonHeaterPassenger();
                }
            }
            if (AutoVentilation
                && InstrumentClusterElectronics.TemperatureOutside >= 20)
            {
                if (DriverSeat.VentilationSpeed == 0)
                {
                    ButtonVentilationDriver();
                }
                if (PassengerSeat.VentilationSpeed == 0)
                {
                    ButtonVentilationPassenger();
                }
            }
        }

        private static void Can_MessageReceived(CanAdapter can, CanMessage message)
        {
            switch (message.ArbitrationId)
            {
                case 0x232:
                    DriverSeat.ParseStatusMessage(message);
                    break;
                case 0x22A:
                    PassengerSeat.ParseStatusMessage(message);
                    break;
            }
        }

        private static void ProcessQueue(object item)
        {
            switch ((QueueCommand)item)
            {
                case QueueCommand.ButtonHeaterDriver:
                    CanAdapter.Current.SendMessage(new CanMessage(buttonClimateDriver, dataButtonHeaterPress));
                    Thread.Sleep(100);
                    CanAdapter.Current.SendMessage(new CanMessage(buttonClimateDriver, dataButtonClimateRelease));
                    Thread.Sleep(100);
                    break;
                case QueueCommand.ButtonHeaterPassenger:
                    CanAdapter.Current.SendMessage(new CanMessage(buttonClimatePassenger, dataButtonHeaterPress));
                    Thread.Sleep(100);
                    CanAdapter.Current.SendMessage(new CanMessage(buttonClimatePassenger, dataButtonClimateRelease));
                    Thread.Sleep(100);
                    break;
                case QueueCommand.ButtonVentilationDriver:
                    CanAdapter.Current.SendMessage(new CanMessage(buttonClimateDriver, dataButtonVentilationPress));
                    Thread.Sleep(100);
                    CanAdapter.Current.SendMessage(new CanMessage(buttonClimateDriver, dataButtonClimateRelease));
                    Thread.Sleep(100);
                    break;
                case QueueCommand.ButtonVentilationPassenger:
                    CanAdapter.Current.SendMessage(new CanMessage(buttonClimatePassenger, dataButtonVentilationPress));
                    Thread.Sleep(100);
                    CanAdapter.Current.SendMessage(new CanMessage(buttonClimatePassenger, dataButtonClimateRelease));
                    Thread.Sleep(100);
                    break;
                case QueueCommand.ButtonMassageDriver:
                    CanAdapter.Current.SendMessage(new CanMessage(buttonMassageDriver, dataButtonMassagePress));
                    Thread.Sleep(100);
                    CanAdapter.Current.SendMessage(new CanMessage(buttonMassageDriver, dataButtonMassageRelease));
                    Thread.Sleep(100);
                    break;
                case QueueCommand.ButtonMassagePassenger:
                    CanAdapter.Current.SendMessage(new CanMessage(buttonMassagePassenger, dataButtonMassagePress));
                    Thread.Sleep(100);
                    CanAdapter.Current.SendMessage(new CanMessage(buttonMassagePassenger, dataButtonMassageRelease));
                    Thread.Sleep(100);
                    break;
                case QueueCommand.ButtonMDriver:
                    CanAdapter.Current.SendMessage(new CanMessage(buttonMemoryDriver, dataButtonMPress));
                    Thread.Sleep(100);
                    CanAdapter.Current.SendMessage(new CanMessage(buttonMemoryDriver, dataButtonMRelease));
                    Thread.Sleep(100);
                    break;
                case QueueCommand.ButtonM1Driver:
                    CanAdapter.Current.SendMessage(new CanMessage(buttonMemoryDriver, dataButtonM1Press));
                    Thread.Sleep(100);
                    CanAdapter.Current.SendMessage(new CanMessage(buttonMemoryDriver, dataButtonMRelease));
                    Thread.Sleep(100);
                    break;
                case QueueCommand.ButtonMPassenger:
                    CanAdapter.Current.SendMessage(new CanMessage(buttonMemoryPassenger, dataButtonMPress));
                    Thread.Sleep(100);
                    CanAdapter.Current.SendMessage(new CanMessage(buttonMemoryPassenger, dataButtonMRelease));
                    Thread.Sleep(100);
                    break;
                case QueueCommand.ButtonM1Passenger:
                    CanAdapter.Current.SendMessage(new CanMessage(buttonMemoryPassenger, dataButtonM1Press));
                    Thread.Sleep(100);
                    CanAdapter.Current.SendMessage(new CanMessage(buttonMemoryPassenger, dataButtonMRelease));
                    Thread.Sleep(100);
                    break;
            }
        }

        public static void ButtonHeaterDriver()
        {
            queueWorker.Enqueue(QueueCommand.ButtonHeaterDriver);
        }

        public static void ButtonHeaterPassenger()
        {
            queueWorker.Enqueue(QueueCommand.ButtonHeaterPassenger);
        }

        public static void ButtonVentilationDriver()
        {
            queueWorker.Enqueue(QueueCommand.ButtonVentilationDriver);
        }

        public static void ButtonVentilationPassenger()
        {
            queueWorker.Enqueue(QueueCommand.ButtonVentilationPassenger);
        }

        public static void ButtonMassageDriver()
        {
            queueWorker.Enqueue(QueueCommand.ButtonMassageDriver);
        }

        public static void ButtonMassagePassenger()
        {
            queueWorker.Enqueue(QueueCommand.ButtonMassagePassenger);
        }

        static uint GetMoveArbId(bool driver)
        {
            return driver ? buttonMoveDriver : buttonMovePassenger;
        }

        public static void MoveFrontPress(bool driver)
        {
            CanAdapter.Current.SendMessage(new CanMessage(GetMoveArbId(driver), dataMoveFront));
        }

        public static void MoveBackPress(bool driver)
        {
            CanAdapter.Current.SendMessage(new CanMessage(GetMoveArbId(driver), dataMoveBack));
        }

        public static void MoveUpPress(bool driver)
        {
            CanAdapter.Current.SendMessage(new CanMessage(GetMoveArbId(driver), dataMoveUp));
        }

        public static void MoveDownPress(bool driver)
        {
            CanAdapter.Current.SendMessage(new CanMessage(GetMoveArbId(driver), dataMoveDown));
        }

        public static void TiltBackPress(bool driver)
        {
            CanAdapter.Current.SendMessage(new CanMessage(GetMoveArbId(driver), dataTiltBack));
        }

        public static void TiltFrontPress(bool driver)
        {
            CanAdapter.Current.SendMessage(new CanMessage(GetMoveArbId(driver), dataTiltFront));
        }

        public static void ButtonM(bool driver)
        {
            queueWorker.Enqueue(driver ? QueueCommand.ButtonMDriver : QueueCommand.ButtonMPassenger);
        }

        public static void ButtonM1(bool driver)
        {
            queueWorker.Enqueue(driver ? QueueCommand.ButtonM1Driver : QueueCommand.ButtonM1Passenger);
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

                    var dimmer = MathEx.Min(LightControlModule.DimmerRaw, 0xFD);
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
