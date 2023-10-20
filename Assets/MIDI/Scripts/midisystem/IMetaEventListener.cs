namespace jp.kshoji.midisystem
{
	/// <summary>
	/// EventListener for MIDI Meta messages.
	/// </summary>
	public interface IMetaEventListener
    {
	    /// <summary>
	    /// Called at <see cref="MetaMessage" /> event has fired
	    /// </summary>
	    /// <param name="meta">the source event</param>
	    void Meta(MetaMessage meta);
    }
}