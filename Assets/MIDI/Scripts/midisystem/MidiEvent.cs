namespace jp.kshoji.midisystem
{
    /// <summary>
    /// Represents MIDI Event
    /// </summary>
    public class MidiEvent
    {
        private readonly MidiMessage message;
        private long tick;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">the message</param>
        /// <param name="tick">the timeStamp in tick. -1 if timeStamp not supported.</param>
        public MidiEvent(MidiMessage message, long tick)
        {
            this.message = message;
            this.tick = tick;
        }

        /// <summary>
        /// Get the <see cref="MidiMessage" /> of this <see cref="MidiEvent" />
        /// </summary>
        /// <returns>the <see cref="MidiMessage" /> of this <see cref="MidiEvent" /></returns>
        public MidiMessage GetMessage()
        {
            return message;
        }

        /// <summary>
        /// Get the timeStamp in tick
        /// </summary>
        /// <returns>-1 if timeStamp not supported.</returns>
        public long GetTick()
        {
            return tick;
        }

        /// <summary>
        /// Set the timeStamp in tick
        /// </summary>
        /// <param name="tick">tick timeStamp</param>
        public void SetTick(long tick)
        {
            this.tick = tick;
        }
    }
}