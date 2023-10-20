using System.IO;

namespace jp.kshoji.midisystem
{
    /// <summary>
    /// Interface for MIDI Sequencer
    /// </summary>
    public interface ISequencer : IMidiDevice
    {
        /// <summary>
        /// Get the available <see cref="SyncMode"/> for master.
        /// </summary>
        /// <returns>the available <see cref="SyncMode"/> for master.</returns>
        SyncMode[] GetMasterSyncModes();

        /// <summary>
        /// Get the <see cref="SyncMode"/> for master.
        /// </summary>
        /// <returns>the <see cref="SyncMode"/> for master.</returns>
        SyncMode GetMasterSyncMode();

        /// <summary>
        /// Set the <see cref="SyncMode"/> for master.
        /// </summary>
        /// <param name="sync">the <see cref="SyncMode"/> for master.</param>
        void SetMasterSyncMode(SyncMode sync);

        /// <summary>
        /// Get the available <see cref="SyncMode"/> for slave.
        /// </summary>
        /// <returns>the available <see cref="SyncMode"/> for slave.</returns>
        SyncMode[] GetSlaveSyncModes();

        /// <summary>
        /// Get the <see cref="SyncMode"/> for slave.
        /// </summary>
        /// <returns>the <see cref="SyncMode"/> for slave.</returns>
        SyncMode GetSlaveSyncMode();

        /// <summary>
        /// Set the <see cref="SyncMode"/> for slave.
        /// </summary>
        /// <param name="sync">sync the <see cref="SyncMode"/> for slave.</param>
        void SetSlaveSyncMode(SyncMode sync);

        /// <summary>
        /// Get the <see cref="Sequence"/>
        /// </summary>
        /// <returns>the <see cref="Sequence"/></returns>
        Sequence GetSequence();

        /// <summary>
        /// Load a <see cref="Sequence"/> from stream.
        /// </summary>
        /// <param name="stream">sequence source</param>
        void SetSequence(Stream stream);

        /// <summary>
        /// Set the <see cref="Sequence"/> for the <see cref="ISequencer"/>
        /// </summary>
        /// <param name="sourceSequence">the <see cref="Sequence"/></param>
        void SetSequence(Sequence sourceSequence);

        /// <summary>
        /// Add EventListener for <see cref="ShortMessage.ControlChange"/>
        /// </summary>
        /// <param name="listener">event listener</param>
        /// <param name="controllers">controller codes</param>
        /// <returns>registered controllers for the specified listener</returns>
        int[] AddControllerEventListener(IControllerEventListener listener, int[] controllers);

        /// <summary>
        /// Remove EventListener for <see cref="ShortMessage.ControlChange"/>
        /// </summary>
        /// <param name="listener">event listener</param>
        /// <param name="controllers">controller codes</param>
        /// <returns>registered controllers for the specified listener</returns>
        int[] RemoveControllerEventListener(IControllerEventListener listener, int[] controllers);

        /// <summary>
        /// Add EventListener for <see cref="MetaMessage"/>
        /// </summary>
        /// <param name="listener">event listener</param>
        /// <returns>true if registered successfully</returns>
        bool AddMetaEventListener(IMetaEventListener listener);

        /// <summary>
        /// Remove EventListener for <see cref="MetaMessage"/>
        /// </summary>
        /// <param name="listener">event listener</param>
        void RemoveMetaEventListener(IMetaEventListener listener);

        /// <summary>
        /// Get if the <see cref="ISequencer"/> is recording.
        /// </summary>
        /// <returns>true if the <see cref="ISequencer"/> is recording</returns>
        bool GetIsRecording();

        /// <summary>
        /// Get if the <see cref="ISequencer"/> is playing OR recording.
        /// </summary>
        /// <returns>true if the <see cref="ISequencer"/> is playing OR recording</returns>
        bool GetIsRunning();

        /// <summary>
        /// Set the <see cref="Track"/> to disable recording
        /// </summary>
        /// <param name="track">track the <see cref="Track"/> to disable recording</param>
        void RecordDisable(Track track);

        /// <summary>
        /// Set the <see cref="Track"/> to enable recording on the specified channel.
        /// </summary>
        /// <param name="track">the <see cref="Track"/></param>
        /// <param name="channel">the channel, 0-15</param>
        void SetRecordEnable(Track track, int channel);

        /// <summary>
        /// Get the count of loop.
        /// </summary>
        /// <returns>the count of loop
        /// <ul>
        ///     <li><see cref="SequencerImpl.LoopContinuously"/>: play loops eternally</li>
        ///     <li>0: play once(no loop)</li>
        ///     <li>1: play twice(loop once)</li>
        /// </ul>
        /// </returns>
        int GetLoopCount();

        /// <summary>
        /// Set count of loop.
        /// </summary>
        /// <param name="count">
        /// <ul>
        ///     <li><see cref="SequencerImpl.LoopContinuously"/>: play loops eternally</li>
        ///     <li>0: play once(no loop)</li>
        ///     <li>1: play twice(loop once)</li>
        /// </ul>
        /// </param>
        void SetLoopCount(int count);

        /// <summary>
        /// Get start point(ticks) of loop.
        /// </summary>
        /// <returns>ticks</returns>
        long GetLoopStartPoint();

        /// <summary>
        /// Set start point(ticks) of loop.
        /// </summary>
        /// <param name="tick">0: start of <see cref="Sequence"/></param>
        void SetLoopStartPoint(long tick);

        /// <summary>
        /// Get the end point(ticks) of loop.
        /// </summary>
        /// <returns>the end point(ticks) of loop</returns>
        long GetLoopEndPoint();

        /// <summary>
        /// Set end point(ticks) of loop.
        /// </summary>
        /// <param name="tick">-1: end of <see cref="Sequence"/></param>
        void SetLoopEndPoint(long tick);

        /// <summary>
        /// Get the tempo factor.
        /// </summary>
        /// <returns>the tempo factor</returns>
        float GetTempoFactor();

        /// <summary>
        /// Set the tempo factor. This method don't change <see cref="Sequence"/>'s tempo.
        /// </summary>
        /// <param name="factor">
        /// <ul>
        ///     <li>1.0f : the normal tempo</li>
        ///     <li>0.5f : half slow tempo</li>
        ///     <li>2.0f : 2x fast tempo</li>
        /// </ul>
        /// </param>
        void SetTempoFactor(float factor);

        /// <summary>
        /// Get the tempo in the Beats per minute.
        /// </summary>
        /// <returns>the tempo in the Beats per minute.</returns>
        float GetTempoInBpm();

        /// <summary>
        /// Set the tempo in the Beats per minute.
        /// </summary>
        /// <param name="bpm">the tempo in the Beats per minute</param>
        void SetTempoInBpm(float bpm);

        /// <summary>
        /// Get the tempos in the microseconds per quarter note.
        /// </summary>
        /// <returns>the tempos in the microseconds per quarter note</returns>
        float GetTempoInMpq();

        /// <summary>
        /// Set the tempos in the microseconds per quarter note.
        /// </summary>
        /// <param name="mpq">the tempos in the microseconds per quarter note</param>
        void SetTempoInMpq(float mpq);

        /// <summary>
        /// Get the <see cref="Sequence"/> length in ticks.
        /// </summary>
        /// <returns>the <see cref="Sequence"/> length in ticks</returns>
        long GetTickLength();

        /// <summary>
        /// Get the <see cref="Sequence"/> length in microseconds.
        /// </summary>
        /// <returns>the <see cref="Sequence"/> length in microseconds</returns>
        long GetMicrosecondLength();

        /// <summary>
        /// Get the current tick position.
        /// </summary>
        /// <returns>the current tick position</returns>
        long GetTickPosition();

        /// <summary>
        /// Set the current tick position.
        /// </summary>
        /// <param name="tick">tick the current tick position</param>
        void SetTickPosition(long tick);

        /// <summary>
        /// Set the current microsecond position.
        /// </summary>
        /// <param name="microseconds">the current microsecond position</param>
        void SetMicrosecondPosition(long microseconds);

        /// <summary>
        /// Get if the track is mute on the playback.
        /// </summary>
        /// <param name="track">the track number</param>
        /// <returns>true if the track is mute on the playback</returns>
        bool GetTrackMute(int track);

        /// <summary>
        /// Set the track to mute on the playback.
        /// </summary>
        /// <param name="track">the track number</param>
        /// <param name="mute">true to set mute the track</param>
        void SetTrackMute(int track, bool mute);

        /// <summary>
        /// Get if the track is solo on the playback.
        /// </summary>
        /// <param name="track">the track number</param>
        /// <returns>true if the track is solo on the playback.</returns>
        bool GetTrackSolo(int track);

        /// <summary>
        /// Set track to solo on the playback.
        /// </summary>
        /// <param name="track">the track number</param>
        /// <param name="solo">true to set solo the track</param>
        void SetTrackSolo(int track, bool solo);

        /// <summary>
        /// Start playing (starting at current sequencer position)
        /// </summary>
        void Start();

        /// <summary>
        /// Start recording (starting at current sequencer position)
        /// Current <see cref="Sequence"/>'s events are sent to the all <see cref="ITransmitter"/>s.
        /// Received events are also sent to the all <see cref="ITransmitter"/>s.
        /// </summary>
        void StartRecording();

        /// <summary>
        /// Stop playing AND recording.
        /// </summary>
        void Stop();

        /// <summary>
        /// Stop recording. Playing continues.
        /// </summary>
        void StopRecording();
    }

    /// <summary>
    /// <see cref="ISequencer"/>'s Synchronization mode
    /// </summary>
    public class SyncMode
    {
        public static readonly SyncMode InternalClock = new SyncMode("Internal Clock");
        public static readonly SyncMode NoSync = new SyncMode("No Sync");

        private readonly string name;

        protected SyncMode(string name)
        {
            this.name = name;
        }

        /// <inheritdoc cref="object.Equals(object)"/>
        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj == null)
            {
                return false;
            }

            if (GetType() != obj.GetType())
            {
                return false;
            }

            var other = (SyncMode)obj;
            if (!name.Equals(other.name))
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc cref="object.GetHashCode"/>
        public override int GetHashCode()
        {
            var PRIME = 31;
            var result = base.GetHashCode();
            result = PRIME * result + name.GetHashCode();
            return result;
        }

        /// <inheritdoc cref="object.ToString"/>
        public override string ToString()
        {
            return name;
        }
    }
}