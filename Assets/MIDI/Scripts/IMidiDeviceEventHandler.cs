using UnityEngine.EventSystems;

namespace jp.kshoji.unity.midi
{
    /// <summary>
    /// MIDI Device Connection event handler
    /// </summary>
    public interface IMidiDeviceEventHandler : IEventSystemHandler
    {
        /// <summary>
        /// MIDI Input Device has been attached.
        /// </summary>
        /// <param name="deviceId"></param>
        void OnMidiInputDeviceAttached(string deviceId);

        /// <summary>
        /// MIDI Output Device has been attached.
        /// </summary>
        /// <param name="deviceId"></param>
        void OnMidiOutputDeviceAttached(string deviceId);

        /// <summary>
        /// MIDI Input Device has been detached.
        /// </summary>
        /// <param name="deviceId"></param>
        void OnMidiInputDeviceDetached(string deviceId);

        /// <summary>
        /// MIDI Output Device has been detached.
        /// </summary>
        /// <param name="deviceId"></param>
        void OnMidiOutputDeviceDetached(string deviceId);
    }
}