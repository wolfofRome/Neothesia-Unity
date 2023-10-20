namespace jp.kshoji.midisystem
{
    /// <summary>
    /// Interface for MIDI Transmitter.
    /// </summary>
    public interface ITransmitter
    {
        /// <summary>
        /// Set the <see cref="IReceiver"/> for this <see cref="ITransmitter"/>
        /// </summary>
        /// <param name="theReceiver">the <see cref="IReceiver"/></param>
        void SetReceiver(IReceiver theReceiver);

        /// <summary>
        /// Get the <see cref="IReceiver"/> for this <see cref="ITransmitter"/>
        /// </summary>
        /// <returns>the <see cref="IReceiver"/></returns>
        IReceiver GetReceiver();

        /// <summary>
        /// Close this <see cref="ITransmitter"/>
        /// </summary>
        void Close();
    }
}