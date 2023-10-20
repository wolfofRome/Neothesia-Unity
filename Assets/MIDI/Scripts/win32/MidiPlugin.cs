using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using UnityEngine;

namespace jp.kshoji.unity.midi.win32
{
    public delegate void OnMidiInputDeviceAttachedHandler(string deviceId);
    public delegate void OnMidiInputDeviceDetachedHandler(string deviceId);
    public delegate void OnMidiOutputDeviceAttachedHandler(string deviceId);
    public delegate void OnMidiOutputDeviceDetachedHandler(string deviceId);

    public delegate void OnMidiNoteOnHandler(string deviceId, byte channel, byte note, byte velocity);
    public delegate void OnMidiNoteOffHandler(string deviceId, byte channel, byte note, byte velocity);
    public delegate void OnMidiPolyphonicKeyPressureHandler(string deviceId, byte channel, byte note, byte velocity);
    public delegate void OnMidiControlChangeHandler(string deviceId, byte channel, byte controller, byte controllerValue);
    public delegate void OnMidiProgramChangeHandler(string deviceId, byte channel, byte program);
    public delegate void OnMidiChannelPressureHandler(string deviceId, byte channel, byte pressure);
    public delegate void OnMidiPitchBendChangeHandler(string deviceId, byte channel, ushort bend);
    public delegate void OnMidiSystemExclusiveHandler(string deviceId, [ReadOnlyArray] byte[] systemExclusive);
    public delegate void OnMidiTimeCodeHandler(string deviceId, byte frameType, byte values);
    public delegate void OnMidiSongPositionPointerHandler(string deviceId, ushort beats);
    public delegate void OnMidiSongSelectHandler(string deviceId, byte song);
    public delegate void OnMidiTuneRequestHandler(string deviceId);
    public delegate void OnMidiTimingClockHandler(string deviceId);
    public delegate void OnMidiStartHandler(string deviceId);
    public delegate void OnMidiContinueHandler(string deviceId);
    public delegate void OnMidiStopHandler(string deviceId);
    public delegate void OnMidiActiveSensingHandler(string deviceId);
    public delegate void OnMidiSystemResetHandler(string deviceId);

    /// <summary>
    /// MIDI Plugin for Standalone Windows
    /// </summary>
    public class MidiPlugin
    {
        #region Instantiation
        private static MidiPlugin instance;
        private static readonly object LockObject = new object();

        private Thread thread;
        private bool isThreadRunning;

        /// <summary>
        /// Get an instance
        /// </summary>
        public static MidiPlugin Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                lock (LockObject)
                {
                    instance = new MidiPlugin();
                }

                return instance;
            }
        }

        private MidiPlugin()
        {
            Start();
        }

        void UpdateMidiInPortList()
        {
            var inDevs = Win32API.midiInGetNumDevs();
            lock (inPorts)
            {
                var originalInPortKeys = inPorts.Keys.ToList().AsReadOnly();
                var updatedInPortKeys = new HashSet<string>();
                for (uint deviceId = 0; deviceId < inDevs; deviceId++)
                {
                    Win32API.midiInGetDevCaps((UIntPtr)deviceId, out var caps);
                    var deviceIdentifier = $"M{caps.wMid}-P{caps.wPid}-I{deviceId}";

                    // Apply attached devices
                    updatedInPortKeys.Add(deviceIdentifier);
                    if (!inPorts.ContainsKey(deviceIdentifier))
                    {
                        var midiInPort = new MidiInPort((UIntPtr)deviceId, deviceIdentifier, caps);
                        midiInPort.MessageReceived += InPortMessageReceived;
                        inPorts[deviceIdentifier] = midiInPort;
                        OnMidiInputDeviceAttached?.Invoke(deviceIdentifier);
                    }
                }

                // Apply detached devices
                foreach (var inPortKey in originalInPortKeys)
                {
                    if (!updatedInPortKeys.Contains(inPortKey))
                    {
                        inPorts[inPortKey].Close();
                        inPorts[inPortKey].MessageReceived -= InPortMessageReceived;
                        inPorts.Remove(inPortKey);
                        OnMidiInputDeviceDetached?.Invoke(inPortKey);
                    }
                }
            }
        }

        void UpdateMidiOutPortList()
        {
            var outDevs = Win32API.midiOutGetNumDevs();
            lock (outPorts)
            {
                var originalOutPortKeys = outPorts.Keys.ToList().AsReadOnly();
                var updatedOutPortKeys = new HashSet<string>();
                for (uint deviceId = 0; deviceId < outDevs; deviceId++)
                {
                    Win32API.midiOutGetDevCaps((UIntPtr)deviceId, out var caps);
                    var deviceIdentifier = $"M{caps.wMid}-P{caps.wPid}-O{deviceId}";

                    // Apply attached devices
                    updatedOutPortKeys.Add(deviceIdentifier);
                    if (!outPorts.ContainsKey(deviceIdentifier))
                    {
                        outPorts[deviceIdentifier] = new MidiOutPort((UIntPtr)deviceId, caps);
                        OnMidiOutputDeviceAttached?.Invoke(deviceIdentifier);
                    }
                }

                // Apply detached devices
                foreach (var outPortKey in originalOutPortKeys)
                {
                    if (!updatedOutPortKeys.Contains(outPortKey))
                    {
                        outPorts[outPortKey].Close();
                        outPorts.Remove(outPortKey);
                        OnMidiOutputDeviceDetached?.Invoke(outPortKey);
                    }
                }
            }
        }

        ~MidiPlugin()
        {
            Stop();
        }

        public void Start()
        {
            if (isThreadRunning)
            {
                return;
            }

            thread = new Thread(() =>
            {
                isThreadRunning = true;
                while (isThreadRunning)
                {
                    UpdateMidiInPortList();
                    UpdateMidiOutPortList();
                    Thread.Sleep(100);
                }
                
                // Thread has stopped
                lock (inPorts)
                {
                    foreach (var midiInPort in inPorts.Values)
                    {
                        midiInPort.Close();
                        midiInPort.MessageReceived -= InPortMessageReceived;
                    }

                    inPorts.Clear();
                }

                lock (outPorts)
                {
                    foreach (var midiOutPort in outPorts.Values)
                    {
                        midiOutPort.Close();
                    }

                    outPorts.Clear();
                }
            });
            thread.Start();
        }

        public void Stop()
        {
            isThreadRunning = false;
        }

        #endregion

        #region MidiDeviceConnection

        private readonly Dictionary<string, MidiInPort> inPorts = new Dictionary<string, MidiInPort>();
        private readonly Dictionary<string, IMidiOutPort> outPorts = new Dictionary<string, IMidiOutPort>();

        public event OnMidiInputDeviceAttachedHandler OnMidiInputDeviceAttached;
        public event OnMidiInputDeviceDetachedHandler OnMidiInputDeviceDetached;
        public event OnMidiOutputDeviceAttachedHandler OnMidiOutputDeviceAttached;
        public event OnMidiOutputDeviceDetachedHandler OnMidiOutputDeviceDetached;

        /// <summary>
        /// Get the device name from specified device ID.
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        /// <returns>the device name, empty if device not connected.</returns>
        public string GetDeviceName(string deviceId)
        {
            lock (inPorts)
            {
                if (inPorts.ContainsKey(deviceId))
                {
                    return inPorts[deviceId].Name;
                }
            }

            lock (outPorts)
            {
                if (outPorts.ContainsKey(deviceId))
                {
                    return outPorts[deviceId].Name;
                }
            }

            return string.Empty;
        }
        #endregion

        #region MidiEventReceiving
        public event OnMidiNoteOnHandler OnMidiNoteOn;
        public event OnMidiNoteOffHandler OnMidiNoteOff;
        public event OnMidiPolyphonicKeyPressureHandler OnMidiPolyphonicKeyPressure;
        public event OnMidiControlChangeHandler OnMidiControlChange;
        public event OnMidiProgramChangeHandler OnMidiProgramChange;
        public event OnMidiChannelPressureHandler OnMidiChannelPressure;
        public event OnMidiPitchBendChangeHandler OnMidiPitchBendChange;
        public event OnMidiSystemExclusiveHandler OnMidiSystemExclusive;
        public event OnMidiTimeCodeHandler OnMidiTimeCode;
        public event OnMidiSongPositionPointerHandler OnMidiSongPositionPointer;
        public event OnMidiSongSelectHandler OnMidiSongSelect;
        public event OnMidiTuneRequestHandler OnMidiTuneRequest;
        public event OnMidiTimingClockHandler OnMidiTimingClock;
        public event OnMidiStartHandler OnMidiStart;
        public event OnMidiContinueHandler OnMidiContinue;
        public event OnMidiStopHandler OnMidiStop;
        public event OnMidiActiveSensingHandler OnMidiActiveSensing;
        public event OnMidiSystemResetHandler OnMidiSystemReset;

        private void InPortMessageReceived(MidiInPort sender, IMidiMessage receivedMidiMessage)
        {
            switch (receivedMidiMessage.Type)
            {
                case MidiMessageType.NoteOff:
                    {
                        var noteOff = (MidiNoteOffMessage)receivedMidiMessage;
                        OnMidiNoteOff?.Invoke(sender.DeviceId, noteOff.Channel, noteOff.Note, noteOff.Velocity);
                    }
                    break;
                case MidiMessageType.NoteOn:
                    {
                        var noteOn = (MidiNoteOnMessage)receivedMidiMessage;
                        OnMidiNoteOn?.Invoke(sender.DeviceId, noteOn.Channel, noteOn.Note, noteOn.Velocity);
                    }
                    break;
                case MidiMessageType.PolyphonicKeyPressure:
                    {
                        var polyphonicKeyPressure = (MidiPolyphonicKeyPressureMessage)receivedMidiMessage;
                        OnMidiPolyphonicKeyPressure?.Invoke(sender.DeviceId, polyphonicKeyPressure.Channel, polyphonicKeyPressure.Note, polyphonicKeyPressure.Pressure);
                    }
                    break;
                case MidiMessageType.ControlChange:
                    {
                        var controlChange = (MidiControlChangeMessage)receivedMidiMessage;
                        OnMidiControlChange?.Invoke(sender.DeviceId, controlChange.Channel, controlChange.Controller, controlChange.ControlValue);
                    }
                    break;
                case MidiMessageType.ProgramChange:
                    {
                        var programChange = (MidiProgramChangeMessage)receivedMidiMessage;
                        OnMidiProgramChange?.Invoke(sender.DeviceId, programChange.Channel, programChange.Program);
                    }
                    break;
                case MidiMessageType.ChannelPressure:
                    {
                        var channelPressure = (MidiChannelPressureMessage)receivedMidiMessage;
                        OnMidiChannelPressure?.Invoke(sender.DeviceId, channelPressure.Channel, channelPressure.Pressure);
                    }
                    break;
                case MidiMessageType.PitchBendChange:
                    {
                        var pitchBendChange = (MidiPitchBendChangeMessage)receivedMidiMessage;
                        OnMidiPitchBendChange?.Invoke(sender.DeviceId, pitchBendChange.Channel, pitchBendChange.Bend);
                    }
                    break;
                case MidiMessageType.SystemExclusive:
                    {
                        var systemExclusive = (MidiSystemExclusiveMessage)receivedMidiMessage;
                        OnMidiSystemExclusive?.Invoke(sender.DeviceId, systemExclusive.RawData.ToArray());
                    }
                    break;
                case MidiMessageType.MidiTimeCode:
                    {
                        var midiTimeCode = (MidiTimeCodeMessage)receivedMidiMessage;
                        OnMidiTimeCode?.Invoke(sender.DeviceId, midiTimeCode.FrameType, midiTimeCode.Values);
                    }
                    break;
                case MidiMessageType.SongPositionPointer:
                    {
                        var songPositionPointer = (MidiSongPositionPointerMessage)receivedMidiMessage;
                        OnMidiSongPositionPointer?.Invoke(sender.DeviceId, songPositionPointer.Beats);
                    }
                    break;
                case MidiMessageType.SongSelect:
                    {
                        var songSelect = (MidiSongSelectMessage)receivedMidiMessage;
                        OnMidiSongSelect?.Invoke(sender.DeviceId, songSelect.Song);
                    }
                    break;
                case MidiMessageType.TuneRequest:
                    OnMidiTuneRequest?.Invoke(sender.DeviceId);
                    break;
                case MidiMessageType.TimingClock:
                    OnMidiTimingClock?.Invoke(sender.DeviceId);
                    break;
                case MidiMessageType.Start:
                    OnMidiStart?.Invoke(sender.DeviceId);
                    break;
                case MidiMessageType.Continue:
                    OnMidiContinue?.Invoke(sender.DeviceId);
                    break;
                case MidiMessageType.Stop:
                    OnMidiStop?.Invoke(sender.DeviceId);
                    break;
                case MidiMessageType.ActiveSensing:
                    OnMidiActiveSensing?.Invoke(sender.DeviceId);
                    break;
                case MidiMessageType.SystemReset:
                    OnMidiSystemReset?.Invoke(sender.DeviceId);
                    break;

                case MidiMessageType.EndSystemExclusive:
                case MidiMessageType.None:
                default:
                    break;
            }
        }
        #endregion

        #region MidiEventSending
        /// <summary>
        /// Send a NoteOn message
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        /// <param name="channel">the channel(0-15)</param>
        /// <param name="note">the note number(0-127)</param>
        /// <param name="velocity">the velocity(0-127)</param>
        public void SendMidiNoteOn(string deviceId, byte channel, byte note, byte velocity)
        {
            lock (outPorts)
            {
                if (outPorts.ContainsKey(deviceId))
                {
                    outPorts[deviceId].SendMessage(new MidiNoteOnMessage(channel, note, velocity));
                }
            }
        }

        /// <summary>
        /// Send a NoteOff message
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        /// <param name="channel">the channel(0-15)</param>
        /// <param name="note">the note number(0-127)</param>
        /// <param name="velocity">the velocity(0-127)</param>
        public void SendMidiNoteOff(string deviceId, byte channel, byte note, byte velocity)
        {
            lock (outPorts)
            {
                if (outPorts.ContainsKey(deviceId))
                {
                    outPorts[deviceId].SendMessage(new MidiNoteOffMessage(channel, note, velocity));
                }
            }
        }

        /// <summary>
        /// Send a PolyphonicKeyPressure message
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        /// <param name="channel">the channel(0-15)</param>
        /// <param name="note">the note number(0-127)</param>
        /// <param name="velocity">the velocity(0-127)</param>
        public void SendMidiPolyphonicKeyPressure(string deviceId, byte channel, byte note, byte velocity)
        {
            lock (outPorts)
            {
                if (outPorts.ContainsKey(deviceId))
                {
                    outPorts[deviceId].SendMessage(new MidiPolyphonicKeyPressureMessage(channel, note, velocity));
                }
            }
        }

        /// <summary>
        /// Send a ControlChange message
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        /// <param name="channel">the channel(0-15)</param>
        /// <param name="controller">the controller(0-127)</param>
        /// <param name="controllerValue">the value(0-127)</param>
        public void SendMidiControlChange(string deviceId, byte channel, byte controller, byte controllerValue)
        {
            lock (outPorts)
            {
                if (outPorts.ContainsKey(deviceId))
                {
                    outPorts[deviceId].SendMessage(new MidiControlChangeMessage(channel, controller, controllerValue));
                }
            }
        }

        /// <summary>
        /// Send a ProgramChange message
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        /// <param name="channel">the channel(0-15)</param>
        /// <param name="program">the program(0-127)</param>
        public void SendMidiProgramChange(string deviceId, byte channel, byte program)
        {
            lock (outPorts)
            {
                if (outPorts.ContainsKey(deviceId))
                {
                    outPorts[deviceId].SendMessage(new MidiProgramChangeMessage(channel, program));
                }
            }
        }

        /// <summary>
        /// Send a ChannelPressure message
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        /// <param name="channel">the channel(0-15)</param>
        /// <param name="pressure">the pressure(0-127)</param>
        public void SendMidiChannelPressure(string deviceId, byte channel, byte pressure)
        {
            lock (outPorts)
            {
                if (outPorts.ContainsKey(deviceId))
                {
                    outPorts[deviceId].SendMessage(new MidiChannelPressureMessage(channel, pressure));
                }
            }
        }

        /// <summary>
        /// Send a PitchBendChange message
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        /// <param name="channel">the channel(0-15)</param>
        /// <param name="bend">the pitch bend value(0-16383)</param>
        public void SendMidiPitchBendChange(string deviceId, byte channel, ushort bend)
        {
            lock (outPorts)
            {
                if (outPorts.ContainsKey(deviceId))
                {
                    outPorts[deviceId].SendMessage(new MidiPitchBendChangeMessage(channel, bend));
                }
            }
        }

        /// <summary>
        /// Send a SystemExclusive message
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        /// <param name="systemExclusive">the system exclusive data</param>
        public void SendMidiSystemExclusive(string deviceId, [ReadOnlyArray] byte[] systemExclusive)
        {
            lock (outPorts)
            {
                if (outPorts.ContainsKey(deviceId))
                {
                    outPorts[deviceId].SendMessage(new MidiSystemExclusiveMessage(new MemoryStream(systemExclusive)));
                }
            }
        }

        /// <summary>
        /// Send a TimeCode message
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        /// <param name="frameType">the frame type(0-7)</param>
        /// <param name="values">the time code(0-15)</param>
        public void SendMidiTimeCode(string deviceId, byte frameType, byte values)
        {
            lock (outPorts)
            {
                if (outPorts.ContainsKey(deviceId))
                {
                    outPorts[deviceId].SendMessage(new MidiTimeCodeMessage(frameType, values));
                }
            }
        }

        /// <summary>
        /// Send a SongPositionPointer message
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        /// <param name="beats">the song position pointer(0-16383)</param>
        public void SendMidiSongPositionPointer(string deviceId, ushort beats)
        {
            lock (outPorts)
            {
                if (outPorts.ContainsKey(deviceId))
                {
                    outPorts[deviceId].SendMessage(new MidiSongPositionPointerMessage(beats));
                }
            }
        }

        /// <summary>
        /// Send a SongSelect message
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        /// <param name="song"></param>
        public void SendMidiSongSelect(string deviceId, byte song)
        {
            lock (outPorts)
            {
                if (outPorts.ContainsKey(deviceId))
                {
                    outPorts[deviceId].SendMessage(new MidiSongSelectMessage(song));
                }
            }
        }

        /// <summary>
        /// Send a TuneRequest message
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        public void SendMidiTuneRequest(string deviceId)
        {
            lock (outPorts)
            {
                if (outPorts.ContainsKey(deviceId))
                {
                    outPorts[deviceId].SendMessage(new MidiTuneRequestMessage());
                }
            }
        }

        /// <summary>
        /// Send a TimingClock message
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        public void SendMidiTimingClock(string deviceId)
        {
            lock (outPorts)
            {
                if (outPorts.ContainsKey(deviceId))
                {
                    outPorts[deviceId].SendMessage(new MidiTimingClockMessage());
                }
            }
        }

        /// <summary>
        /// Send a Start message
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        public void SendMidiStart(string deviceId)
        {
            lock (outPorts)
            {
                if (outPorts.ContainsKey(deviceId))
                {
                    outPorts[deviceId].SendMessage(new MidiStartMessage());
                }
            }
        }

        /// <summary>
        /// Send a Continue message
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        public void SendMidiContinue(string deviceId)
        {
            lock (outPorts)
            {
                if (outPorts.ContainsKey(deviceId))
                {
                    outPorts[deviceId].SendMessage(new MidiContinueMessage());
                }
            }
        }

        /// <summary>
        /// Send a Stop message
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        public void SendMidiStop(string deviceId)
        {
            lock (outPorts)
            {
                if (outPorts.ContainsKey(deviceId))
                {
                    outPorts[deviceId].SendMessage(new MidiStopMessage());
                }
            }
        }

        /// <summary>
        /// Send an ActiveSensing message
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        public void SendMidiActiveSensing(string deviceId)
        {
            lock (outPorts)
            {
                if (outPorts.ContainsKey(deviceId))
                {
                    outPorts[deviceId].SendMessage(new MidiActiveSensingMessage());
                }
            }
        }

        /// <summary>
        /// Send a SystemReset message
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        public void SendMidiSystemReset(string deviceId)
        {
            lock (outPorts)
            {
                if (outPorts.ContainsKey(deviceId))
                {
                    outPorts[deviceId].SendMessage(new MidiSystemResetMessage());
                }
            }
        }
        #endregion
    }
}