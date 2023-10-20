using System;
using System.IO;

namespace jp.kshoji.midisystem
{
    /// <summary>
    /// Represents MIDI Meta Message
    /// </summary>
    public class MetaMessage : MidiMessage
    {
        public const int Meta = 0xff;

        public const int SequenceNumber = 0x00;
        public const int Text = 0x01;
        public const int CopyrightNotice = 0x02;
        public const int TrackName = 0x03;
        public const int InstrumentName = 0x04;
        public const int Lyrics = 0x05;
        public const int Marker = 0x06;
        public const int CuePoint = 0x07;
        public const int ChannelPrefix = 0x20;
        public const int EndOfTrack = 0x2f;
        public const int Tempo = 0x51;
        public const int SmpteOffset = 0x54;
        public const int TimeSignature = 0x58;
        public const int KeySignature = 0x59;
        public const int SequencerSpecific = 0x7f;

        private static readonly byte[] DefaultMessage = { Meta, 0, 0 };
        private static readonly byte[] EmptyMessage = { };

        private int dataLength;

        /// <summary>
        /// Constructor with default message
        /// </summary>
        public MetaMessage() : this(DefaultMessage)
        {
        }

        /// <summary>
        /// Constructor with raw data
        /// </summary>
        /// <param name="data">the data source with META header(2 bytes) + length( > 1 byte), the data.length must be >= 3 bytes</param>
        /// <exception cref="InvalidDataException">
        /// MUST be caught. We can't throw <see cref="InvalidMidiDataException" /> because
        /// of API compatibility.
        /// </exception>
        public MetaMessage(byte[] data) : base(data)
        {
            if (data.Length >= 3)
            {
                // check length
                dataLength = data.Length - 3;
                var pos = 2;
                while (pos < data.Length && (data[pos] & 0x80) != 0)
                {
                    dataLength--;
                    pos++;
                }
            }

            if (dataLength < 0)
            {
                // 'dataLength' may negative value. Negative 'dataLength' will throw NegativeArraySizeException when getData() called.
                throw new InvalidDataException("Invalid meta event. data: " + BitConverter.ToString(data).Replace("-", " "));
            }
        }

        /// <summary>
        /// Constructor with the entire information of message
        /// </summary>
        /// <param name="type">the data type</param>
        /// <param name="data">the data source</param>
        public MetaMessage(int type, byte[] data) : base(null)
        {
            SetMessage(type, data);
        }

        /// <summary>
        /// Set the entire information of message.
        /// </summary>
        /// <param name="type">the data type 0-127</param>
        /// <param name="data">the data source</param>
        /// <exception cref="InvalidMidiDataException"></exception>
        public void SetMessage(int type, byte[] data)
        {
            if (type >= 128 || type < 0)
            {
                throw new InvalidMidiDataException("Invalid meta event. type: " + type);
            }

            var newData = data ?? EmptyMessage;

            var headerLength = 2 + GetMidiValuesLength(newData.Length);
            dataLength = newData.Length;
            Data = new byte[headerLength + newData.Length];

            // Write header
            Data[0] = Meta;
            Data[1] = (byte)type;

            // Write data length
            WriteMidiValues(Data, 2, newData.Length);

            // Write data
            if (newData.Length > 0)
            {
                Array.Copy(newData, 0, Data, headerLength, newData.Length);
            }
        }

        /// <summary>
        /// Get the type of {@link MetaMessage}
        /// </summary>
        /// <returns>the type</returns>
        public int GetMessageType()
        {
            if (Data != null && Data.Length >= 2)
            {
                return Data[1] & 0xff;
            }

            return 0;
        }

        /// <summary>
        /// Get the data of {@link MetaMessage}
        /// </summary>
        /// <returns>the data without header(`META`, type, data length)</returns>
        public byte[] GetData()
        {
            if (Data == null)
            {
                return EmptyMessage;
            }

            var returnedArray = new byte[dataLength];
            Array.Copy(Data, Data.Length - dataLength, returnedArray, 0, dataLength);
            return returnedArray;
        }

        /// <inheritdoc cref="ICloneable" />
        public override object Clone()
        {
            if (Data == null)
            {
                return new MetaMessage(EmptyMessage);
            }

            var result = new byte[Data.Length];
            Array.Copy(Data, 0, result, 0, Data.Length);
            return new MetaMessage(result);
        }

        /// <summary>
        /// Get the data length for the specified value
        /// </summary>
        /// <param name="value">the value to write</param>
        /// <returns>the data length</returns>
        private static int GetMidiValuesLength(long value)
        {
            var length = 0;
            var currentValue = value;
            do
            {
                currentValue >>= 7;
                length++;
            } while (currentValue > 0);

            return length;
        }

        /// <summary>
        /// Write the MIDI value to the data
        /// </summary>
        /// <param name="data">output byte array</param>
        /// <param name="offset">the offset</param>
        /// <param name="value">the value to write</param>
        private static void WriteMidiValues(byte[] data, int offset, long value)
        {
            var shift = 63;
            while (shift > 0 && (value & (0x7f << shift)) == 0)
            {
                shift -= 7;
            }

            var currentOffset = offset;
            while (shift > 0)
            {
                data[currentOffset++] = (byte)(((value & (0x7f << shift)) >> shift) | 0x80);
                shift -= 7;
            }

            data[currentOffset] = (byte)(value & 0x7f);
        }
    }
}