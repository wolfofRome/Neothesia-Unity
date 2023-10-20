using System;

namespace jp.kshoji.midisystem
{
    /// <summary>
    /// <see cref="Exception" /> thrown when unable to use <see cref="IMidiDevice" />s.
    /// </summary>
    public class MidiUnavailableException : Exception
    {
        /// <summary>
        /// Constructor with a message
        /// </summary>
        /// <param name="message">the message</param>
        public MidiUnavailableException(string message) : base(message)
        {
        }
    }
}