using System.IO;

namespace jp.kshoji.midisystem
{
    public class StandardMidiFileWriter
    {
	    /**
	     * Represents OutputStream for MIDI Data
	     *
	     * @author K.Shoji
	     */
	    public class MidiDataOutputStream : MemoryStream {

	        /**
	         * Constructor
	         *
	         * @param outputStream the source stream
	         */
			public MidiDataOutputStream() {
		        Seek(0, SeekOrigin.Begin);
			}

	        /// <inheritdoc cref="MemoryStream.Seek"/>
	        public sealed override long Seek(long offset, SeekOrigin loc)
	        {
		        return base.Seek(offset, loc);
	        }

	        /**
	         * Convert the specified value into the value for MIDI data
	         *
	         * @param value the original value
	         * @return the raw data to write
	         */
			private static int GetValueToWrite(int value) {
				var result = value & 0x7f;
				var currentValue = value;

				while ((currentValue >>= 7) != 0) {
					result <<= 8;
					result |= (currentValue & 0x7f) | 0x80;
				}
				return result;
			}

	        /**
	         * Get the data length for the specified value
	         *
	         * @param value the value
	         * @return the data length
	         */
	        internal static int VariableLengthIntLength(int value) {
				var valueToWrite = GetValueToWrite(value);
				var length = 0;

				while (true) {
					length++;

					if ((valueToWrite & 0x80) != 0) {
						valueToWrite >>= 8;
					} else {
						break;
					}
				}

				return length;
			}

	        /**
	         * Write the specified value to the OutputStream
	         *
	         * @param value the value
	         * @throws IOException
	         */
	        internal void WriteVariableLengthInt(int value) {
				var valueToWrite = GetValueToWrite(value);

				while (true) {
					WriteByte((byte)(valueToWrite & 0xff));

					if ((valueToWrite & 0x80) != 0) {
						valueToWrite >>= 8;
					} else {
						break;
					}
				}
			}

	        internal void WriteInt(int value)
	        {
		        WriteByte((byte)((value >> 24) & 0xff));
		        WriteByte((byte)((value >> 16) & 0xff));
		        WriteByte((byte)((value >> 8) & 0xff));
		        WriteByte((byte)(value & 0xff));
	        }

	        internal void WriteShort(int value)
	        {
		        WriteByte((byte)((value >> 8) & 0xff));
		        WriteByte((byte)(value & 0xff));
	        }
	    }

		public int[] GetMidiFileTypes() {
			return new[] { 0, 1 };
		}

		public int[] GetMidiFileTypes(Sequence sequence) {
			if (sequence.GetTracks().Length > 1) {
				return new[] { 1 };
			} else {
				return new[] { 0, 1 };
			}
		}

		public int Write(Sequence sequence, int fileType, MemoryStream outputStream) {
			var midiDataOutputStream = (MidiDataOutputStream)outputStream;

			var tracks = sequence.GetTracks();
			midiDataOutputStream.WriteInt(MidiFileFormat.HeaderMThd);
			midiDataOutputStream.WriteInt(6);
			midiDataOutputStream.WriteShort(fileType);
			midiDataOutputStream.WriteShort(tracks.Length);
			
			var divisionType = sequence.GetDivisionType();
			var resolution = sequence.GetResolution();
			var division = 0;
			if (Sequence.DivisionTypeEquals(divisionType, Sequence.Ppq)) {
				division = resolution & 0x7fff;
			} else if (Sequence.DivisionTypeEquals(divisionType, Sequence.Smpte24)) {
				division = (24 << 8) * -1;
				division += resolution & 0xff;
			} else if (Sequence.DivisionTypeEquals(divisionType, Sequence.Smpte25)) {
				division = (25 << 8) * -1;
				division += resolution & 0xff;
			} else if (Sequence.DivisionTypeEquals(divisionType, Sequence.Smpte30Drop)) {
				division = (29 << 8) * -1;
				division += resolution & 0xff;
			} else if (Sequence.DivisionTypeEquals(divisionType, Sequence.Smpte30)) {
				division = (30 << 8) * -1;
				division += resolution & 0xff;
			}
			midiDataOutputStream.WriteShort(division);
			
			var length = 0;
			foreach (var track in tracks) {
				length += WriteTrack(track, midiDataOutputStream);
			}

			return length + 14;
		}

		/**
		 * Write {@link Track} data into {@link MidiDataOutputStream}
		 * 
		 * @param track the track
		 * @param midiDataOutputStream the OutputStream
		 * @return written byte length
		 * @throws IOException
		 */
		private static int WriteTrack(Track track, MidiDataOutputStream midiDataOutputStream) {
			var eventCount = track.Size();

			// track header
	        midiDataOutputStream.WriteInt(MidiFileFormat.HeaderMTrk);

			// calculate the track length
			var trackLength = 0;
			long lastTick = 0;
			MidiEvent midiEvent = null;
			for (var i = 0; i < eventCount; i++) {
	            midiEvent = track.Get(i);
	            if (midiEvent.GetMessage() is ShortMessage && midiEvent.GetMessage().GetStatus() >= 0xf8)
	            {
		            // ignore system realtime messages
		            continue;
	            }
				var tick = midiEvent.GetTick();
				trackLength += MidiDataOutputStream.VariableLengthIntLength((int) (tick - lastTick));
				lastTick = tick;

				trackLength += midiEvent.GetMessage().GetLength();
			}

	        // process End of Track message
			var needEndOfTrack = true;
			if (midiEvent != null && (midiEvent.GetMessage() is MetaMessage) && //
	            ((MetaMessage)midiEvent.GetMessage()).GetMessageType() == MetaMessage.EndOfTrack) {
	            needEndOfTrack = false;
	        } else {
	            trackLength += 4; // End of Track
	        }
	        midiDataOutputStream.WriteInt(trackLength);

	        // write the track data
			lastTick = 0;
			for (var i = 0; i < eventCount; i++) {
	            midiEvent = track.Get(i);
	            if (midiEvent.GetMessage() is ShortMessage && midiEvent.GetMessage().GetStatus() >= 0xf8)
	            {
		            // ignore system realtime messages
		            continue;
	            }
	            var tick = midiEvent.GetTick();
				midiDataOutputStream.WriteVariableLengthInt((int) (tick - lastTick));
				lastTick = tick;

				if (midiEvent.GetMessage() is SysexMessage sysexMessage)
				{
					midiDataOutputStream.WriteByte((byte)midiEvent.GetMessage().GetStatus());
					var sysexData = sysexMessage.GetData();
					midiDataOutputStream.WriteVariableLengthInt(sysexData.Length);
					midiDataOutputStream.Write(sysexData, 0, sysexData.Length);
				}
				else
				{
					midiDataOutputStream.Write(midiEvent.GetMessage().GetMessage(), 0, midiEvent.GetMessage().GetLength());
				}
	        }

	        // write End of Track message if not found.
	        if (needEndOfTrack) {
	            midiDataOutputStream.WriteVariableLengthInt(0);
	            midiDataOutputStream.WriteByte(MetaMessage.Meta);
	            midiDataOutputStream.WriteByte(MetaMessage.EndOfTrack);
	            midiDataOutputStream.WriteVariableLengthInt(0);
	        }

			return trackLength + 4;
		}
    }
}