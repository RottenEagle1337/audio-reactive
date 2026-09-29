// AudioSystem.cs
// Entry point of the capture layer (LASP's AudioSystem equivalent): shares one InputStream
// per selected device between all trackers (reference counted, closed when the last user
// releases it) and caches device lists for UI. Static state is reset on every play session,
// so it also works with Enter Play Mode Options (domain reload disabled).
using System.Collections.Generic;
using UnityEngine;

namespace AudioReactive
{
    /// <summary>
    /// Entry point of the capture layer: device enumeration, backend capabilities and shared
    /// <see cref="InputStream"/> instances (one per device).
    /// </summary>
    public static class AudioSystem
    {
        private static readonly List<InputStream> s_streams = new List<InputStream>();
        private static List<AudioDeviceInfo> s_inputDevices, s_playbackDevices;
        private static bool? s_loopbackSupported;
        private static string s_backendName;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            foreach (InputStream s in s_streams) s.Dispose();
            s_streams.Clear();
            s_inputDevices = s_playbackDevices = null;
            s_loopbackSupported = null;
            s_backendName = null;
        }

        /// <summary>True when the native capture plugin exists for this platform and is compatible.</summary>
        public static bool IsAvailable => AudioCaptureContext.IsAvailable;

        /// <summary>
        /// Returns the shared stream for the device, opening it on first use. Every call must be paired
        /// with <see cref="Release"/>. Trackers do this automatically in OnEnable / OnDisable.
        /// </summary>
        public static InputStream Acquire(in DeviceSelection device)
        {
            foreach (InputStream s in s_streams)
                if (s.Device.SameDevice(device)) { s.RefCount++; return s; }

            var stream = new InputStream(device) { RefCount = 1 };
            s_streams.Add(stream);
            return stream;
        }

        /// <summary>Releases a stream obtained from <see cref="Acquire"/>; the last release closes the device.</summary>
        public static void Release(InputStream stream)
        {
            if (stream == null || --stream.RefCount > 0) return;
            stream.Dispose();
            s_streams.Remove(stream);
        }

        /// <summary>
        /// Whether the platform backend can capture playback devices directly (WASAPI loopback, Windows only).
        /// Elsewhere capture system output through an input device (PulseAudio monitor source, BlackHole...).
        /// </summary>
        public static bool IsLoopbackSupported
        {
            get { if (!s_loopbackSupported.HasValue) QueryBackend(); return s_loopbackSupported.Value; }
        }

        /// <summary>Name of the audio backend ("WASAPI", "Core Audio", "PulseAudio", "ALSA" or "unavailable").</summary>
        public static string BackendName
        {
            get { if (s_backendName == null) QueryBackend(); return s_backendName; }
        }

        /// <summary>
        /// Cached device list: playback devices when <paramref name="loopback"/> is true and supported,
        /// input devices otherwise. Call <see cref="RefreshDevices"/> to re-enumerate.
        /// </summary>
        public static IReadOnlyList<AudioDeviceInfo> GetDevices(bool loopback)
        {
            if (loopback && !IsLoopbackSupported) loopback = false;
            ref List<AudioDeviceInfo> cache = ref loopback ? ref s_playbackDevices : ref s_inputDevices;
            if (cache == null)
            {
                using AudioCaptureContext ctx = AudioCaptureContext.Create();
                cache = ctx != null ? ctx.GetDevices(loopback) : new List<AudioDeviceInfo>();
            }
            return cache;
        }

        /// <summary>Drops the cached device lists; the next <see cref="GetDevices"/> call re-enumerates.</summary>
        public static void RefreshDevices()
        {
            s_inputDevices = null;
            s_playbackDevices = null;
        }

        private static void QueryBackend()
        {
            using AudioCaptureContext ctx = AudioCaptureContext.Create();
            s_loopbackSupported = ctx != null && ctx.IsLoopbackSupported;
            s_backendName = ctx != null ? ctx.BackendName : "unavailable";
        }
    }
}
