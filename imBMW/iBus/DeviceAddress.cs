using System;
using System.Reflection;

namespace imBMW.iBus
{
    enum DeviceAddress : short
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
        OnBoardMonitorOperatingPart = 0xF0,
        Broadcast = 0xFF,
        Unset = 0x100,
        Unknown = 0x101
    }

    static class DeviceAddressConverter
    {
        public static string ToStringValue(this DeviceAddress e)
        {
            // :) Sorry, it's .NET MF, w/o FieldInfo.GetCustomAttributes()
            switch (e)
            {
                case DeviceAddress.BodyModule: return "BodyModule";
                case DeviceAddress.SunroofControl: return "SunroofControl";
                case DeviceAddress.CDChanger: return "CDChanger";
                case DeviceAddress.RadioControlledClock: return "RadioControlledClock";
                case DeviceAddress.CheckControlModule: return "CheckControlModule";
                case DeviceAddress.GraphicsNavigationDriver: return "GraphicsNavigationDriver";
                case DeviceAddress.Diagnostic: return "Diagnostic";
                case DeviceAddress.RemoteControlCentralLocking: return "RemoteControlCentralLocking";
                case DeviceAddress.GraphicsDriverRearScreen: return "GraphicsDriverRearScreen";
                case DeviceAddress.Immobiliser: return "Immobiliser";
                case DeviceAddress.CentralInformationDisplay: return "CentralInformationDisplay";
                case DeviceAddress.MultiFunctionSteeringWheel: return "MultiFunctionSteeringWheel";
                case DeviceAddress.MirrorMemory: return "MirrorMemory";
                case DeviceAddress.IntegratedHeatingAndAirConditioning: return "IntegratedHeatingAndAirConditioning";
                case DeviceAddress.ParkDistanceControl: return "ParkDistanceControl";
                case DeviceAddress.Radio: return "Radio";
                case DeviceAddress.DigitalSignalProcessingAudioAmplifier: return "DigitalSignalProcessingAudioAmplifier";
                case DeviceAddress.SeatMemory: return "SeatMemory";
                case DeviceAddress.SiriusRadio: return "SiriusRadio";
                case DeviceAddress.CDChangerDINsize: return "CDChangerDINsize";
                case DeviceAddress.NavigationEurope: return "NavigationEurope";
                case DeviceAddress.InstrumentClusterElectronics: return "InstrumentClusterElectronics";
                case DeviceAddress.MirrorMemorySecond: return "MirrorMemorySecond";
                case DeviceAddress.MirrorMemoryThird: return "MirrorMemoryThird";
                case DeviceAddress.RearMultiInfoDisplay: return "RearMultiInfoDisplay";
                case DeviceAddress.AirBagModule: return "AirBagModule";
                case DeviceAddress.SpeedRecognitionSystem: return "SpeedRecognitionSystem";
                case DeviceAddress.NavigationJapan: return "NavigationJapan";
                case DeviceAddress.GlobalBroadcastAddress: return "GlobalBroadcastAddress";
                case DeviceAddress.MultiInfoDisplay: return "MultiInfoDisplay";
                case DeviceAddress.Telephone: return "Telephone";
                case DeviceAddress.Assist: return "Assist";
                case DeviceAddress.LightControlModule: return "LightControlModule";
                case DeviceAddress.SeatMemorySecond: return "SeatMemorySecond";
                case DeviceAddress.IntegratedRadioInformationSystem: return "IntegratedRadioInformationSystem";
                case DeviceAddress.FrontDisplay: return "FrontDisplay";
                case DeviceAddress.RainLightSensor: return "RainLightSensor";
                case DeviceAddress.Television: return "Television";
                case DeviceAddress.OnBoardMonitorOperatingPart: return "OnBoardMonitorOperatingPart";
                case DeviceAddress.Broadcast: return "Broadcast";
                case DeviceAddress.Unset: return "Unset";
                case DeviceAddress.Unknown: return "Unknown";
            }
            return "NotSpecified";
        }
    }
}