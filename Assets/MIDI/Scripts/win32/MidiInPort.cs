using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace jp.kshoji.unity.midi.win32
{
    internal class MidiInPort
    {
        private bool isOpen;
        private bool isClosing;
        private readonly Win32API.HMIDIIN handle;
        private readonly Win32API.MidiInProc inputCallbackDelegate;
        private IntPtr longBuffer;

        public delegate void MidiMessageHandler(MidiInPort sender, IMidiMessage message);

        public event MidiMessageHandler MessageReceived;
        public MidiInPort(UIntPtr deviceId, string deviceIdentifier, Win32API.MIDIINCAPS caps)
        {
            lock (this)
            {
                Name = caps.szPname;
                DeviceId = deviceIdentifier;
                inputCallbackDelegate = InputCallback;

                if (isOpen)
                {
                    return;
                }

                var rc = Win32API.midiInOpen(out handle, deviceId, inputCallbackDelegate, (UIntPtr)0);
                if (rc != Win32API.MMRESULT.MMSYSERR_NOERROR)
                {
                    var errorMsg = new StringBuilder(128);
                    rc = Win32API.midiInGetErrorText(rc, errorMsg);
                    if (rc != Win32API.MMRESULT.MMSYSERR_NOERROR)
                    {
                        throw new ApplicationException("no error details");
                    }
                    throw new ApplicationException(errorMsg.ToString());
                }

                Win32API.midiInStart(handle);
                longBuffer = CreateLongMsgBuffer();

                isClosing = false;
                isOpen = true;
            }
        }

        public void Close()
        {
            if (!isOpen)
            {
                return;
            }

            isClosing = true;
            DestroyLongMsgBuffer(longBuffer);
            longBuffer = IntPtr.Zero;

            Win32API.midiInStop(handle);
            Win32API.midiInReset(handle);
            Win32API.midiInClose(handle);
            isOpen = false;
        }

        #region Buffers

        private IntPtr CreateLongMsgBuffer()
        {
            //add a buffer so we can receive SysEx messages
            IntPtr ptr;
            var size = (UInt32)Marshal.SizeOf(typeof(Win32API.MIDIHDR));
            var header = new Win32API.MIDIHDR
            {
                lpData = Marshal.AllocHGlobal(4096),
                dwBufferLength = 4096,
                dwFlags = 0
            };

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

            Win32API.midiInPrepareHeader(handle, ptr, size);
            Win32API.midiInAddBuffer(handle, ptr, size);

            return ptr;
        }

        private void DestroyLongMsgBuffer(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return;
            }

            var size = (UInt32)Marshal.SizeOf(typeof(Win32API.MIDIHDR));
            Win32API.midiInUnprepareHeader(handle, ptr, size);
            
            var header = (Win32API.MIDIHDR)Marshal.PtrToStructure(ptr, typeof(Win32API.MIDIHDR));
            Marshal.FreeHGlobal(header.lpData);
            Marshal.FreeHGlobal(ptr);
        }        

        #endregion
        
        private void InputCallback(Win32API.HMIDIIN hMidiIn, Win32API.MidiInMessage wMsg,
            UIntPtr dwInstance, UIntPtr dwParam1, UIntPtr dwParam2)
        {
            if (wMsg == Win32API.MidiInMessage.MIM_DATA)
            {
                switch ((int)dwParam1 & 0xf0)
                {
                    case 0x80:
                        MessageReceived?.Invoke(this, new MidiNoteOffMessage((byte)((int)dwParam1 & 0xf), (byte)(((int)dwParam1 & 0xff00) >> 8), (byte)(((int)dwParam1 & 0xff0000) >> 16)));
                        break;
                    case 0x90:
                        MessageReceived?.Invoke(this, new MidiNoteOnMessage((byte)((int)dwParam1 & 0xf), (byte)(((int)dwParam1 & 0xff00) >> 8), (byte)(((int)dwParam1 & 0xff0000) >> 16)));
                        break;
                    case 0xa0:
                        MessageReceived?.Invoke(this, new MidiPolyphonicKeyPressureMessage((byte)((int)dwParam1 & 0xf), (byte)(((int)dwParam1 & 0xff00) >> 8), (byte)(((int)dwParam1 & 0xff0000) >> 16)));
                        break;
                    case 0xb0:
                        MessageReceived?.Invoke(this, new MidiControlChangeMessage((byte)((int)dwParam1 & 0xf), (byte)(((int)dwParam1 & 0xff00) >> 8), (byte)(((int)dwParam1 & 0xff0000) >> 16)));
                        break;
                    case 0xc0:
                        MessageReceived?.Invoke(this, new MidiProgramChangeMessage((byte)((int)dwParam1 & 0xf), (byte)(((int)dwParam1 & 0xff00) >> 8)));
                        break;
                    case 0xd0:
                        MessageReceived?.Invoke(this, new MidiChannelPressureMessage((byte)((int)dwParam1 & 0xf), (byte)(((int)dwParam1 & 0xff00) >> 8)));
                        break;
                    case 0xe0:
                        MessageReceived?.Invoke(this, new MidiPitchBendChangeMessage((byte)((int)dwParam1 & 0xf), (ushort)((((int)dwParam1 >> 9) & 0x3f80) | (((int)dwParam1 >> 8) & 0x7f))));
                        break;
                    case 0xf0:
                        switch ((int)dwParam1 & 0xff)
                        {
                            case 0xf1:
                                MessageReceived?.Invoke(this, new MidiTimeCodeMessage((byte)(((int)dwParam1 >> 4) & 0x7), (byte)((int)dwParam1 & 0xf)));
                                break;
                            case 0xf2:
                                MessageReceived?.Invoke(this, new MidiSongPositionPointerMessage((ushort)((int)dwParam1 & 0x3fff)));
                                break;
                            case 0xf3:
                                MessageReceived?.Invoke(this, new MidiSongSelectMessage((byte)((int)dwParam1 & 0x7f)));
                                break;
                            case 0xf6:
                                MessageReceived?.Invoke(this, new MidiTuneRequestMessage());
                                break;
                            case 0xf8:
                                MessageReceived?.Invoke(this, new MidiTimingClockMessage());
                                break;
                            case 0xfa:
                                MessageReceived?.Invoke(this, new MidiStartMessage());
                                break;
                            case 0xfb:
                                MessageReceived?.Invoke(this, new MidiContinueMessage());
                                break;
                            case 0xfc:
                                MessageReceived?.Invoke(this, new MidiStopMessage());
                                break;
                            case 0xfe:
                                MessageReceived?.Invoke(this, new MidiActiveSensingMessage());
                                break;
                            case 0xff:
                                MessageReceived?.Invoke(this, new MidiSystemResetMessage());
                                break;
                        }
                        break;
                }
            }
            else if (wMsg == Win32API.MidiInMessage.MIM_LONGDATA)
            {
                var newPtr = unchecked((IntPtr)(long)(ulong)dwParam1);
                var header = (Win32API.MIDIHDR)Marshal.PtrToStructure(newPtr, typeof(Win32API.MIDIHDR));
                var data = new byte[header.dwBytesRecorded];
                try
                {
                    for (var i = 0; i < header.dwBytesRecorded; i++)
                    {
                        data[i] = Marshal.ReadByte(header.lpData, i);
                    }
                    MessageReceived?.Invoke(this, new MidiSystemExclusiveMessage(new MemoryStream(data)));

                    // prepare next buffer
                    DestroyLongMsgBuffer(longBuffer);
                    longBuffer = IntPtr.Zero;
                    if (!isClosing)
                    {
                        longBuffer = CreateLongMsgBuffer();
                    }
                }
                catch (NullReferenceException)
                {
                    
                }
            }
            else if (wMsg == Win32API.MidiInMessage.MIM_OPEN || wMsg == Win32API.MidiInMessage.MIM_CLOSE)
            {
                // ignore these messages
            }
            else
            {
                Debug.Log($"Invalid MessageType: {wMsg}");
            }
        }

        public string DeviceId { get; }
        public string Name { get; }
    }
}