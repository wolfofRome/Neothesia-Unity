namespace jp.kshoji.midisystem
{
	/// <summary>
	/// Interface for {@link MidiMessage} receiver.
	/// </summary>
	public interface IReceiver
    {
	    /// <summary>
	    /// Called at <see cref="MidiMessage" /> receiving
	    /// </summary>
	    /// <param name="message">the received message</param>
	    /// <param name="timeStamp">-1 if the timeStamp information is not available</param>
	    void Send(MidiMessage message, long timeStamp);

	    /// <summary>
	    /// Close the <see cref="IReceiver" />
	    /// </summary>
	    void Close();
    }
}