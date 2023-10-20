using System;
using System.Text;

namespace jp.kshoji.midisystem
{
    /// <summary>
    /// Abstract class for MIDI Message
    /// </summary>
    public abstract class MidiMessage : ICloneable
    {
        protected byte[] Data;

        /// <summary>
        /// Constructor with the raw data
        /// </summary>
        /// <param name="data">the raw data</param>
        protected MidiMessage(byte[] data)
        {
            Data = data;
        }

        /// <inheritdoc cref="ICloneable" />
        public abstract object Clone();

        /// <summary>
        /// Constructor with the raw data, and its length
        /// </summary>
        /// <param name="data">the raw data</param>
        protected virtual void SetMessage(byte[] data)
        {
            if (data == null)
            {
                Data = null;
            }
            else
            {
                if (Data == null || Data.Length != data.Length)
                {
                    Data = new byte[data.Length];
                }

                Array.Copy(data, 0, Data, 0, data.Length);
            }
        }

        /// <summary>
        /// Get the message source data
        /// </summary>
        /// <returns>the message source data</returns>
        public byte[] GetMessage()
        {
            return (byte[])Data?.Clone();
        }

        /// <summary>
        /// Get the status of the <see cref="MidiMessage" />
        /// </summary>
        /// <returns>the status</returns>
        public int GetStatus()
        {
            if (Data == null || Data.Length < 1)
            {
                return 0;
            }

            return Data[0] & 0xff;
        }

        /// <summary>
        /// Get the length of the <see cref="MidiMessage" />
        /// </summary>
        /// <returns>the length</returns>
        public int GetLength()
        {
            if (Data == null)
            {
                return 0;
            }

            return Data.Length;
        }

        /// <summary>
        /// Convert the byte array to the hex dumped string
        /// </summary>
        /// <param name="src">the byte array</param>
        /// <returns>dumped string</returns>
        private static string ToHexString(byte[] src)
        {
            if (src == null)
            {
                return "null";
            }

            var buffer = new StringBuilder();
            buffer.Append("[");
            var needComma = false;
            foreach (var srcByte in src)
            {
                if (needComma)
                {
                    buffer.Append(", ");
                }

                buffer.Append($"{srcByte & 0xff:x2}");
                needComma = true;
            }

            buffer.Append("]");

            return buffer.ToString();
        }

        /// <inheritdoc cref="object.ToString"/>
        public override string ToString()
        {
            return GetType().Name + ":" + ToHexString(Data);
        }
    }
}