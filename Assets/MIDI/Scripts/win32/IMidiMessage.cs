using System.IO;

namespace jp.kshoji.unity.midi.win32
{
    public enum MidiMessageType
    {
        ActiveSensing = 254,
        ChannelPressure = 208,
        Continue = 251,
        ControlChange = 176,
        EndSystemExclusive = 247,
        MidiTimeCode = 241,
        None = 0,
        NoteOff = 128,
        NoteOn = 144,
        PitchBendChange = 224,
        PolyphonicKeyPressure = 160,
        ProgramChange = 192,
        SongPositionPointer = 242,
        SongSelect = 243,
        Start = 250,
        Stop = 252,
        SystemExclusive = 240,
        SystemReset = 255,
        TimingClock = 248,
        TuneRequest = 246,
    }

    public interface IMidiMessage
    {
        MidiMessageType Type { get; }
    }

    public class MidiActiveSensingMessage : IMidiMessage
    {
        public MidiActiveSensingMessage()
        {
            Type = MidiMessageType.ActiveSensing;
        }

        public MidiMessageType Type { get; }
    }

    internal class MidiNoteOffMessage : IMidiMessage
    {
        public MidiNoteOffMessage(byte channel, byte note, byte velocity)
        {
            Type = MidiMessageType.NoteOff;
            Channel = channel;
            Note = note;
            Velocity = velocity;
        }

        public MidiMessageType Type { get; }
        public byte Channel { get; }
        public byte Note { get; }
        public byte Velocity { get; }
    }
    
    internal class MidiNoteOnMessage : IMidiMessage
    {
        public MidiNoteOnMessage(byte channel, byte note, byte velocity)
        {
            Type = MidiMessageType.NoteOn;
            Channel = channel;
            Note = note;
            Velocity = velocity;
        }

        public MidiMessageType Type { get; }
        public byte Channel { get; }
        public byte Note { get; }
        public byte Velocity { get; }
    }

    internal class MidiPolyphonicKeyPressureMessage : IMidiMessage
    {
        public MidiPolyphonicKeyPressureMessage(byte channel, byte note, byte velocity)
        {
            Type = MidiMessageType.PolyphonicKeyPressure;
            Channel = channel;
            Note = note;
            Pressure = velocity;
        }

        public MidiMessageType Type { get; }
        public byte Channel { get; }
        public byte Note { get; }
        public byte Pressure { get; }
    }

    internal class MidiControlChangeMessage : IMidiMessage
    {
        public MidiControlChangeMessage(byte channel, byte controller, byte controllerValue)
        {
            Type = MidiMessageType.ControlChange;
            Channel = channel;
            Controller = controller;
            ControlValue = controllerValue;
        }

        public MidiMessageType Type { get; }
        public byte Channel { get; }
        public byte Controller { get; }
        public byte ControlValue { get; }
    }

    internal class MidiProgramChangeMessage : IMidiMessage
    {
        public MidiProgramChangeMessage(byte channel, byte program)
        {
            Type = MidiMessageType.ProgramChange;
            Channel = channel;
            Program = program;
        }

        public MidiMessageType Type { get; }
        public byte Channel { get; }
        public byte Program { get; }
    }

    internal class MidiChannelPressureMessage : IMidiMessage
    {
        public MidiChannelPressureMessage(byte channel, byte pressure)
        {
            Type = MidiMessageType.ChannelPressure;
            Channel = channel;
            Pressure = pressure;
        }

        public MidiMessageType Type { get; }
        public byte Channel { get; }
        public byte Pressure { get; }
    }

    internal class MidiPitchBendChangeMessage : IMidiMessage
    {
        public MidiPitchBendChangeMessage(byte channel, ushort bend)
        {
            Type = MidiMessageType.PitchBendChange;
            Channel = channel;
            Bend = bend;
        }

        public MidiMessageType Type { get; }
        public byte Channel { get; }
        public ushort Bend { get; }
    }

    internal class MidiSystemExclusiveMessage : IMidiMessage
    {
        public MidiSystemExclusiveMessage(MemoryStream memoryStream)
        {
            Type = MidiMessageType.SystemExclusive;
            RawData = memoryStream;
        }

        public MemoryStream RawData { get; }
        public MidiMessageType Type { get; }
    }

    internal class MidiTimeCodeMessage : IMidiMessage
    {
        public MidiTimeCodeMessage(byte frameType, byte values)
        {
            Type = MidiMessageType.MidiTimeCode;
            FrameType = frameType;
            Values = values;
        }

        public MidiMessageType Type { get; }
        public byte FrameType { get; }
        public byte Values { get; }
    }

    internal class MidiSongPositionPointerMessage : IMidiMessage
    {
        public MidiSongPositionPointerMessage(ushort beats)
        {
            Type = MidiMessageType.SongPositionPointer;
            Beats = beats;
        }

        public MidiMessageType Type { get; }
        public ushort Beats { get; }
    }

    internal class MidiSongSelectMessage : IMidiMessage
    {
        public MidiSongSelectMessage(byte song)
        {
            Type = MidiMessageType.SongSelect;
            Song = song;
        }

        public MidiMessageType Type { get; }
        public byte Song { get; }
    }
    
    internal class MidiTuneRequestMessage : IMidiMessage
    {
        public MidiTuneRequestMessage()
        {
            Type = MidiMessageType.TuneRequest;
        }

        public MidiMessageType Type { get; }
    }
    
    internal class MidiTimingClockMessage : IMidiMessage
    {
        public MidiTimingClockMessage()
        {
            Type = MidiMessageType.TimingClock;
        }

        public MidiMessageType Type { get; }
    }
    
    internal class MidiStartMessage : IMidiMessage
    {
        public MidiStartMessage()
        {
            Type = MidiMessageType.Start;
        }

        public MidiMessageType Type { get; }
    }
    
    internal class MidiContinueMessage : IMidiMessage
    {
        public MidiContinueMessage()
        {
            Type = MidiMessageType.Continue;
        }

        public MidiMessageType Type { get; }
    }
    
    internal class MidiStopMessage : IMidiMessage
    {
        public MidiStopMessage()
        {
            Type = MidiMessageType.Stop;
        }

        public MidiMessageType Type { get; }
    }
    
    internal class MidiSystemResetMessage : IMidiMessage
    {
        public MidiSystemResetMessage()
        {
            Type = MidiMessageType.SystemReset;
        }

        public MidiMessageType Type { get; }
    }
}
