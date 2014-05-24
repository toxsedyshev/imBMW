using System;
using Microsoft.SPOT.Hardware;

namespace imBMW.Tools
{
    public class RCSwitch
    {
        #region Private fields

        private readonly OutputPort _pin;
        private byte _protocol;

        #endregion

        #region Public constructor

        public RCSwitch(Cpu.Pin pin)
            : this(pin, 1, 10)
        { }

        public RCSwitch(Cpu.Pin pin, byte protocol, byte repeat)
        {
            _pin = new OutputPort(pin, false);
            Protocol = protocol;
            RepeatTransmit = repeat;
        }

        #endregion

        #region Public properties

        public byte Protocol
        {
            get { return _protocol; }
            set
            {
                if (value > 3) throw new ArgumentException("Only 1, 2, 3 protocol is supported");
                _protocol = value;
                switch (_protocol)
                {
                    case 1:
                        PulseLength = 350;
                        break;
                    case 2:
                        PulseLength = 650;
                        break;
                    case 3:
                        PulseLength = 100;
                        break;
                }
            }
        }

        public ushort PulseLength { get; set; }

        public byte RepeatTransmit { get; set; }

        #endregion

        #region Private static methods

        private void DelayMicroseconds(int microseconds)
        {
            long delayTime = microseconds * 10;
            long delayStart = Utility.GetMachineTime().Ticks;
            while ((Utility.GetMachineTime().Ticks - delayStart) < delayTime)
            {
            }
        }

        private static string DecimalToBinaryWithZeroFill(ulong dec, uint bitLength)
        {
            return DecimalToBinaryWithCharFill(dec, bitLength, '0');
        }

        private static string DecimalToBinaryWithCharFill(ulong dec, uint bitLength, char fill)
        {
            var bin = new char[64];
            uint i = 0;

            while (dec > 0)
            {
                bin[32 + i++] = ((dec & 1) > 0) ? '1' : fill;
                dec = dec >> 1;
            }

            for (uint j = 0; j < bitLength; j++)
            {
                if (j >= bitLength - i)
                {
                    bin[j] = bin[31 + i - (j - (bitLength - i))];
                }
                else
                {
                    bin[j] = fill;
                }
            }
            return new string(bin);
        }

        /**
         * Returns a string, representing the Code Word to be send.
         */
        private static string GetCodeWordA(string group, string device, bool isOn)
        {
            var dipSwitches = new char[13];
            int i;
            int j = 0;

            for (i = 0; i < 5; i++)
            {
                if (group[i] == '0')
                {
                    dipSwitches[j++] = 'F';
                }
                else
                {
                    dipSwitches[j++] = '0';
                }
            }

            for (i = 0; i < 5; i++)
            {
                if (device[i] == '0')
                {
                    dipSwitches[j++] = 'F';
                }
                else
                {
                    dipSwitches[j++] = '0';
                }
            }

            if (isOn)
            {
                dipSwitches[j++] = '0';
                dipSwitches[j] = 'F';
            }
            else
            {
                dipSwitches[j++] = 'F';
                dipSwitches[j] = '0';
            }

            return new string(dipSwitches);
        }

        /**
         * Returns a char[13], representing the Code Word to be send.
         * A Code Word consists of 9 address bits, 3 data bits and one sync bit but in our case only the first 8 address bits and the last 2 data bits were used.
         * A Code Bit can have 4 different states: "F" (floating), "0" (low), "1" (high), "S" (synchronous bit)
         *
         * +-------------------------------+--------------------------------+-----------------------------------------+-----------------------------------------+----------------------+------------+
         * | 4 bits address (switch group) | 4 bits address (switch number) | 1 bit address (not used, so never mind) | 1 bit address (not used, so never mind) | 2 data bits (on|off) | 1 sync bit |
         * | 1=0FFF 2=F0FF 3=FF0F 4=FFF0   | 1=0FFF 2=F0FF 3=FF0F 4=FFF0    | F                                       | F                                       | on=FF off=F0         | S          |
         * +-------------------------------+--------------------------------+-----------------------------------------+-----------------------------------------+----------------------+------------+
         *
         * nAddressCode  Number of the switch group (1..4)
         * nChannelCode  Number of the switch itself (1..4)
         * bStatus       Whether to switch on (true) or off (false)
         *
         */
        private static string GetCodeWordB(int addressCode, int channelCode, bool status)
        {
            int returnPos = 0;
            var r = new char[13];

            string[] code = { "FFFF", "0FFF", "F0FF", "FF0F", "FFF0" };
            if (addressCode < 1 || addressCode > 4 || channelCode < 1 || channelCode > 4)
            {
                return string.Empty;
            }
            for (int i = 0; i < 4; i++)
            {
                r[returnPos++] = code[addressCode][i];
            }

            for (int i = 0; i < 4; i++)
            {
                r[returnPos++] = code[channelCode][i];
            }

            r[returnPos++] = 'F';
            r[returnPos++] = 'F';
            r[returnPos++] = 'F';

            r[returnPos] = status ? 'F' : '0';

            return new string(r);
        }

        /**
         * Like getCodeWord (Type C = Intertechno)
         */
        private static string GetCodeWordC(char family, uint group, uint device, bool status)
        {
            var r = new char[13];
            int returnPos = 0;

            if ((byte)family < 97 || (byte)family > 112 || group < 1 || group > 4 || device < 1 || device > 4)
            {
                return string.Empty;
            }

            string sDeviceGroupCode = DecimalToBinaryWithZeroFill((device - 1) + (group - 1) * 4, 4);
            string[] familycode =
				{
					"0000", "F000", "0F00", "FF00", "00F0", "F0F0", "0FF0", "FFF0", "000F", "F00F", "0F0F", "FF0F"
					, "00FF", "F0FF", "0FFF", "FFFF"
				};
            for (int i = 0; i < 4; i++)
            {
                r[returnPos++] = familycode[family - 97][i];
            }
            for (int i = 0; i < 4; i++)
            {
                r[returnPos++] = (sDeviceGroupCode[3 - i] == '1' ? 'F' : '0');
            }
            r[returnPos++] = '0';
            r[returnPos++] = 'F';
            r[returnPos++] = 'F';
            r[returnPos] = status ? 'F' : '0';
            return new string(r);
        }

        /**
         * Decoding for the REV Switch Type
         *
         * Returns a char[13], representing the Tristate to be send.
         * A Code Word consists of 7 address bits and 5 command data bits.
         * A Code Bit can have 3 different states: "F" (floating), "0" (low), "1" (high)
         *
         * +-------------------------------+--------------------------------+-----------------------+
         * | 4 bits address (switch group) | 3 bits address (device number) | 5 bits (command data) |
         * | A=1FFF B=F1FF C=FF1F D=FFF1   | 1=0FFF 2=F0FF 3=FF0F 4=FFF0    | on=00010 off=00001    |
         * +-------------------------------+--------------------------------+-----------------------+
         *
         * Source: http://www.the-intruder.net/funksteckdosen-von-rev-uber-arduino-ansteuern/
         *
         * Group        Name of the switch group (A..D, resp. a..d) 
         * Device       Number of the switch itself (1..3)
         * bStatus       Whether to switch on (true) or off (false)
         *
         */
        private static string GetCodeWordD(char group, int deviceCode, bool status)
        {
            var r = new char[13];
            int returnPos = 0;

            // Building 4 bits address
            // (Potential problem if dec2binWcharfill not returning correct string)
            string groupCode;
            switch (group)
            {
                case 'a':
                case 'A':
                    groupCode = DecimalToBinaryWithCharFill(8, 4, 'F');
                    break;
                case 'b':
                case 'B':
                    groupCode = DecimalToBinaryWithCharFill(4, 4, 'F');
                    break;
                case 'c':
                case 'C':
                    groupCode = DecimalToBinaryWithCharFill(2, 4, 'F');
                    break;
                case 'd':
                case 'D':
                    groupCode = DecimalToBinaryWithCharFill(1, 4, 'F');
                    break;
                default:
                    return string.Empty;
            }

            for (int i = 0; i < 4; i++)
            {
                r[returnPos++] = groupCode[i];
            }

            // Building 3 bits address
            // (Potential problem if dec2binWcharfill not returning correct string)
            string device;
            switch (deviceCode)
            {
                case 1:
                    device = DecimalToBinaryWithCharFill(4, 3, 'F');
                    break;
                case 2:
                    device = DecimalToBinaryWithCharFill(2, 3, 'F');
                    break;
                case 3:
                    device = DecimalToBinaryWithCharFill(1, 3, 'F');
                    break;
                default:
                    return string.Empty;
            }

            for (int i = 0; i < 3; i++)
                r[returnPos++] = device[i];

            // fill up rest with zeros
            for (int i = 0; i < 5; i++)
                r[returnPos++] = '0';

            // encode on or off
            if (status)
                r[10] = '1';
            else
                r[11] = '1';

            // last position terminate string
            return new string(r);
        }

        #endregion

        #region Private methods

        private void Transmit(int highPulses, int lowPulses)
        {
            if (_pin != null)
            {
                _pin.Write(true);
                DelayMicroseconds(PulseLength * highPulses);
                _pin.Write(false);
                DelayMicroseconds(PulseLength * lowPulses);
            }
        }

        /**
         * Sends a "0" Bit
         *                       _    
         * Waveform Protocol 1: | |___
         *                       _  
         * Waveform Protocol 2: | |__
         */
        private void Send0()
        {
            switch (Protocol)
            {
                case 1:
                    Transmit(1, 3);
                    break;
                case 2:
                    Transmit(1, 2);
                    break;
                case 3:
                    Transmit(4, 11);
                    break;
            }
        }

        /**
         * Sends a "1" Bit
         *                       ___  
         * Waveform Protocol 1: |   |_
         *                       __  
         * Waveform Protocol 2: |  |_
         */
        private void Send1()
        {
            switch (Protocol)
            {
                case 1:
                    Transmit(3, 1);
                    break;
                case 2:
                    Transmit(2, 1);
                    break;
                case 3:
                    Transmit(9, 6);
                    break;
            }
        }

        /**
         * Sends a Tri-State "0" Bit
         *            _     _
         * Waveform: | |___| |___
         */
        private void SendT0()
        {
            Transmit(1, 3);
            Transmit(1, 3);
        }

        /**
         * Sends a Tri-State "1" Bit
         *            ___   ___
         * Waveform: |   |_|   |_
         */
        private void SendT1()
        {
            Transmit(3, 1);
            Transmit(3, 1);
        }

        /**
         * Sends a Tri-State "F" Bit
         *            _     ___
         * Waveform: | |___|   |_
         */
        private void SendTF()
        {
            Transmit(1, 3);
            Transmit(3, 1);
        }

        /**
         * Sends a "Sync" Bit
         *                       _
         * Waveform Protocol 1: | |_______________________________
         *                       _
         * Waveform Protocol 2: | |__________
         */
        private void SendSync()
        {
            switch (Protocol)
            {
                case 1:
                    Transmit(1, 31);
                    break;
                case 2:
                    Transmit(1, 10);
                    break;
                case 3:
                    Transmit(1, 71);
                    break;
            }
        }

        #endregion

        #region Public methods

        public void Send(ulong code, uint length)
        {
            Send(DecimalToBinaryWithZeroFill(code, length));
        }

        public void Send(string codeWord)
        {
            for (int nRepeat = 0; nRepeat < RepeatTransmit; nRepeat++)
            {
                for (int i = 0; i < codeWord.Length; i++)
                {
                    switch (codeWord[i])
                    {
                        case '0':
                            Send0();
                            break;
                        case '1':
                            Send1();
                            break;
                    }
                }
                SendSync();
            }
        }

        public void SendTriState(string codeWord)
        {
            for (int nRepeat = 0; nRepeat < RepeatTransmit; nRepeat++)
            {
                for (int i = 0; i < codeWord.Length; i++)
                {
                    switch (codeWord[i])
                    {
                        case '0':
                            SendT0();
                            break;
                        case 'F':
                            SendTF();
                            break;
                        case '1':
                            SendT1();
                            break;
                    }
                }
                SendSync();
            }
        }

        /**
         * Switch a remote switch on (Type D REV)
         *
         * sGroup        Code of the switch group (A,B,C,D)
         * nDevice       Number of the switch itself (1..3)
         */
        public void SwitchOn(char group, int device)
        {
            SendTriState(GetCodeWordD(group, device, true));
        }

        /**
         * Switch a remote switch off (Type D REV)
         *
         * sGroup        Code of the switch group (A,B,C,D)
         * nDevice       Number of the switch itself (1..3)
         */
        public void SwitchOff(char group, int device)
        {
            SendTriState(GetCodeWordD(group, device, false));
        }

        /**
         * Switch a remote switch on (Type C Intertechno)
         *
         * sFamily  Familycode (a..f)
         * nGroup   Number of group (1..4)
         * nDevice  Number of device (1..4)
          */
        public void SwitchOn(char family, uint group, uint device)
        {
            SendTriState(GetCodeWordC(family, group, device, true));
        }

        /**
         * Switch a remote switch off (Type C Intertechno)
         *
         * sFamily  Familycode (a..f)
         * nGroup   Number of group (1..4)
         * nDevice  Number of device (1..4)
         */
        public void SwitchOff(char family, uint group, uint device)
        {
            SendTriState(GetCodeWordC(family, group, device, false));
        }

        /**
         * Switch a remote switch on (Type B with two rotary/sliding switches)
         *
         * nAddressCode  Number of the switch group (1..4)
         * nChannelCode  Number of the switch itself (1..4)
         */
        public void SwitchOn(int addressCode, int channelCode)
        {
            SendTriState(GetCodeWordB(addressCode, channelCode, true));
        }

        /**
         * Switch a remote switch off (Type B with two rotary/sliding switches)
         *
         * nAddressCode  Number of the switch group (1..4)
         * nChannelCode  Number of the switch itself (1..4)
         */
        public void SwitchOff(int addressCode, int channelCode)
        {
            SendTriState(GetCodeWordB(addressCode, channelCode, false));
        }

        /**
         * Switch a remote switch on (Type A with 10 pole DIP switches)
         *
         * sGroup        Code of the switch group (refers to DIP switches 1..5 where "1" = on and "0" = off, if all DIP switches are on it's "11111")
         * sDevice       Code of the switch device (refers to DIP switches 6..10 (A..E) where "1" = on and "0" = off, if all DIP switches are on it's "11111")
         */
        public void SwitchOn(string group, string device)
        {
            SendTriState(GetCodeWordA(group, device, true));
        }

        /**
         * Switch a remote switch off (Type A with 10 pole DIP switches)
         *
         * sGroup        Code of the switch group (refers to DIP switches 1..5 where "1" = on and "0" = off, if all DIP switches are on it's "11111")
         * sDevice       Code of the switch device (refers to DIP switches 6..10 (A..E) where "1" = on and "0" = off, if all DIP switches are on it's "11111")
         */
        public void SwitchOff(string group, string device)
        {
            SendTriState(GetCodeWordA(group, device, false));
        }

        #endregion
    }
}
