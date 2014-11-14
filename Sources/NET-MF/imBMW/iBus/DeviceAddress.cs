using System;
using System.Reflection;

namespace imBMW.iBus
{
    public enum DeviceAddress : short
    {
        BodyModule = 0x00,
        SunroofControl = 0x08,
        CDChanger = 0x18,
        RadioControlledClock = 0x28,
        CheckControlModule = 0x30,
        GraphicsNavigationDriver = 0x3B,
        Diagnostic = 0x3F,
        RemoteControlCentralLocking = 0x40,
        GraphicsDriverRearScreen = 0x43,
        Immobiliser = 0x44,
        CentralInformationDisplay = 0x46,
        MultiFunctionSteeringWheel = 0x50,
        MirrorMemory = 0x51,
        IntegratedHeatingAndAirConditioning = 0x5B,
        ParkDistanceControl = 0x60,
        Radio = 0x68,
        DigitalSignalProcessingAudioAmplifier = 0x6A,
        SeatMemory = 0x72,
        SiriusRadio = 0x73,
        CDChangerDINsize = 0x76,
        NavigationEurope = 0x7F,
        InstrumentClusterElectronics = 0x80,
        MirrorMemorySecond = 0x9B,
        MirrorMemoryThird = 0x9C,
        RearMultiInfoDisplay = 0xA0,
        AirBagModule = 0xA4,
        SpeedRecognitionSystem = 0xB0,
        NavigationJapan = 0xBB,
        GlobalBroadcastAddress = 0xBF,
        MultiInfoDisplay = 0xC0,
        Telephone = 0xC8,
        Assist = 0xCA,
        LightControlModule = 0xD0,
        SeatMemorySecond = 0xDA,
        IntegratedRadioInformationSystem = 0xE0,
        FrontDisplay = 0xE7,
        RainLightSensor = 0xE8,
        Television = 0xED,
        OnBoardMonitor = 0xF0,
        Broadcast = 0xFF,

        imBMWPlayer = 0xFD,
        imBMWLogger = 0xFE,
        
        Unset = 0x100,
        Unknown = 0x101
    }

}