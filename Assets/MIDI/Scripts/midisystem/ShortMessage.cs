using System;

namespace jp.kshoji.midisystem
{
    /// <summary>
    /// Represents MIDI Short Message
    /// </summary>
    public class ShortMessage : MidiMessage
    {
        public const int NoteOff = 0x80;
        public const int NoteOn = 0x90;
        public const int PolyPressure = 0xa0;
        public const int ControlChange = 0xb0;
        public const int ProgramChange = 0xc0;
        public const int ChannelPressure = 0xd0;
        public const int PitchBend = 0xe0;
        public const int StartOfExclusive = 0xf0;
        public const int MidiTimeCode = 0xf1;
        public const int SongPositionPointer = 0xf2;
        public const int SongSelect = 0xf3;
        public const int BusSelect = 0xf5;
        public const int TuneRequest = 0xf6;
        public const int EndOfExclusive = 0xf7;
        public const int TimingClock = 0xf8;
        public const int Start = 0xfa;
        public const int Continue = 0xfb;
        public const int Stop = 0xfc;
        public const int ActiveSensing = 0xfe;
        public const int SystemReset = 0xff;

        public const int MaskEvent = 0xf0;
        public const int MaskChannel = 0x0f;

        /// <summary>
        /// Default constructor, set up 'note on' message.
        /// </summary>
        public ShortMessage() : this(new byte[] { NoteOn, 0x40, 0x7f })
        {
        }

        /// <summary>
        /// Constructor with raw data.
        /// </summary>
        /// <param name="data">the raw data</param>
        private ShortMessage(byte[] data) : base(data)
        {
        }

        /// <summary>
        /// Constructor with the kind of message
        /// </summary>
        /// <param name="status">the status data</param>
        public ShortMessage(int status) : base(null)
        {
            SetMessage(status);
        }

        /// <summary>
        /// Constructor with the entire information of message
        /// </summary>
        /// <param name="status">the status data</param>
        /// <param name="data1">the first data</param>
        /// <param name="data2">the second data</param>
        public ShortMessage(int status, int data1, int data2) : base(null)
        {
            SetMessage(status, data1, data2);
        }

        /// <summary>
        /// Constructor with the entire information of message
        /// </summary>
        /// <param name="command">the command</param>
        /// <param name="channel">the channel</param>
        /// <param name="data1">the first data</param>
        /// <param name="data2">the second data</param>
        public ShortMessage(int command, int channel, int data1, int data2) : base(null)
        {
            SetMessage(command, channel, data1, data2);
        }

        /// <summary>
        /// Set the kind of message.
        /// </summary>
        /// <param name="status">the status data</param>
        /// <exception cref="InvalidMidiDataException"></exception>
        public void SetMessage(int status)
        {
            var dataLength = GetDataLength(status);
            if (dataLength != 0)
            {
                throw new InvalidMidiDataException($"Status byte: {status} requires {dataLength} data bytes length");
            }

            SetMessage(status, 0, 0);
        }

        /// <summary>
        /// Set the entire information of message.
        /// </summary>
        /// <param name="status">the status data</param>
        /// <param name="data1">the first data</param>
        /// <param name="data2">the second data</param>
        /// <exception cref="InvalidMidiDataException"></exception>
        public void SetMessage(int status, int data1, int data2)
        {
            var dataLength = GetDataLength(status);
            if (dataLength > 0)
            {
                if (data1 < 0 || data1 > 0x7f)
                {
                    throw new InvalidMidiDataException("data1 out of range: " + data1);
                }

                if (dataLength > 1)
                {
                    if (data2 < 0 || data2 > 0x7f)
                    {
                        throw new InvalidMidiDataException("data2 out of range: " + data2);
                    }
                }
            }

            if (Data == null || Data.Length != dataLength + 1)
            {
                Data = new byte[dataLength + 1];
            }

            Data[0] = (byte)(status & 0xff);
            if (Data.Length > 1)
            {
                Data[1] = (byte)(data1 & 0xff);
                if (Data.Length > 2)
                {
                    Data[2] = (byte)(data2 & 0xff);
                }
            }
        }

        /// <summary>
        /// Set the entire information of message.
        /// </summary>
        /// <param name="command">the command</param>
        /// <param name="channel">the channel</param>
        /// <param name="data1">the first data</param>
        /// <param name="data2">the second data</param>
        /// <exception cref="InvalidMidiDataException"></exception>
        public void SetMessage(int command, int channel, int data1, int data2)
        {
            if (command >= 0xf0 || command < 0x80)
            {
                throw new InvalidMidiDataException($"command out of range: 0x{command:X}");
            }

            if (channel > 0x0f)
            {
                throw new InvalidMidiDataException($"channel out of range: {channel}");
            }

            SetMessage((command & 0xf0) | (channel & 0x0f), data1, data2);
        }

        /// <summary>
        /// Get the channel of this message.
        /// </summary>
        /// <returns>the channel</returns>
        public int GetChannel()
        {
            return GetStatus() & 0x0f;
        }

        /// <summary>
        /// Get the kind of command for this message.
        /// </summary>
        /// <returns>the kind of command</returns>
        public int GetCommand()
        {
            return GetStatus() & 0xf0;
        }

        /// <summary>
        /// Get the first data for this message.
        /// </summary>
        /// <returns>the first data</returns>
        public int GetData1()
        {
            if (Data.Length > 1)
            {
                return Data[1] & 0xff;
            }

            return 0;
        }

        /// <summary>
        /// Get the second data for this message.
        /// </summary>
        /// <returns>the second data</returns>
        public int GetData2()
        {
            if (Data.Length > 2)
            {
                return Data[2] & 0xff;
            }

            return 0;
        }

        /// <inheritdoc cref="ICloneable"/>
        public override object Clone()
        {
            var result = new byte[Data.Length];
            Array.Copy(Data, 0, result, 0, result.Length);
            return new ShortMessage(result);
        }

        /// <summary>
        /// Get data length of MIDI message from MIDI event status
        /// </summary>
        /// <param name="status">MIDI event status</param>
        /// <returns>length of MIDI message</returns>
        /// <exception cref="InvalidMidiDataException"></exception>
        private static int GetDataLength(int status)
        {
            switch (status)
            {
                case TuneRequest:
                case EndOfExclusive:
                case TimingClock:
                case 0xf9:
                case Start:
                case Continue:
                case Stop:
                case 0xfd:
                case ActiveSensing:
                case SystemReset:
                    return 0;
                case MidiTimeCode:
                case SongSelect:
                    return 1;
                case SongPositionPointer:
                    return 2;
            }

            switch (status & MaskEvent)
            {
                case NoteOff:
                case NoteOn:
                case PolyPressure:
                case ControlChange:
                case PitchBend:
                    return 2;
                case ProgramChange:
                case ChannelPressure:
                    return 1;
                default:
                    throw new InvalidMidiDataException("Invalid status byte: " + status);
            }
        }
    }
}