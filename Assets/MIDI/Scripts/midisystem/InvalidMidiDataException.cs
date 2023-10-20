using System;

namespace jp.kshoji.midisystem
{
    /// <summary>
    /// <see cref="Exception" /> for invalid MIDI data.
    /// </summary>
    public class InvalidMidiDataException : Exception
    {
        /// <summary>
        /// Constructor with the message
        /// </summary>
        /// <param name="message">the message</param>
        public InvalidMidiDataException(string message) : base(message)
        {
        }
    }
}