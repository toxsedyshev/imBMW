using System;
using Microsoft.SPOT;
using imBMW.iBus.Devices;

namespace imBMW.iBus
{
    static class EnumConverter
{
        /**
        * :) Sorry, it's .NET MF,
        * so there is no pretty way to print enums
        */

        public static string ToStringValue(this iPodChanger.iPodCommand e)
        {
            switch (e)
            {
                case iPodChanger.iPodCommand.Next: return "Next";
                case iPodChanger.iPodCommand.Prev: return "Prev";
                case iPodChanger.iPodCommand.Play: return "Play";
                case iPodChanger.iPodCommand.Pause: return "Pause";
                case iPodChanger.iPodCommand.PlayPauseToggle: return "PlayPauseToggle";
                case iPodChanger.iPodCommand.VoiceOverCurrent: return "VoiceOverCurrent";
                case iPodChanger.iPodCommand.VoiceOverMenu: return "VoiceOverMenu";
            }
            return "NotSpecified";
        }

        public static string ToStringValue(this DeviceAddress e)
        {
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
