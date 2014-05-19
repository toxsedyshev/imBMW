using System;

namespace imBMW.Tools
{
    /// <summary>
    /// Construct a larger string by appending strings together.
    /// </summary>
    public class StringBuilder
    {
        private const int InitialSize = 16;
        private const int MinGrowthSize = 64;

        private char[] _content;

        /// <summary>
        /// Public constructor
        /// </summary>
        public StringBuilder()
            : this(InitialSize)
        {
        }

        /// <summary>
        /// Public constructor
        /// </summary>
        /// <param name="capacity">Set initial builder capacity</param>
        public StringBuilder(int capacity)
        {
            _content = new char[capacity];
        }

        /// <summary>
        /// Public constructor
        /// </summary>
        /// <param name="initital">The initial content of the string builder</param>
        public StringBuilder(string initital)
        {
            _content = initital.ToCharArray();
            Length = _content.Length;
        }

        /// <summary>
        /// Append a character to the current string builder
        /// </summary>
        /// <param name="c"></param>
        public void Append(char c)
        {
            Append(new string(new[] { c }));
        }

        /// <summary>
        /// Append a string to the current string builder
        /// </summary>
        /// <param name="toAppend">String to be appended.</param>
        public void Append(string toAppend)
        {
            int additionalSpaceRequired = (toAppend.Length + Length) - _content.Length;

            if (additionalSpaceRequired > 0)
            {
                // ensure at least minimum growth size is done to minimize future copying / manipulation
                if (additionalSpaceRequired < MinGrowthSize)
                {
                    additionalSpaceRequired = MinGrowthSize;
                }

                var tmp = new char[_content.Length + additionalSpaceRequired];

                // copy content to new array
                Array.Copy(_content, tmp, Length);

                // replace the content array.
                _content = tmp;
            }

            // copy the new content to the holding array
            Array.Copy(toAppend.ToCharArray(), 0, _content, Length, toAppend.Length);
            Length += toAppend.Length;
        }


        /// <summary>
        /// Append the provided line along with a new line.
        /// </summary>
        /// <param name="str"></param>
        public void AppendLine(string str)
        {
            Append(str);
            Append("\r\n");
        }

        /// <summary>
        /// Append to the string builder using format string and placeholder arguments
        /// </summary>
        /// <param name="format">String to be formatted</param>
        /// <param name="args">Arguments to be placed into the formatted string</param>
        public void AppendFormat(string format, params object[] args)
        {
            Append(StringHelpers.Format(format, args));
        }

        /// <summary>
        /// Gets the length of the string builder.
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// Gets char at specified position.
        /// </summary>
        /// <param name="index">Position to return char for.</param>
        /// <returns>Char is returned.</returns>
        public int Get(int index)
        {
            if (index >= Length) throw new ArgumentException("Invalid index length");
            if (Length == 0) return 0;

            return _content[index];
        }

        /// <summary>
        /// Clear the current string builder back to an empty string.
        /// </summary>
        public void Clear()
        {
            Length = 0;
        }

        /// <summary>
        /// Get the final built string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return new string(_content, 0, Length);
        }
    }
}
