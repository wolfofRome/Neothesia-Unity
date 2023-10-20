using System;
using System.IO;
using System.Net;

namespace jp.kshoji.midisystem
{
    /// <summary>
    /// The implementation SMF reader
    /// </summary>
    public class StandardMidiFileReader
    {
        private static int ReadInt(Stream stream)
        {
            return (stream.ReadByte() << 24) | (stream.ReadByte() << 16) | (stream.ReadByte() << 8) | stream.ReadByte();
        }

        private static int ReadShort(Stream stream)
        {
            return (stream.ReadByte() << 8) | stream.ReadByte();
        }

        private MidiFileFormat GetMidiFileFormat(Stream dataInputStream)
        {
            if (ReadInt(dataInputStream) != MidiFileFormat.HeaderMThd)
            {
                throw new InvalidMidiDataException("Invalid header");
            }

            var bytes = ReadInt(dataInputStream);
            if (bytes < 6)
            {
                throw new InvalidMidiDataException("Invalid header");
            }

            var type = ReadShort(dataInputStream);
            if (type < 0 || type > 2)
            {
                throw new InvalidMidiDataException("Invalid header");
            }

            var numberOfTracks = ReadShort(dataInputStream);
            if (numberOfTracks <= 0)
            {
                throw new InvalidMidiDataException("Invalid tracks");
            }

            var division = ReadShort(dataInputStream);
            float divisionType;
            int resolution;
            if ((division & 0x8000) != 0)
            {
                resolution = division & 0xff;

                division = 256 - ((division >> 8) & 0xff);
                switch (division)
                {
                    case 24:
                        divisionType = Sequence.Smpte24;
                        break;
                    case 25:
                        divisionType = Sequence.Smpte25;
                        break;
                    case 29:
                        divisionType = Sequence.Smpte30Drop;
                        break;
                    case 30:
                        divisionType = Sequence.Smpte30;
                        break;

                    default:
                        throw new InvalidMidiDataException("Invalid sequence information");
                }
            }
            else
            {
                divisionType = Sequence.Ppq;
                resolution = division & 0x7fff;
            }

            dataInputStream.Seek(bytes - 6, SeekOrigin.Current);

            return new ExtendedMidiFileFormat(type, divisionType, resolution, MidiFileFormat.UnknownLength,
                MidiFileFormat.UnknownLength, numberOfTracks);
        }


        public MidiFileFormat GetMidiFileFormat(Uri url)
        {
            var inputStream = WebRequest.Create(url).GetResponse().GetResponseStream();
            try
            {
                return GetMidiFileFormat(inputStream);
            }
            finally
            {
                inputStream?.Close();
            }
        }


        public MidiFileFormat GetMidiFileFormat(string file)
        {
            var inputStream = new FileStream(file, FileMode.Open);
            try
            {
                return GetMidiFileFormat(inputStream);
            }
            finally
            {
                inputStream.Close();
            }
        }


        public Sequence GetSequence(Stream inputStream)
        {
            var midiDataInputStream = new MidiDataInputStream(ConvertToMemoryStream(inputStream));

            try
            {
                var midiFileFormat = (ExtendedMidiFileFormat)GetMidiFileFormat(midiDataInputStream);
                var sequence = new Sequence(midiFileFormat.GetDivisionType(), midiFileFormat.GetResolution());

                var numberOfTracks = midiFileFormat.GetNumberTracks();

                while (numberOfTracks-- > 0)
                {
                    var track = sequence.CreateTrack();
                    if (ReadInt(midiDataInputStream) != MidiFileFormat.HeaderMTrk)
                    {
                        throw new InvalidMidiDataException("Invalid track header");
                    }

                    // track length: ignored
                    ReadInt(midiDataInputStream);

                    var runningStatus = -1;
                    var ticks = 0;
                    var isTrackRunning = true;

                    // Read all of the events.
                    while (isTrackRunning)
                    {
                        ticks += midiDataInputStream.ReadVariableLengthInt(); // add deltaTime

                        var data = midiDataInputStream.ReadByte();
                        MidiMessage message;
                        if (data < 0x80)
                        {
                            // data values
                            if (runningStatus >= 0 && runningStatus < 0xf0)
                            {
                                message = ProcessRunningMessage(runningStatus, data, midiDataInputStream);
                            }
                            else if (runningStatus >= 0xf0 && runningStatus <= 0xff)
                            {
                                message = ProcessSystemMessage(runningStatus, data, midiDataInputStream);
                            }
                            else
                            {
                                throw new InvalidMidiDataException($"Invalid data: {runningStatus:X2} {data:X2}");
                            }
                        }
                        else if (data < 0xf0)
                        {
                            // Control messages
                            message = ProcessRunningMessage(data, midiDataInputStream.ReadByte(), midiDataInputStream);

                            runningStatus = data;
                        }
                        else if (data == ShortMessage.StartOfExclusive || data == ShortMessage.EndOfExclusive)
                        {
                            // System Exclusive event
                            var sysexLength = midiDataInputStream.ReadVariableLengthInt();
                            if (sysexLength > midiDataInputStream.Length - midiDataInputStream.Position)
                            {
                                throw new InvalidMidiDataException($"Invalid system exclusive length: {sysexLength}");
                            }
                            var sysexData = new byte[sysexLength];
                            midiDataInputStream.ReadFully(sysexData);

                            var sysexMessage = new SysexMessage();
                            sysexMessage.SetMessage(data, sysexData);
                            message = sysexMessage;

                            runningStatus = -1;
                        }
                        else if (data == MetaMessage.Meta)
                        {
                            // Meta Message
                            var type = midiDataInputStream.ReadByte();

                            var metaLength = midiDataInputStream.ReadVariableLengthInt();
                            var metaData = new byte[metaLength];
                            midiDataInputStream.ReadFully(metaData);

                            var metaMessage = new MetaMessage();
                            metaMessage.SetMessage(type, metaData);
                            message = metaMessage;

                            runningStatus = -1;

                            if (type == MetaMessage.EndOfTrack)
                            {
                                isTrackRunning = false;
                            }
                        }
                        else
                        {
                            // f1-f6, f8-fe
                            message = ProcessSystemMessage(data, null, midiDataInputStream);

                            runningStatus = data;
                        }

                        track.Add(new MidiEvent(message, ticks));
                    }

                    Track.TrackUtils.SortEvents(track);
                }

                return sequence;
            }
            finally
            {
                midiDataInputStream.Close();
            }
        }

        /// <summary>
        /// Process the <see cref="SysexMessage"/>
        /// </summary>
        /// <param name="data1">the first data</param>
        /// <param name="data2">the second data</param>
        /// <param name="midiDataInputStream">the InputStream</param>
        /// <returns>the processed MIDI message</returns>
        /// <exception cref="InvalidMidiDataException">invalid MIDI data inputted</exception>
        private static ShortMessage ProcessSystemMessage(int data1, int? data2, Stream midiDataInputStream)
        {
            ShortMessage shortMessage;
            switch (data1)
            {
                case ShortMessage.SongPositionPointer: //f2
                    shortMessage = new ShortMessage();
                    if (data2 == null)
                    {
                        shortMessage.SetMessage(data1, midiDataInputStream.ReadByte(), midiDataInputStream.ReadByte());
                    }
                    else
                    {
                        shortMessage.SetMessage(data1, data2.Value, midiDataInputStream.ReadByte());
                    }

                    break;

                case ShortMessage.SongSelect: //f3
                case ShortMessage.BusSelect: //f5
                    shortMessage = new ShortMessage();
                    if (data2 == null)
                    {
                        shortMessage.SetMessage(data1, midiDataInputStream.ReadByte(), 0);
                    }
                    else
                    {
                        shortMessage.SetMessage(data1, data2.Value, 0);
                    }

                    break;

                case ShortMessage.TuneRequest: //f6
                case ShortMessage.TimingClock: //f8
                case ShortMessage.Start: //fa
                case ShortMessage.Continue: //fb
                case ShortMessage.Stop: //fc
                case ShortMessage.ActiveSensing: //fe
                    if (data2 != null)
                    {
                        // XXX must be ignored??
                        throw new InvalidMidiDataException($"Invalid data: {data2:X2}");
                    }

                    shortMessage = new ShortMessage();
                    shortMessage.SetMessage(data1, 0, 0);
                    break;

                default: //f1, f9, fd
                    throw new InvalidMidiDataException($"Invalid data: {data1:X2}");
            }

            return shortMessage;
        }

        /// <summary>
        /// Process the MIDI running message
        /// </summary>
        /// <param name="status">running status</param>
        /// <param name="data1">the first data</param>
        /// <param name="midiDataInputStream">the InputStream</param>
        /// <returns>the processed MIDI message</returns>
        /// <exception cref="InvalidMidiDataException">invalid MIDI data inputted</exception>
        private static ShortMessage ProcessRunningMessage(int status, int data1, Stream midiDataInputStream)
        {
            ShortMessage shortMessage;
            switch (status & ShortMessage.MaskEvent)
            {
                case ShortMessage.NoteOff: //80
                case ShortMessage.NoteOn: //90
                case ShortMessage.PolyPressure: //a0
                case ShortMessage.ControlChange: //b0
                case ShortMessage.PitchBend: //e0
                    shortMessage = new ShortMessage();
                    shortMessage.SetMessage(status, data1, midiDataInputStream.ReadByte());
                    break;

                case ShortMessage.ProgramChange: //c0
                case ShortMessage.ChannelPressure: //d0
                    shortMessage = new ShortMessage();
                    shortMessage.SetMessage(status, data1, 0);
                    break;

                default:
                    throw new InvalidMidiDataException($"Invalid data: {status:X2} {data1:X2}");
            }

            return shortMessage;
        }

        /// <summary>
        /// Convert inputStream into <see cref="MemoryStream"/>
        /// </summary>
        /// <param name="inputStream">the <see cref="Stream"/> instance</param>
        /// <returns>the <see cref="MemoryStream"/></returns>
        private static MemoryStream ConvertToMemoryStream(Stream inputStream)
        {
            if (inputStream is MemoryStream)
            {
                return (MemoryStream)inputStream;
            }

            var outputStream = new MemoryStream();
            inputStream.CopyTo(outputStream);
            return new MemoryStream(outputStream.ToArray());
        }


        public Sequence GetSequence(Uri url)
        {
            var inputStream = WebRequest.Create(url).GetResponse().GetResponseStream();
            try
            {
                return GetSequence(inputStream);
            }
            finally
            {
                inputStream?.Close();
            }
        }


        public Sequence GetSequence(string file)
        {
            Stream inputStream = new FileStream(file, FileMode.Open);
            try
            {
                return GetSequence(inputStream);
            }
            finally
            {
                inputStream.Close();
            }
        }

        /// <summary>
        /// Represents Extended MIDI File format
        /// </summary>
        private class ExtendedMidiFileFormat : MidiFileFormat
        {
            private readonly int numberOfTracks;

            /// <summary>
            /// Create an <see cref="ExtendedMidiFileFormat"/> object from the given parameters.
            /// </summary>
            /// <param name="type">the MIDI file type (0, 1, or 2)</param>
            /// <param name="divisionType">the MIDI file division type</param>
            /// <param name="resolution">the MIDI file timing resolution</param>
            /// <param name="bytes">the MIDI file size in bytes</param>
            /// <param name="microseconds">the MIDI file length in microseconds</param>
            /// <param name="numberOfTracks">the number of tracks</param>
            public ExtendedMidiFileFormat(int type, float divisionType, int resolution, int bytes, long microseconds,
                int numberOfTracks) : base(type, divisionType, resolution, bytes, microseconds)
            {
                this.numberOfTracks = numberOfTracks;
            }

            /// <summary>
            /// Get the number of tracks for this MIDI file.
            /// </summary>
            /// <returns>the number of tracks for this MIDI file</returns>
            public int GetNumberTracks()
            {
                return numberOfTracks;
            }
        }

        /// <summary>
        /// Represents InputStream for MIDI Data
        /// </summary>
        private class MidiDataInputStream : MemoryStream
        {
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="inputStream">the source stream</param>
            public MidiDataInputStream(Stream inputStream)
            {
                inputStream.CopyTo(this);
                Seek(0, SeekOrigin.Begin);
            }

            /// <inheritdoc cref="MemoryStream.Seek"/>
            public sealed override long Seek(long offset, SeekOrigin loc)
            {
                return base.Seek(offset, loc);
            }

            /// <summary>
            /// Read value from InputStream
            /// </summary>
            /// <returns>the variable</returns>
            public int ReadVariableLengthInt()
            {
                var value = ReadByte();

                if ((value & 0x80) != 0)
                {
                    value &= 0x7f;
                    int c;
                    do
                    {
                        value = (value << 7) + ((c = ReadByte()) & 0x7f);
                    } while ((c & 0x80) != 0);
                }

                return value;
            }

            public void ReadFully(byte[] data)
            {
                Read(data, 0, data.Length);
            }
        }
    }
}