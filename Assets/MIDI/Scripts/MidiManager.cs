using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using jp.kshoji.midisystem;
using jp.kshoji.rtpmidi;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.EventSystems;

#if !UNITY_WEBGL || UNITY_EDITOR
using System.ComponentModel;
using AsyncOperation = System.ComponentModel.AsyncOperation;
#endif

#if UNITY_WSA && !UNITY_EDITOR
using jp.kshoji.unity.midi.uwp;
#endif

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using jp.kshoji.unity.midi.win32;
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
using System.Text;
using System.Threading;
#endif

#if UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_WEBGL || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
using System.Runtime.InteropServices;
#endif

namespace jp.kshoji.unity.midi
{
    /// <summary>
    /// MIDI Manager, will be registered as `DontDestroyOnLoad` GameObject
    /// </summary>
    public class MidiManager : MonoBehaviour
#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
        , IRtpMidiDeviceConnectionListener
#endif
    {
        private readonly HashSet<string> deviceIdSet = new HashSet<string>();

        /// <summary>
        /// the set of MIDI device ID string.
        /// </summary>
        public HashSet<string> DeviceIdSet
        {
            get
            {
                lock (deviceIdSet)
                {
                    return new HashSet<string>(deviceIdSet);
                }
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static Thread mainThread;
        private AndroidJavaObject usbMidiPlugin;
        private AndroidJavaObject bleMidiPlugin;
#endif
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void midiPluginInitialize();

        [DllImport("__Internal")]
        private static extern void midiPluginTerminate();

        [DllImport("__Internal")]
        private static extern void sendMidiData(string deviceId, byte[] byteArray, int length);

        [DllImport("__Internal")]
        private static extern void startScanBluetoothMidiDevices();

        [DllImport("__Internal")]
        private static extern void stopScanBluetoothMidiDevices();

        [DllImport("__Internal")]
        private static extern string getDeviceName(string deviceId);
#endif

#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        public delegate void OnSendMessageDelegate(string method, string message);

        [DllImport("MIDIPlugin")]
        private static extern void SetSendMessageCallback(OnSendMessageDelegate callback);

        [AOT.MonoPInvokeCallback(typeof(OnSendMessageDelegate))]
        private static void LinuxOnSendMessageDelegate(string method, string message) =>
            Instance.asyncOperation.Post(o => Instance.gameObject.SendMessage((string)((object[])o)[0], (string)((object[])o)[1]), new object[] {method, message});

        [DllImport("MIDIPlugin")]
        private static extern void InitializeMidiLinux();
        [DllImport("MIDIPlugin")]
        private static extern void TerminateMidiLinux();

        [DllImport("MIDIPlugin")]
        private static extern string GetDeviceNameLinux(string deviceId);

        [DllImport("MIDIPlugin")]
        private static extern void SendMidiNoteOff(string deviceId, byte channel, byte note, byte velocity);
        [DllImport("MIDIPlugin")]
        private static extern void SendMidiNoteOn(string deviceId, byte channel, byte note, byte velocity);
        [DllImport("MIDIPlugin")]
        private static extern void SendMidiPolyphonicAftertouch(string deviceId, byte channel, byte note, byte pressure);
        [DllImport("MIDIPlugin")]
        private static extern void SendMidiControlChange(string deviceId, byte channel, byte func, byte value);
        [DllImport("MIDIPlugin")]
        private static extern void SendMidiProgramChange(string deviceId, byte channel, byte program);
        [DllImport("MIDIPlugin")]
        private static extern void SendMidiChannelAftertouch(string deviceId, byte channel, byte pressure);
        [DllImport("MIDIPlugin")]
        private static extern void SendMidiPitchWheel(string deviceId, byte channel, short amount);
        [DllImport("MIDIPlugin")]
        private static extern void SendMidiSystemExclusive(string deviceId, byte[] data, int length);
        [DllImport("MIDIPlugin")]
        private static extern void SendMidiTimeCodeQuarterFrame(string deviceId, byte value);
        [DllImport("MIDIPlugin")]
        private static extern void SendMidiSongPositionPointer(string deviceId, short position);
        [DllImport("MIDIPlugin")]
        private static extern void SendMidiSongSelect(string deviceId, byte song);
        [DllImport("MIDIPlugin")]
        private static extern void SendMidiTuneRequest(string deviceId);
        [DllImport("MIDIPlugin")]
        private static extern void SendMidiTimingClock(string deviceId);
        [DllImport("MIDIPlugin")]
        private static extern void SendMidiStart(string deviceId);
        [DllImport("MIDIPlugin")]
        private static extern void SendMidiContinue(string deviceId);
        [DllImport("MIDIPlugin")]
        private static extern void SendMidiStop(string deviceId);
        [DllImport("MIDIPlugin")]
        private static extern void SendMidiActiveSensing(string deviceId);
        [DllImport("MIDIPlugin")]
        private static extern void SendMidiReset(string deviceId);
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void midiPluginInitialize();
    [DllImport("__Internal")]
    private static extern string getDeviceName(string deviceId);
    [DllImport("__Internal")]
    private static extern void sendMidiNoteOff(string deviceId, byte channel, byte note, byte velocity);
    [DllImport("__Internal")]
    private static extern void sendMidiNoteOn(string deviceId, byte channel, byte note, byte velocity);
    [DllImport("__Internal")]
    private static extern void sendMidiPolyphonicAftertouch(string deviceId, byte channel, byte note, byte pressure);
    [DllImport("__Internal")]
    private static extern void sendMidiControlChange(string deviceId, byte channel, byte function, byte value);
    [DllImport("__Internal")]
    private static extern void sendMidiProgramChange(string deviceId, byte channel, byte program);
    [DllImport("__Internal")]
    private static extern void sendMidiChannelAftertouch(string deviceId, byte channel, byte pressure);
    [DllImport("__Internal")]
    private static extern void sendMidiPitchWheel(string deviceId, byte channel, int amount);
    [DllImport("__Internal")]
    private static extern void sendMidiSystemExclusive(string deviceId, byte[] data);
    [DllImport("__Internal")]
    private static extern void sendMidiTimeCodeQuarterFrame(string deviceId, int value);
    [DllImport("__Internal")]
    private static extern void sendMidiSongPositionPointer(string deviceId, int position);
    [DllImport("__Internal")]
    private static extern void sendMidiSongSelect(string deviceId, byte song);
    [DllImport("__Internal")]
    private static extern void sendMidiTuneRequest(string deviceId);
    [DllImport("__Internal")]
    private static extern void sendMidiTimingClock(string deviceId);
    [DllImport("__Internal")]
    private static extern void sendMidiStart(string deviceId);
    [DllImport("__Internal")]
    private static extern void sendMidiContinue(string deviceId);
    [DllImport("__Internal")]
    private static extern void sendMidiStop(string deviceId);
    [DllImport("__Internal")]
    private static extern void sendMidiActiveSensing(string deviceId);
    [DllImport("__Internal")]
    private static extern void sendMidiReset(string deviceId);
#endif

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        [DllImport("MIDIPlugin")]
        private static extern void midiPluginInitialize();

        [DllImport("MIDIPlugin")]
        private static extern void midiPluginTerminate();

        [DllImport("MIDIPlugin")]
        private static extern void midiPluginStartForEditor();

        [DllImport("MIDIPlugin")]
        private static extern void midiPluginStopForEditor();

        [DllImport("MIDIPlugin")]
        private static extern void sendMidiData(string deviceId, byte[] byteArray, int length);

        [DllImport("MIDIPlugin")]
        private static extern string getDeviceName(string deviceId);
#endif

#if !UNITY_WEBGL || UNITY_EDITOR
        private AsyncOperation asyncOperation;
#endif

#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
        private readonly Dictionary<int, RtpMidiServer> rtpMidiServers = new Dictionary<int, RtpMidiServer>();
        private RtpMidiEventHandler rtpMidiEventHandler;
#endif

        /// <summary>
        /// Get an instance<br />
        /// SHOULD be called by Unity's main thread.
        /// </summary>
        public static MidiManager Instance => lazyInstance.Value;

        private static readonly Lazy<MidiManager> lazyInstance = new Lazy<MidiManager>(() =>
        {
            var instance = new GameObject("MidiManager").AddComponent<MidiManager>();

#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
            instance.rtpMidiEventHandler = new RtpMidiEventHandler();
#endif
#if !UNITY_WEBGL || UNITY_EDITOR
            instance.asyncOperation = AsyncOperationManager.CreateOperation(null);
#endif

#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                DontDestroyOnLoad(instance);
            }
            else
            {
                Debug.Log("Don't initialize MidiManager while Unity Editor is not playing!");
            }
#else
            DontDestroyOnLoad(instance);
#endif                

#if UNITY_ANDROID && !UNITY_EDITOR
            mainThread = Thread.CurrentThread;
#endif

            return instance;
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        private MidiManager()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            usbMidiPlugin = new AndroidJavaObject("jp.kshoji.unity.midi.UsbMidiUnityPlugin");
            bleMidiPlugin = new AndroidJavaObject("jp.kshoji.unity.midi.BleMidiUnityPlugin");
#endif
        }

        ~MidiManager()
        {
            TerminateMidi();
        }

#if UNITY_ANDROID && !UNITY_EDITOR
#if !FEATURE_ANDROID_COMPANION_DEVICE
        private const string LocationPermission = "android.permission.ACCESS_FINE_LOCATION";
#endif
        private const string BluetoothPermission = "android.permission.BLUETOOTH";
        private const string BluetoothAdminPermission = "android.permission.BLUETOOTH_ADMIN";

        private const string BluetoothScanPermission = "android.permission.BLUETOOTH_SCAN";
        private const string BluetoothConnectPermission = "android.permission.BLUETOOTH_CONNECT";
        private const string BluetoothAdvertisePermission = "android.permission.BLUETOOTH_ADVERTISE";

        private bool permissionRequested;
        private Action onInitializeCompleted;

        /// <summary>
        /// Check and request BLE MIDI permissions for Android M or later.
        /// If all permissions are granted, this method do nothing.
        /// </summary>
        public void CheckPermission()
        {
            permissionRequested = false;
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            var osVersion = new AndroidJavaClass("android.os.Build$VERSION");
            var osVersionInt = osVersion.GetStatic<int>("SDK_INT");

            if (osVersionInt >= 23)
            {
                // Android M or later
                var requestPermissions = AndroidBluetoothRequiredPermissions(osVersionInt);
                if (requestPermissions.Count > 0)
                {
                    // need asking permission
                    permissionRequested = true;

                    var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    activity.Call("requestPermissions", requestPermissions.ToArray(), 0);
                }
            }
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
        }

        private List<string> AndroidBluetoothRequiredPermissions(int osVersionInt)
        {
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var requestPermissions = new List<string>();

            if (osVersionInt >= 31)
            {
                // Android 12 or later
#if !FEATURE_ANDROID_COMPANION_DEVICE
                if (activity.Call<int>("checkSelfPermission", LocationPermission) != 0)
                {
                    requestPermissions.Add(LocationPermission);
                }
#endif
                if (activity.Call<int>("checkSelfPermission", BluetoothScanPermission) != 0)
                {
                    requestPermissions.Add(BluetoothScanPermission);
                }
                if (activity.Call<int>("checkSelfPermission", BluetoothConnectPermission) != 0)
                {
                    requestPermissions.Add(BluetoothConnectPermission);
                }
                if (activity.Call<int>("checkSelfPermission", BluetoothAdvertisePermission) != 0)
                {
                    requestPermissions.Add(BluetoothAdvertisePermission);
                }
            }
            else if (osVersionInt >= 23)
            {
                // Before Android 12
#if !FEATURE_ANDROID_COMPANION_DEVICE
                if (activity.Call<int>("checkSelfPermission", LocationPermission) != 0)
                {
                    requestPermissions.Add(LocationPermission);
                }
#endif

                if (activity.Call<int>("checkSelfPermission", BluetoothPermission) != 0)
                {
                    requestPermissions.Add(BluetoothPermission);
                }

                if (activity.Call<int>("checkSelfPermission", BluetoothAdminPermission) != 0)
                {
                    requestPermissions.Add(BluetoothAdminPermission);
                }
            }

            return requestPermissions;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus && permissionRequested)
            {
                if (Thread.CurrentThread != mainThread)
                {
                    AndroidJNI.AttachCurrentThread();
                }
                var osVersion = new AndroidJavaClass("android.os.Build$VERSION");
                var osVersionInt = osVersion.GetStatic<int>("SDK_INT");
                if (Thread.CurrentThread != mainThread)
                {
                    AndroidJNI.DetachCurrentThread();
                }

                if (osVersionInt >= 23)
                {
                    // Android M or later
                    if (Thread.CurrentThread != mainThread)
                    {
                        AndroidJNI.AttachCurrentThread();
                    }
                    var requestPermissions = AndroidBluetoothRequiredPermissions(osVersionInt);
                    if (Thread.CurrentThread != mainThread)
                    {
                        AndroidJNI.DetachCurrentThread();
                    }

                    if (requestPermissions.Count == 0)
                    {
                        permissionRequested = false;
                        // all permissions granted
                        if (Thread.CurrentThread != mainThread)
                        {
                            AndroidJNI.AttachCurrentThread();
                        }
                        var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                        var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
#if ENABLE_IL2CPP
                        Instance.bleMidiPlugin.Call("initialize", activity, new BleMidiDeviceConnectionListener(), new BleMidiInputEventListener());
#else
                        Instance.bleMidiPlugin.Call("initialize", activity);
#endif
                        if (Thread.CurrentThread != mainThread)
                        {
                            AndroidJNI.DetachCurrentThread();
                        }

                        onInitializeCompleted?.Invoke();
                        onInitializeCompleted = null;
                    }
                    else
                    {
                        Debug.Log($"These permissions are not granted: {string.Join(", ", requestPermissions)}. BLE MIDI function doesn't work.");
                    }
                }
                else
                {

                    onInitializeCompleted?.Invoke();
                    onInitializeCompleted = null;
                }
            }
        }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>
        /// USB MIDI device connection listener
        /// </summary>
        internal class UsbMidiDeviceConnectionListener : AndroidJavaProxy
        {
            public UsbMidiDeviceConnectionListener() : base("jp.kshoji.unity.midi.OnUsbMidiDeviceConnectionListener")
            {
            }
            public void onMidiInputDeviceAttached(string midiInputDevice)
                => Instance.asyncOperation.Post(o => Instance.OnMidiInputDeviceAttached((string)o), midiInputDevice);
            public void onMidiOutputDeviceAttached(string midiOutputDevice)
                => Instance.asyncOperation.Post(o => Instance.OnMidiOutputDeviceAttached((string)o), midiOutputDevice);
            public void onMidiInputDeviceDetached(string midiInputDevice)
                => Instance.asyncOperation.Post(o => Instance.OnMidiInputDeviceDetached((string)o), midiInputDevice);
            public void onMidiOutputDeviceDetached(string midiOutputDevice)
                => Instance.asyncOperation.Post(o => Instance.OnMidiOutputDeviceDetached((string)o), midiOutputDevice);
        }

        /// <summary>
        /// USB MIDI Event listener
        /// </summary>
        internal class UsbMidiInputEventListener : AndroidJavaProxy
        {
            public UsbMidiInputEventListener() : base("jp.kshoji.unity.midi.OnUsbMidiInputEventListener")
            {
            }

            public void onMidiMiscellaneousFunctionCodes(string sender, int cable, int byte1, int byte2, int byte3)
                => Instance.asyncOperation.Post(
                    o => Instance.OnMidiMiscellaneousFunctionCodes((string)((object[])o)[0], (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3], (int)((object[])o)[4]),
                    new object[] {sender, cable, byte1, byte2, byte3});
            public void onMidiCableEvents(string sender, int cable, int byte1, int byte2, int byte3)
                => Instance.asyncOperation.Post(o => Instance.OnMidiCableEvents((string)((object[])o)[0], (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3], (int)((object[])o)[4]), new object[] {sender, cable, byte1, byte2, byte3});
            public void onMidiSystemCommonMessage(string sender, int cable, byte[] bytes)
                => Instance.asyncOperation.Post(o => Instance.OnMidiSystemCommonMessage((string)((object[])o)[0], (int)((object[])o)[1], (byte[])((object[])o)[2]), new object[] {sender, cable, bytes});
            public void onMidiSystemExclusive(string sender, int cable, byte[] systemExclusive)
                => Instance.asyncOperation.Post(o => Instance.OnMidiSystemExclusive((string)((object[])o)[0], (int)((object[])o)[1], (byte[])((object[])o)[2]), new object[] {sender, cable, systemExclusive});
            public void onMidiNoteOff(string sender, int cable, int channel, int note, int velocity)
                => Instance.asyncOperation.Post(o => Instance.OnMidiNoteOff((string)((object[])o)[0], (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3], (int)((object[])o)[4]), new object[] {sender, cable, channel, note, velocity});
            public void onMidiNoteOn(string sender, int cable, int channel, int note, int velocity)
                => Instance.asyncOperation.Post(o => Instance.OnMidiNoteOn((string)((object[])o)[0], (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3], (int)((object[])o)[4]), new object[] {sender, cable, channel, note, velocity});
            public void onMidiPolyphonicAftertouch(string sender, int cable, int channel, int note, int pressure)
                => Instance.asyncOperation.Post(o => Instance.OnMidiPolyphonicAftertouch((string)((object[])o)[0], (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3], (int)((object[])o)[4]), new object[] {sender, cable, channel, note, pressure});
            public void onMidiControlChange(string sender, int cable, int channel, int function, int value)
                => Instance.asyncOperation.Post(o => Instance.OnMidiControlChange((string)((object[])o)[0], (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3], (int)((object[])o)[4]), new object[] {sender, cable, channel, function, value});
            public void onMidiProgramChange(string sender, int cable, int channel, int program)
                => Instance.asyncOperation.Post(o => Instance.OnMidiProgramChange((string)((object[])o)[0], (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3]), new object[] {sender, cable, channel, program});
            public void onMidiChannelAftertouch(string sender, int cable, int channel, int pressure)
                => Instance.asyncOperation.Post(o => Instance.OnMidiChannelAftertouch((string)((object[])o)[0], (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3]), new object[] {sender, cable, channel, pressure});
            public void onMidiPitchWheel(string sender, int cable, int channel, int amount)
                => Instance.asyncOperation.Post(o => Instance.OnMidiPitchWheel((string)((object[])o)[0], (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3]), new object[] {sender, cable, channel, amount});
            public void onMidiSingleByte(string sender, int cable, int byte1)
                => Instance.asyncOperation.Post(o => Instance.OnMidiSingleByte((string)((object[])o)[0], (int)((object[])o)[1], (int)((object[])o)[2]), new object[] {sender, cable, byte1});
            public void onMidiTimeCodeQuarterFrame(string sender, int cable, int timing)
                => Instance.asyncOperation.Post(o => Instance.OnMidiTimeCodeQuarterFrame((string)((object[])o)[0], (int)((object[])o)[1], (int)((object[])o)[2]), new object[] {sender, cable, timing});
            public void onMidiSongSelect(string sender, int cable, int song)
                => Instance.asyncOperation.Post(o => Instance.OnMidiSongSelect((string)((object[])o)[0], (int)((object[])o)[1], (int)((object[])o)[2]), new object[] {sender, cable, song});
            public void onMidiSongPositionPointer(string sender, int cable, int position)
                => Instance.asyncOperation.Post(o => Instance.OnMidiSongPositionPointer((string)((object[])o)[0], (int)((object[])o)[1], (int)((object[])o)[2]), new object[] {sender, cable, position});
            public void onMidiTuneRequest(string sender, int cable)
                => Instance.asyncOperation.Post(o => Instance.OnMidiTuneRequest((string)((object[])o)[0], (int)((object[])o)[1]), new object[] {sender, cable});
            public void onMidiTimingClock(string sender, int cable)
                => Instance.asyncOperation.Post(o => Instance.OnMidiTimingClock((string)((object[])o)[0], (int)((object[])o)[1]), new object[] {sender, cable});
            public void onMidiStart(string sender, int cable)
                => Instance.asyncOperation.Post(o => Instance.OnMidiStart((string)((object[])o)[0], (int)((object[])o)[1]), new object[] {sender, cable});
            public void onMidiContinue(string sender, int cable)
                => Instance.asyncOperation.Post(o => Instance.OnMidiContinue((string)((object[])o)[0], (int)((object[])o)[1]), new object[] {sender, cable});
            public void onMidiStop(string sender, int cable)
                => Instance.asyncOperation.Post(o => Instance.OnMidiStop((string)((object[])o)[0], (int)((object[])o)[1]), new object[] {sender, cable});
            public void onMidiActiveSensing(string sender, int cable)
                => Instance.asyncOperation.Post(o => Instance.OnMidiActiveSensing((string)((object[])o)[0], (int)((object[])o)[1]), new object[] {sender, cable});
            public void onMidiReset(string sender, int cable)
                => Instance.asyncOperation.Post(o => Instance.OnMidiReset((string)((object[])o)[0], (int)((object[])o)[1]), new object[] {sender, cable});
        }

        /// <summary>
        /// Bluetooth LE MIDI device attached listener
        /// </summary>
        internal class BleMidiDeviceConnectionListener : AndroidJavaProxy
        {
            public BleMidiDeviceConnectionListener() : base("jp.kshoji.unity.midi.OnBleMidiDeviceConnectionListener")
            {
            }
            public void onMidiInputDeviceAttached(string deviceId)
                => Instance.asyncOperation.Post(o => Instance.OnMidiInputDeviceAttached((string)o), deviceId);
            public void onMidiOutputDeviceAttached(string deviceId)
                => Instance.asyncOperation.Post(o => Instance.OnMidiOutputDeviceAttached((string)o), deviceId);
            public void onMidiInputDeviceDetached(string deviceId)
                => Instance.asyncOperation.Post(o => Instance.OnMidiInputDeviceDetached((string)o), deviceId);
            public void onMidiOutputDeviceDetached(string deviceId)
                => Instance.asyncOperation.Post(o => Instance.OnMidiOutputDeviceDetached((string)o), deviceId);
        }

        /// <summary>
        /// Bluetooth LE MIDI Event listener
        /// </summary>
        internal class BleMidiInputEventListener : AndroidJavaProxy
        {
            public BleMidiInputEventListener() : base("jp.kshoji.unity.midi.OnBleMidiInputEventListener")
            {
            }
            public void onMidiSystemExclusive(string deviceId, byte[] systemExclusive)
                => Instance.asyncOperation.Post(o => Instance.OnMidiSystemExclusive((string)((object[])o)[0], 0, (byte[])((object[])o)[1]), new object[] {deviceId, systemExclusive});
            public void onMidiNoteOff(string deviceId, int channel, int note, int velocity)
                => Instance.asyncOperation.Post(o => Instance.OnMidiNoteOff((string)((object[])o)[0], 0, (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3]), new object[] {deviceId, channel, note, velocity});
            public void onMidiNoteOn(string deviceId, int channel, int note, int velocity)
                => Instance.asyncOperation.Post(o => Instance.OnMidiNoteOn((string)((object[])o)[0], 0, (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3]), new object[] {deviceId, channel, note, velocity});
            public void onMidiPolyphonicAftertouch(string deviceId, int channel, int note, int pressure)
                => Instance.asyncOperation.Post(o => Instance.OnMidiPolyphonicAftertouch((string)((object[])o)[0], 0, (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3]), new object[] {deviceId, channel, note, pressure});
            public void onMidiControlChange(string deviceId, int channel, int function, int value)
                => Instance.asyncOperation.Post(o => Instance.OnMidiControlChange((string)((object[])o)[0], 0, (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3]), new object[] {deviceId, channel, function, value});
            public void onMidiProgramChange(string deviceId, int channel, int program)
                => Instance.asyncOperation.Post(o => Instance.OnMidiProgramChange((string)((object[])o)[0], 0, (int)((object[])o)[1], (int)((object[])o)[2]), new object[] {deviceId, channel, program});
            public void onMidiChannelAftertouch(string deviceId, int channel, int pressure)
                => Instance.asyncOperation.Post(o => Instance.OnMidiChannelAftertouch((string)((object[])o)[0], 0, (int)((object[])o)[1], (int)((object[])o)[2]), new object[] {deviceId, channel, pressure});
            public void onMidiPitchWheel(string deviceId, int channel, int amount)
                => Instance.asyncOperation.Post(o => Instance.OnMidiPitchWheel((string)((object[])o)[0], 0, (int)((object[])o)[1], (int)((object[])o)[2]), new object[] {deviceId, channel, amount});
            public void onMidiSingleByte(string deviceId, int byte1)
                => Instance.asyncOperation.Post(o => Instance.OnMidiSingleByte((string)((object[])o)[0], 0, (int)((object[])o)[1]), new object[] {deviceId, byte1});
            public void onMidiTimeCodeQuarterFrame(string deviceId, int timing)
                => Instance.asyncOperation.Post(o => Instance.OnMidiTimeCodeQuarterFrame((string)((object[])o)[0], 0, (int)((object[])o)[1]), new object[] {deviceId, timing});
            public void onMidiSongSelect(string deviceId, int song)
                => Instance.asyncOperation.Post(o => Instance.OnMidiSongSelect((string)((object[])o)[0], 0, (int)((object[])o)[1]), new object[] {deviceId, song});
            public void onMidiSongPositionPointer(string deviceId, int position)
                => Instance.asyncOperation.Post(o => Instance.OnMidiSongPositionPointer((string)((object[])o)[0], 0, (int)((object[])o)[1]), new object[] {deviceId, position});
            public void onMidiTuneRequest(string deviceId)
                => Instance.asyncOperation.Post(o => Instance.OnMidiTuneRequest((string)o, 0), deviceId);
            public void onMidiTimingClock(string deviceId)
                => Instance.asyncOperation.Post(o => Instance.OnMidiTimingClock((string)o, 0), deviceId);
            public void onMidiStart(string deviceId)
                => Instance.asyncOperation.Post(o => Instance.OnMidiStart((string)o, 0), deviceId);
            public void onMidiContinue(string deviceId)
                => Instance.asyncOperation.Post(o => Instance.OnMidiContinue((string)o, 0), deviceId);
            public void onMidiStop(string deviceId)
                => Instance.asyncOperation.Post(o => Instance.OnMidiStop((string)o, 0), deviceId);
            public void onMidiActiveSensing(string deviceId)
                => Instance.asyncOperation.Post(o => Instance.OnMidiActiveSensing((string)o, 0), deviceId);
            public void onMidiReset(string deviceId)
                => Instance.asyncOperation.Post(o => Instance.OnMidiReset((string)o, 0), deviceId);
        }
#endif

        /// <summary>
        /// Initializes MIDI Plugin system
        /// </summary>
        /// <param name="initializeCompletedAction"></param>
        public void InitializeMidi(Action initializeCompletedAction)
        {
            if (EventSystem.current == null)
            {
                // NOTE: if the EventSystem already exists at another place, remove this AddComponent method calling. 
                gameObject.AddComponent<EventSystem>();
            }

#if UNITY_EDITOR
#if UNITY_EDITOR_LINUX
            SetSendMessageCallback(LinuxOnSendMessageDelegate);
            InitializeMidiLinux();
#endif
#if UNITY_EDITOR_OSX
            SetMidiInputDeviceAttachedCallback(IosOnMidiInputDeviceAttached);
            SetMidiOutputDeviceAttachedCallback(IosOnMidiOutputDeviceAttached);
            SetMidiInputDeviceDetachedCallback(IosOnMidiInputDeviceDetached);
            SetMidiOutputDeviceDetachedCallback(IosOnMidiOutputDeviceDetached);

            SetMidiNoteOnCallback(IosOnMidiNoteOn);
            SetMidiNoteOffCallback(IosOnMidiNoteOff);
            SetMidiPolyphonicAftertouchDelegate(IosOnMidiPolyphonicAftertouch);
            SetMidiControlChangeDelegate(IosOnMidiControlChange);
            SetMidiProgramChangeDelegate(IosOnMidiProgramChange);
            SetMidiChannelAftertouchDelegate(IosOnMidiChannelAftertouch);
            SetMidiPitchWheelDelegate(IosOnMidiPitchWheel);
            SetMidiSystemExclusiveDelegate(IosOnMidiSystemExclusive);
            SetMidiTimeCodeQuarterFrameDelegate(IosOnMidiTimeCodeQuarterFrame);
            SetMidiSongSelectDelegate(IosOnMidiSongSelect);
            SetMidiSongPositionPointerDelegate(IosOnMidiSongPositionPointer);
            SetMidiTuneRequestDelegate(IosOnMidiTuneRequest);
            SetMidiTimingClockDelegate(IosOnMidiTimingClock);
            SetMidiStartDelegate(IosOnMidiStart);
            SetMidiContinueDelegate(IosOnMidiContinue);
            SetMidiStopDelegate(IosOnMidiStop);
            SetMidiActiveSensingDelegate(IosOnMidiActiveSensing);
            SetMidiResetDelegate(IosOnMidiReset);

            midiPluginStartForEditor();
            midiPluginInitialize();
#endif
#if UNITY_EDITOR_WIN
            MidiPlugin.Instance.OnMidiInputDeviceAttached += MidiPlugin_OnMidiInputDeviceAttached;
            MidiPlugin.Instance.OnMidiInputDeviceDetached += MidiPlugin_OnMidiInputDeviceDetached;
            MidiPlugin.Instance.OnMidiOutputDeviceAttached += MidiPlugin_OnMidiOutputDeviceAttached;
            MidiPlugin.Instance.OnMidiOutputDeviceDetached += MidiPlugin_OnMidiOutputDeviceDetached;
            
            MidiPlugin.Instance.OnMidiNoteOn += MidiPlugin_OnMidiNoteOn;
            MidiPlugin.Instance.OnMidiNoteOff += MidiPlugin_OnMidiNoteOff;
            MidiPlugin.Instance.OnMidiPolyphonicKeyPressure += MidiPlugin_OnMidiPolyphonicKeyPressure;
            MidiPlugin.Instance.OnMidiControlChange += MidiPlugin_OnMidiControlChange;
            MidiPlugin.Instance.OnMidiProgramChange += MidiPlugin_OnMidiProgramChange;
            MidiPlugin.Instance.OnMidiChannelPressure += MidiPlugin_OnMidiChannelPressure;
            MidiPlugin.Instance.OnMidiPitchBendChange += MidiPlugin_OnMidiPitchBendChange;
            MidiPlugin.Instance.OnMidiSystemExclusive += MidiPlugin_OnMidiSystemExclusive;
            MidiPlugin.Instance.OnMidiTimeCode += MidiPlugin_OnMidiTimeCode;
            MidiPlugin.Instance.OnMidiSongPositionPointer += MidiPlugin_OnMidiSongPositionPointer;
            MidiPlugin.Instance.OnMidiSongSelect += MidiPlugin_OnMidiSongSelect;
            MidiPlugin.Instance.OnMidiTuneRequest += MidiPlugin_OnMidiTuneRequest;
            MidiPlugin.Instance.OnMidiTimingClock += MidiPlugin_OnMidiTimingClock;
            MidiPlugin.Instance.OnMidiStart += MidiPlugin_OnMidiStart;
            MidiPlugin.Instance.OnMidiContinue += MidiPlugin_OnMidiContinue;
            MidiPlugin.Instance.OnMidiStop += MidiPlugin_OnMidiStop;
            MidiPlugin.Instance.OnMidiActiveSensing += MidiPlugin_OnMidiActiveSensing;
            MidiPlugin.Instance.OnMidiSystemReset += MidiPlugin_OnMidiSystemReset;
#endif
            initializeCompletedAction?.Invoke();
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var context = activity.Call<AndroidJavaObject>("getApplicationContext");
#if ENABLE_IL2CPP
            Instance.usbMidiPlugin.Call("initialize", context, new UsbMidiDeviceConnectionListener(), new UsbMidiInputEventListener());
#else
            Instance.usbMidiPlugin.Call("initialize", context);
#endif
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
            CheckPermission();
            if (permissionRequested)
            {
                onInitializeCompleted = initializeCompletedAction;
            }
            else
            {
                if (Thread.CurrentThread != mainThread)
                {
                    AndroidJNI.AttachCurrentThread();
                }
#if ENABLE_IL2CPP
                Instance.bleMidiPlugin.Call("initialize", activity, new BleMidiDeviceConnectionListener(), new BleMidiInputEventListener());
#else
                Instance.bleMidiPlugin.Call("initialize", activity);
#endif
                if (Thread.CurrentThread != mainThread)
                {
                    AndroidJNI.DetachCurrentThread();
                }
                initializeCompletedAction?.Invoke();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            SetMidiInputDeviceAttachedCallback(IosOnMidiInputDeviceAttached);
            SetMidiOutputDeviceAttachedCallback(IosOnMidiOutputDeviceAttached);
            SetMidiInputDeviceDetachedCallback(IosOnMidiInputDeviceDetached);
            SetMidiOutputDeviceDetachedCallback(IosOnMidiOutputDeviceDetached);

            SetMidiNoteOnCallback(IosOnMidiNoteOn);
            SetMidiNoteOffCallback(IosOnMidiNoteOff);
            SetMidiPolyphonicAftertouchDelegate(IosOnMidiPolyphonicAftertouch);
            SetMidiControlChangeDelegate(IosOnMidiControlChange);
            SetMidiProgramChangeDelegate(IosOnMidiProgramChange);
            SetMidiChannelAftertouchDelegate(IosOnMidiChannelAftertouch);
            SetMidiPitchWheelDelegate(IosOnMidiPitchWheel);
            SetMidiSystemExclusiveDelegate(IosOnMidiSystemExclusive);
            SetMidiTimeCodeQuarterFrameDelegate(IosOnMidiTimeCodeQuarterFrame);
            SetMidiSongSelectDelegate(IosOnMidiSongSelect);
            SetMidiSongPositionPointerDelegate(IosOnMidiSongPositionPointer);
            SetMidiTuneRequestDelegate(IosOnMidiTuneRequest);
            SetMidiTimingClockDelegate(IosOnMidiTimingClock);
            SetMidiStartDelegate(IosOnMidiStart);
            SetMidiContinueDelegate(IosOnMidiContinue);
            SetMidiStopDelegate(IosOnMidiStop);
            SetMidiActiveSensingDelegate(IosOnMidiActiveSensing);
            SetMidiResetDelegate(IosOnMidiReset);
            
            midiPluginInitialize();
            initializeCompletedAction?.Invoke();
#elif UNITY_WSA || UNITY_STANDALONE_WIN
            MidiPlugin.Instance.OnMidiInputDeviceAttached += MidiPlugin_OnMidiInputDeviceAttached;
            MidiPlugin.Instance.OnMidiInputDeviceDetached += MidiPlugin_OnMidiInputDeviceDetached;
            MidiPlugin.Instance.OnMidiOutputDeviceAttached += MidiPlugin_OnMidiOutputDeviceAttached;
            MidiPlugin.Instance.OnMidiOutputDeviceDetached += MidiPlugin_OnMidiOutputDeviceDetached;

            MidiPlugin.Instance.OnMidiNoteOn += MidiPlugin_OnMidiNoteOn;
            MidiPlugin.Instance.OnMidiNoteOff += MidiPlugin_OnMidiNoteOff;
            MidiPlugin.Instance.OnMidiPolyphonicKeyPressure += MidiPlugin_OnMidiPolyphonicKeyPressure;
            MidiPlugin.Instance.OnMidiControlChange += MidiPlugin_OnMidiControlChange;
            MidiPlugin.Instance.OnMidiProgramChange += MidiPlugin_OnMidiProgramChange;
            MidiPlugin.Instance.OnMidiChannelPressure += MidiPlugin_OnMidiChannelPressure;
            MidiPlugin.Instance.OnMidiPitchBendChange += MidiPlugin_OnMidiPitchBendChange;
            MidiPlugin.Instance.OnMidiSystemExclusive += MidiPlugin_OnMidiSystemExclusive;
            MidiPlugin.Instance.OnMidiTimeCode += MidiPlugin_OnMidiTimeCode;
            MidiPlugin.Instance.OnMidiSongPositionPointer += MidiPlugin_OnMidiSongPositionPointer;
            MidiPlugin.Instance.OnMidiSongSelect += MidiPlugin_OnMidiSongSelect;
            MidiPlugin.Instance.OnMidiTuneRequest += MidiPlugin_OnMidiTuneRequest;
            MidiPlugin.Instance.OnMidiTimingClock += MidiPlugin_OnMidiTimingClock;
            MidiPlugin.Instance.OnMidiStart += MidiPlugin_OnMidiStart;
            MidiPlugin.Instance.OnMidiContinue += MidiPlugin_OnMidiContinue;
            MidiPlugin.Instance.OnMidiStop += MidiPlugin_OnMidiStop;
            MidiPlugin.Instance.OnMidiActiveSensing += MidiPlugin_OnMidiActiveSensing;
            MidiPlugin.Instance.OnMidiSystemReset += MidiPlugin_OnMidiSystemReset;
            initializeCompletedAction?.Invoke();
#elif UNITY_STANDALONE_LINUX
            SetSendMessageCallback(LinuxOnSendMessageDelegate);
            InitializeMidiLinux();
            initializeCompletedAction?.Invoke();
#elif UNITY_WEBGL
            midiPluginInitialize();
            initializeCompletedAction?.Invoke();
#endif
        }

#if UNITY_EDITOR
        private void Awake()
        {
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }

        void PlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (stateChange == PlayModeStateChange.EnteredEditMode)
            {
#if UNITY_EDITOR_WIN
                MidiPlugin.Instance.Stop();
#endif
#if UNITY_EDITOR_OSX
#endif
#if UNITY_EDITOR_LINUX
                TerminateMidiLinux();
#endif
            }
            else if (stateChange == PlayModeStateChange.EnteredPlayMode)
            {
#if UNITY_EDITOR_WIN
                MidiPlugin.Instance.Start();
#endif
#if UNITY_EDITOR_OSX
#endif
#if UNITY_EDITOR_LINUX
                InitializeMidiLinux();
#endif
            }
        }
#endif

#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
        class RtpMidiEventHandler : IRtpMidiEventHandler
        {
            public void OnMidiNoteOn(string deviceId, int channel, int note, int velocity)
                => Instance.asyncOperation.Post(o => Instance.OnMidiNoteOn((string)((object[])o)[0], 0, (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3]), new object[] {deviceId, channel, note, velocity});
            public void OnMidiNoteOff(string deviceId, int channel, int note, int velocity)
                => Instance.asyncOperation.Post(o => Instance.OnMidiNoteOff((string)((object[])o)[0], 0, (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3]), new object[] {deviceId, channel, note, velocity});
            public void OnMidiPolyphonicAftertouch(string deviceId, int channel, int note, int pressure)
                => Instance.asyncOperation.Post(o => Instance.OnMidiPolyphonicAftertouch((string)((object[])o)[0], 0, (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3]), new object[] {deviceId, channel, note, pressure});
            public void OnMidiControlChange(string deviceId, int channel, int function, int value)
                => Instance.asyncOperation.Post(o => Instance.OnMidiControlChange((string)((object[])o)[0], 0, (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3]), new object[] {deviceId, channel, function, value});
            public void OnMidiProgramChange(string deviceId, int channel, int program)
                => Instance.asyncOperation.Post(o => Instance.OnMidiProgramChange((string)((object[])o)[0], 0, (int)((object[])o)[1], (int)((object[])o)[2]), new object[] {deviceId, channel, program});
            public void OnMidiChannelAftertouch(string deviceId, int channel, int pressure)
                => Instance.asyncOperation.Post(o => Instance.OnMidiChannelAftertouch((string)((object[])o)[0], 0, (int)((object[])o)[1], (int)((object[])o)[2]), new object[] {deviceId, channel, pressure});
            public void OnMidiPitchWheel(string deviceId, int channel, int amount)
                => Instance.asyncOperation.Post(o => Instance.OnMidiPitchWheel((string)((object[])o)[0], 0, (int)((object[])o)[1], (int)((object[])o)[2]), new object[] {deviceId, channel, amount});
            public void OnMidiSystemExclusive(string deviceId, byte[] systemExclusive)
                => Instance.asyncOperation.Post(o => Instance.OnMidiSystemExclusive((string)((object[])o)[0], 0, (byte[])((object[])o)[1]), new object[] {deviceId, systemExclusive});
            public void OnMidiTimeCodeQuarterFrame(string deviceId, int timing)
                => Instance.asyncOperation.Post(o => Instance.OnMidiTimeCodeQuarterFrame((string)((object[])o)[0], 0, (int)((object[])o)[1]), new object[] {deviceId, timing});
            public void OnMidiSongSelect(string deviceId, int song)
                => Instance.asyncOperation.Post(o => Instance.OnMidiSongSelect((string)((object[])o)[0], 0, (int)((object[])o)[1]), new object[] {deviceId, song});
            public void OnMidiSongPositionPointer(string deviceId, int position)
                => Instance.asyncOperation.Post(o => Instance.OnMidiSongPositionPointer((string)((object[])o)[0], 0, (int)((object[])o)[1]), new object[] {deviceId, position});
            public void OnMidiTuneRequest(string deviceId)
                => Instance.asyncOperation.Post(o => Instance.OnMidiTuneRequest((string)o, 0), deviceId);
            public void OnMidiTimingClock(string deviceId)
                => Instance.asyncOperation.Post(o => Instance.OnMidiTimingClock((string)o, 0), deviceId);
            public void OnMidiStart(string deviceId)
                => Instance.asyncOperation.Post(o => Instance.OnMidiStart((string)o, 0), deviceId);
            public void OnMidiContinue(string deviceId)
                => Instance.asyncOperation.Post(o => Instance.OnMidiContinue((string)o, 0), deviceId);
            public void OnMidiStop(string deviceId)
                => Instance.asyncOperation.Post(o => Instance.OnMidiStop((string)o, 0), deviceId);
            public void OnMidiActiveSensing(string deviceId)
                => Instance.asyncOperation.Post(o => Instance.OnMidiActiveSensing((string)o, 0), deviceId);
            public void OnMidiReset(string deviceId)
                => Instance.asyncOperation.Post(o => Instance.OnMidiReset((string)o, 0), deviceId);
        }
#endif

        /// <summary>
        /// Starts RTP MIDI Listener
        /// </summary>
        /// <param name="sessionName">the name of session</param>
        /// <param name="listenPort">UDP port number(0-65534)</param>
        /// <exception cref="NotImplementedException">iOS platform isn't available</exception>
        public void StartRtpMidiServer(string sessionName, int listenPort)
        {
#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
            throw new NotImplementedException("iOS / WebGL platform isn't available");
#else
            lock (rtpMidiServers)
            {
                RtpMidiServer rtpMidiServer = null;
                if (rtpMidiServers.TryGetValue(listenPort, out var server))
                {
                    rtpMidiServer = server;
                }

                if (rtpMidiServer == null)
                {
                    // starts RTP MIDI server with UDP specified control port
                    rtpMidiServer = new RtpMidiServer(sessionName, listenPort, this, rtpMidiEventHandler);
                    rtpMidiServer.Start();
                    rtpMidiServers[listenPort] = rtpMidiServer;
                }
            }
#endif
        }

        /// <summary>
        /// Check RTP MIDI Listener is running
        /// </summary>
        /// <param name="listenPort">UDP port number(0-65534)</param>
        public bool IsRtpMidiRunning(int listenPort)
        {
#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
            throw new NotImplementedException("iOS / WebGL platform isn't available");
#else
            lock (rtpMidiServers)
            {
                if (rtpMidiServers.TryGetValue(listenPort, out var rtpMidiServer))
                {
                    return rtpMidiServer != null && rtpMidiServer.IsStarted();
                }

                return false;
            }
#endif
        }

#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
        public void OnRtpMidiDeviceAttached(string deviceId)
            => Instance.asyncOperation.Post(o =>
            {
                Instance.OnMidiInputDeviceAttached((string)o);
                Instance.OnMidiOutputDeviceAttached((string)o);
            }, deviceId);

        public void OnRtpMidiDeviceDetached(string deviceId)
            => Instance.asyncOperation.Post(o =>
            {
                Instance.OnMidiInputDeviceDetached((string)o);
                Instance.OnMidiOutputDeviceDetached((string)o);
            }, deviceId);
#endif

        /// <summary>
        /// Stops RTP MIDI Listener with the specified port
        /// </summary>
        public void StopRtpMidi(int listenPort)
        {
#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
            throw new NotImplementedException("iOS / WebGL platform isn't available");
#else
            lock (rtpMidiServers)
            {
                if (rtpMidiServers.TryGetValue(listenPort, out var rtpMidiServer))
                {
                    rtpMidiServer?.Stop();
                    rtpMidiServers.Remove(listenPort);
                }
            }
#endif
        }

        /// <summary>
        /// Stops all RTP MIDI servers
        /// </summary>
        public void StopAllRtpMidi()
        {
#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
            throw new NotImplementedException("iOS / WebGL platform isn't available");
#else
            lock (rtpMidiServers)
            {
                foreach (var rtpMidiServer in rtpMidiServers.Values)
                {
                    rtpMidiServer?.Stop();
                }
                rtpMidiServers.Clear();
            }
#endif
        }
        
        /// <summary>
        /// Initiate RTP MIDI Connection with specified IPEndPoint
        /// </summary>
        /// <param name="sessionName">the name of session</param>
        /// <param name="listenPort">port to listen</param>
        /// <param name="ipEndPoint">IP address and port to connect with</param>
        public void ConnectToRtpMidiServer(string sessionName, int listenPort, IPEndPoint ipEndPoint)
        {
#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
            throw new NotImplementedException("iOS / WebGL platform isn't available");
#else
            lock (rtpMidiServers)
            {
                RtpMidiServer rtpMidiServer = null;
                if (rtpMidiServers.TryGetValue(listenPort, out var server))
                {
                    rtpMidiServer = server;
                }

                if (rtpMidiServer == null)
                {
                    StartRtpMidiServer(sessionName, listenPort);
                    rtpMidiServer = rtpMidiServers[listenPort];
                }

                rtpMidiServer.ConnectToListener(ipEndPoint);
            }
#endif
        }

#if UNITY_WSA || UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private void MidiPlugin_OnMidiInputDeviceAttached(string deviceId) => asyncOperation.Post(o => OnMidiInputDeviceAttached((string)o), deviceId);
        private void MidiPlugin_OnMidiInputDeviceDetached(string deviceId) => asyncOperation.Post(o => OnMidiInputDeviceDetached((string)o), deviceId);
        private void MidiPlugin_OnMidiOutputDeviceAttached(string deviceId) => asyncOperation.Post(o => OnMidiOutputDeviceAttached((string)o), deviceId);
        private void MidiPlugin_OnMidiOutputDeviceDetached(string deviceId) => asyncOperation.Post(o => OnMidiOutputDeviceDetached((string)o), deviceId);

        private void MidiPlugin_OnMidiNoteOn(string deviceId, byte channel, byte note, byte velocity) => asyncOperation.Post(o => OnMidiNoteOn((string)((object[])o)[0], 0, (byte)((object[])o)[1], (byte)((object[])o)[2], (byte)((object[])o)[3]), new object[] {deviceId, channel, note, velocity});
        private void MidiPlugin_OnMidiNoteOff(string deviceId, byte channel, byte note, byte velocity) => asyncOperation.Post(o => OnMidiNoteOff((string)((object[])o)[0], 0, (byte)((object[])o)[1], (byte)((object[])o)[2], (byte)((object[])o)[3]), new object[] {deviceId, channel, note, velocity});
        private void MidiPlugin_OnMidiPolyphonicKeyPressure(string deviceId, byte channel, byte note, byte velocity) => asyncOperation.Post(o => OnMidiPolyphonicAftertouch((string)((object[])o)[0], 0, (byte)((object[])o)[1], (byte)((object[])o)[2], (byte)((object[])o)[3]), new object[] {deviceId, channel, note, velocity});
        private void MidiPlugin_OnMidiControlChange(string deviceId, byte channel, byte controller, byte controllerValue) => asyncOperation.Post(o => OnMidiControlChange((string)((object[])o)[0], 0, (byte)((object[])o)[1], (byte)((object[])o)[2], (byte)((object[])o)[3]), new object[] {deviceId, channel, controller, controllerValue});
        private void MidiPlugin_OnMidiProgramChange(string deviceId, byte channel, byte program) => asyncOperation.Post(o => OnMidiProgramChange((string)((object[])o)[0], 0, (byte)((object[])o)[1], (byte)((object[])o)[2]), new object[] {deviceId, channel, program});
        private void MidiPlugin_OnMidiChannelPressure(string deviceId, byte channel, byte pressure) => asyncOperation.Post(o => OnMidiChannelAftertouch((string)((object[])o)[0], 0, (byte)((object[])o)[1], (byte)((object[])o)[2]), new object[] {deviceId, channel, pressure});
        private void MidiPlugin_OnMidiPitchBendChange(string deviceId, byte channel, ushort bend) => asyncOperation.Post(o => OnMidiPitchWheel((string)((object[])o)[0], 0, (byte)((object[])o)[1], (ushort)((object[])o)[2]), new object[] {deviceId, channel, bend});
        private void MidiPlugin_OnMidiSystemExclusive(string deviceId, byte[] systemExclusive) => asyncOperation.Post(o => OnMidiSystemExclusive((string)((object[])o)[0], 0, (byte[])((object[])o)[1]), new object[] {deviceId, systemExclusive});
        private void MidiPlugin_OnMidiTimeCode(string deviceId, byte frameType, byte values) => asyncOperation.Post(o => OnMidiTimeCodeQuarterFrame((string)((object[])o)[0], 0, (((byte)((object[])o)[1] & 0x7) << 4) | ((byte)((object[])o)[2] & 0xf)), new object[] {deviceId, frameType, values});
        private void MidiPlugin_OnMidiSongPositionPointer(string deviceId, ushort beats) => asyncOperation.Post(o => OnMidiSongPositionPointer((string)((object[])o)[0], 0, (ushort)((object[])o)[1]), new object[] {deviceId, beats});
        private void MidiPlugin_OnMidiSongSelect(string deviceId, byte song) => asyncOperation.Post(o => OnMidiSongSelect((string)((object[])o)[0], 0, (byte)((object[])o)[1]), new object[] {deviceId, song});
        private void MidiPlugin_OnMidiTuneRequest(string deviceId) => asyncOperation.Post(o => OnMidiTuneRequest((string)o, 0), deviceId);
        private void MidiPlugin_OnMidiTimingClock(string deviceId) => asyncOperation.Post(o => OnMidiTimingClock((string)o, 0), deviceId);
        private void MidiPlugin_OnMidiStart(string deviceId) => asyncOperation.Post(o => OnMidiStart((string)o, 0), deviceId);
        private void MidiPlugin_OnMidiContinue(string deviceId) => asyncOperation.Post(o => OnMidiContinue((string)o, 0), deviceId);
        private void MidiPlugin_OnMidiStop(string deviceId) => asyncOperation.Post(o => OnMidiStop((string)o, 0), deviceId);
        private void MidiPlugin_OnMidiActiveSensing(string deviceId) => asyncOperation.Post(o => OnMidiActiveSensing((string)o, 0), deviceId);
        private void MidiPlugin_OnMidiSystemReset(string deviceId) => asyncOperation.Post(o => OnMidiReset((string)o, 0), deviceId);
#endif

        /// <summary>
        /// Starts to scan BLE MIDI devices
        /// for Android / iOS devices only
        /// </summary>
        /// <param name="timeout">timeout milliseconds, 0 : no timeout</param>
        public void StartScanBluetoothMidiDevices(int timeout)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            CheckPermission();
            if (permissionRequested)
            {
                onInitializeCompleted = () =>
                {
                    if (Thread.CurrentThread != mainThread)
                    {
                        AndroidJNI.AttachCurrentThread();
                    }
                    Instance.bleMidiPlugin.Call("startScanDevice", timeout);
                    if (Thread.CurrentThread != mainThread)
                    {
                        AndroidJNI.DetachCurrentThread();
                    }
                };
            }
            else
            {
                if (Thread.CurrentThread != mainThread)
                {
                    AndroidJNI.AttachCurrentThread();
                }
                Instance.bleMidiPlugin.Call("startScanDevice", timeout);
                if (Thread.CurrentThread != mainThread)
                {
                    AndroidJNI.DetachCurrentThread();
                }
            }
#elif UNITY_IOS && !UNITY_EDITOR
            startScanBluetoothMidiDevices();
#else
            throw new NotImplementedException("this platform isn't available");
#endif
        }

        /// <summary>
        /// Stops to scan BLE MIDI devices
        /// for Android / iOS devices only
        /// </summary>
        public void StopScanBluetoothMidiDevices()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.bleMidiPlugin.Call("stopScanDevice");
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS && !UNITY_EDITOR
            stopScanBluetoothMidiDevices();
#else
            throw new NotImplementedException("this platform isn't available");
#endif
        }

        /// <summary>
        /// Start to advertise BLE MIDI Peripheral device
        /// for Android devices only
        /// </summary>
        /// <exception cref="NotImplementedException">the platform isn't available</exception>
        public void StartAdvertisingBluetoothMidiDevice()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.bleMidiPlugin.Call("startAdvertising");
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#else
            throw new NotImplementedException("this platform isn't available");
#endif
        }

        /// <summary>
        /// Stop to advertise BLE MIDI Peripheral device
        /// for Android devices only
        /// </summary>
        /// <exception cref="NotImplementedException">the platform isn't available</exception>
        public void StopAdvertisingBluetoothMidiDevice()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.bleMidiPlugin.Call("stopAdvertising");
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#else
            throw new NotImplementedException("this platform isn't available");
#endif
        }

        private void OnApplicationQuit()
        {
            // terminates MIDI system if not terminated
            TerminateMidi();
        }

        /// <summary>
        /// Terminates MIDI Plugin system
        /// </summary>
        public void TerminateMidi()
        {
#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
            StopAllRtpMidi();
#endif

            // close all sequencer threads
            SequencerImpl.CloseAllSequencers();

#if UNITY_EDITOR
#if UNITY_EDITOR_LINUX
            TerminateMidiLinux();
#endif
#if UNITY_EDITOR_WIN
            MidiPlugin.Instance.OnMidiInputDeviceAttached -= OnMidiInputDeviceAttached;
            MidiPlugin.Instance.OnMidiInputDeviceDetached -= OnMidiInputDeviceDetached;
            MidiPlugin.Instance.OnMidiOutputDeviceAttached -= OnMidiOutputDeviceAttached;
            MidiPlugin.Instance.OnMidiOutputDeviceDetached -= OnMidiOutputDeviceDetached;

            MidiPlugin.Instance.OnMidiNoteOn -= MidiPlugin_OnMidiNoteOn;
            MidiPlugin.Instance.OnMidiNoteOff -= MidiPlugin_OnMidiNoteOff;
            MidiPlugin.Instance.OnMidiPolyphonicKeyPressure -= MidiPlugin_OnMidiPolyphonicKeyPressure;
            MidiPlugin.Instance.OnMidiControlChange -= MidiPlugin_OnMidiControlChange;
            MidiPlugin.Instance.OnMidiProgramChange -= MidiPlugin_OnMidiProgramChange;
            MidiPlugin.Instance.OnMidiChannelPressure -= MidiPlugin_OnMidiChannelPressure;
            MidiPlugin.Instance.OnMidiPitchBendChange -= MidiPlugin_OnMidiPitchBendChange;
            MidiPlugin.Instance.OnMidiSystemExclusive -= MidiPlugin_OnMidiSystemExclusive;
            MidiPlugin.Instance.OnMidiTimeCode -= MidiPlugin_OnMidiTimeCode;
            MidiPlugin.Instance.OnMidiSongPositionPointer -= MidiPlugin_OnMidiSongPositionPointer;
            MidiPlugin.Instance.OnMidiSongSelect -= MidiPlugin_OnMidiSongSelect;
            MidiPlugin.Instance.OnMidiTuneRequest -= MidiPlugin_OnMidiTuneRequest;
            MidiPlugin.Instance.OnMidiTimingClock -= MidiPlugin_OnMidiTimingClock;
            MidiPlugin.Instance.OnMidiStart -= MidiPlugin_OnMidiStart;
            MidiPlugin.Instance.OnMidiContinue -= MidiPlugin_OnMidiContinue;
            MidiPlugin.Instance.OnMidiStop -= MidiPlugin_OnMidiStop;
            MidiPlugin.Instance.OnMidiActiveSensing -= MidiPlugin_OnMidiActiveSensing;
            MidiPlugin.Instance.OnMidiSystemReset -= MidiPlugin_OnMidiSystemReset;
#endif
#if UNITY_EDITOR_OSX
            midiPluginStopForEditor();
#endif
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            if (Instance.usbMidiPlugin != null)
            {
                Instance.usbMidiPlugin.Call("terminate");
                Instance.usbMidiPlugin = null;
            }

            if (Instance.bleMidiPlugin != null)
            {
                Instance.bleMidiPlugin.Call("terminate");
                Instance.bleMidiPlugin = null;
            }
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            midiPluginTerminate();
#elif UNITY_WSA || UNITY_STANDALONE_WIN
            MidiPlugin.Instance.OnMidiInputDeviceAttached -= OnMidiInputDeviceAttached;
            MidiPlugin.Instance.OnMidiInputDeviceDetached -= OnMidiInputDeviceDetached;
            MidiPlugin.Instance.OnMidiOutputDeviceAttached -= OnMidiOutputDeviceAttached;
            MidiPlugin.Instance.OnMidiOutputDeviceDetached -= OnMidiOutputDeviceDetached;

            MidiPlugin.Instance.OnMidiNoteOn -= MidiPlugin_OnMidiNoteOn;
            MidiPlugin.Instance.OnMidiNoteOff -= MidiPlugin_OnMidiNoteOff;
            MidiPlugin.Instance.OnMidiPolyphonicKeyPressure -= MidiPlugin_OnMidiPolyphonicKeyPressure;
            MidiPlugin.Instance.OnMidiControlChange -= MidiPlugin_OnMidiControlChange;
            MidiPlugin.Instance.OnMidiProgramChange -= MidiPlugin_OnMidiProgramChange;
            MidiPlugin.Instance.OnMidiChannelPressure -= MidiPlugin_OnMidiChannelPressure;
            MidiPlugin.Instance.OnMidiPitchBendChange -= MidiPlugin_OnMidiPitchBendChange;
            MidiPlugin.Instance.OnMidiSystemExclusive -= MidiPlugin_OnMidiSystemExclusive;
            MidiPlugin.Instance.OnMidiTimeCode -= MidiPlugin_OnMidiTimeCode;
            MidiPlugin.Instance.OnMidiSongPositionPointer -= MidiPlugin_OnMidiSongPositionPointer;
            MidiPlugin.Instance.OnMidiSongSelect -= MidiPlugin_OnMidiSongSelect;
            MidiPlugin.Instance.OnMidiTuneRequest -= MidiPlugin_OnMidiTuneRequest;
            MidiPlugin.Instance.OnMidiTimingClock -= MidiPlugin_OnMidiTimingClock;
            MidiPlugin.Instance.OnMidiStart -= MidiPlugin_OnMidiStart;
            MidiPlugin.Instance.OnMidiContinue -= MidiPlugin_OnMidiContinue;
            MidiPlugin.Instance.OnMidiStop -= MidiPlugin_OnMidiStop;
            MidiPlugin.Instance.OnMidiActiveSensing -= MidiPlugin_OnMidiActiveSensing;
            MidiPlugin.Instance.OnMidiSystemReset -= MidiPlugin_OnMidiSystemReset;
#elif UNITY_STANDALONE_LINUX
            TerminateMidiLinux();
#endif
        }

        private readonly HashSet<GameObject> midiDeviceEventHandlers = new HashSet<GameObject>();

        /// <summary>
        /// Registers Unity GameObject to receive MIDI events, and Connection events
        /// </summary>
        /// <param name="eventHandler"></param>
        public void RegisterEventHandleObject(GameObject eventHandler)
        {
            midiDeviceEventHandlers.Add(eventHandler);
        }

        private Dictionary<string, string> deviceNameCache = new Dictionary<string, string>();

        /// <summary>
        /// Obtains device name for deviceId
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public string GetDeviceName(string deviceId)
        {
            lock (deviceNameCache)
            {
                if (deviceNameCache.TryGetValue(deviceId, out var deviceName))
                {
                    return deviceName;
                }
            }

#if UNITY_EDITOR
#if UNITY_EDITOR_WIN
            {
                var deviceName = MidiPlugin.Instance.GetDeviceName(deviceId);
                if (!string.IsNullOrEmpty(deviceName))
                {
                    lock (deviceNameCache)
                    {
                        deviceNameCache[deviceId] = deviceName;
                    }
                    return deviceName;
                }
            }
#endif
#if UNITY_EDITOR_OSX
            {
                var deviceName = getDeviceName(deviceId);
                if (!string.IsNullOrEmpty(deviceName))
                {
                    lock (deviceNameCache)
                    {
                        deviceNameCache[deviceId] = deviceName;
                    }
                    return deviceName;
                }
            }
#endif
#if UNITY_EDITOR_LINUX
            {
                var deviceName = GetDeviceNameLinux(deviceId);
                if (!string.IsNullOrEmpty(deviceName))
                {
                    lock (deviceNameCache)
                    {
                        deviceNameCache[deviceId] = deviceName;
                    }
                    return GetDeviceNameLinux(deviceId);
                }
            }
#endif
#elif UNITY_ANDROID
            if (Instance.usbMidiPlugin != null)
            {
                if (Thread.CurrentThread != mainThread)
                {
                    AndroidJNI.AttachCurrentThread();
                }
                var result = Instance.usbMidiPlugin.Call<string>("getDeviceName", deviceId);
                if (Thread.CurrentThread != mainThread)
                {
                    AndroidJNI.DetachCurrentThread();
                }
                if (result != null)
                {
                    if (!deviceNameCache.ContainsKey(deviceId))
                    {
                        lock (deviceNameCache)
                        {
                            deviceNameCache[deviceId] = result;
                        }
                    }
                    return result;
                }
            }

            if (Instance.bleMidiPlugin != null)
            {
                if (Thread.CurrentThread != mainThread)
                {
                    AndroidJNI.AttachCurrentThread();
                }
                var result = Instance.bleMidiPlugin.Call<string>("getDeviceName", deviceId);
                if (Thread.CurrentThread != mainThread)
                {
                    AndroidJNI.DetachCurrentThread();
                }
                if (result != null)
                {
                    if (!deviceNameCache.ContainsKey(deviceId))
                    {
                        lock (deviceNameCache)
                        {
                            deviceNameCache[deviceId] = result;
                        }
                    }
                    return result;
                }
            }
#elif UNITY_IOS
            {
                var deviceName = getDeviceName(deviceId);
                if (!string.IsNullOrEmpty(deviceName))
                {
                    lock (deviceNameCache)
                    {
                        deviceNameCache[deviceId] = deviceName;
                    }
                    return deviceName;
                }
            }
#elif UNITY_WSA || UNITY_STANDALONE_WIN
            {
                var deviceName = MidiPlugin.Instance.GetDeviceName(deviceId);
                if (!string.IsNullOrEmpty(deviceName))
                {
                    lock (deviceNameCache)
                    {
                        deviceNameCache[deviceId] = deviceName;
                    }
                    return deviceName;
                }
            }
#elif UNITY_WEBGL
            {
                var deviceName = getDeviceName(deviceId);
                if (!string.IsNullOrEmpty(deviceName))
                {
                    lock (deviceNameCache)
                    {
                        deviceNameCache[deviceId] = deviceName;
                    }
                    return deviceName;
                }
            }
#elif UNITY_STANDALONE_OSX
            {
                var deviceName = getDeviceName(deviceId);
                if (!string.IsNullOrEmpty(deviceName))
                {
                    lock (deviceNameCache)
                    {
                        deviceNameCache[deviceId] = deviceName;
                    }
                    return deviceName;
                }
            }
#elif UNITY_WSA
            {
                var deviceName = MidiPlugin.Instance.GetDeviceName(deviceId);
                if (!string.IsNullOrEmpty(deviceName))
                {
                    lock (deviceNameCache)
                    {
                        deviceNameCache[deviceId] = deviceName;
                    }
                    return deviceName;
                }
            }
#elif UNITY_STANDALONE_LINUX
            {
                var deviceName = GetDeviceNameLinux(deviceId);
                if (!string.IsNullOrEmpty(deviceName))
                {
                    lock (deviceNameCache)
                    {
                        deviceNameCache[deviceId] = deviceName;
                    }
                    return deviceName;
                }
            }
#endif

#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
            lock (rtpMidiServers)
            {
                foreach (var rtpMidiServer in rtpMidiServers)
                {
                    var deviceName = rtpMidiServer.Value.GetDeviceName(deviceId);
                    if (!string.IsNullOrEmpty(deviceName))
                    {
                        lock (deviceNameCache)
                        {
                            deviceNameCache[deviceId] = deviceName;
                        }
                        return deviceName;
                    }
                }
            }
#endif
            return string.Empty;
        }

        private class MidiMessage
        {
            internal string DeviceId;
            internal int Group;
            internal decimal[] Messages;
            internal byte[] SystemExclusive;
        }

        private static MidiMessage DeserializeMidiMessage(string midiMessage, bool isSysEx = false)
        {
            var split = midiMessage.Split(',');
            decimal[] midiMessageArray = null;
            byte[] systemExclusive = null;
            if (isSysEx)
            {
                systemExclusive = new byte[split.Length - 2];
                for (var i = 2; i < split.Length; i++)
                {
                    systemExclusive[i - 2] = byte.Parse(split[i]);
                }
            }
            else
            {
                midiMessageArray = new decimal[split.Length - 2];
                for (var i = 2; i < split.Length; i++)
                {
                    midiMessageArray[i - 2] = int.Parse(split[i]);
                }
            }

            return new MidiMessage
            {
                Messages = midiMessageArray,
                DeviceId = split[0],
                Group = int.Parse(split[1]),
                SystemExclusive = systemExclusive,
            };
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static string SerializeMidiMessage(string deviceId, decimal[] midiMessage)
        {
            var sb = new StringBuilder();
            sb.Append(deviceId);
            sb.Append(",");
            for (var i = 0; i < midiMessage.Length; i++)
            {
                sb.Append(midiMessage[i]);
                if (i == midiMessage.Length - 1)
                {
                    return sb.ToString();
                }

                sb.Append(",");
            }

            return sb.ToString();
        }
#endif

        private class MidiEventData : BaseEventData
        {
            internal readonly MidiMessage Message;

            public MidiEventData(MidiMessage message, EventSystem eventSystem) : base(eventSystem)
            {
                Message = message;
            }
        }

        private class MidiDeviceEventData : BaseEventData
        {
            internal readonly string DeviceId;

            public MidiDeviceEventData(string deviceId, EventSystem eventSystem) : base(eventSystem)
            {
                DeviceId = deviceId;
            }
        }

        /// <summary>
        /// Sends a Note On message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="group">0-15</param>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="velocity">0-127</param>
        public void SendMidiNoteOn(string deviceId, int group, int channel, int note, int velocity)
        {
#if UNITY_EDITOR
#if UNITY_EDITOR_LINUX
            SendMidiNoteOn(deviceId, (byte)channel, (byte)note, (byte)velocity);
#endif
#if UNITY_EDITOR_OSX
            sendMidiData(deviceId, new[] {(byte) (0x90 | channel), (byte) note, (byte) velocity}, 3);
#endif
#if UNITY_EDITOR_WIN
            MidiPlugin.Instance.SendMidiNoteOn(deviceId, (byte)channel, (byte)note, (byte)velocity);
#endif
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.usbMidiPlugin.Call("sendMidiNoteOn", deviceId, group, channel, note, velocity);
            Instance.bleMidiPlugin.Call("sendMidiNoteOn", deviceId, channel, note, velocity);
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            sendMidiData(deviceId, new[] {(byte) (0x90 | channel), (byte) note, (byte) velocity}, 3);
#elif UNITY_WSA || UNITY_STANDALONE_WIN
            MidiPlugin.Instance.SendMidiNoteOn(deviceId, (byte)channel, (byte)note, (byte)velocity);
#elif UNITY_STANDALONE_LINUX
            SendMidiNoteOn(deviceId, (byte)channel, (byte)note, (byte)velocity);
#elif UNITY_WEBGL
            sendMidiNoteOn(deviceId, (byte)channel, (byte)note, (byte)velocity);
#endif

#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
            lock (rtpMidiServers)
            {
                var port = RtpMidiSession.GetPortFromDeviceId(deviceId);
                if (rtpMidiServers.TryGetValue(port, out var rtpMidiServer))
                {
                    rtpMidiServer.SendMidiNoteOn(deviceId, channel, note, velocity);
                }
            }
#endif
        }

        /// <summary>
        /// Sends a Note Off message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="group">0-15</param>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="velocity">0-127</param>
        public void SendMidiNoteOff(string deviceId, int group, int channel, int note, int velocity)
        {
#if UNITY_EDITOR
#if UNITY_EDITOR_LINUX
            SendMidiNoteOff(deviceId, (byte)channel, (byte)note, (byte)velocity);
#endif
#if UNITY_EDITOR_OSX
            sendMidiData(deviceId, new[] {(byte) (0x80 | channel), (byte) note, (byte) velocity}, 3);
#endif
#if UNITY_EDITOR_WIN
            MidiPlugin.Instance.SendMidiNoteOff(deviceId, (byte)channel, (byte)note, (byte)velocity);
#endif
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.usbMidiPlugin.Call("sendMidiNoteOff", deviceId, group, channel, note, velocity);
            Instance.bleMidiPlugin.Call("sendMidiNoteOff", deviceId, channel, note, velocity);
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            sendMidiData(deviceId, new[] {(byte) (0x80 | channel), (byte) note, (byte) velocity}, 3);
#elif UNITY_WSA || UNITY_STANDALONE_WIN
            MidiPlugin.Instance.SendMidiNoteOff(deviceId, (byte)channel, (byte)note, (byte)velocity);
#elif UNITY_STANDALONE_LINUX
            SendMidiNoteOff(deviceId, (byte)channel, (byte)note, (byte)velocity);
#elif UNITY_WEBGL
            sendMidiNoteOff(deviceId, (byte)channel, (byte)note, (byte)velocity);
#endif
#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
            lock (rtpMidiServers)
            {
                var port = RtpMidiSession.GetPortFromDeviceId(deviceId);
                if (rtpMidiServers.TryGetValue(port, out var rtpMidiServer))
                {
                    rtpMidiServer.SendMidiNoteOff(deviceId, channel, note, velocity);
                }
            }
#endif
        }

        /// <summary>
        /// Sends a Polyphonic Aftertouch message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="group">0-15</param>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="pressure">0-127</param>
        public void SendMidiPolyphonicAftertouch(string deviceId, int group, int channel, int note, int pressure)
        {
#if UNITY_EDITOR
#if UNITY_EDITOR_LINUX
            SendMidiPolyphonicAftertouch(deviceId, (byte)channel, (byte)note, (byte)pressure);
#endif
#if UNITY_EDITOR_OSX
            sendMidiData(deviceId, new[] {(byte) (0xa0 | channel), (byte) note, (byte) pressure}, 3);
#endif
#if UNITY_EDITOR_WIN
            MidiPlugin.Instance.SendMidiPolyphonicKeyPressure(deviceId, (byte)channel, (byte)note, (byte)pressure);
#endif
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.usbMidiPlugin.Call("sendMidiPolyphonicAftertouch", deviceId, group, channel, note, pressure);
            Instance.bleMidiPlugin.Call("sendMidiPolyphonicAftertouch", deviceId, channel, note, pressure);
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            sendMidiData(deviceId, new[] {(byte) (0xa0 | channel), (byte) note, (byte) pressure}, 3);
#elif UNITY_WSA || UNITY_STANDALONE_WIN
            MidiPlugin.Instance.SendMidiPolyphonicKeyPressure(deviceId, (byte)channel, (byte)note, (byte)pressure);
#elif UNITY_STANDALONE_LINUX
            SendMidiPolyphonicAftertouch(deviceId, (byte)channel, (byte)note, (byte)pressure);
#elif UNITY_WEBGL
            sendMidiPolyphonicAftertouch(deviceId, (byte)channel, (byte)note, (byte)pressure);
#endif
#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
            lock (rtpMidiServers)
            {
                var port = RtpMidiSession.GetPortFromDeviceId(deviceId);
                if (rtpMidiServers.TryGetValue(port, out var rtpMidiServer))
                {
                    rtpMidiServer.SendMidiPolyphonicAftertouch(deviceId, channel, note, pressure);
                }
            }
#endif
        }

        /// <summary>
        /// Sends a Control Change message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="group">0-15</param>
        /// <param name="channel">0-15</param>
        /// <param name="function">0-127</param>
        /// <param name="value">0-127</param>
        public void SendMidiControlChange(string deviceId, int group, int channel, int function, int value)
        {
#if UNITY_EDITOR
#if UNITY_EDITOR_LINUX
            SendMidiControlChange(deviceId, (byte)channel, (byte)function, (byte)value);
#endif
#if UNITY_EDITOR_OSX
            sendMidiData(deviceId, new[] {(byte) (0xb0 | channel), (byte) function, (byte) value}, 3);
#endif
#if UNITY_EDITOR_WIN
            MidiPlugin.Instance.SendMidiControlChange(deviceId, (byte)channel, (byte)function, (byte)value);
#endif
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.usbMidiPlugin.Call("sendMidiControlChange", deviceId, group, channel, function, value);
            Instance.bleMidiPlugin.Call("sendMidiControlChange", deviceId, channel, function, value);
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            sendMidiData(deviceId, new[] {(byte) (0xb0 | channel), (byte) function, (byte) value}, 3);
#elif UNITY_WSA || UNITY_STANDALONE_WIN
            MidiPlugin.Instance.SendMidiControlChange(deviceId, (byte)channel, (byte)function, (byte)value);
#elif UNITY_STANDALONE_LINUX
            SendMidiControlChange(deviceId, (byte)channel, (byte)function, (byte)value);
#elif UNITY_WEBGL
            sendMidiControlChange(deviceId, (byte)channel, (byte)function, (byte)value);
#endif
#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
            lock (rtpMidiServers)
            {
                var port = RtpMidiSession.GetPortFromDeviceId(deviceId);
                if (rtpMidiServers.TryGetValue(port, out var rtpMidiServer))
                {
                    rtpMidiServer.SendMidiControlChange(deviceId, channel, function, value);
                }
            }
#endif
        }

        /// <summary>
        /// Sends a Program Change message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="group">0-15</param>
        /// <param name="channel">0-15</param>
        /// <param name="program">0-127</param>
        public void SendMidiProgramChange(string deviceId, int group, int channel, int program)
        {
#if UNITY_EDITOR
#if UNITY_EDITOR_LINUX
            SendMidiProgramChange(deviceId, (byte)channel, (byte)program);
#endif
#if UNITY_EDITOR_OSX
            sendMidiData(deviceId, new[] {(byte) (0xc0 | channel), (byte) program}, 2);
#endif
#if UNITY_EDITOR_WIN
            MidiPlugin.Instance.SendMidiProgramChange(deviceId, (byte)channel, (byte)program);
#endif
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.usbMidiPlugin.Call("sendMidiProgramChange", deviceId, group, channel, program);
            Instance.bleMidiPlugin.Call("sendMidiProgramChange", deviceId, channel, program);
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            sendMidiData(deviceId, new[] {(byte) (0xc0 | channel), (byte) program}, 2);
#elif UNITY_WSA || UNITY_STANDALONE_WIN
            MidiPlugin.Instance.SendMidiProgramChange(deviceId, (byte)channel, (byte)program);
#elif UNITY_STANDALONE_LINUX
            SendMidiProgramChange(deviceId, (byte)channel, (byte)program);
#elif UNITY_WEBGL
            sendMidiProgramChange(deviceId, (byte)channel, (byte)program);
#endif
#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
            lock (rtpMidiServers)
            {
                var port = RtpMidiSession.GetPortFromDeviceId(deviceId);
                if (rtpMidiServers.TryGetValue(port, out var server))
                {
                    server.SendMidiProgramChange(deviceId, channel, program);
                }
            }
#endif
        }

        /// <summary>
        /// Sends a Channel Aftertouch message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="group">0-15</param>
        /// <param name="channel">0-15</param>
        /// <param name="pressure">0-127</param>
        public void SendMidiChannelAftertouch(string deviceId, int group, int channel, int pressure)
        {
#if UNITY_EDITOR
#if UNITY_EDITOR_LINUX
            SendMidiChannelAftertouch(deviceId, (byte)channel, (byte)pressure);
#endif
#if UNITY_EDITOR_OSX
            sendMidiData(deviceId, new[] {(byte) (0xd0 | channel), (byte) pressure}, 2);
#endif
#if UNITY_EDITOR_WIN
            MidiPlugin.Instance.SendMidiChannelPressure(deviceId, (byte)channel, (byte)pressure);
#endif
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.usbMidiPlugin.Call("sendMidiChannelAftertouch", deviceId, group, channel, pressure);
            Instance.bleMidiPlugin.Call("sendMidiChannelAftertouch", deviceId, channel, pressure);
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            sendMidiData(deviceId, new[] {(byte) (0xd0 | channel), (byte) pressure}, 2);
#elif UNITY_WSA || UNITY_STANDALONE_WIN
            MidiPlugin.Instance.SendMidiChannelPressure(deviceId, (byte)channel, (byte)pressure);
#elif UNITY_STANDALONE_LINUX
            SendMidiChannelAftertouch(deviceId, (byte)channel, (byte)pressure);
#elif UNITY_WEBGL
            sendMidiChannelAftertouch(deviceId, (byte)channel, (byte)pressure);
#endif
#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
            lock (rtpMidiServers)
            {
                var port = RtpMidiSession.GetPortFromDeviceId(deviceId);
                if (rtpMidiServers.TryGetValue(port, out var rtpMidiServer))
                {
                    rtpMidiServer.SendMidiChannelAftertouch(deviceId, channel, pressure);
                }
            }
#endif
        }

        /// <summary>
        /// Sends a Pitch Wheel message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="group">0-15</param>
        /// <param name="channel">0-15</param>
        /// <param name="amount">0-16383</param>
        public void SendMidiPitchWheel(string deviceId, int group, int channel, int amount)
        {
#if UNITY_EDITOR
#if UNITY_EDITOR_LINUX
            SendMidiPitchWheel(deviceId, (byte)channel, (short)amount);
#endif
#if UNITY_EDITOR_OSX
            sendMidiData(deviceId, new[] {(byte) (0xe0 | channel), (byte) (amount & 0x7f), (byte) ((amount >> 7) & 0x7f)}, 3);
#endif
#if UNITY_EDITOR_WIN
            MidiPlugin.Instance.SendMidiPitchBendChange(deviceId, (byte)channel, (ushort)amount);
#endif
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.usbMidiPlugin.Call("sendMidiPitchWheel", deviceId, group, channel, amount);
            Instance.bleMidiPlugin.Call("sendMidiPitchWheel", deviceId, channel, amount);
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            sendMidiData(deviceId, new[] {(byte) (0xe0 | channel), (byte) (amount & 0x7f), (byte) ((amount >> 7) & 0x7f)}, 3);
#elif UNITY_WSA || UNITY_STANDALONE_WIN
            MidiPlugin.Instance.SendMidiPitchBendChange(deviceId, (byte)channel, (ushort)amount);
#elif UNITY_STANDALONE_LINUX
            SendMidiPitchWheel(deviceId, (byte)channel, (short)amount);
#elif UNITY_WEBGL
            sendMidiPitchWheel(deviceId, (byte)channel, (byte)amount);
#endif
#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
            lock (rtpMidiServers)
            {
                var port = RtpMidiSession.GetPortFromDeviceId(deviceId);
                if (rtpMidiServers.TryGetValue(port, out var rtpMidiServer))
                {
                    rtpMidiServer.SendMidiPitchWheel(deviceId, channel, amount);
                }
            }
#endif
        }

        /// <summary>
        /// Sends a System Exclusive message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="group">0-15</param>
        /// <param name="sysEx">byte array starts with F0, ends with F7</param>
        public void SendMidiSystemExclusive(string deviceId, int group, byte[] sysEx)
        {
#if UNITY_EDITOR
#if UNITY_EDITOR_LINUX
            SendMidiSystemExclusive(deviceId, sysEx, sysEx.Length);
#endif
#if UNITY_EDITOR_OSX
            sendMidiData(deviceId, sysEx, sysEx.Length);
#endif
#if UNITY_EDITOR_WIN
            MidiPlugin.Instance.SendMidiSystemExclusive(deviceId, sysEx);
#endif
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.usbMidiPlugin.Call("sendMidiSystemExclusive", deviceId, group, Array.ConvertAll(sysEx, b => unchecked((sbyte)b)));
            Instance.bleMidiPlugin.Call("sendMidiSystemExclusive", deviceId, Array.ConvertAll(sysEx, b => unchecked((sbyte)b)));
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            sendMidiData(deviceId, sysEx, sysEx.Length);
#elif UNITY_WSA || UNITY_STANDALONE_WIN
            MidiPlugin.Instance.SendMidiSystemExclusive(deviceId, sysEx);
#elif UNITY_STANDALONE_LINUX
            SendMidiSystemExclusive(deviceId, sysEx, sysEx.Length);
#elif UNITY_WEBGL
            sendMidiSystemExclusive(deviceId, sysEx);
#endif
#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
            lock (rtpMidiServers)
            {
                var port = RtpMidiSession.GetPortFromDeviceId(deviceId);
                if (rtpMidiServers.TryGetValue(port, out var rtpMidiServer))
                {
                    rtpMidiServer.SendMidiSystemExclusive(deviceId, sysEx);
                }
            }
#endif
        }

        /// <summary>
        /// Sends a System Common message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="group">0-15</param>
        /// <param name="message">0-255</param>
        public void SendMidiSystemCommonMessage(string deviceId, int group, int message)
        {
#if UNITY_EDITOR
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.usbMidiPlugin.Call("sendMidiSystemCommonMessage", deviceId, group, message);
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            sendMidiData(deviceId, new[] {(byte) message}, 1);
#endif
        }

        /// <summary>
        /// Sends a Single Byte message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="group">0-15</param>
        /// <param name="byte1">0-255</param>
        public void SendMidiSingleByte(string deviceId, int group, int byte1)
        {
#if UNITY_EDITOR
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.usbMidiPlugin.Call("sendMidiSingleByte", deviceId, group, byte1);
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            sendMidiData(deviceId, new[] {(byte) byte1}, 1);
#endif
        }

        /// <summary>
        /// Sends a Time Code Quarter Frame message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="group">0-15</param>
        /// <param name="timing">0-127</param>
        public void SendMidiTimeCodeQuarterFrame(string deviceId, int group, int timing)
        {
#if UNITY_EDITOR
#if UNITY_EDITOR_LINUX
            SendMidiTimeCodeQuarterFrame(deviceId, (byte)timing);
#endif
#if UNITY_EDITOR_OSX
            sendMidiData(deviceId, new[] {(byte) 0xf1, (byte) timing}, 2);
#endif
#if UNITY_EDITOR_WIN
            MidiPlugin.Instance.SendMidiTimeCode(deviceId, (byte)(timing >> 4), (byte)(timing & 0xf));
#endif
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.usbMidiPlugin.Call("sendMidiTimeCodeQuarterFrame", deviceId, group, timing);
            Instance.bleMidiPlugin.Call("sendMidiTimeCodeQuarterFrame", deviceId, timing);
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            sendMidiData(deviceId, new[] {(byte) 0xf1, (byte) timing}, 2);
#elif UNITY_WSA || UNITY_STANDALONE_WIN
            MidiPlugin.Instance.SendMidiTimeCode(deviceId, (byte)(timing >> 4), (byte)(timing & 0xf));
#elif UNITY_STANDALONE_LINUX
            SendMidiTimeCodeQuarterFrame(deviceId, (byte)timing);
#elif UNITY_WEBGL
            sendMidiTimeCodeQuarterFrame(deviceId, timing);
#endif
#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
            lock (rtpMidiServers)
            {
                var port = RtpMidiSession.GetPortFromDeviceId(deviceId);
                if (rtpMidiServers.TryGetValue(port, out var rtpMidiServer))
                {
                    rtpMidiServer.SendMidiTimeCodeQuarterFrame(deviceId, timing);
                }
            }
#endif
        }

        /// <summary>
        /// Sends a Song Select message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="group">0-15</param>
        /// <param name="song">0-127</param>
        public void SendMidiSongSelect(string deviceId, int group, int song)
        {
#if UNITY_EDITOR
#if UNITY_EDITOR_LINUX
            SendMidiSongSelect(deviceId, (byte)song);
#endif
#if UNITY_EDITOR_OSX
            sendMidiData(deviceId, new[] {(byte) 0xf3, (byte) song}, 2);
#endif
#if UNITY_EDITOR_WIN
            MidiPlugin.Instance.SendMidiSongSelect(deviceId, (byte)song);
#endif
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.usbMidiPlugin.Call("sendMidiSongSelect", deviceId, group, song);
            Instance.bleMidiPlugin.Call("sendMidiSongSelect", deviceId, song);
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            sendMidiData(deviceId, new[] {(byte) 0xf3, (byte) song}, 2);
#elif UNITY_WSA || UNITY_STANDALONE_WIN
            MidiPlugin.Instance.SendMidiSongSelect(deviceId, (byte)song);
#elif UNITY_STANDALONE_LINUX
            SendMidiSongSelect(deviceId, (byte)song);
#elif UNITY_WEBGL
            sendMidiSongSelect(deviceId, (byte)song);
#endif
#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
            lock (rtpMidiServers)
            {
                var port = RtpMidiSession.GetPortFromDeviceId(deviceId);
                if (rtpMidiServers.TryGetValue(port, out var rtpMidiServer))
                {
                    rtpMidiServer.SendMidiSongSelect(deviceId, song);
                }
            }
#endif
        }

        /// <summary>
        /// Sends a Song Position Pointer message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="group">0-15</param>
        /// <param name="position">0-16383</param>
        public void SendMidiSongPositionPointer(string deviceId, int group, int position)
        {
#if UNITY_EDITOR
#if UNITY_EDITOR_LINUX
            SendMidiSongPositionPointer(deviceId, (short)position);
#endif
#if UNITY_EDITOR_OSX
            sendMidiData(deviceId, new[] {(byte) 0xf2, (byte) (position & 0x7f), (byte) ((position >> 7) & 0x7f)}, 3);
#endif
#if UNITY_EDITOR_WIN
            MidiPlugin.Instance.SendMidiSongPositionPointer(deviceId, (ushort)position);
#endif
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.usbMidiPlugin.Call("sendMidiSongPositionPointer", deviceId, group, position);
            Instance.bleMidiPlugin.Call("sendMidiSongPositionPointer", deviceId, position);
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            sendMidiData(deviceId, new[] {(byte) 0xf2, (byte) (position & 0x7f), (byte) ((position >> 7) & 0x7f)}, 3);
#elif UNITY_WSA || UNITY_STANDALONE_WIN
            MidiPlugin.Instance.SendMidiSongPositionPointer(deviceId, (ushort)position);
#elif UNITY_STANDALONE_LINUX
            SendMidiSongPositionPointer(deviceId, (short)position);
#elif UNITY_WEBGL
            sendMidiSongPositionPointer(deviceId, position);
#endif
#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
            lock (rtpMidiServers)
            {
                var port = RtpMidiSession.GetPortFromDeviceId(deviceId);
                if (rtpMidiServers.TryGetValue(port, out var rtpMidiServer))
                {
                    rtpMidiServer.SendMidiSongPositionPointer(deviceId, position);
                }
            }
#endif
        }

        /// <summary>
        /// Sends a Tune Request message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="group">0-15</param>
        public void SendMidiTuneRequest(string deviceId, int group)
        {
#if UNITY_EDITOR
#if UNITY_EDITOR_LINUX
            SendMidiTuneRequest(deviceId);
#endif
#if UNITY_EDITOR_OSX
            sendMidiData(deviceId, new[] {(byte) 0xf6}, 1);
#endif
#if UNITY_EDITOR_WIN
            MidiPlugin.Instance.SendMidiTuneRequest(deviceId);
#endif
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.usbMidiPlugin.Call("sendMidiTuneRequest", deviceId, group);
            Instance.bleMidiPlugin.Call("sendMidiTuneRequest", deviceId);
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            sendMidiData(deviceId, new[] {(byte) 0xf6}, 1);
#elif UNITY_WSA || UNITY_STANDALONE_WIN
            MidiPlugin.Instance.SendMidiTuneRequest(deviceId);
#elif UNITY_STANDALONE_LINUX
            SendMidiTuneRequest(deviceId);
#elif UNITY_WEBGL
            sendMidiTuneRequest(deviceId);
#endif
#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
            lock (rtpMidiServers)
            {
                var port = RtpMidiSession.GetPortFromDeviceId(deviceId);
                if (rtpMidiServers.TryGetValue(port, out var rtpMidiServer))
                {
                    rtpMidiServer.SendMidiTuneRequest(deviceId);
                }
            }
#endif
        }

        /// <summary>
        /// Sends a Timing Clock message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="group">0-15</param>
        public void SendMidiTimingClock(string deviceId, int group)
        {
#if UNITY_EDITOR
#if UNITY_EDITOR_LINUX
            SendMidiTimingClock(deviceId);
#endif
#if UNITY_EDITOR_OSX
            sendMidiData(deviceId, new[] {(byte) 0xf8}, 1);
#endif
#if UNITY_EDITOR_WIN
            MidiPlugin.Instance.SendMidiTimingClock(deviceId);
#endif
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.usbMidiPlugin.Call("sendMidiTimingClock", deviceId, group);
            Instance.bleMidiPlugin.Call("sendMidiTimingClock", deviceId);
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            sendMidiData(deviceId, new[] {(byte) 0xf8}, 1);
#elif UNITY_WSA || UNITY_STANDALONE_WIN
            MidiPlugin.Instance.SendMidiTimingClock(deviceId);
#elif UNITY_STANDALONE_LINUX
            SendMidiTimingClock(deviceId);
#elif UNITY_WEBGL
            sendMidiTimingClock(deviceId);
#endif
#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
            lock (rtpMidiServers)
            {
                var port = RtpMidiSession.GetPortFromDeviceId(deviceId);
                if (rtpMidiServers.TryGetValue(port, out var rtpMidiServer))
                {
                    rtpMidiServer.SendMidiTimingClock(deviceId);
                }
            }
#endif
        }

        /// <summary>
        /// Sends a Start message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="group">0-15</param>
        public void SendMidiStart(string deviceId, int group)
        {
#if UNITY_EDITOR
#if UNITY_EDITOR_LINUX
            SendMidiStart(deviceId);
#endif
#if UNITY_EDITOR_OSX
            sendMidiData(deviceId, new[] {(byte) 0xfa}, 1);
#endif
#if UNITY_EDITOR_WIN
            MidiPlugin.Instance.SendMidiStart(deviceId);
#endif
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.usbMidiPlugin.Call("sendMidiStart", deviceId, group);
            Instance.bleMidiPlugin.Call("sendMidiStart", deviceId);
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            sendMidiData(deviceId, new[] {(byte) 0xfa}, 1);
#elif UNITY_WSA || UNITY_STANDALONE_WIN
            MidiPlugin.Instance.SendMidiStart(deviceId);
#elif UNITY_STANDALONE_LINUX
            SendMidiStart(deviceId);
#elif UNITY_WEBGL
            sendMidiStart(deviceId);
#endif
#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
            lock (rtpMidiServers)
            {
                var port = RtpMidiSession.GetPortFromDeviceId(deviceId);
                if (rtpMidiServers.TryGetValue(port, out var rtpMidiServer))
                {
                    rtpMidiServer.SendMidiStart(deviceId);
                }
            }
#endif
        }

        /// <summary>
        /// Sends a Continue message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="group">0-15</param>
        public void SendMidiContinue(string deviceId, int group)
        {
#if UNITY_EDITOR
#if UNITY_EDITOR_LINUX
            SendMidiContinue(deviceId);
#endif
#if UNITY_EDITOR_OSX
            sendMidiData(deviceId, new[] {(byte) 0xfb}, 1);
#endif
#if UNITY_EDITOR_WIN
            MidiPlugin.Instance.SendMidiContinue(deviceId);
#endif
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.usbMidiPlugin.Call("sendMidiContinue", deviceId, group);
            Instance.bleMidiPlugin.Call("sendMidiContinue", deviceId);
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            sendMidiData(deviceId, new[] {(byte) 0xfb}, 1);
#elif UNITY_WSA || UNITY_STANDALONE_WIN
            MidiPlugin.Instance.SendMidiContinue(deviceId);
#elif UNITY_STANDALONE_LINUX
            SendMidiContinue(deviceId);
#elif UNITY_WEBGL
            sendMidiContinue(deviceId);
#endif
#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
            lock (rtpMidiServers)
            {
                var port = RtpMidiSession.GetPortFromDeviceId(deviceId);
                if (rtpMidiServers.TryGetValue(port, out var rtpMidiServer))
                {
                    rtpMidiServer.SendMidiContinue(deviceId);
                }
            }
#endif
        }

        /// <summary>
        /// Sends a Stop message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="group">0-15</param>
        public void SendMidiStop(string deviceId, int group)
        {
#if UNITY_EDITOR
#if UNITY_EDITOR_LINUX
            SendMidiStop(deviceId);
#endif
#if UNITY_EDITOR_OSX
            sendMidiData(deviceId, new[] {(byte) 0xfc}, 1);
#endif
#if UNITY_EDITOR_WIN
            MidiPlugin.Instance.SendMidiStop(deviceId);
#endif
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.usbMidiPlugin.Call("sendMidiStop", deviceId, group);
            Instance.bleMidiPlugin.Call("sendMidiStop", deviceId);
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            sendMidiData(deviceId, new[] {(byte) 0xfc}, 1);
#elif UNITY_WSA || UNITY_STANDALONE_WIN
            MidiPlugin.Instance.SendMidiStop(deviceId);
#elif UNITY_STANDALONE_LINUX
            SendMidiStop(deviceId);
#elif UNITY_WEBGL
            sendMidiStop(deviceId);
#endif
#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
            lock (rtpMidiServers)
            {
                var port = RtpMidiSession.GetPortFromDeviceId(deviceId);
                if (rtpMidiServers.TryGetValue(port, out var rtpMidiServer))
                {
                    rtpMidiServer.SendMidiStop(deviceId);
                }
            }
#endif
        }

        /// <summary>
        /// Sends an Active Sensing message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="group">0-15</param>
        public void SendMidiActiveSensing(string deviceId, int group)
        {
#if UNITY_EDITOR
#if UNITY_EDITOR_LINUX
            SendMidiActiveSensing(deviceId);
#endif
#if UNITY_EDITOR_OSX
            sendMidiData(deviceId, new[] {(byte) 0xfe}, 1);
#endif
#if UNITY_EDITOR_WIN
            MidiPlugin.Instance.SendMidiActiveSensing(deviceId);
#endif
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.usbMidiPlugin.Call("sendMidiActiveSensing", deviceId, group);
            Instance.bleMidiPlugin.Call("sendMidiActiveSensing", deviceId);
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            sendMidiData(deviceId, new[] {(byte) 0xfe}, 1);
#elif UNITY_WSA || UNITY_STANDALONE_WIN
            MidiPlugin.Instance.SendMidiActiveSensing(deviceId);
#elif UNITY_STANDALONE_LINUX
            SendMidiActiveSensing(deviceId);
#elif UNITY_WEBGL
            sendMidiActiveSensing(deviceId);
#endif
#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
            lock (rtpMidiServers)
            {
                var port = RtpMidiSession.GetPortFromDeviceId(deviceId);
                if (rtpMidiServers.TryGetValue(port, out var rtpMidiServer))
                {
                    rtpMidiServer.SendMidiActiveSensing(deviceId);
                }
            }
#endif
        }

        /// <summary>
        /// Sends a Reset message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="group">0-15</param>
        public void SendMidiReset(string deviceId, int group)
        {
#if UNITY_EDITOR
#if UNITY_EDITOR_LINUX
            SendMidiReset(deviceId);
#endif
#if UNITY_EDITOR_OSX
            sendMidiData(deviceId, new[] {(byte) 0xff}, 1);
#endif
#if UNITY_EDITOR_WIN
            MidiPlugin.Instance.SendMidiSystemReset(deviceId);
#endif
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.usbMidiPlugin.Call("sendMidiReset", deviceId, group);
            Instance.bleMidiPlugin.Call("sendMidiReset", deviceId);
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            sendMidiData(deviceId, new[] {(byte) 0xff}, 1);
#elif UNITY_WSA || UNITY_STANDALONE_WIN
            MidiPlugin.Instance.SendMidiSystemReset(deviceId);
#elif UNITY_STANDALONE_LINUX
            SendMidiReset(deviceId);
#elif UNITY_WEBGL
            sendMidiReset(deviceId);
#endif
#if (!UNITY_IOS && !UNITY_WEBGL) || UNITY_EDITOR
            lock (rtpMidiServers)
            {
                var port = RtpMidiSession.GetPortFromDeviceId(deviceId);
                if (rtpMidiServers.TryGetValue(port, out var rtpMidiServer))
                {
                    rtpMidiServer.SendMidiReset(deviceId);
                }
            }
#endif
        }

        /// <summary>
        /// Sends a Miscellaneous Function Codes message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="group">0-15</param>
        /// <param name="byte1"></param>
        /// <param name="byte2"></param>
        /// <param name="byte3"></param>
        public void SendMidiMiscellaneousFunctionCodes(string deviceId, int group, int byte1, int byte2, int byte3)
        {
#if UNITY_EDITOR
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.usbMidiPlugin.Call("sendMidiMiscellaneousFunctionCodes", deviceId, group, byte1, byte2, byte3);
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            sendMidiData(deviceId, new[] {(byte) byte1, (byte) byte2, (byte) byte3}, 3);
#endif
        }

        /// <summary>
        /// Sends a Cable Events message
        /// </summary>
        /// <param name="deviceId">the Device Id</param>
        /// <param name="group">0-15</param>
        /// <param name="byte1">0-255</param>
        /// <param name="byte2">0-255</param>
        /// <param name="byte3">0-255</param>
        public void SendMidiCableEvents(string deviceId, int group, int byte1, int byte2, int byte3)
        {
#if UNITY_EDITOR
#elif UNITY_ANDROID
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.AttachCurrentThread();
            }
            Instance.usbMidiPlugin.Call("sendMidiCableEvents", deviceId, group, byte1, byte2, byte3);
            if (Thread.CurrentThread != mainThread)
            {
                AndroidJNI.DetachCurrentThread();
            }
#elif UNITY_IOS || UNITY_STANDALONE_OSX
            sendMidiData(deviceId, new[] {(byte) byte1, (byte) byte2, (byte) byte3}, 3);
#endif
        }

        private void OnMidiInputDeviceAttached(string deviceId)
        {
            lock (deviceIdSet)
            {
                deviceIdSet.Add(deviceId);
            }

            MidiSystem.AddTransmitter(deviceId, new TransmitterImpl(deviceId));

            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiDeviceEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiDeviceEventHandler>(midiDeviceEventHandler,
                    new MidiDeviceEventData(deviceId, EventSystem.current), (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiDeviceEventData midiDeviceEventData)
                        {
                            eventHandler.OnMidiInputDeviceAttached(midiDeviceEventData.DeviceId);
                        }
                    });
            }
        }

        private void OnMidiOutputDeviceAttached(string deviceId)
        {
            lock (deviceIdSet)
            {
                deviceIdSet.Add(deviceId);
            }

            MidiSystem.AddReceiver(deviceId, new ReceiverImpl(deviceId));

            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiDeviceEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiDeviceEventHandler>(midiDeviceEventHandler,
                    new MidiDeviceEventData(deviceId, EventSystem.current), (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiDeviceEventData midiDeviceEventData)
                        {
                            eventHandler.OnMidiOutputDeviceAttached(midiDeviceEventData.DeviceId);
                        }
                    });
            }
        }

        private void OnMidiInputDeviceDetached(string deviceId)
        {
            lock (deviceIdSet)
            {
                deviceIdSet.Remove(deviceId);
            }

            lock (deviceNameCache)
            {
                if (deviceNameCache.ContainsKey(deviceId))
                {
                    deviceNameCache.Remove(deviceId);
                }
            }

            MidiSystem.RemoveTransmitter(deviceId);

            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiDeviceEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiDeviceEventHandler>(midiDeviceEventHandler,
                    new MidiDeviceEventData(deviceId, EventSystem.current), (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiDeviceEventData midiDeviceEventData)
                        {
                            eventHandler.OnMidiInputDeviceDetached(midiDeviceEventData.DeviceId);
                        }
                    });
            }
        }

        private void OnMidiOutputDeviceDetached(string deviceId)
        {
            lock (deviceIdSet)
            {
                deviceIdSet.Remove(deviceId);
            }

            lock (deviceNameCache)
            {
                if (deviceNameCache.ContainsKey(deviceId))
                {
                    deviceNameCache.Remove(deviceId);
                }
            }

            MidiSystem.RemoveReceiver(deviceId);

            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiDeviceEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiDeviceEventHandler>(midiDeviceEventHandler,
                    new MidiDeviceEventData(deviceId, EventSystem.current), (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiDeviceEventData midiDeviceEventData)
                        {
                            eventHandler.OnMidiOutputDeviceDetached(midiDeviceEventData.DeviceId);
                        }
                    });
            }
        }

        private static void SendMidiMessageToTransmitters(MidiEventData eventData, int status)
        {
            var transmitters = MidiSystem.GetTransmitters();
            var parsed = eventData.Message;
            try
            {
                ShortMessage message;
                if (parsed.Messages == null)
                {
                    message = new ShortMessage(status, 0, 0);
                }
                else
                {
                    message = new ShortMessage(
                        status | (parsed.Messages.Length > 0 ? (int)parsed.Messages[0] : 0),
                        parsed.Messages.Length > 1 ? (int)parsed.Messages[1] : 0,
                        parsed.Messages.Length > 2 ? (int)parsed.Messages[2] : 0);
                }

                foreach (var transmitter in transmitters)
                {
                    transmitter.GetReceiver()?.Send(message, 0);
                }
            }
            catch (InvalidMidiDataException)
            {
                // ignore invalid message
            }
        }

#if UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        [AOT.MonoPInvokeCallback(typeof(OnMidiInputDeviceAttachedDelegate))]
        private static void IosOnMidiInputDeviceAttached(string deviceId) =>
            Instance.asyncOperation.Post(o => Instance.OnMidiInputDeviceAttached((string)o), deviceId);
        [AOT.MonoPInvokeCallback(typeof(OnMidiInputDeviceAttachedDelegate))]
        private static void IosOnMidiOutputDeviceAttached(string deviceId) =>
            Instance.asyncOperation.Post(o => Instance.OnMidiOutputDeviceAttached((string)o), deviceId);
        [AOT.MonoPInvokeCallback(typeof(OnMidiOutputDeviceDetachedDelegate))]
        private static void IosOnMidiInputDeviceDetached(string deviceId) =>
            Instance.asyncOperation.Post(o => Instance.OnMidiInputDeviceDetached((string)o), deviceId);
        [AOT.MonoPInvokeCallback(typeof(OnMidiInputDeviceDetachedDelegate))]
        private static void IosOnMidiOutputDeviceDetached(string deviceId) =>
            Instance.asyncOperation.Post(o => Instance.OnMidiOutputDeviceDetached((string)o), deviceId);

        [AOT.MonoPInvokeCallback(typeof(OnMidiNoteOnDelegate))]
        private static void IosOnMidiNoteOn(string deviceId, int group, int channel, int note, int velocity) =>
            Instance.asyncOperation.Post(o => Instance.OnMidiNoteOn((string)((object[])o)[0], (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3], (int)((object[])o)[4]), new object[] {deviceId, group, channel, note, velocity});
        [AOT.MonoPInvokeCallback(typeof(OnMidiNoteOffDelegate))]
        private static void IosOnMidiNoteOff(string deviceId, int group, int channel, int note, int velocity) =>
            Instance.asyncOperation.Post(o => Instance.OnMidiNoteOff((string)((object[])o)[0], (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3], (int)((object[])o)[4]), new object[] {deviceId, group, channel, note, velocity});
        [AOT.MonoPInvokeCallback(typeof(OnMidiPolyphonicAftertouchDelegate))]
        private static void IosOnMidiPolyphonicAftertouch(string deviceId, int group, int channel, int note, int pressure) =>
            Instance.asyncOperation.Post(o => Instance.OnMidiPolyphonicAftertouch((string)((object[])o)[0], (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3], (int)((object[])o)[4]), new object[] {deviceId, group, channel, note, pressure});
        [AOT.MonoPInvokeCallback(typeof(OnMidiControlChangeDelegate))]
        private static void IosOnMidiControlChange(string deviceId, int group, int channel, int function, int value) =>
            Instance.asyncOperation.Post(o => Instance.OnMidiControlChange((string)((object[])o)[0], (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3], (int)((object[])o)[4]), new object[] {deviceId, group, channel, function, value});
        [AOT.MonoPInvokeCallback(typeof(OnMidiProgramChangeDelegate))]
        private static void IosOnMidiProgramChange(string deviceId, int group, int channel, int program) =>
            Instance.asyncOperation.Post(o => Instance.OnMidiProgramChange((string)((object[])o)[0], (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3]), new object[] {deviceId, group, channel, program});
        [AOT.MonoPInvokeCallback(typeof(OnMidiChannelAftertouchDelegate))]
        private static void IosOnMidiChannelAftertouch(string deviceId, int group, int channel, int pressure) =>
            Instance.asyncOperation.Post(o => Instance.OnMidiChannelAftertouch((string)((object[])o)[0], (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3]), new object[] {deviceId, group, channel, pressure});
        [AOT.MonoPInvokeCallback(typeof(OnMidiPitchWheelDelegate))]
        private static void IosOnMidiPitchWheel(string deviceId, int group, int channel, int amount) =>
            Instance.asyncOperation.Post(o => Instance.OnMidiPitchWheel((string)((object[])o)[0], (int)((object[])o)[1], (int)((object[])o)[2], (int)((object[])o)[3]), new object[] {deviceId, group, channel, amount});
        [AOT.MonoPInvokeCallback(typeof(OnMidiSystemExclusiveDelegate))]
        private static void IosOnMidiSystemExclusive(string deviceId, int group, IntPtr exclusive, int length) {
            var systemExclusive = new byte[length];
            Marshal.Copy(exclusive, systemExclusive, 0, length);
            Instance.asyncOperation.Post(o => Instance.OnMidiSystemExclusive((string)((object[])o)[0], (int)((object[])o)[1], (byte[])((object[])o)[2]), new object[] {deviceId, group, systemExclusive});
        }
        [AOT.MonoPInvokeCallback(typeof(OnMidiTimeCodeQuarterFrameDelegate))]
        private static void IosOnMidiTimeCodeQuarterFrame(string deviceId, int group, int timing) =>
            Instance.asyncOperation.Post(o => Instance.OnMidiTimeCodeQuarterFrame((string)((object[])o)[0], (int)((object[])o)[1], (int)((object[])o)[2]), new object[] {deviceId, group, timing});
        [AOT.MonoPInvokeCallback(typeof(OnMidiSongSelectDelegate))]
        private static void IosOnMidiSongSelect(string deviceId, int group, int song) =>
            Instance.asyncOperation.Post(o => Instance.OnMidiSongSelect((string)((object[])o)[0], (int)((object[])o)[1], (int)((object[])o)[2]), new object[] {deviceId, group, song});
        [AOT.MonoPInvokeCallback(typeof(OnMidiSongPositionPointerDelegate))]
        private static void IosOnMidiSongPositionPointer(string deviceId, int group, int position) =>
            Instance.asyncOperation.Post(o => Instance.OnMidiSongPositionPointer((string)((object[])o)[0], (int)((object[])o)[1], (int)((object[])o)[2]), new object[] {deviceId, group, position});
        [AOT.MonoPInvokeCallback(typeof(OnMidiTuneRequestDelegate))]
        private static void IosOnMidiTuneRequest(string deviceId, int group) =>
            Instance.asyncOperation.Post(o => Instance.OnMidiTuneRequest((string)((object[])o)[0], (int)((object[])o)[1]), new object[] {deviceId, group});
        [AOT.MonoPInvokeCallback(typeof(OnMidiTimingClockDelegate))]
        private static void IosOnMidiTimingClock(string deviceId, int group) =>
            Instance.asyncOperation.Post(o => Instance.OnMidiTimingClock((string)((object[])o)[0], (int)((object[])o)[1]), new object[] {deviceId, group});
        [AOT.MonoPInvokeCallback(typeof(OnMidiStartDelegate))]
        private static void IosOnMidiStart(string deviceId, int group) =>
            Instance.asyncOperation.Post(o => Instance.OnMidiStart((string)((object[])o)[0], (int)((object[])o)[1]), new object[] {deviceId, group});
        [AOT.MonoPInvokeCallback(typeof(OnMidiContinueDelegate))]
        private static void IosOnMidiContinue(string deviceId, int group) =>
            Instance.asyncOperation.Post(o => Instance.OnMidiContinue((string)((object[])o)[0], (int)((object[])o)[1]), new object[] {deviceId, group});
        [AOT.MonoPInvokeCallback(typeof(OnMidiStopDelegate))]
        private static void IosOnMidiStop(string deviceId, int group) =>
            Instance.asyncOperation.Post(o => Instance.OnMidiStop((string)((object[])o)[0], (int)((object[])o)[1]), new object[] {deviceId, group});
        [AOT.MonoPInvokeCallback(typeof(OnMidiActiveSensingDelegate))]
        private static void IosOnMidiActiveSensing(string deviceId, int group) =>
            Instance.asyncOperation.Post(o => Instance.OnMidiActiveSensing((string)((object[])o)[0], (int)((object[])o)[1]), new object[] {deviceId, group});
        [AOT.MonoPInvokeCallback(typeof(OnMidiResetDelegate))]
        private static void IosOnMidiReset(string deviceId, int group) =>
            Instance.asyncOperation.Post(o => Instance.OnMidiReset((string)((object[])o)[0], (int)((object[])o)[1]), new object[] {deviceId, group});

        public delegate void OnMidiInputDeviceAttachedDelegate(string deviceId);
        public delegate void OnMidiOutputDeviceAttachedDelegate(string deviceId);
        public delegate void OnMidiInputDeviceDetachedDelegate(string deviceId);
        public delegate void OnMidiOutputDeviceDetachedDelegate(string deviceId);
        public delegate void OnMidiNoteOnDelegate(string deviceId, int group, int channel, int note, int velocity);
        public delegate void OnMidiNoteOffDelegate(string deviceId, int group, int channel, int note, int velocity);
        public delegate void OnMidiPolyphonicAftertouchDelegate(string deviceId, int group, int channel, int note, int pressure);
        public delegate void OnMidiControlChangeDelegate(string deviceId, int group, int channel, int function, int value);
        public delegate void OnMidiProgramChangeDelegate(string deviceId, int group, int channel, int program);
        public delegate void OnMidiChannelAftertouchDelegate(string deviceId, int group, int channel, int pressure);
        public delegate void OnMidiPitchWheelDelegate(string deviceId, int group, int channel, int amount);
        public delegate void OnMidiSystemExclusiveDelegate(string deviceId, int group, IntPtr systemExclusive, int length);
        public delegate void OnMidiTimeCodeQuarterFrameDelegate(string deviceId, int group, int timing);
        public delegate void OnMidiSongSelectDelegate(string deviceId, int group, int song);
        public delegate void OnMidiSongPositionPointerDelegate(string deviceId, int group, int position);
        public delegate void OnMidiTuneRequestDelegate(string deviceId, int group);
        public delegate void OnMidiTimingClockDelegate(string deviceId, int group);
        public delegate void OnMidiStartDelegate(string deviceId, int group);
        public delegate void OnMidiContinueDelegate(string deviceId, int group);
        public delegate void OnMidiStopDelegate(string deviceId, int group);
        public delegate void OnMidiActiveSensingDelegate(string deviceId, int group);
        public delegate void OnMidiResetDelegate(string deviceId, int group);

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        private const string DllName = "MIDIPlugin";
#else
        private const string DllName = "__Internal";
#endif

        [DllImport(DllName)]
        private static extern void SetMidiInputDeviceAttachedCallback(OnMidiInputDeviceAttachedDelegate callback);
        [DllImport(DllName)]
        private static extern void SetMidiOutputDeviceAttachedCallback(OnMidiOutputDeviceAttachedDelegate callback);
        [DllImport(DllName)]
        private static extern void SetMidiInputDeviceDetachedCallback(OnMidiInputDeviceDetachedDelegate callback);
        [DllImport(DllName)]
        private static extern void SetMidiOutputDeviceDetachedCallback(OnMidiOutputDeviceDetachedDelegate callback);

        [DllImport(DllName)]
        private static extern void SetMidiNoteOnCallback(OnMidiNoteOnDelegate callback);
        [DllImport(DllName)]
        private static extern void SetMidiNoteOffCallback(OnMidiNoteOffDelegate callback);
        [DllImport(DllName)]
        private static extern void SetMidiPolyphonicAftertouchDelegate(OnMidiPolyphonicAftertouchDelegate callback);
        [DllImport(DllName)]
        private static extern void SetMidiControlChangeDelegate(OnMidiControlChangeDelegate callback);
        [DllImport(DllName)]
        private static extern void SetMidiProgramChangeDelegate(OnMidiProgramChangeDelegate callback);
        [DllImport(DllName)]
        private static extern void SetMidiChannelAftertouchDelegate(OnMidiChannelAftertouchDelegate callback);
        [DllImport(DllName)]
        private static extern void SetMidiPitchWheelDelegate(OnMidiPitchWheelDelegate callback);
        [DllImport(DllName)]
        private static extern void SetMidiSystemExclusiveDelegate(OnMidiSystemExclusiveDelegate callback);
        [DllImport(DllName)]
        private static extern void SetMidiTimeCodeQuarterFrameDelegate(OnMidiTimeCodeQuarterFrameDelegate callback);
        [DllImport(DllName)]
        private static extern void SetMidiSongSelectDelegate(OnMidiSongSelectDelegate callback);
        [DllImport(DllName)]
        private static extern void SetMidiSongPositionPointerDelegate(OnMidiSongPositionPointerDelegate callback);
        [DllImport(DllName)]
        private static extern void SetMidiTuneRequestDelegate(OnMidiTuneRequestDelegate callback);
        [DllImport(DllName)]
        private static extern void SetMidiTimingClockDelegate(OnMidiTimingClockDelegate callback);
        [DllImport(DllName)]
        private static extern void SetMidiStartDelegate(OnMidiStartDelegate callback);
        [DllImport(DllName)]
        private static extern void SetMidiContinueDelegate(OnMidiContinueDelegate callback);
        [DllImport(DllName)]
        private static extern void SetMidiStopDelegate(OnMidiStopDelegate callback);
        [DllImport(DllName)]
        private static extern void SetMidiActiveSensingDelegate(OnMidiActiveSensingDelegate callback);
        [DllImport(DllName)]
        private static extern void SetMidiResetDelegate(OnMidiResetDelegate callback);
#endif

        private void OnMidiNoteOn(string midiMessage)
        {
            var deserializedMidiMessage = DeserializeMidiMessage(midiMessage);
            OnMidiNoteOn(deserializedMidiMessage);
        }
        private void OnMidiNoteOn(string deviceId, int group, int channel, int note, int velocity)
        {
            var midiMessage = new MidiMessage
            {
                DeviceId = deviceId,
                Group = group,
                Messages = new decimal[] {channel, note, velocity},
            };
            OnMidiNoteOn(midiMessage);
        }
        private void OnMidiNoteOn(MidiMessage midiMessage)
        {
            var eventData = new MidiEventData(midiMessage, EventSystem.current);
            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiNoteOnEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiNoteOnEventHandler>(midiDeviceEventHandler,
                    eventData, (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiEventData midiEventData)
                        {
                            var parsed = midiEventData.Message;
                            eventHandler.OnMidiNoteOn(parsed.DeviceId, parsed.Group,
                                (int)parsed.Messages[0], (int)parsed.Messages[1], (int)parsed.Messages[2]);
                        }
                    });
            }

            SendMidiMessageToTransmitters(eventData, ShortMessage.NoteOn);
        }

        private void OnMidiNoteOff(string midiMessage)
        {
            var deserializedMidiMessage = DeserializeMidiMessage(midiMessage);
            OnMidiNoteOff(deserializedMidiMessage);
        }
        private void OnMidiNoteOff(string deviceId, int group, int channel, int note, int velocity)
        {
            var midiMessage = new MidiMessage
            {
                DeviceId = deviceId,
                Group = group,
                Messages = new decimal[] {channel, note, velocity},
            };
            OnMidiNoteOff(midiMessage);
        }
        private void OnMidiNoteOff(MidiMessage midiMessage)
        {
            var eventData = new MidiEventData(midiMessage, EventSystem.current);
            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiNoteOffEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiNoteOffEventHandler>(midiDeviceEventHandler,
                    eventData, (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiEventData midiEventData)
                        {
                            var parsed = midiEventData.Message;
                            eventHandler.OnMidiNoteOff(parsed.DeviceId, parsed.Group,
                                (int)parsed.Messages[0], (int)parsed.Messages[1], (int)parsed.Messages[2]);
                        }
                    });
            }

            SendMidiMessageToTransmitters(eventData, ShortMessage.NoteOff);
        }

        private void OnMidiPolyphonicAftertouch(string midiMessage)
        {
            var deserializedMidiMessage = DeserializeMidiMessage(midiMessage);
            OnMidiPolyphonicAftertouch(deserializedMidiMessage);
        }
        private void OnMidiPolyphonicAftertouch(string deviceId, int group, int channel, int note, int pressure)
        {
            var midiMessage = new MidiMessage
            {
                DeviceId = deviceId,
                Group = group,
                Messages = new decimal[] {channel, note, pressure},
            };
            OnMidiPolyphonicAftertouch(midiMessage);
        }
        private void OnMidiPolyphonicAftertouch(MidiMessage midiMessage)
        {
            var eventData = new MidiEventData(midiMessage, EventSystem.current);
            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiPolyphonicAftertouchEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiPolyphonicAftertouchEventHandler>(midiDeviceEventHandler,
                    eventData, (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiEventData midiEventData)
                        {
                            var parsed = midiEventData.Message;
                            eventHandler.OnMidiPolyphonicAftertouch(parsed.DeviceId, parsed.Group,
                                (int)parsed.Messages[0], (int)parsed.Messages[1], (int)parsed.Messages[2]);
                        }
                    });
            }

            SendMidiMessageToTransmitters(eventData, ShortMessage.PolyPressure);
        }

        private void OnMidiControlChange(string midiMessage)
        {
            var deserializedMidiMessage = DeserializeMidiMessage(midiMessage);
            OnMidiControlChange(deserializedMidiMessage);
        }
        private void OnMidiControlChange(string deviceId, int group, int channel, int function, int value)
        {
            var midiMessage = new MidiMessage
            {
                DeviceId = deviceId,
                Group = group,
                Messages = new decimal[] {channel, function, value},
            };
            OnMidiControlChange(midiMessage);
        }
        private void OnMidiControlChange(MidiMessage midiMessage)
        {
            var eventData = new MidiEventData(midiMessage, EventSystem.current);
            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiControlChangeEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiControlChangeEventHandler>(midiDeviceEventHandler,
                    eventData, (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiEventData midiEventData)
                        {
                            var parsed = midiEventData.Message;
                            eventHandler.OnMidiControlChange(parsed.DeviceId, parsed.Group,
                                (int)parsed.Messages[0], (int)parsed.Messages[1], (int)parsed.Messages[2]);
                        }
                    });
            }

            SendMidiMessageToTransmitters(eventData, ShortMessage.ControlChange);
        }

        private void OnMidiProgramChange(string midiMessage)
        {
            var deserializedMidiMessage = DeserializeMidiMessage(midiMessage);
            OnMidiProgramChange(deserializedMidiMessage);
        }
        private void OnMidiProgramChange(string deviceId, int group, int channel, int program)
        {
            var midiMessage = new MidiMessage
            {
                DeviceId = deviceId,
                Group = group,
                Messages = new decimal[] {channel, program},
            };
            OnMidiProgramChange(midiMessage);
        }
        private void OnMidiProgramChange(MidiMessage midiMessage)
        {
            var eventData = new MidiEventData(midiMessage, EventSystem.current);
            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiProgramChangeEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiProgramChangeEventHandler>(midiDeviceEventHandler,
                    eventData, (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiEventData midiEventData)
                        {
                            var parsed = midiEventData.Message;
                            eventHandler.OnMidiProgramChange(parsed.DeviceId, parsed.Group,
                                (int)parsed.Messages[0], (int)parsed.Messages[1]);
                        }
                    });
            }

            SendMidiMessageToTransmitters(eventData, ShortMessage.ProgramChange);
        }

        private void OnMidiChannelAftertouch(string midiMessage)
        {
            var deserializedMidiMessage = DeserializeMidiMessage(midiMessage);
            OnMidiChannelAftertouch(deserializedMidiMessage);
        }
        private void OnMidiChannelAftertouch(string deviceId, int group, int channel, int pressure)
        {
            var midiMessage = new MidiMessage
            {
                DeviceId = deviceId,
                Group = group,
                Messages = new decimal[] {channel, pressure},
            };
            OnMidiChannelAftertouch(midiMessage);
        }
        private void OnMidiChannelAftertouch(MidiMessage midiMessage)
        {
            var eventData = new MidiEventData(midiMessage, EventSystem.current);
            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiChannelAftertouchEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiChannelAftertouchEventHandler>(midiDeviceEventHandler,
                    eventData, (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiEventData midiEventData)
                        {
                            var parsed = midiEventData.Message;
                            eventHandler.OnMidiChannelAftertouch(parsed.DeviceId, parsed.Group,
                                (int)parsed.Messages[0], (int)parsed.Messages[1]);
                        }
                    });
            }

            SendMidiMessageToTransmitters(eventData, ShortMessage.ChannelPressure);
        }

        private void OnMidiPitchWheel(string midiMessage)
        {
            var deserializedMidiMessage = DeserializeMidiMessage(midiMessage);
            OnMidiPitchWheel(deserializedMidiMessage);
        }
        private void OnMidiPitchWheel(string deviceId, int group, int channel, int amount)
        {
            var midiMessage = new MidiMessage
            {
                DeviceId = deviceId,
                Group = group,
                Messages = new decimal[] {channel, amount},
            };
            OnMidiPitchWheel(midiMessage);
        }
        private void OnMidiPitchWheel(MidiMessage midiMessage)
        {
            var eventData = new MidiEventData(midiMessage, EventSystem.current);
            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiPitchWheelEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiPitchWheelEventHandler>(midiDeviceEventHandler,
                    eventData, (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiEventData midiEventData)
                        {
                            var parsed = midiEventData.Message;
                            eventHandler.OnMidiPitchWheel(parsed.DeviceId, parsed.Group,
                                (int)parsed.Messages[0], (int)parsed.Messages[1]);
                        }
                    });
            }

            {
                var transmitters = MidiSystem.GetTransmitters();
                var parsed = midiMessage;
                var message = new ShortMessage(ShortMessage.PitchBend | ((int)parsed.Messages[0] & ShortMessage.MaskChannel),
                    (int)parsed.Messages[1] & 0x7f,
                    ((int)parsed.Messages[1] >> 7) & 0x7f);
                foreach (var transmitter in transmitters)
                {
                    transmitter.GetReceiver()?.Send(message, 0);
                }
            }
        }

        private void OnMidiSystemExclusive(string midiMessage)
        {
            var deserializedMidiMessage = DeserializeMidiMessage(midiMessage, true);
            OnMidiSystemExclusive(deserializedMidiMessage);
        }
        private void OnMidiSystemExclusive(string deviceId, int group, byte[] systemExclusive)
        {
            var midiMessage = new MidiMessage
            {
                DeviceId = deviceId,
                Group = group,
                SystemExclusive = systemExclusive,
            };
            OnMidiSystemExclusive(midiMessage);
        }
        private void OnMidiSystemExclusive(MidiMessage midiMessage)
        {
            var eventData = new MidiEventData(midiMessage, EventSystem.current);
            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiSystemExclusiveEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiSystemExclusiveEventHandler>(midiDeviceEventHandler,
                    eventData, (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiEventData midiEventData)
                        {
                            var parsed = midiEventData.Message;
                            eventHandler.OnMidiSystemExclusive(parsed.DeviceId, parsed.Group, parsed.SystemExclusive);
                        }
                    });
            }

            {
                var transmitters = MidiSystem.GetTransmitters();
                var message = new SysexMessage(ShortMessage.StartOfExclusive, midiMessage.SystemExclusive);
                foreach (var transmitter in transmitters)
                {
                    transmitter.GetReceiver()?.Send(message, 0);
                }
            }
        }

        private void OnMidiSystemCommonMessage(string midiMessage)
        {
            var deserializedMidiMessage = DeserializeMidiMessage(midiMessage, true);
            OnMidiSystemCommonMessage(deserializedMidiMessage);
        }
        private void OnMidiSystemCommonMessage(string deviceId, int group, byte[] bytes)
        {
            var midiMessage = new MidiMessage
            {
                DeviceId = deviceId,
                Group = group,
                SystemExclusive = bytes,
            };
            OnMidiSystemCommonMessage(midiMessage);
        }
        private void OnMidiSystemCommonMessage(MidiMessage midiMessage)
        {
            var eventData = new MidiEventData(midiMessage, EventSystem.current);
            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiSystemCommonMessageEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiSystemCommonMessageEventHandler>(midiDeviceEventHandler,
                    eventData, (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiEventData midiEventData)
                        {
                            var parsed = midiEventData.Message;
                            eventHandler.OnMidiSystemCommonMessage(parsed.DeviceId, parsed.Group,
                                parsed.SystemExclusive);
                        }
                    });
            }

            {
                var transmitters = MidiSystem.GetTransmitters();
                var parsed = eventData.Message;
                var message = new SysexMessage(ShortMessage.StartOfExclusive, parsed.SystemExclusive);
                foreach (var transmitter in transmitters)
                {
                    transmitter.GetReceiver()?.Send(message, 0);
                }
            }
        }

        private void OnMidiSingleByte(string midiMessage)
        {
            var deserializedMidiMessage = DeserializeMidiMessage(midiMessage);
            OnMidiSingleByte(deserializedMidiMessage);
        }
        private void OnMidiSingleByte(string deviceId, int group, int byte1)
        {
            var midiMessage = new MidiMessage
            {
                DeviceId = deviceId,
                Group = group,
                Messages = new decimal[] {byte1},
            };
            OnMidiSingleByte(midiMessage);
        }
        private void OnMidiSingleByte(MidiMessage midiMessage)
        {
            var eventData = new MidiEventData(midiMessage, EventSystem.current);
            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiSingleByteEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiSingleByteEventHandler>(midiDeviceEventHandler,
                    eventData, (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiEventData midiEventData)
                        {
                            var parsed = midiEventData.Message;
                            eventHandler.OnMidiSingleByte(parsed.DeviceId, parsed.Group,
                                (int)parsed.Messages[0]);
                        }
                    });
            }

            SendMidiMessageToTransmitters(eventData, 0);
        }

        private void OnMidiTimeCodeQuarterFrame(string midiMessage)
        {
            var deserializedMidiMessage = DeserializeMidiMessage(midiMessage);
            OnMidiTimeCodeQuarterFrame(deserializedMidiMessage);
        }
        private void OnMidiTimeCodeQuarterFrame(string deviceId, int group, int timing)
        {
            var midiMessage = new MidiMessage
            {
                DeviceId = deviceId,
                Group = group,
                Messages = new decimal[] {timing},
            };
            OnMidiTimeCodeQuarterFrame(midiMessage);
        }
        private void OnMidiTimeCodeQuarterFrame(MidiMessage midiMessage)
        {
            var eventData = new MidiEventData(midiMessage, EventSystem.current);
            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiTimeCodeQuarterFrameEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiTimeCodeQuarterFrameEventHandler>(midiDeviceEventHandler,
                    eventData, (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiEventData midiEventData)
                        {
                            var parsed = midiEventData.Message;
                            eventHandler.OnMidiTimeCodeQuarterFrame(parsed.DeviceId, parsed.Group,
                                (int)parsed.Messages[0]);
                        }
                    });
            }

            SendMidiMessageToTransmitters(eventData, ShortMessage.MidiTimeCode);
        }

        private void OnMidiSongSelect(string midiMessage)
        {
            var deserializedMidiMessage = DeserializeMidiMessage(midiMessage);
            OnMidiSongSelect(deserializedMidiMessage);
        }
        private void OnMidiSongSelect(string deviceId, int group, int song)
        {
            var midiMessage = new MidiMessage
            {
                DeviceId = deviceId,
                Group = group,
                Messages = new decimal[] {song},
            };
            OnMidiSongSelect(midiMessage);
        }
        private void OnMidiSongSelect(MidiMessage midiMessage)
        {
            var eventData = new MidiEventData(midiMessage, EventSystem.current);
            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiSongSelectEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiSongSelectEventHandler>(midiDeviceEventHandler,
                    eventData, (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiEventData midiEventData)
                        {
                            var parsed = midiEventData.Message;
                            eventHandler.OnMidiSongSelect(parsed.DeviceId, parsed.Group,
                                (int)parsed.Messages[0]);
                        }
                    });
            }

            SendMidiMessageToTransmitters(eventData, ShortMessage.SongSelect);
        }

        private void OnMidiSongPositionPointer(string midiMessage)
        {
            var deserializedMidiMessage = DeserializeMidiMessage(midiMessage);
            OnMidiSongPositionPointer(deserializedMidiMessage);
        }
        private void OnMidiSongPositionPointer(string deviceId, int group, int position)
        {
            var midiMessage = new MidiMessage
            {
                DeviceId = deviceId,
                Group = group,
                Messages = new decimal[] {position},
            };
            OnMidiSongPositionPointer(midiMessage);
        }
        private void OnMidiSongPositionPointer(MidiMessage midiMessage)
        {
            var eventData = new MidiEventData(midiMessage, EventSystem.current);
            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiSongPositionPointerEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiSongPositionPointerEventHandler>(midiDeviceEventHandler,
                    eventData, (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiEventData midiEventData)
                        {
                            var parsed = midiEventData.Message;
                            eventHandler.OnMidiSongPositionPointer(parsed.DeviceId, parsed.Group,
                                (int)parsed.Messages[0]);
                        }
                    });
            }

            {
                var transmitters = MidiSystem.GetTransmitters();
                var message = new ShortMessage(ShortMessage.SongPositionPointer,
                    (int)midiMessage.Messages[0] & 0x7f,
                    ((int)midiMessage.Messages[0] >> 7) & 0x7f);
                foreach (var transmitter in transmitters)
                {
                    transmitter.GetReceiver()?.Send(message, 0);
                }
            }
        }

        private void OnMidiTuneRequest(string midiMessage)
        {
            var deserializedMidiMessage = DeserializeMidiMessage(midiMessage);
            OnMidiTuneRequest(deserializedMidiMessage);
        }
        private void OnMidiTuneRequest(string deviceId, int group)
        {
            var midiMessage = new MidiMessage
            {
                DeviceId = deviceId,
                Group = group,
            };
            OnMidiTuneRequest(midiMessage);
        }
        private void OnMidiTuneRequest(MidiMessage midiMessage)
        {
            var eventData = new MidiEventData(midiMessage, EventSystem.current);
            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiTuneRequestEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiTuneRequestEventHandler>(midiDeviceEventHandler,
                    eventData, (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiEventData midiEventData)
                        {
                            var parsed = midiEventData.Message;
                            eventHandler.OnMidiTuneRequest(parsed.DeviceId, parsed.Group);
                        }
                    });
            }

            SendMidiMessageToTransmitters(eventData, ShortMessage.TuneRequest);
        }

        private void OnMidiTimingClock(string midiMessage)
        {
            var deserializedMidiMessage = DeserializeMidiMessage(midiMessage);
            OnMidiTimingClock(deserializedMidiMessage);
        }
        private void OnMidiTimingClock(string deviceId, int group)
        {
            var midiMessage = new MidiMessage
            {
                DeviceId = deviceId,
                Group = group,
            };
            OnMidiTimingClock(midiMessage);
        }
        private void OnMidiTimingClock(MidiMessage midiMessage)
        {
            var eventData = new MidiEventData(midiMessage, EventSystem.current);
            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiTimingClockEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiTimingClockEventHandler>(midiDeviceEventHandler,
                    eventData, (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiEventData midiEventData)
                        {
                            var parsed = midiEventData.Message;
                            eventHandler.OnMidiTimingClock(parsed.DeviceId, parsed.Group);
                        }
                    });
            }

            SendMidiMessageToTransmitters(eventData, ShortMessage.TimingClock);
        }

        private void OnMidiStart(string midiMessage)
        {
            var deserializedMidiMessage = DeserializeMidiMessage(midiMessage);
            OnMidiStart(deserializedMidiMessage);
        }
        private void OnMidiStart(string deviceId, int group)
        {
            var midiMessage = new MidiMessage
            {
                DeviceId = deviceId,
                Group = group,
            };
            OnMidiStart(midiMessage);
        }
        private void OnMidiStart(MidiMessage midiMessage)
        {
            var eventData = new MidiEventData(midiMessage, EventSystem.current);
            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiStartEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiStartEventHandler>(midiDeviceEventHandler,
                    eventData, (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiEventData midiEventData)
                        {
                            var parsed = midiEventData.Message;
                            eventHandler.OnMidiStart(parsed.DeviceId, parsed.Group);
                        }
                    });
            }

            SendMidiMessageToTransmitters(eventData, ShortMessage.Start);
        }

        private void OnMidiContinue(string midiMessage)
        {
            var deserializedMidiMessage = DeserializeMidiMessage(midiMessage);
            OnMidiContinue(deserializedMidiMessage);
        }
        private void OnMidiContinue(string deviceId, int group)
        {
            var midiMessage = new MidiMessage
            {
                DeviceId = deviceId,
                Group = group,
            };
            OnMidiContinue(midiMessage);
        }
        private void OnMidiContinue(MidiMessage midiMessage)
        {
            var eventData = new MidiEventData(midiMessage, EventSystem.current);
            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiContinueEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiContinueEventHandler>(midiDeviceEventHandler,
                    eventData, (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiEventData midiEventData)
                        {
                            var parsed = midiEventData.Message;
                            eventHandler.OnMidiContinue(parsed.DeviceId, parsed.Group);
                        }
                    });
            }

            SendMidiMessageToTransmitters(eventData, ShortMessage.Continue);
        }

        private void OnMidiStop(string midiMessage)
        {
            var deserializedMidiMessage = DeserializeMidiMessage(midiMessage);
            OnMidiStop(deserializedMidiMessage);
        }
        private void OnMidiStop(string deviceId, int group)
        {
            var midiMessage = new MidiMessage
            {
                DeviceId = deviceId,
                Group = group,
            };
            OnMidiStop(midiMessage);
        }
        private void OnMidiStop(MidiMessage midiMessage)
        {
            var eventData = new MidiEventData(midiMessage, EventSystem.current);
            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiStopEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiStopEventHandler>(midiDeviceEventHandler,
                    eventData, (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiEventData midiEventData)
                        {
                            var parsed = midiEventData.Message;
                            eventHandler.OnMidiStop(parsed.DeviceId, parsed.Group);
                        }
                    });
            }
 
            SendMidiMessageToTransmitters(eventData, ShortMessage.Stop);
        }

        private void OnMidiActiveSensing(string midiMessage)
        {
            var deserializedMidiMessage = DeserializeMidiMessage(midiMessage);
            OnMidiActiveSensing(deserializedMidiMessage);
        }
        private void OnMidiActiveSensing(string deviceId, int group)
        {
            var midiMessage = new MidiMessage
            {
                DeviceId = deviceId,
                Group = group,
            };
            OnMidiActiveSensing(midiMessage);
        }
        private void OnMidiActiveSensing(MidiMessage midiMessage)
        {
            var eventData = new MidiEventData(midiMessage, EventSystem.current);
            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiActiveSensingEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiActiveSensingEventHandler>(midiDeviceEventHandler,
                    eventData, (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiEventData midiEventData)
                        {
                            var parsed = midiEventData.Message;
                            eventHandler.OnMidiActiveSensing(parsed.DeviceId, parsed.Group);
                        }
                    });
            }
 
            SendMidiMessageToTransmitters(eventData, ShortMessage.ActiveSensing);
        }

        private void OnMidiReset(string midiMessage)
        {
            var deserializedMidiMessage = DeserializeMidiMessage(midiMessage);
            OnMidiReset(deserializedMidiMessage);
        }
        private void OnMidiReset(string deviceId, int group)
        {
            var midiMessage = new MidiMessage
            {
                DeviceId = deviceId,
                Group = group,
            };
            OnMidiReset(midiMessage);
        }
        private void OnMidiReset(MidiMessage midiMessage)
        {
            var eventData = new MidiEventData(midiMessage, EventSystem.current);
            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiResetEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiResetEventHandler>(midiDeviceEventHandler,
                    eventData, (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiEventData midiEventData)
                        {
                            var parsed = midiEventData.Message;
                            eventHandler.OnMidiReset(parsed.DeviceId, parsed.Group);
                        }
                    });
            }
 
            SendMidiMessageToTransmitters(eventData, ShortMessage.SystemReset);
        }

        private void OnMidiMiscellaneousFunctionCodes(string midiMessage)
        {
            var deserializedMidiMessage = DeserializeMidiMessage(midiMessage);
            OnMidiMiscellaneousFunctionCodes(deserializedMidiMessage);
        }
        private void OnMidiMiscellaneousFunctionCodes(string deviceId, int group, int byte1, int byte2, int byte3)
        {
            var midiMessage = new MidiMessage
            {
                DeviceId = deviceId,
                Group = group,
                Messages = new decimal[] {byte1, byte2, byte3},
            };
            OnMidiMiscellaneousFunctionCodes(midiMessage);
        }
        private void OnMidiMiscellaneousFunctionCodes(MidiMessage midiMessage)
        {
            var eventData = new MidiEventData(midiMessage, EventSystem.current);
            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiMiscellaneousFunctionCodesEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiMiscellaneousFunctionCodesEventHandler>(midiDeviceEventHandler,
                    eventData, (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiEventData midiEventData)
                        {
                            var parsed = midiEventData.Message;
                            eventHandler.OnMidiMiscellaneousFunctionCodes(parsed.DeviceId, parsed.Group,
                                (int)(parsed.Messages.Length > 0 ? parsed.Messages[0] : 0),
                                (int)(parsed.Messages.Length > 1 ? parsed.Messages[1] : 0),
                                (int)(parsed.Messages.Length > 2 ? parsed.Messages[2] : 0));
                        }
                    });
            }
 
            SendMidiMessageToTransmitters(eventData, 0);
        }

        private void OnMidiCableEvents(string midiMessage)
        {
            var deserializedMidiMessage = DeserializeMidiMessage(midiMessage);
            OnMidiCableEvents(deserializedMidiMessage);
        }
        private void OnMidiCableEvents(string deviceId, int group, int byte1, int byte2, int byte3)
        {
            var midiMessage = new MidiMessage
            {
                DeviceId = deviceId,
                Group = group,
                Messages = new decimal[] {byte1, byte2, byte3},
            };
            OnMidiCableEvents(midiMessage);
        }
        private void OnMidiCableEvents(MidiMessage midiMessage)
        {
            var eventData = new MidiEventData(midiMessage, EventSystem.current);
            foreach (var midiDeviceEventHandler in midiDeviceEventHandlers)
            {
                if (!ExecuteEvents.CanHandleEvent<IMidiCableEventsEventHandler>(midiDeviceEventHandler))
                {
                    continue;
                }

                ExecuteEvents.Execute<IMidiCableEventsEventHandler>(midiDeviceEventHandler,
                    eventData, (eventHandler, baseEventData) =>
                    {
                        if (baseEventData is MidiEventData midiEventData)
                        {
                            var parsed = midiEventData.Message;
                            eventHandler.OnMidiCableEvents(parsed.DeviceId, parsed.Group,
                                (int)(parsed.Messages.Length > 0 ? parsed.Messages[0] : 0),
                                (int)(parsed.Messages.Length > 1 ? parsed.Messages[1] : 0),
                                (int)(parsed.Messages.Length > 2 ? parsed.Messages[2] : 0));
                        }
                    });
            }
 
            SendMidiMessageToTransmitters(eventData, 0);
        }
    }

    /// <summary>
    /// Receiver
    /// </summary>
    internal class ReceiverImpl : IReceiver
    {
        private readonly string deviceId;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        internal ReceiverImpl(string deviceId)
        {
            this.deviceId = deviceId;
        }

        /// <inheritdoc cref="object.ToString"/>
        public override string ToString()
        {
            return deviceId;
        }

        /// <inheritdoc cref="IReceiver.Send"/>
        public void Send(MidiMessage message, long timeStamp)
        {
            if (message is ShortMessage shortMessage)
            {
                var midiMessage = shortMessage.GetMessage();
                Debug.LogError("short message");
                switch (shortMessage.GetStatus() & ShortMessage.MaskEvent)
                {
                    case ShortMessage.NoteOff:
                        MidiManager.Instance.SendMidiNoteOff(deviceId, 0,
                            midiMessage[0] & ShortMessage.MaskChannel, midiMessage[1], midiMessage[2]);
                        break;
                    case ShortMessage.NoteOn:
                        MidiManager.Instance.SendMidiNoteOn(deviceId, 0,
                            midiMessage[0] & ShortMessage.MaskChannel, midiMessage[1], midiMessage[2]);
                        break;
                    case ShortMessage.PolyPressure:
                        MidiManager.Instance.SendMidiPolyphonicAftertouch(deviceId, 0,
                            midiMessage[0] & ShortMessage.MaskChannel, midiMessage[1], midiMessage[2]);
                        break;
                    case ShortMessage.ControlChange:
                        Debug.LogError("control change+"+midiMessage[2]);
                        MidiManager.Instance.SendMidiControlChange(deviceId, 0,
                            midiMessage[0] & ShortMessage.MaskChannel, midiMessage[1], midiMessage[2]);
                        break;
                    case ShortMessage.ProgramChange:
                        MidiManager.Instance.SendMidiProgramChange(deviceId, 0,
                            midiMessage[0] & ShortMessage.MaskChannel, midiMessage[1]);
                        break;
                    case ShortMessage.ChannelPressure:
                        MidiManager.Instance.SendMidiChannelAftertouch(deviceId, 0,
                            midiMessage[0] & ShortMessage.MaskChannel, midiMessage[1]);
                        break;
                    case ShortMessage.PitchBend:
                        MidiManager.Instance.SendMidiPitchWheel(deviceId, 0,
                            midiMessage[0] & ShortMessage.MaskChannel, midiMessage[1] | (midiMessage[2] << 7));
                        break;
                    case ShortMessage.MidiTimeCode:
                        MidiManager.Instance.SendMidiTimeCodeQuarterFrame(deviceId, 0,
                            midiMessage[1]);
                        break;
                    case ShortMessage.SongPositionPointer:
                        MidiManager.Instance.SendMidiSongPositionPointer(deviceId, 0,
                            midiMessage[1] | (midiMessage[2] << 7));
                        break;
                    case ShortMessage.SongSelect:
                        MidiManager.Instance.SendMidiSongSelect(deviceId, 0,
                            midiMessage[1]);
                        break;
                    case ShortMessage.TuneRequest:
                        MidiManager.Instance.SendMidiTuneRequest(deviceId, 0);
                        break;
                    case ShortMessage.TimingClock:
                        MidiManager.Instance.SendMidiTimingClock(deviceId, 0);
                        break;
                    case ShortMessage.Start:
                        MidiManager.Instance.SendMidiStart(deviceId, 0);
                        break;
                    case ShortMessage.Continue:
                        MidiManager.Instance.SendMidiContinue(deviceId, 0);
                        break;
                    case ShortMessage.Stop:
                        MidiManager.Instance.SendMidiStop(deviceId, 0);
                        break;
                    case ShortMessage.ActiveSensing:
                        MidiManager.Instance.SendMidiActiveSensing(deviceId, 0);
                        break;
                    case ShortMessage.SystemReset:
                        MidiManager.Instance.SendMidiReset(deviceId, 0);
                        break;
                }
            }
            else if (message is MetaMessage)
            {
                // ignore meta messages
            }
            else if (message is SysexMessage sysexMessage)
            {
                MidiManager.Instance.SendMidiSystemExclusive(deviceId, 0, sysexMessage.GetData());
            }
        }

        /// <inheritdoc cref="IReceiver.Close"/>
        public void Close()
        {
            // do nothing
        }
    }

    /// <summary>
    /// Transmitter
    /// </summary>
    internal class TransmitterImpl : ITransmitter
    {
        private readonly string deviceId;
        private IReceiver receiver;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="deviceId">the device ID</param>
        internal TransmitterImpl(string deviceId)
        {
            this.deviceId = deviceId;
        }

        /// <inheritdoc cref="object.ToString"/>
        public override string ToString()
        {
            return deviceId;
        }

        /// <inheritdoc cref="ITransmitter.SetReceiver"/>
        public void SetReceiver(IReceiver theReceiver)
        {
            receiver = theReceiver;
        }

        /// <inheritdoc cref="ITransmitter.GetReceiver"/>
        public IReceiver GetReceiver()
        {
            return receiver;
        }

        /// <inheritdoc cref="ITransmitter.Close"/>
        public void Close()
        {
            receiver.Close();
        }
    }
}