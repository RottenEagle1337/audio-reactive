// DeviceSelection.cs
// Which device and channel a tracker listens to (DeviceSelection), plus the device description
// returned by AudioSystem.GetDevices (AudioDeviceInfo). Trackers with the same device
// (loopback flag + id) share one InputStream through AudioSystem.
using System;
using UnityEngine;

namespace AudioReactive
{
    /// <summary>Audio device description returned by <see cref="AudioSystem.GetDevices"/>.</summary>
    public readonly struct AudioDeviceInfo
    {
        /// <summary>Backend-specific stable id (WASAPI endpoint id, Core Audio UID, PulseAudio source name...).</summary>
        public readonly string Id;
        /// <summary>Human-readable device name.</summary>
        public readonly string Name;
        public AudioDeviceInfo(string id, string name) { Id = id; Name = name; }
        public override string ToString() => Name;
    }

    /// <summary>Channel of a device to analyze. Values match the native channel selectors.</summary>
    public enum AudioChannel
    {
        Mono = -1,
        Left = 0,
        Right = 1,
    }

    /// <summary>Device and channel selection of a tracker.</summary>
    [Serializable]
    public struct DeviceSelection
    {
        [Tooltip("Capture a playback device (what you hear). Windows only; elsewhere pick an input " +
                 "device such as a PulseAudio monitor source or a virtual device like BlackHole.")]
        public bool loopback;

        [Tooltip("Backend-specific device id. Empty = system default device.")]
        public string deviceId;

        [Tooltip("Mono mix, or the left / right channel of a stereo device.")]
        public AudioChannel channel;

        /// <summary>System default device, loopback where supported, mono mix.</summary>
        public static DeviceSelection Default => new DeviceSelection { loopback = true, deviceId = "", channel = AudioChannel.Mono };

        /// <summary>True when both select the same device (the channel does not matter for stream sharing).</summary>
        public bool SameDevice(in DeviceSelection other) =>
            loopback == other.loopback && string.Equals(deviceId ?? "", other.deviceId ?? "", StringComparison.Ordinal);
    }
}
