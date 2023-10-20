LibMIDIPlugin = {
    $Data: {
        midiAccess: null,
        attachedDevices: [],

        ParseMidi: function (message) {
            if (message.data == null || message.data.length < 1) {
                return;
            }
            switch (message.data[0] & 0xf0) {
                case 0x80: // Note Off
                    if (message.data.length >= 3) {
                        unityInstance.SendMessage('MidiManager', 'OnMidiNoteOff', '' + message.target.id + ',0,' + (message.data[0] & 0xf) + ',' + message.data[1] + ',' + message.data[2]);
                    }
                break;
                case 0x90: // Note On
                    if (message.data.length >= 3) {
                        unityInstance.SendMessage('MidiManager', 'OnMidiNoteOn', '' + message.target.id + ',0,' + (message.data[0] & 0xf) + ',' + message.data[1] + ',' + message.data[2]);
                    }
                break;
                case 0xa0: // Polyphonic Aftertouch
                    if (message.data.length >= 3) {
                        unityInstance.SendMessage('MidiManager', 'OnMidiPolyphonicAftertouch', '' + message.target.id + ',0,' + (message.data[0] & 0xf) + ',' + message.data[1] + ',' + message.data[2]);
                    }
                break;
                case 0xb0: // Control Change
                    if (message.data.length >= 3) {
                        unityInstance.SendMessage('MidiManager', 'OnMidiControlChange', '' + message.target.id + ',0,' + (message.data[0] & 0xf) + ',' + message.data[1] + ',' + message.data[2]);
                    }
                break;
                case 0xc0: // Program Change
                    if (message.data.length >= 2) {
                        unityInstance.SendMessage('MidiManager', 'OnMidiProgramChange', '' + message.target.id + ',0,' + (message.data[0] & 0xf) + ',' + message.data[1]);
                    }
                break;
                case 0xd0: // Channel Aftertouch
                    if (message.data.length >= 2) {
                        unityInstance.SendMessage('MidiManager', 'OnMidiChannelAftertouch', '' + message.target.id + ',0,' + (message.data[0] & 0xf) + ',' + message.data[1]);
                    }
                break;
                case 0xe0: // Pitch Wheel
                    if (message.data.length >= 3) {
                        unityInstance.SendMessage('MidiManager', 'OnMidiPitchWheel', '' + message.target.id + ',0,' + (message.data[0] & 0xf) + ',' + (message.data[1] | (message.data[2] << 7)));
                    }
                break;
                case 0xf0: // System
                    switch (message.data[0]) {
                        case 0xf0: // Sysex
                            unityInstance.SendMessage('MidiManager', 'OnMidiSystemExclusive', '' + message.target.id + ',0,' + message.data.join(','));
                        break;
                        case 0xf1: // Time Code Quarter Frame
                            if (message.data.length >= 2) {
                                unityInstance.SendMessage('MidiManager', 'OnMidiTimeCodeQuarterFrame', '' + message.target.id + ',0,' + message.data[1]);
                            }
                        break;
                        case 0xf2: // Song Position Pointer
                            if (message.data.length >= 3) {
                                unityInstance.SendMessage('MidiManager', 'OnMidiSongPositionPointer', '' + message.target.id + ',0,' + (message.data[1] | (message.data[2] << 7)));
                            }
                        break;
                        case 0xf3: // Song Select
                            if (message.data.length >= 2) {
                                unityInstance.SendMessage('MidiManager', 'OnMidiSongSelect', '' + message.target.id + ',0,' + message.data[1]);
                            }
                        break;
                        case 0xf6: // Tune Request
                            unityInstance.SendMessage('MidiManager', 'OnMidiTuneRequest', '' + message.target.id + ',0');
                        break;
                        case 0xf8: // Timing Clock
                            unityInstance.SendMessage('MidiManager', 'OnMidiTimingClock', '' + message.target.id + ',0');
                        break;
                        case 0xfa: // Start
                            unityInstance.SendMessage('MidiManager', 'OnMidiStart', '' + message.target.id + ',0');
                        break;
                        case 0xfb: // Continue
                            unityInstance.SendMessage('MidiManager', 'OnMidiContinue', '' + message.target.id + ',0');
                        break;
                        case 0xfc: // Stop
                            unityInstance.SendMessage('MidiManager', 'OnMidiStop', '' + message.target.id + ',0');
                        break;
                        case 0xfe: // Active Sensing
                            unityInstance.SendMessage('MidiManager', 'OnMidiActiveSensing', '' + message.target.id + ',0');
                        break;
                        case 0xff: // Reset
                            unityInstance.SendMessage('MidiManager', 'OnMidiReset', '' + message.target.id + ',0');
                        break;
                    }
                break;
            }
        },

        OnMIDISuccess: function (midiAccess) {
            Data.midiAccess = midiAccess;

            midiAccess.onstatechange = function (event) {
                if (event.port.state == 'disconnected') {
                    if (Data.attachedDevices.includes(event.port.id)) {
                        Data.attachedDevices.splice(Data.attachedDevices.indexOf(event.port.id), 1);
                        if (event.port.type == 'input') {
                            unityInstance.SendMessage('MidiManager', 'OnMidiInputDeviceDetached', event.port.id);
                        } else if (event.port.type == 'output') {
                            unityInstance.SendMessage('MidiManager', 'OnMidiOutputDeviceDetached', event.port.id);
                        }
                        event.port.close();
                    }
                } else if (event.port.state == 'connected') {
                    if (Data.attachedDevices.includes(event.port.id)) {
                        return;
                    }
                    Data.attachedDevices.push(event.port.id);
                    if (event.port.type == 'input') {
                        event.port.onmidimessage = function (message) {
                            // incoming events
                            Data.ParseMidi(message);
                        };
                        event.port.open();
                        unityInstance.SendMessage('MidiManager', 'OnMidiInputDeviceAttached', event.port.id);
                    } else if (event.port.type == 'output') {
                        event.port.open();
                        unityInstance.SendMessage('MidiManager', 'OnMidiOutputDeviceAttached', event.port.id);
                    }
                }
            };

            Data.inputs = midiAccess.inputs;
            Data.inputs.forEach(function(input, key) {
                if (Data.attachedDevices.includes(input.id)) {
                    return;
                }
                Data.attachedDevices.push(input.id);
                input.onmidimessage = function (message) {
                    // incoming events
                    Data.ParseMidi(message);
                };
                input.open();
                unityInstance.SendMessage('MidiManager', 'OnMidiInputDeviceAttached', input.id);
            });

            Data.outputs = midiAccess.outputs;
            Data.outputs.forEach(function(output, key) {
                if (Data.attachedDevices.includes(output.id)) {
                    return;
                }
                Data.attachedDevices.push(output.id);
                output.open();
                unityInstance.SendMessage('MidiManager', 'OnMidiOutputDeviceAttached', output.id);
            });
        },
        
        OnMIDIFailure: function (msg) {
            console.error("Failed to get MIDI access - " + msg);
        },
    },

    midiPluginInitialize: function () {
        navigator.requestMIDIAccess({
            sysex: true,
            software: true
        }).then(Data.OnMIDISuccess, Data.OnMIDIFailure);
    },

    getDeviceName: function(deviceIdStr) {
        var deviceId = UTF8ToString(deviceIdStr);
        var deviceName = null;
        {
            var device = Data.midiAccess.inputs.get(deviceId);
            if (device != null) {
                deviceName = device.name;
            }
        }

        if (deviceName == null)
        {
            var device = Data.midiAccess.outputs.get(deviceId);
            if (device != null) {
                deviceName = device.name;
            }
        }
        if (deviceName == null) {
            return null;
        }

        var bufferSize = lengthBytesUTF8(deviceName) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(deviceName, buffer, bufferSize);
        return buffer;
    },

    sendMidiNoteOff: function(deviceIdStr, channel, note, velocity) {
        var deviceId = UTF8ToString(deviceIdStr);
        var device = Data.midiAccess.outputs.get(deviceId);
        if (device != null) {
            device.send([0x80 | channel, note, velocity]);
        }
    },

    sendMidiNoteOn: function(deviceIdStr, channel, note, velocity) {
        var deviceId = UTF8ToString(deviceIdStr);
        var device = Data.midiAccess.outputs.get(deviceId);
        if (device != null) {
            device.send([0x90 | channel, note, velocity]);
        }
    },
    
    sendMidiPolyphonicAftertouch: function(deviceIdStr, channel, note, pressure) {
        var deviceId = UTF8ToString(deviceIdStr);
        var device = Data.midiAccess.outputs.get(deviceId);
        if (device != null) {
            device.send([0xa0 | channel, note, pressure]);
        }
    },

    sendMidiControlChange: function(deviceIdStr, channel, func, value) {
        var deviceId = UTF8ToString(deviceIdStr);
        var device = Data.midiAccess.outputs.get(deviceId);
        if (device != null) {
            device.send([0xb0 | channel, func, value]);
        }
    },

    sendMidiProgramChange: function(deviceIdStr, channel, program) {
        var deviceId = UTF8ToString(deviceIdStr);
        var device = Data.midiAccess.outputs.get(deviceId);
        if (device != null) {
            device.send([0xc0 | channel, program]);
        }
    },

    sendMidiChannelAftertouch: function(deviceIdStr, channel, pressure) {
        var deviceId = UTF8ToString(deviceIdStr);
        var device = Data.midiAccess.outputs.get(deviceId);
        if (device != null) {
            device.send([0xd0 | channel, pressure]);
        }
    },

    sendMidiPitchWheel: function(deviceIdStr, channel, amount) {
        var deviceId = UTF8ToString(deviceIdStr);
        var device = Data.midiAccess.outputs.get(deviceId);
        if (device != null) {
            device.send([0xe0 | channel, amount & 0x7f, (amount >> 7) & 0x7f]);
        }
    },

    sendMidiSystemExclusive: function(deviceIdStr, data) {
        var deviceId = UTF8ToString(deviceIdStr);
        var device = Data.midiAccess.outputs.get(deviceId);
        if (device != null) {
            device.send(data);
        }
    },

    sendMidiTimeCodeQuarterFrame: function(deviceIdStr, value) {
        var deviceId = UTF8ToString(deviceIdStr);
        var device = Data.midiAccess.outputs.get(deviceId);
        if (device != null) {
            device.send([0xf1, value]);
        }
    },

    sendMidiSongPositionPointer: function(deviceIdStr, position) {
        var deviceId = UTF8ToString(deviceIdStr);
        var device = Data.midiAccess.outputs.get(deviceId);
        if (device != null) {
            device.send([0xf2, position & 0x7f, (position >> 7) & 0x7f]);
        }
    },

    sendMidiSongSelect: function(deviceIdStr, song) {
        var deviceId = UTF8ToString(deviceIdStr);
        var device = Data.midiAccess.outputs.get(deviceId);
        if (device != null) {
            device.send([0xf3, song]);
        }
    },

    sendMidiTuneRequest: function(deviceIdStr) {
        var deviceId = UTF8ToString(deviceIdStr);
        var device = Data.midiAccess.outputs.get(deviceId);
        if (device != null) {
            device.send([0xf6]);
        }
    },

    sendMidiTimingClock: function(deviceIdStr) {
        var deviceId = UTF8ToString(deviceIdStr);
        var device = Data.midiAccess.outputs.get(deviceId);
        if (device != null) {
            device.send([0xf8]);
        }
    },

    sendMidiStart: function(deviceIdStr) {
        var deviceId = UTF8ToString(deviceIdStr);
        var device = Data.midiAccess.outputs.get(deviceId);
        if (device != null) {
            device.send([0xfa]);
        }
    },

    sendMidiContinue: function(deviceIdStr) {
        var deviceId = UTF8ToString(deviceIdStr);
        var device = Data.midiAccess.outputs.get(deviceId);
        if (device != null) {
            device.send([0xfb]);
        }
    },

    sendMidiStop: function(deviceIdStr) {
        var deviceId = UTF8ToString(deviceIdStr);
        var device = Data.midiAccess.outputs.get(deviceId);
        if (device != null) {
            device.send([0xfc]);
        }
    },

    sendMidiActiveSensing: function(deviceIdStr) {
        var deviceId = UTF8ToString(deviceIdStr);
        var device = Data.midiAccess.outputs.get(deviceId);
        if (device != null) {
            device.send([0xfe]);
        }
    },

    sendMidiReset: function(deviceIdStr) {
        var deviceId = UTF8ToString(deviceIdStr);
        var device = Data.midiAccess.outputs.get(deviceId);
        if (device != null) {
            device.send([0xff]);
        }
    }
};

autoAddDeps(LibMIDIPlugin, '$Data');
mergeInto(LibraryManager.library, LibMIDIPlugin);
