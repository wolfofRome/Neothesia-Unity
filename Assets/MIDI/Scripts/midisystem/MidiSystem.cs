using System;
using System.Collections.Generic;
using System.IO;

namespace jp.kshoji.midisystem
{
    /// <summary>
    /// MIDI System
    /// </summary>
    public static class MidiSystem
    {
        /// <summary>Stores MIDI Receivers</summary>
        private static readonly Dictionary<string, IReceiver> Receivers = new Dictionary<string, IReceiver>();

        /// <summary>MIDI File Reader</summary>
        private static StandardMidiFileReader midiFileReader = new StandardMidiFileReader();

        /// <summary>MIDI File Writer</summary>
        private static StandardMidiFileWriter midiFileWriter = new StandardMidiFileWriter();

        /// <summary>
        /// Add a <see cref="IReceiver"/> for the device ID
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        /// <param name="receiver"><see cref="IReceiver"/></param>
        public static void AddReceiver(string deviceId, IReceiver receiver)
        {
            lock (Receivers)
            {
                if (!Receivers.ContainsKey(deviceId))
                {
                    Receivers.Add(deviceId, receiver);
                }
            }
        }

        /// <summary>
        /// Remove Receiver for the device ID
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        public static void RemoveReceiver(string deviceId)
        {
            lock (Receivers)
            {
                if (Receivers.ContainsKey(deviceId))
                {
                    Receivers.Remove(deviceId);
                }
            }
        }

        /// <summary>
        /// Get the <see cref="IReceiver"/> for the device ID
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        /// <returns><see cref="IReceiver"/> or null</returns>
        public static IReceiver GetReceiver(string deviceId)
        {
            lock (Receivers)
            {
                if (Receivers.ContainsKey(deviceId))
                {
                    return Receivers[deviceId];
                }
            }

            return null;
        }

        /// <summary>
        /// Get currently connected <see cref="IReceiver" />s
        /// </summary>
        /// <returns>currently connected <see cref="IReceiver" />s</returns>
        public static IEnumerable<IReceiver> GetReceivers()
        {
            lock (Receivers)
            {
                return Receivers.Values;
            }
        }

        /// <summary>Stores MIDI Transmitters</summary>
        private static readonly Dictionary<string, ITransmitter> Transmitters = new Dictionary<string, ITransmitter>();

        /// <summary>
        /// Add a <see cref="IReceiver"/> for the device ID
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        /// <param name="transmitter"><see cref="ITransmitter"/></param>
        public static void AddTransmitter(string deviceId, ITransmitter transmitter)
        {
            lock (Transmitters)
            {
                if (!Transmitters.ContainsKey(deviceId))
                {
                    Transmitters.Add(deviceId, transmitter);
                }
            }
        }

        /// <summary>
        /// Remove Receiver for the device ID
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        public static void RemoveTransmitter(string deviceId)
        {
            lock (Transmitters)
            {
                if (Transmitters.ContainsKey(deviceId))
                {
                    Transmitters.Remove(deviceId);
                }
            }
        }

        /// <summary>
        /// Get the <see cref="ITransmitter"/> for the device ID
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        /// <returns><see cref="ITransmitter"/> or null</returns>
        public static ITransmitter GetTransmitter(string deviceId)
        {
            lock (Transmitters)
            {
                if (Transmitters.ContainsKey(deviceId))
                {
                    return Transmitters[deviceId];
                }
            }

            return null;
        }

        /// <summary>
        /// Get currently connected <see cref="ITransmitter" />s
        /// </summary>
        /// <returns>currently connected <see cref="ITransmitter" />s</returns>
        public static IEnumerable<ITransmitter> GetTransmitters()
        {
            lock (Transmitters)
            {
                return Transmitters.Values;
            }
        }

        /// <summary>
        /// Read <see cref="Sequence"/> from <see cref="Stream"/>
        /// </summary>
        /// <param name="stream">input stream</param>
        /// <returns><see cref="Sequence"/></returns>
        public static Sequence ReadSequence(Stream stream)
        {
            using (stream)
            {
                return midiFileReader.GetSequence(stream);
            }
        }

        /// <summary>
        /// Write <see cref="Sequence"/> to <see cref="Stream"/> with Standard MIDI File format
        /// </summary>
        /// <param name="sequence">input <see cref="Sequence"/></param>
        /// <param name="stream">output stream</param>
        /// <returns></returns>
        public static void WriteSequence(Sequence sequence, Stream stream)
        {
            var fileType = sequence.GetTracks().Length == 1 ? 0 : 1;

            using (var midiDataOutputStream = new StandardMidiFileWriter.MidiDataOutputStream())
			{
            	midiFileWriter.Write(sequence, fileType, midiDataOutputStream);
            	stream.Write(midiDataOutputStream.ToArray(), 0, (int)midiDataOutputStream.Length);
			}
        }
    }
}