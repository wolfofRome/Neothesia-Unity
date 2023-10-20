using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_5_3_OR_NEWER
using System.Collections;
using UnityEngine;
#else
using System.Threading;
#endif

namespace jp.kshoji.midisystem
{
    /// <summary>
    /// <see cref="ISequencer"/> implementation
    /// </summary>
    public class SequencerImpl : ISequencer
    {
        public const int LoopContinuously = -1;
        private static readonly SyncMode[] MasterSyncModes = { SyncMode.InternalClock };
        private static readonly SyncMode[] SlaveSyncModes = { SyncMode.NoSync };

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly HashSet<IControllerEventListener>[] controllerEventListenerMap =
            new HashSet<IControllerEventListener>[128];

        private readonly HashSet<IMetaEventListener> metaEventListeners = new HashSet<IMetaEventListener>();
        private readonly List<IReceiver> receivers = new List<IReceiver>();
        private readonly Dictionary<Track, HashSet<int>> recordEnable = new Dictionary<Track, HashSet<int>>();
        private readonly Dictionary<int, bool> trackMute = new Dictionary<int, bool>();
        private readonly Dictionary<int, bool> trackSolo = new Dictionary<int, bool>();

        private readonly List<ITransmitter> transmitters = new List<ITransmitter>();

        private volatile bool isOpen;
        private int loopCount;
        private long loopEndPoint = -1;
        private long loopStartPoint;
        private SyncMode masterSyncMode = SyncMode.InternalClock;
        private bool needRefreshPlayingTrack;

        // playing
        private Track playingTrack;

        // recording
        private long recordingStartedTime;
        private Track recordingTrack;
        private long recordStartedTick;
        private long runningStoppedTime;
        private Sequence sequence;

        private SequencerThread sequencerThread;
        private SyncMode slaveSyncMode = SyncMode.NoSync;
        private volatile float tempoFactor = 1.0f;
        private float tempoInBpm = 120.0f;
#if !UNITY_5_3_OR_NEWER
        private Thread thread;
#endif
        private long tickPositionSetTime;
        private readonly IReceiver midiEventRecordingReceiver;
        private readonly Action onOpened;

        private static HashSet<SequencerImpl> sequencers = new HashSet<SequencerImpl>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="onOpened">Action called on opened the sequencer</param>
        public SequencerImpl(Action onOpened)
        {
            midiEventRecordingReceiver = new MidiEventRecordingReceiver(this);
            this.onOpened = onOpened;
        }

        /// <inheritdoc cref="IMidiDevice.GetDeviceInfo"/>
        public Info GetDeviceInfo()
        {
            return new Info("sequencer", "vendor", "description", "version");
        }

        public void UpdateDeviceConnections()
        {
            lock (receivers)
            {
                receivers.Clear();
                receivers.AddRange(MidiSystem.GetReceivers());
            }

            lock (transmitters)
            {
                transmitters.Clear();
                transmitters.AddRange(MidiSystem.GetTransmitters());

                foreach (var transmitter in transmitters)
                {
                    // receive from all transmitters
                    transmitter.SetReceiver(midiEventRecordingReceiver);
                }
            }
        }

#if UNITY_5_3_OR_NEWER
        public IEnumerator OpenCoroutine()
        {
            // open devices
            UpdateDeviceConnections();

            sequencerThread = new SequencerThread();
            return sequencerThread.StartSequencerThread(this, () =>
            {
                isOpen = true;
                onOpened();
            });
        }
#endif

        public void Open()
        {
#if !UNITY_5_3_OR_NEWER
            if (thread == null)
            {
                sequencerThread = new SequencerThread();
                lock (sequencers)
                {
                    sequencers.Add(this);
                }
                thread = new Thread(() => sequencerThread.StartSequencerThread(this, () =>
                {
                    isOpen = true;
                    onOpened();
                }));
                thread.Name = "MidiSequencer_" + thread.ManagedThreadId;
                try
                {
                    thread.Start();
                }
                catch (ThreadStateException)
                {
                    // maybe already started
                }
            }
            else
            {
                onOpened();
            }

            lock (thread)
            {
                Monitor.PulseAll(thread);
            }
#endif
        }

        public static void CloseAllSequencers()
        {
            lock (sequencers)
            {
                foreach (var sequencer in sequencers)
                {
                    sequencer.Close();
                }
            }
        }
        
        public void Close()
        {
            lock (receivers)
            {
                receivers.Clear();
            }

            lock (transmitters)
            {
                transmitters.Clear();
            }

            if (sequencerThread != null)
            {
                sequencerThread.StopPlaying();
                isOpen = false;
#if !UNITY_5_3_OR_NEWER
                lock (thread)
                {
                    Monitor.PulseAll(thread);
                }
#endif
                sequencerThread = null;
#if !UNITY_5_3_OR_NEWER
                thread = null;
#endif
            }

            lock (metaEventListeners)
            {
                metaEventListeners.Clear();
            }

            lock (controllerEventListenerMap)
            {
                foreach (var controllerEventListeners in controllerEventListenerMap)
                {
                    controllerEventListeners?.Clear();
                }
            }
        }

        public bool GetIsOpen()
        {
            return isOpen;
        }

        public int GetMaxReceivers()
        {
            lock (receivers)
            {
                return receivers.Count;
            }
        }

        public int GetMaxTransmitters()
        {
            lock (transmitters)
            {
                return transmitters.Count;
            }
        }

        public IReceiver GetReceiver()
        {
            lock (receivers)
            {
                if (receivers.Count == 0)
                {
                    throw new MidiUnavailableException("Receiver not found");
                }

                return receivers[0];
            }
        }

        public List<IReceiver> GetReceivers()
        {
            lock (receivers)
            {
                return receivers.ToList();
            }
        }

        public ITransmitter GetTransmitter()
        {
            lock (transmitters)
            {
                if (transmitters.Count == 0)
                {
                    throw new MidiUnavailableException("Transmitter not found");
                }

                return transmitters[0];
            }
        }

        public List<ITransmitter> GetTransmitters()
        {
            lock (transmitters)
            {
                return transmitters.ToList();
            }
        }

        public int[] AddControllerEventListener(IControllerEventListener listener, int[] controllers)
        {
            lock (controllerEventListenerMap)
            {
                foreach (var controllerId in controllers)
                {
                    var listeners = controllerEventListenerMap[controllerId];
                    if (listeners == null)
                    {
                        listeners = new HashSet<IControllerEventListener>();
                    }

                    listeners.Add(listener);
                    controllerEventListenerMap[controllerId] = listeners;
                }

                return controllers;
            }
        }

        public int[] RemoveControllerEventListener(IControllerEventListener listener, int[] controllers)
        {
            lock (controllerEventListenerMap)
            {
                var resultList = new List<int>();
                foreach (var controllerId in controllers)
                {
                    var listeners = controllerEventListenerMap[controllerId];
                    if (listeners != null && listeners.Contains(listener))
                    {
                        listeners.Remove(listener);
                    }
                    else
                    {
                        // remaining controller id
                        resultList.Add(controllerId);
                    }

                    controllerEventListenerMap[controllerId] = listeners;
                }

                // returns currently registered controller ids for the argument specified listener
                var resultPrimitiveArray = new int[resultList.Count];
                for (var i = 0; i < resultPrimitiveArray.Length; i++)
                {
                    resultPrimitiveArray[i] = resultList[i];
                }

                return resultPrimitiveArray;
            }
        }

        public bool AddMetaEventListener(IMetaEventListener listener)
        {
            // return true if registered successfully
            lock (metaEventListeners)
            {
                return metaEventListeners.Add(listener);
            }
        }

        public void RemoveMetaEventListener(IMetaEventListener listener)
        {
            lock (metaEventListeners)
            {
                metaEventListeners.Remove(listener);
            }
        }

        public int GetLoopCount()
        {
            return loopCount;
        }

        public void SetLoopCount(int count)
        {
            if (count != LoopContinuously && count < 0)
            {
                throw new ArgumentException($"Invalid loop count value: {count}");
            }

            loopCount = count;
        }

        public long GetLoopStartPoint()
        {
            return loopStartPoint;
        }

        public void SetLoopStartPoint(long tick)
        {
            if (tick > GetTickLength() || loopEndPoint != -1 && tick > loopEndPoint || tick < 0)
            {
                throw new ArgumentException($"Invalid loop start point value: {tick}");
            }

            loopStartPoint = tick;
        }

        public long GetLoopEndPoint()
        {
            return loopEndPoint;
        }

        public void SetLoopEndPoint(long tick)
        {
            if (tick > GetTickLength() || tick != -1 && loopStartPoint > tick || tick < -1)
            {
                throw new ArgumentException($"Invalid loop end point value: {tick}");
            }

            loopEndPoint = tick;
        }


        public SyncMode GetMasterSyncMode()
        {
            return masterSyncMode;
        }

        public void SetMasterSyncMode(SyncMode sync)
        {
            foreach (var availableMode in GetMasterSyncModes())
            {
                if (Equals(availableMode, sync))
                {
                    masterSyncMode = sync;
                }
            }
        }

        public SyncMode[] GetMasterSyncModes()
        {
            return MasterSyncModes;
        }

        public long GetMicrosecondPosition()
        {
            return (long)(GetTickPosition() / GetTicksPerMicrosecond());
        }

        public void SetMicrosecondPosition(long microseconds)
        {
            SetTickPosition((long)(GetTicksPerMicrosecond() * microseconds));
        }

        public long GetMicrosecondLength()
        {
            return sequence.GetMicrosecondLength();
        }

        public Sequence GetSequence()
        {
            return sequence;
        }

        public void SetSequence(Stream stream)
        {
            SetSequence(MidiSystem.ReadSequence(stream));
        }

        public void SetSequence(Sequence sourceSequence)
        {
            sequence = sourceSequence;

            if (sourceSequence != null)
            {
                needRefreshPlayingTrack = true;
                SetTickPosition(0);
            }
        }

        public SyncMode GetSlaveSyncMode()
        {
            return slaveSyncMode;
        }

        public void SetSlaveSyncMode(SyncMode sync)
        {
            foreach (var availableMode in GetSlaveSyncModes())
            {
                if (Equals(availableMode, sync))
                {
                    slaveSyncMode = sync;
                }
            }
        }

        public SyncMode[] GetSlaveSyncModes()
        {
            return SlaveSyncModes;
        }

        public float GetTempoFactor()
        {
            return tempoFactor;
        }

        public void SetTempoFactor(float factor)
        {
            if (factor <= 0.0f)
            {
                throw new ArgumentException("The tempo factor must be larger than 0f.");
            }

            tempoFactor = factor;
        }

        public float GetTempoInBpm()
        {
            return tempoInBpm;
        }

        public void SetTempoInBpm(float bpm)
        {
            tempoInBpm = bpm;
        }

        public float GetTempoInMpq()
        {
            return 60000000.0f / tempoInBpm;
        }

        public void SetTempoInMpq(float mpq)
        {
            tempoInBpm = 60000000.0f / mpq;
        }

        public long GetTickLength()
        {
            if (sequence == null)
            {
                return 0;
            }

            return sequence.GetTickLength();
        }

        public long GetTickPosition()
        {
            if (sequencerThread == null)
            {
                return 0;
            }

            return sequencerThread.GetTickPosition();
        }

        public void SetTickPosition(long tick)
        {
            if (sequencerThread != null)
            {
                sequencerThread.SetTickPosition(tick);
            }
        }

        public bool GetTrackMute(int track)
        {
            return trackMute.ContainsKey(track) && trackMute[track];
        }

        public void SetTrackMute(int track, bool mute)
        {
            trackMute[track] = mute;
        }

        public bool GetTrackSolo(int track)
        {
            return trackSolo.ContainsKey(track) && trackSolo[track];
        }

        public void SetTrackSolo(int track, bool solo)
        {
            trackSolo[track] = solo;
        }

        public void RecordDisable(Track track)
        {
            if (track == null)
            {
                // disable all track
                recordEnable.Clear();
            }
            else
            {
                // disable specified track
                var trackRecordEnable = recordEnable[track];
                if (trackRecordEnable != null)
                {
                    recordEnable.Remove(track);
                }
            }
        }

        public void SetRecordEnable(Track track, int channel)
        {
            var trackRecordEnable = recordEnable.ContainsKey(track) ? recordEnable[track] : new HashSet<int>();
 
            if (channel == -1)
            {
                // record to the all channels
                for (var i = 0; i < 16; i++)
                {
                    trackRecordEnable.Add(i);
                }
            }
            else if (channel >= 0 && channel < 16)
            {
                trackRecordEnable.Add(channel);
            }

            recordEnable[track] = trackRecordEnable;
        }

        public void StartRecording()
        {
            if (sequencerThread != null)
            {
                sequencerThread.StartRecording();
                sequencerThread.StartPlaying();
            }
        }

        public bool GetIsRecording()
        {
            if (sequencerThread == null)
            {
                return false;
            }

            return sequencerThread.IsRecording;
        }

        public void StopRecording()
        {
            // stop recording
            if (sequencerThread != null)
            {
                sequencerThread.StopRecording();
            }
        }

        public void Start()
        {
            // start playing
            if (sequencerThread != null)
            {
                sequencerThread.StartPlaying();
            }
        }

        public bool GetIsRunning()
        {
            if (sequencerThread == null)
            {
                return false;
            }

            return sequencerThread.IsRunning;
        }

        public void Stop()
        {
            // stop playing AND recording
            if (sequencerThread != null)
            {
                sequencerThread.StopRecording();
                sequencerThread.StopPlaying();
            }
        }

        private static long CurrentTimeMillis()
        {
            return (long)(DateTime.UtcNow - Epoch).TotalMilliseconds;
        }

        /// <summary>
        /// convert parameter from microseconds to tick
        /// </summary>
        /// <returns>ticks per microsecond, NaN: sequence is null</returns>
        private float GetTicksPerMicrosecond()
        {
            if (sequence == null)
            {
                return float.NaN;
            }

            float ticksPerMicrosecond;
            if (Sequence.DivisionTypeEquals(sequence.GetDivisionType(), Sequence.Ppq))
            {
                // PPQ : tempoInBPM / 60f * resolution / 1000000 ticks per microsecond
                ticksPerMicrosecond = tempoInBpm / 60.0f * sequence.GetResolution() / 1000000.0f;
            }
            else
            {
                // SMPTE : divisionType * resolution / 1000000 ticks per microsecond
                ticksPerMicrosecond = sequence.GetDivisionType() * sequence.GetResolution() / 1000000.0f;
            }

            return ticksPerMicrosecond;
        }

        private class MidiEventRecordingReceiver : IReceiver
        {
            private readonly SequencerImpl sequencer;

            internal MidiEventRecordingReceiver(SequencerImpl sequencer)
            {
                this.sequencer = sequencer;
            }

            public void Send(MidiMessage message, long timeStamp)
            {
                if (sequencer.sequencerThread.IsRecording)
                {
                    sequencer.recordingTrack.Add(
                        new MidiEvent(
                            message,
                            (long)(sequencer.recordStartedTick +
                                   (CurrentTimeMillis() - sequencer.recordingStartedTime) * 1000.0f *
                                   sequencer.GetTicksPerMicrosecond())));
                }

                sequencer.sequencerThread.FireEventListeners(message);
            }

            public void Close()
            {
                // do nothing
            }
        }

        private class SequencerThread
        {
            internal volatile bool IsRecording;

            internal volatile bool IsRunning;

            private SequencerImpl sequencer;
            private long tickPosition;

            /// <summary>
            /// Thread / Coroutine for this Sequencer
            /// </summary>
            /// <param name="sourceSequencer">the <see cref="SequencerImpl"/></param>
            /// <param name="onOpened">Called on sequencer opened</param>
            public 
#if UNITY_5_3_OR_NEWER
                IEnumerator
#else
                void
#endif
                StartSequencerThread(SequencerImpl sourceSequencer, Action onOpened)
            {
                sequencer = sourceSequencer;
                RefreshPlayingTrack();

                onOpened();

                // playing
                while (sequencer.isOpen)
                {
                    // wait for being notified
                    while (!IsRunning && sequencer.isOpen)
                    {
#if UNITY_5_3_OR_NEWER
                        yield return null;
#else
                        lock (sequencer.thread)
                        {
                            Monitor.Wait(sequencer.thread);
                        }
#endif
                    }

                    if (sequencer.playingTrack == null)
                    {
                        if (sequencer.needRefreshPlayingTrack)
                        {
                            RefreshPlayingTrack();
                        }

                        if (sequencer.playingTrack == null)
                        {
                            continue;
                        }
                    }

                    // process looping
                    var loopCount = sequencer.GetLoopCount() == LoopContinuously ? 1 : sequencer.GetLoopCount() + 1;
                    for (var loop = 0; loop < loopCount; loop += sequencer.GetLoopCount() == LoopContinuously ? 0 : 1)
                    {
                        if (sequencer.needRefreshPlayingTrack)
                        {
                            RefreshPlayingTrack();
                        }

                        for (var i = 0; i < sequencer.playingTrack.Size(); i++)
                        {
                            var midiEvent = sequencer.playingTrack.Get(i);
                            var midiMessage = midiEvent.GetMessage();

                            if (sequencer.needRefreshPlayingTrack)
                            {
                                // skip to lastTick
                                if (midiEvent.GetTick() < tickPosition)
                                {
                                    if (midiMessage is MetaMessage metaMessage)
                                    {
                                        // process tempo change message
                                        if (ProcessTempoChange(metaMessage) == false)
                                        {
                                            // not tempo message, process the event
                                            lock (sequencer.receivers)
                                            {
                                                foreach (var receiver in sequencer.receivers)
                                                {
                                                    receiver.Send(metaMessage, 0);
                                                }
                                            }
                                        }
                                    }
                                    else if (midiMessage is SysexMessage)
                                    {
                                        // process system messages
                                        lock (sequencer.receivers)
                                        {
                                            foreach (var receiver in sequencer.receivers)
                                            {
                                                receiver.Send(midiMessage, 0);
                                            }
                                        }
                                    }
                                    else if (midiMessage is ShortMessage shortMessage)
                                    {
                                        // process control change / program change messages
                                        switch (shortMessage.GetCommand())
                                        {
                                            case ShortMessage.NoteOn:
                                            case ShortMessage.NoteOff:
                                                break;
                                            default:
                                                lock (sequencer.receivers)
                                                {
                                                    foreach (var receiver in sequencer.receivers)
                                                    {
                                                        receiver.Send(shortMessage, 0);
                                                    }
                                                }
                                                break;
                                        }
                                    }

                                    continue;
                                }

                                // refresh playingTrack completed
                                sequencer.needRefreshPlayingTrack = false;
                            }

                            if (midiEvent.GetTick() < sequencer.GetLoopStartPoint() ||
                                sequencer.GetLoopEndPoint() != -1 && midiEvent.GetTick() > sequencer.GetLoopEndPoint())
                            {
                                // outer loop
                                tickPosition = midiEvent.GetTick();
                                sequencer.tickPositionSetTime = CurrentTimeMillis();
                                continue;
                            }

                            if (midiEvent.GetTick() != tickPosition)
                            {
#if UNITY_5_3_OR_NEWER
                                var sleepLength = 1.0f / sequencer.GetTicksPerMicrosecond() *
                                    (midiEvent.GetTick() - tickPosition) / 1000f / sequencer.GetTempoFactor();
                                sleepLength -= CurrentTimeMillis() - sequencer.tickPositionSetTime;
                                if (sleepLength > Time.smoothDeltaTime * 1000f)
                                {
                                    yield return new WaitForSecondsRealtime(sleepLength / 1000f);
                                }
#else
                                var sleepLength = (int)(1.0f / sequencer.GetTicksPerMicrosecond() *
                                    (midiEvent.GetTick() - tickPosition) / 1000f / sequencer.GetTempoFactor());
                                if (sleepLength > 0)
                                {
                                    lock (sequencer.thread)
                                    {
                                        Monitor.Wait(sequencer.thread, sleepLength);
                                    }
                                }
#endif

                                tickPosition = midiEvent.GetTick();
                                sequencer.tickPositionSetTime = CurrentTimeMillis();
                            }

                            // pause / resume
#if UNITY_5_3_OR_NEWER
                            while (!IsRunning && sequencer.isOpen)
#else
                            while (!IsRunning && sequencer.isOpen && sequencer.thread != null)
#endif
                            {
                                var pausing = !IsRunning;
                                // wait for being notified
                                while (!IsRunning && sequencer.isOpen)
                                {
#if UNITY_5_3_OR_NEWER
                                    yield return null;
#else
                                    lock (sequencer.thread)
                                    {
                                        Monitor.Wait(sequencer.thread);
                                    }
#endif
                                }
                                if (!pausing)
                                {
                                    continue;
                                }

                                if (sequencer.needRefreshPlayingTrack)
                                {
                                    RefreshPlayingTrack();
                                }
                                for (var index = 0; index < sequencer.playingTrack.Size(); index++)
                                {
                                    if (sequencer.playingTrack.Get(index).GetTick() >= tickPosition)
                                    {
                                        i = index;
                                        break;
                                    }
                                }
                            }

                            if (!sequencer.isOpen)
                            {
                                break;
                            }

                            if (sequencer.needRefreshPlayingTrack)
                            {
                                continue;
                            }

                            // process tempo change message
                            if (midiMessage is MetaMessage message)
                            {
                                if (ProcessTempoChange(message))
                                {
                                    FireEventListeners(message);

                                    // do not send tempo message to the receivers.
                                    continue;
                                }
                            }

                            // send MIDI events
                            lock (sequencer.receivers)
                            {
                                foreach (var receiver in sequencer.receivers)
                                {
                                    receiver.Send(midiMessage, 0);
                                }
                            }

                            FireEventListeners(midiMessage);
                        }
                    }

                    // loop end
                    IsRunning = false;
                    sequencer.runningStoppedTime = CurrentTimeMillis();
                }
            }

            /// <summary>
            /// Get current tick position
            /// </summary>
            /// <returns>current tick position</returns>
            internal long GetTickPosition()
            {
                if (IsRunning)
                {
                    // running
                    return (long)(tickPosition + (CurrentTimeMillis() - sequencer.tickPositionSetTime) * 1000.0f *
                        sequencer.GetTicksPerMicrosecond());
                }

                // stopping
                return (long)(tickPosition + (sequencer.runningStoppedTime - sequencer.tickPositionSetTime) * 1000.0f *
                    sequencer.GetTicksPerMicrosecond());
            }

            /// <summary>
            /// Set current tick position
            /// </summary>
            /// <param name="tick">current tick position</param>
            internal void SetTickPosition(long tick)
            {
                tickPosition = tick;
                if (IsRunning)
                {
                    sequencer.tickPositionSetTime = CurrentTimeMillis();
                }
            }

            /// <summary>
            /// Start recording
            /// </summary>
            internal void StartRecording()
            {
                if (IsRecording)
                {
                    // already recording
                    return;
                }

                sequencer.recordingTrack = sequencer.sequence.CreateTrack();
                sequencer.SetRecordEnable(sequencer.recordingTrack, -1);
                sequencer.recordingStartedTime = CurrentTimeMillis();
                sequencer.recordStartedTick = GetTickPosition();
                IsRecording = true;
            }

            /// <summary>
            /// Stop recording
            /// </summary>
            internal void StopRecording()
            {
                if (IsRecording == false)
                {
                    // already stopped
                    return;
                }

                var recordEndedTime = CurrentTimeMillis();
                IsRecording = false;

                var eventToRemoval = new HashSet<MidiEvent>();
                foreach (var track in sequencer.sequence.GetTracks())
                {
                    if (track == sequencer.recordingTrack)
                    {
                        continue;
                    }

                    HashSet<int> recordEnableChannels = null;
                    if (sequencer.recordEnable.ContainsKey(track))
                    {
                        recordEnableChannels = sequencer.recordEnable[track];
                    }

                    // remove events while recorded time
                    eventToRemoval.Clear();
                    for (var trackIndex = 0; trackIndex < track.Size(); trackIndex++)
                    {
                        var midiEvent = track.Get(trackIndex);
                        if (isRecordable(recordEnableChannels, midiEvent) &&
                            midiEvent.GetTick() >= sequencer.recordingStartedTime &&
                            midiEvent.GetTick() <= recordEndedTime)
                        {
                            // recorded time
                            eventToRemoval.Add(midiEvent);
                        }
                    }

                    foreach (var anEvent in eventToRemoval)
                    {
                        track.Remove(anEvent);
                    }

                    // add recorded events
                    for (var eventIndex = 0; eventIndex < sequencer.recordingTrack.Size(); eventIndex++)
                    {
                        var midiEvent = sequencer.recordingTrack.Get(eventIndex);
                        if (isRecordable(recordEnableChannels, midiEvent))
                        {
                            track.Add(midiEvent);
                        }
                    }

                    Track.TrackUtils.SortEvents(track);
                }

                // refresh playingTrack
                sequencer.needRefreshPlayingTrack = true;
            }

            /// <summary>
            /// Start playing
            /// </summary>
            internal void StartPlaying()
            {
                if (IsRunning)
                {
                    // already playing
                    return;
                }

                if (sequencer == null)
                {
                    throw new MidiUnavailableException(
                        "sequencer == null, please wait for SequencerImpl will be opened.");
                }

                sequencer.tickPositionSetTime = CurrentTimeMillis();
                IsRunning = true;

#if !UNITY_5_3_OR_NEWER
                lock (sequencer.thread)
                {
                    Monitor.PulseAll(sequencer.thread);
                }
#endif
            }

            /// <summary>
            /// Stop playing
            /// </summary>
            internal void StopPlaying()
            {
                if (IsRunning == false)
                {
#if !UNITY_5_3_OR_NEWER
                    // already stopping
                    if (sequencer != null && sequencer.thread != null)
                    {
                        lock (sequencer.thread)
                        {
                            Monitor.PulseAll(sequencer.thread);
                        }
                    }
#endif

                    return;
                }

                IsRunning = false;
                sequencer.runningStoppedTime = CurrentTimeMillis();

#if !UNITY_5_3_OR_NEWER
                // force stop sleeping
                lock (sequencer.thread)
                {
                    Monitor.PulseAll(sequencer.thread);
                }
#endif
            }

            /// <summary>
            /// Process the specified <see cref="MidiMessage"/> and fire events to registered event listeners.
            /// </summary>
            /// <param name="message">the <see cref="MidiMessage"/></param>
            internal void FireEventListeners(MidiMessage message)
            {
                if (message is MetaMessage metaMessage)
                {
                    lock (sequencer.metaEventListeners)
                    {
                        foreach (var metaEventListener in sequencer.metaEventListeners)
                        {
                            metaEventListener.Meta(metaMessage);
                        }
                    }
                }
                else if (message is ShortMessage shortMessage)
                {
                    if (shortMessage.GetCommand() == ShortMessage.ControlChange)
                    {
                        lock (sequencer.controllerEventListenerMap)
                        {
                            var eventListeners = sequencer.controllerEventListenerMap[shortMessage.GetData1()];
                            if (eventListeners != null)
                            {
                                foreach (var eventListener in eventListeners)
                                {
                                    eventListener.ControlChange(shortMessage);
                                }
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Process the tempo change events
            /// </summary>
            /// <param name="metaMessage">the <see cref="MetaMessage"/></param>
            /// <returns>true if the tempo changed</returns>
            private bool ProcessTempoChange(MetaMessage metaMessage)
            {
                if (metaMessage.GetLength() == 6 && metaMessage.GetStatus() == MetaMessage.Meta)
                {
                    var message = metaMessage.GetMessage();
                    if (message != null && (message[1] & 0xff) == MetaMessage.Tempo && message[2] == 3)
                    {
                        var tempo = (message[5] & 0xff) | //
                                    ((message[4] & 0xff) << 8) | //
                                    ((message[3] & 0xff) << 16);

                        sequencer.SetTempoInMpq(tempo);
                        return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// Merge current sequence's track to play
            /// </summary>
            private void RefreshPlayingTrack()
            {
                if (sequencer.sequence == null)
                {
                    return;
                }

                var tracks = sequencer.sequence.GetTracks();
                if (tracks.Length > 0)
                {
                    try
                    {
                        // at first, merge all track into one track
                        sequencer.playingTrack =
                            Track.TrackUtils.MergeSequenceToTrack(sequencer, sequencer.recordEnable);
                    }
                    catch (InvalidMidiDataException)
                    {
                        // ignore exception
                    }
                }
            }

            /// <summary>
            /// Check if the event can be recorded
            /// </summary>
            /// <param name="recordEnableChannels">the channel IDs that are able to record.</param>
            /// <param name="midiEvent">the <see cref="MidiEvent"/></param>
            /// <returns>true if the event can be recorded</returns>
            private bool isRecordable(HashSet<int> recordEnableChannels, MidiEvent midiEvent)
            {
                if (recordEnableChannels == null)
                {
                    return false;
                }

                if (recordEnableChannels.Contains(-1))
                {
                    return true;
                }

                var status = midiEvent.GetMessage().GetStatus();
                switch (status & ShortMessage.MaskEvent)
                {
                    // channel messages
                    case ShortMessage.NoteOff:
                    case ShortMessage.NoteOn:
                    case ShortMessage.PolyPressure:
                    case ShortMessage.ControlChange:
                    case ShortMessage.ProgramChange:
                    case ShortMessage.ChannelPressure:
                    case ShortMessage.PitchBend:
                        // recorded Track and channel
                        return recordEnableChannels.Contains(status & ShortMessage.MaskChannel);
                    // exclusive messages
                    default:
                        return true;
                }
            }
        }
    }
}