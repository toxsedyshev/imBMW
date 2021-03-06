using System;
using imBMW.Tools;
using System.Collections;
using System.Text;

namespace imBMW.iBus
{
    public static class MessageRegistry
    {
        #region Registries

        static string[] messageTypeDescriptions = {
            #if !MF_FRAMEWORK_VERSION_V4_1
            "", // "0x00",
            "Device status request",
            "Device status ready",
            "Bus status request",
            "Bus status",
            "", // "0x05",
            "DIAG read memory",
            "DIAG write memory",
            "DIAG read coding data",
            "DIAG write coding data",
            "", // "0x0A",
            "", // "0x0B",
            "Vehicle control",
            "", // "0x0D",
            "", // "0x0E",
            "", // "0x0F",
            "Ignition status request",
            "Ignition status",
            "IKE sensor status request",
            "IKE sensor status",
            "Country coding status request",
            "Country coding status",
            "Odometer request",
            "Odometer",
            "Speed/RPM",
            "Temperature",
            "IKE text display/Gong",
            "IKE text status",
            "Gong",
            "Temperature request",
            "", // "0x1E",
            "UTC time and date",
            "", // "0x20",
            "Radio Short cuts",
            "Text display confirmation",
            "Display Text",
            "Update ANZV",
            "", // "0x25",
            "", // "0x26",
            "", // "0x27",
            "", // "0x28",
            "", // "0x29",
            "On-Board Computer State Update",
            "Phone LEDs",
            "Phone symbol", // "0x2C",
            "", // "0x2D",
            "", // "0x2E",
            "", // "0x2F",
            "", // "0x30",
            "Select screen item", // "0x31",
            "MFL volume buttons",
            "", // "0x33",
            "DSP Equalizer Button",
            "", // "0x35",
            "", // "0x36",
            "", // "0x37",
            "CD status request",
            "CD status",
            "", // "0x3A",
            "MFL media buttons",
            "", // "0x3C",
            "SDRS status request",
            "SDRS status",
            "", // "0x3F",
            "Set On-Board Computer Data",
            "On-Board Computer Data Request",
            "", // "0x42",
            "", // "0x43",
            "", // "0x44",
            "", // "0x45",
            "LCD Clear",
            "BMBT buttons",
            "BMBT buttons",
            "KNOB button",
            "Monitor CD/Tape control",
            "Monitor CD/Tape status",
            "", // "0x4C",
            "", // "0x4D",
            "", // "0x4E",
            "Monitor Control",
            "", // "0x50",
            "", // "0x51",
            "", // "0x52",
            "Vehicle data request",
            "Vehicle data status",
            "", // "0x55",
            "", // "0x56",
            "", // "0x57",
            "", // "0x58",
            "", // "0x59",
            "Lamp status request",
            "Lamp status",
            "Instrument cluster lighting status",
            "", // "0x5D",
            "", // "0x5E",
            "", // "0x5F",
            "", // "0x60",
            "", // "0x61",
            "", // "0x62",
            "", // "0x63",
            "", // "0x64",
            "", // "0x65",
            "", // "0x66",
            "", // "0x67",
            "", // "0x68",
            "", // "0x69",
            "", // "0x6A",
            "", // "0x6B",
            "", // "0x6C",
            "Sideview Mirror", // "0x6D",
            "", // "0x6E",
            "", // "0x6F",
            "", // "0x70",
            "Rain sensor status request",
            "Remote Key buttons",
            "", // "0x73",
            "EWS key status",
            "", // "0x75",
            "External lights", // "0x76",
            "", // "0x77",
            "", // "0x78",
            "Doors/windows status request",
            "Doors/windows status",
            "", // "0x7B",
            "SHD status",
            "", // "0x7D",
            "", // "0x7E",
            "", // "0x7F",
            "", // "0x80",
            "", // "0x81",
            "", // "0x82",
            "", // "0x83",
            "", // "0x84",
            "", // "0x85",
            "", // "0x86",
            "", // "0x87",
            "", // "0x88",
            "", // "0x89",
            "", // "0x8A",
            "", // "0x8B",
            "", // "0x8C",
            "", // "0x8D",
            "", // "0x8E",
            "", // "0x8F",
            "", // "0x90",
            "", // "0x91",
            "", // "0x92",
            "", // "0x93",
            "", // "0x94",
            "", // "0x95",
            "", // "0x96",
            "", // "0x97",
            "", // "0x98",
            "", // "0x99",
            "", // "0x9A",
            "", // "0x9B",
            "", // "0x9C",
            "", // "0x9D",
            "", // "0x9E",
            "", // "0x9F",
            "DIAG data",
            "", // "0xA1",
            "Current position and time",
            "", // "0xA3",
            "Current location",
            "Screen text",
            "", // "0xA6",
            "TMC status request",
            "", // "0xA8",
            "", // "0xA9",
            "Navigation Control",
            "", // "0xAB",
            "", // "0xAC",
            "", // "0xAD",
            "", // "0xAE",
            "", // "0xAF",
            "", // "0xB0",
            "", // "0xB1",
            "", // "0xB2",
            "", // "0xB3",
            "", // "0xB4",
            "", // "0xB5",
            "", // "0xB6",
            "", // "0xB7",
            "", // "0xB8",
            "", // "0xB9",
            "", // "0xBA",
            "", // "0xBB",
            "", // "0xBC",
            "", // "0xBD",
            "", // "0xBE",
            "", // "0xBF",
            "", // "0xC0",
            "", // "0xC1",
            "", // "0xC2",
            "", // "0xC3",
            "", // "0xC4",
            "", // "0xC5",
            "", // "0xC6",
            "", // "0xC7",
            "", // "0xC8",
            "", // "0xC9",
            "", // "0xCA",
            "", // "0xCB",
            "", // "0xCC",
            "", // "0xCD",
            "", // "0xCE",
            "", // "0xCF",
            "", // "0xD0",
            "", // "0xD1",
            "", // "0xD2",
            "", // "0xD3",
            "RDS channel list",
            "", // "0xD5",
            "", // "0xD6",
            "", // "0xD7",
            "", // "0xD8",
            "", // "0xD9",
            "", // "0xDA",
            "", // "0xDB",
            "", // "0xDC",
            "", // "0xDD",
            "", // "0xDE",
            "", // "0xDF",
            "", // "0xE0",
            "", // "0xE1",
            "", // "0xE2",
            "", // "0xE3",
            "", // "0xE4",
            "", // "0xE5",
            "", // "0xE6",
            "", // "0xE7",
            "", // "0xE8",
            "", // "0xE9",
            "", // "0xEA",
            "", // "0xEB",
            "", // "0xEC",
            "", // "0xED",
            "", // "0xEE",
            "", // "0xEF",
            "", // "0xF0",
            "", // "0xF1",
            "", // "0xF2",
            "", // "0xF3",
            "", // "0xF4",
            "", // "0xF5",
            "", // "0xF6",
            "", // "0xF7",
            "", // "0xF8",
            "", // "0xF9",
            "", // "0xFA",
            "", // "0xFB",
            "", // "0xFC",
            "", // "0xFD",
            "", // "0xFE",
            "", // "0xFF"
            #endif
        };

        public static byte[] DataPollRequest = new byte[] { 0x01 };
        public static byte[] DataPollResponse = new byte[] { 0x02, 0x00 };
        public static byte[] DataAnnounce = new byte[] { 0x02, 0x01 };

        static Hashtable messageDescriptions;

        static MessageRegistry()
        {
            messageDescriptions = new Hashtable();
            messageDescriptions.Add(DataPollRequest.ToHex(' '), "Poll request");
            messageDescriptions.Add(DataPollResponse.ToHex(' '), "Poll response");
            messageDescriptions.Add(DataAnnounce.ToHex(' '), "Announce");
        }

        #endregion

        public static string ToPrettyString(this Message message, bool withPerformanceInfo = false, bool withBytesAsAscii = false)
        {
            #if !MF_FRAMEWORK_VERSION_V4_1
            if (message is InternalMessage)
            {
                var m = (InternalMessage)message;
                return m.Device.ToStringValue() + ": " + (m.ReceiverDescription ?? m.DataDump);
            }
            #endif

            string description = message.Describe();
            if (description == null)
            {
                description = message.DataDump;
            }
            description = message.SourceDevice.ToStringValue() + " > " + message.DestinationDevice.ToStringValue() + ": " + description;
            if (withBytesAsAscii)
            {
                description += " (" + ASCIIEncoding.GetString(message.Data, 0, -1, true) + ")";
            }
            if (withPerformanceInfo)
            {
                description += " (" + message.PerformanceInfo.ToString() + ")";
            }
            return description;
        }

        public static string Describe(this Message message)
        {
            if (message.Data.Length == 0)
            {
                return "";
            }

            if (message.ReceiverDescription != null)
            {
                return message.ReceiverDescription;
            }

            if (messageDescriptions.Contains(message.DataDump))
            {
                return (string)messageDescriptions[message.DataDump];
            }

            byte firstByte = message.Data[0];
            if (firstByte >= messageTypeDescriptions.Length || messageTypeDescriptions[firstByte] == "")
            {
                return null;
            }
            return message.DataDump + " (" + messageTypeDescriptions[firstByte] + ')';
        }

        public static bool IsInternal(this byte device)
        {
            return ((DeviceAddress)device).IsInternal();
        }

        public static bool IsInternal(this DeviceAddress device)
        {
            return device == DeviceAddress.imBMWLogger || device == DeviceAddress.imBMWPlayer;
        }
    }
}
