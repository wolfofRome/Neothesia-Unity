namespace jp.kshoji.midisystem
{
    /// <summary>
    /// EventListener for MIDI Control Change messages.
    /// </summary>
    public interface IControllerEventListener
    {
        /// <summary>
        /// Called at <see cref="ShortMessage" /> event has fired
        /// </summary>
        /// <param name="message">the source message</param>
        void ControlChange(ShortMessage message);
    }
}