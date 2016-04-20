using System;

namespace System.Text
{
    // TODO
    public class ASCIIEncoding// : Encoding
    {
        public static string GetString(byte[] bytes, int offset = 0, int length = -1, bool eolAsSpace = true)
        {
            if (length < 0)
            {
                length = Math.Max(0, bytes.Length - offset);
            }
            if (length == 0)
            {
                return String.Empty;
            }
            var chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = (char)bytes[i + offset];
                if (chars[i] == '\0')
                {
                    if (eolAsSpace)
                    {
                        chars[i] = ' ';
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return new string(chars);
        }

        /*Decoder decoder;

        class ASCIIDecoder : Decoder
        {
            public override void Convert(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed)
            {
            }
        }

        public override Decoder GetDecoder()
        {
            if (decoder == null)
            {
                decoder = new ASCIIDecoder();
            }
            return decoder;
        }*/
    }
}
