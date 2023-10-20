using System;
using System.Runtime.InteropServices;

namespace jp.kshoji.unity.midi.win32
{
    public interface IMidiOutPort
    {
        void SendMessage(IMidiMessage midiMessage);
        string Name { get; }
        void Close();
    }

    internal class MidiOutPort : IMidiOutPort
    {
        private bool isOpen;
        private readonly Win32API.HMIDIOUT handle;

        public MidiOutPort(UIntPtr deviceId, Win32API.MIDIOUTCAPS caps)
        {
            lock (this)
            {
                Name = caps.szPname;
                if (isOpen)
                {
                    return;
                }

                Win32API.midiOutOpen(out handle, deviceId, null, (UIntPtr)0);
                isOpen = true;
            }
        }

        public void Close()
        {
            if (!isOpen)
            {
                return;
            }

            Win32API.midiOutClose(handle);
            isOpen = false;
        }
        
        public void SendMessage(IMidiMessage receivedMidiMessage)
        {
            switch (receivedMidiMessage.Type)
            {
                case MidiMessageType.NoteOff:
                    {
                        var noteOff = (MidiNoteOffMessage)receivedMidiMessage;
                        Win32API.midiOutShortMsg(handle,
                            (UInt32)(0x80 | noteOff.Channel | (noteOff.Note << 8) | (noteOff.Velocity << 16)));
                    }
                    break;
                case MidiMessageType.NoteOn:
                    {
                        var noteOn = (MidiNoteOnMessage)receivedMidiMessage;
                        Win32API.midiOutShortMsg(handle,
                            (UInt32)(0x90 | noteOn.Channel | (noteOn.Note << 8) | (noteOn.Velocity << 16)));
                    }
                    break;
                case MidiMessageType.PolyphonicKeyPressure:
                    {
                        var polyphonicKeyPressure = (MidiPolyphonicKeyPressureMessage)receivedMidiMessage;
                        Win32API.midiOutShortMsg(handle,
                            (UInt32)(0xa0 | polyphonicKeyPressure.Channel | (polyphonicKeyPressure.Note << 8) | (polyphonicKeyPressure.Pressure << 16)));
                    }
                    break;
                case MidiMessageType.ControlChange:
                    {
                        var controlChange = (MidiControlChangeMessage)receivedMidiMessage;
                        Win32API.midiOutShortMsg(handle,
                            (UInt32)(0xb0 | controlChange.Channel | (controlChange.Controller << 8) | (controlChange.ControlValue << 16)));
                    }
                    break;
                case MidiMessageType.ProgramChange:
                    {
                        var programChange = (MidiProgramChangeMessage)receivedMidiMessage;
                        Win32API.midiOutShortMsg(handle,
                            (UInt32)(0xc0 | programChange.Channel | (programChange.Program << 8)));
                    }
                    break;
                case MidiMessageType.ChannelPressure:
                    {
                        var channelPressure = (MidiChannelPressureMessage)receivedMidiMessage;
                        Win32API.midiOutShortMsg(handle,
                            (UInt32)(0xd0 | channelPressure.Channel | (channelPressure.Pressure << 8)));
                    }
                    break;
                case MidiMessageType.PitchBendChange:
                    {
                        var pitchBendChange = (MidiPitchBendChangeMessage)receivedMidiMessage;
                        Win32API.midiOutShortMsg(handle,
                            (UInt32)(0xe0 | pitchBendChange.Channel | (pitchBendChange.Bend << 8)));
                    }
                    break;
                case MidiMessageType.SystemExclusive:
                    {
                        var systemExclusive = (MidiSystemExclusiveMessage)receivedMidiMessage;

                        var data = systemExclusive.RawData.ToArray();
                        IntPtr ptr;
                        var size = (UInt32)Marshal.SizeOf(typeof(Win32API.MIDIHDR));
                        var header = new Win32API.MIDIHDR
                        {
                            lpData = Marshal.AllocHGlobal(data.Length),
                            dwBufferLength = data.Length,
                            dwBytesRecorded = data.Length,
                            dwFlags = 0
                        };
                        for (var i = 0; i < data.Length; i++)
                        {
                            Marshal.WriteByte(header.lpData, i, data[i]);
                        }

                        try
                        {
                            ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Win32API.MIDIHDR)));
                        }
                        catch (Exception)
                        {
                            Marshal.FreeHGlobal(header.lpData);
                            throw;
                        }

                        try
                        {
                            Marshal.StructureToPtr(header, ptr, false);
                        }
                        catch (Exception)
                        {
                            Marshal.FreeHGlobal(header.lpData);
                            Marshal.FreeHGlobal(ptr);
                            throw;
                        }

                        Win32API.midiOutPrepareHeader(handle, ptr, size);
                        Win32API.midiOutLongMsg(handle, ptr, size);
                        Win32API.midiOutUnprepareHeader(handle, ptr, size);

                        Marshal.FreeHGlobal(header.lpData);
                        Marshal.FreeHGlobal(ptr);
                    }
                    break;
                case MidiMessageType.MidiTimeCode:
                    {
                        var midiTimeCode = (MidiTimeCodeMessage)receivedMidiMessage;
                        Win32API.midiOutShortMsg(handle,
                            (UInt32)(0xf1 | (midiTimeCode.FrameType << 12) | midiTimeCode.Values << 8));
                    }
                    break;
                case MidiMessageType.SongPositionPointer:
                    {
                        var songPositionPointer = (MidiSongPositionPointerMessage)receivedMidiMessage;
                        Win32API.midiOutShortMsg(handle, (UInt32)(0xf2 | (songPositionPointer.Beats << 8)));
                    }
                    break;
                case MidiMessageType.SongSelect:
                    {
                        var songSelect = (MidiSongSelectMessage)receivedMidiMessage;
                        Win32API.midiOutShortMsg(handle, (UInt32)(0xf3 | (songSelect.Song << 8)));
                    }
                    break;
                case MidiMessageType.TuneRequest:
                    Win32API.midiOutShortMsg(handle, 0xf6);
                    break;
                case MidiMessageType.TimingClock:
                    Win32API.midiOutShortMsg(handle, 0xf8);
                    break;
                case MidiMessageType.Start:
                    Win32API.midiOutShortMsg(handle, 0xfa);
                    break;
                case MidiMessageType.Continue:
                    Win32API.midiOutShortMsg(handle, 0xfb);
                    break;
                case MidiMessageType.Stop:
                    Win32API.midiOutShortMsg(handle, 0xfc);
                    break;
                case MidiMessageType.ActiveSensing:
                    Win32API.midiOutShortMsg(handle, 0xfe);
                    break;
                case MidiMessageType.SystemReset:
                    Win32API.midiOutShortMsg(handle, 0xff);
                    break;

                case MidiMessageType.EndSystemExclusive:
                case MidiMessageType.None:
                default:
                    break;
            }
        }

        public string Name { get; set; }
    }
}