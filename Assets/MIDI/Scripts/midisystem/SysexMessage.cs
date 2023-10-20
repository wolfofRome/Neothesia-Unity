using System;

namespace jp.kshoji.midisystem
{
    /// <summary>
    /// Represents MIDI SysEx Message
    /// </summary>
    public class SysexMessage : MidiMessage
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public SysexMessage() : this(new byte[]
            { ShortMessage.StartOfExclusive & 0xff, ShortMessage.EndOfExclusive & 0xff })
        {
        }

        /// <summary>
        /// Constructor with raw data and length.
        /// </summary>
        /// <param name="data">the SysEx data</param>
        private SysexMessage(byte[] data) : base(null)
        {
            SetMessage(data);
        }

        /// <summary>
        /// Constructor with raw data and length.
        /// </summary>
        /// <param name="status">must be <see cref="ShortMessage.StartOfExclusive"/> or <see cref="ShortMessage.EndOfExclusive"/></param>
        /// <param name="data">the SysEx data</param>
        public SysexMessage(int status, byte[] data) : base(null)
        {
            SetMessage(status, data);
        }

        /// <inheritdoc cref="MidiMessage.SetMessage"/>
        protected override void SetMessage(byte[] data)
        {
            if (data == null)
            {
                throw new InvalidMidiDataException("SysexMessage data is null");
            }

            var status = data[0] & 0xff;
            if (status != ShortMessage.StartOfExclusive && status != ShortMessage.EndOfExclusive)
            {
                throw new InvalidMidiDataException("Invalid status byte for SysexMessage: 0x" + status.ToString("X"));
            }

            base.SetMessage(data);
        }

        /// <summary>
        /// Set the entire information of message.
        /// </summary>
        /// <param name="status">must be <see cref="ShortMessage.StartOfExclusive"/> or <see cref="ShortMessage.EndOfExclusive"/></param>
        /// <param name="data">the SysEx data</param>
        /// <exception cref="InvalidMidiDataException"></exception>
        public void SetMessage(int status, byte[] data)
        {
            if (status != ShortMessage.StartOfExclusive && status != ShortMessage.EndOfExclusive)
            {
                throw new InvalidMidiDataException("Invalid status byte for SysexMessage: 0x" + status.ToString("X"));
            }

            // extend 1 byte
            Data = new byte[data.Length + 1];

            Data[0] = (byte)(status & 0xff);
            if (data.Length > 0)
            {
                Array.Copy(data, 0, Data, 1, data.Length);
            }
        }

        /// <summary>
        /// Get the SysEx data.
        /// </summary>
        /// <returns>SysEx data</returns>
        public byte[] GetData()
        {
            if (Data.Length == 0)
            {
                return new byte[] {};
            }

            var result = new byte[Data.Length - 1];
            Array.Copy(Data, 1, result, 0, result.Length);
            return result;
        }

        public override object Clone()
        {
            return new SysexMessage(GetData());
        }
    }
}