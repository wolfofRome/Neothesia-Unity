using System.Collections.Generic;
using System.Linq;

namespace jp.kshoji.midisystem
{
    /// <summary>
    /// Represents MIDI Track
    /// </summary>
    public class Track
    {
        private static readonly byte[] EndOfTrack = { 0xff, 0x2f, 0 };
        private static readonly Track[] EmptyTracks = { };

        /// <summary>
        /// <see cref="Comparer{T}"/> for MIDI data sorting
        /// </summary>
        private static readonly Comparer<MidiEvent> MidiEventComparer = new MidiEventComparer();

        private readonly List<MidiEvent> events = new List<MidiEvent>();

        /// <summary>
        /// Add {@link MidiEvent} to this {@link Track}
        /// </summary>
        /// <param name="midiEvent">event to add</param>
        /// <returns>true if the event has been added</returns>
        public bool Add(MidiEvent midiEvent)
        {
            lock (events)
            {
                events.Add(midiEvent);
            }

            return true;
        }

        /// <summary>
        /// Get specified index of <see cref="MidiEvent"/>
        /// </summary>
        /// <param name="index">the index of event</param>
        /// <returns>the <see cref="MidiEvent"/></returns>
        public MidiEvent Get(int index)
        {
            lock (events)
            {
                return events[index];
            }
        }

        /// <summary>
        /// Remove <see cref="MidiEvent"/> from this <see cref="Track"/>
        /// </summary>
        /// <param name="midiEvent">event to remove</param>
        /// <returns>true if the event has been removed</returns>
        public bool Remove(MidiEvent midiEvent)
        {
            lock (events)
            {
                return events.Remove(midiEvent);
            }
        }

        /// <summary>
        /// Get the number of events in the <see cref="Track"/>
        /// </summary>
        /// <returns>the number of events</returns>
        public int Size()
        {
            lock (events)
            {
                return events.Count;
            }
        }

        /// <summary>
        /// Get length of ticks for this <see cref="Track"/>
        /// </summary>
        /// <returns>the length of ticks</returns>
        public long Ticks()
        {
            TrackUtils.SortEvents(this);

            lock (events)
            {
                if (events.Count == 0)
                {
                    return 0L;
                }

                return events[events.Count - 1].GetTick();
            }
        }

        /// <summary>
        /// Utilities for <see cref="Track"/>
        /// </summary>
        public static class TrackUtils
        {
            /// <summary>
            /// Merge the specified <see cref="ISequencer"/>'s <see cref="Track"/>s into one <see cref="Track"/>
            /// </summary>
            /// <param name="sequencer">the Sequencer</param>
            /// <param name="recordEnable">track recordable flags</param>
            /// <returns>merged <see cref="Sequence"/></returns>
            public static Track MergeSequenceToTrack(ISequencer sequencer, Dictionary<Track, HashSet<int>> recordEnable)
            {
                var sourceSequence = sequencer.GetSequence();
                var mergedTrack = new Track();

                // apply track mute and solo
                var tracks = sourceSequence == null ? EmptyTracks : sourceSequence.GetTracks();

                var hasSoloTrack = false;
                for (var trackIndex = 0; trackIndex < tracks.Length; trackIndex++)
                {
                    if (sequencer.GetTrackSolo(trackIndex))
                    {
                        hasSoloTrack = true;
                        break;
                    }
                }

                var tickEventMap = new Dictionary<long, List<MidiEvent>>();
                for (var trackIndex = 0; trackIndex < tracks.Length; trackIndex++)
                {
                    if (sequencer.GetTrackMute(trackIndex))
                    {
                        // muted track, ignore
                        continue;
                    }

                    if (hasSoloTrack && sequencer.GetTrackSolo(trackIndex) == false)
                    {
                        // not solo track, ignore
                        continue;
                    }

                    if (sequencer.GetIsRecording() && recordEnable.ContainsKey(tracks[trackIndex]) &&
                        recordEnable[tracks[trackIndex]].Count > 0)
                    {
                        // currently recording track, ignore
                        continue;
                    }

                    // store MIDI events with the same tick, grouped by track ID
                    foreach (var midiEvent in tracks[trackIndex].events)
                    {
                        if (!tickEventMap.ContainsKey(midiEvent.GetTick()))
                        {
                            tickEventMap[midiEvent.GetTick()] = new List<MidiEvent>();
                        }
                        tickEventMap[midiEvent.GetTick()].Add(midiEvent);
                    }
                }

                // create tick list, and merge MIDI events to one track
                var tickList = tickEventMap.Keys.ToList();
                tickList.Sort();
                foreach (var tick in tickList)
                {
                    mergedTrack.events.AddRange(tickEventMap[tick]);
                }

                // remove all of END_OF_TRACK
                mergedTrack.events.RemoveAll(e => EndOfTrack.SequenceEqual(e.GetMessage().GetMessage()));

                // add END_OF_TRACK to last
                if (mergedTrack.events.Count == 0)
                {
                    mergedTrack.events.Add(new MidiEvent(new MetaMessage(EndOfTrack), 0));
                }
                else
                {
                    mergedTrack.events.Add(new MidiEvent(new MetaMessage(EndOfTrack),
                        mergedTrack.events[mergedTrack.events.Count - 1].GetTick() + 1));
                }

                return mergedTrack;
            }

            /// <summary>
            /// Sort the <see cref="Track"/>'s <see cref="MidiEvent"/>, order by tick and events
            /// </summary>
            /// <param name="track">the Track</param>
            public static void SortEvents(Track track)
            {
                lock (track.events)
                {
                    // remove all of END_OF_TRACK
                    var filtered = new List<MidiEvent>();
                    foreach (var midiEvent in track.events)
                    {
                        if (!EndOfTrack.SequenceEqual(midiEvent.GetMessage().GetMessage()))
                        {
                            filtered.Add(midiEvent);
                        }
                    }

                    track.events.Clear();
                    track.events.AddRange(filtered);

                    // sort the events
                    track.events.Sort(MidiEventComparer);

                    // add END_OF_TRACK to last
                    if (track.events.Count == 0)
                    {
                        track.events.Add(new MidiEvent(new MetaMessage(EndOfTrack), 0));
                    }
                    else
                    {
                        track.events.Add(new MidiEvent(new MetaMessage(EndOfTrack),
                            track.events[track.events.Count - 1].GetTick() + 1));
                    }
                }
            }
        }
    }
}