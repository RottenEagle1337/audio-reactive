// AudioCaptureContext.cs
// Internal managed wrapper over one native capture context: backend info, device enumeration,
// stream open/close and reads. One context owns at most one open stream. The public entry
// point of the capture layer is AudioSystem.
// Returns null from Create() when the native plugin is missing (unsupported platform),
// outdated or the audio backend fails to initialize; the reason is logged once.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace AudioReactive
{
    internal sealed class AudioCaptureContext : IDisposable
    {
        private const int IdBufferSize = 512;
        private const int NameBufferSize = 256;

        private IntPtr _handle;
        private readonly byte[] _idBuf = new byte[IdBufferSize];
        private readonly byte[] _nameBuf = new byte[NameBufferSize];

        private static bool? s_available;

        /// <summary>True when the native plugin is present and ABI-compatible (checked once per play session).</summary>
        public static bool IsAvailable
        {
            get
            {
                if (s_available.HasValue) return s_available.Value;
                s_available = CheckPlugin(out string error);
                if (!s_available.Value) Debug.LogWarning("[AudioReactive] " + error);
                return s_available.Value;
            }
        }

        // Enter Play Mode without domain reload keeps statics: check the plugin again each session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_available = null;

        public static AudioCaptureContext Create()
        {
            if (!IsAvailable) return null;
            var ctx = new AudioCaptureContext();
            if (ctx._handle != IntPtr.Zero) return ctx;
            ctx.Dispose();
            Debug.LogWarning("[AudioReactive] Failed to initialize the audio backend.");
            return null;
        }

        private static bool CheckPlugin(out string error)
        {
            error = null;
            try
            {
                int version = AudioCaptureNative.AC_GetApiVersion();
                if (version == AudioCaptureNative.ApiVersion) return true;
                error = $"AudioCapture native plugin API version {version}, expected {AudioCaptureNative.ApiVersion}. Rebuild and redeploy the plugin.";
            }
            catch (EntryPointNotFoundException)
            {
                error = "AudioCapture native plugin is outdated. Rebuild and redeploy the plugin.";
            }
            catch (DllNotFoundException)
            {
                error = $"Audio capture is unavailable: no AudioCapture native plugin for {Application.platform}.";
            }
            return false;
        }

        private AudioCaptureContext() => _handle = AudioCaptureNative.AC_CreateContext();

        /// <summary>
        /// Whether playback devices can be captured directly (WASAPI). Elsewhere capture
        /// system output through an input device (PulseAudio monitor source, BlackHole...).
        /// </summary>
        public bool IsLoopbackSupported => AudioCaptureNative.AC_IsLoopbackSupported(_handle);

        public string BackendName => Marshal.PtrToStringAnsi(AudioCaptureNative.AC_GetBackendName(_handle)) ?? "";

        /// <summary>loopback = false: input devices; true: playback devices (only if IsLoopbackSupported).</summary>
        public List<AudioDeviceInfo> GetDevices(bool loopback)
        {
            var list = new List<AudioDeviceInfo>();
            int count = AudioCaptureNative.AC_GetDeviceCount(_handle, loopback);
            for (int i = 0; i < count; i++)
                if (AudioCaptureNative.AC_GetDeviceInfo(_handle, loopback, i,
                        _idBuf, _idBuf.Length, _nameBuf, _nameBuf.Length))
                    list.Add(new AudioDeviceInfo(ReadUtf8(_idBuf), ReadUtf8(_nameBuf)));
            return list;
        }

        /// <summary>Null / empty id opens the system default device.</summary>
        public bool Open(string deviceId, bool loopback) =>
            AudioCaptureNative.AC_Open(_handle, loopback, string.IsNullOrEmpty(deviceId) ? null : deviceId);

        public void Close() => AudioCaptureNative.AC_Close(_handle);

        public float SampleRate => AudioCaptureNative.AC_GetSampleRate(_handle);

        public int ChannelCount => AudioCaptureNative.AC_GetChannelCount(_handle);

        /// <summary>Copies samples of `channel` captured since `cursor` into dest and advances the cursor.</summary>
        public int ReadNew(AudioChannel channel, float[] dest, ref ulong cursor) =>
            AudioCaptureNative.AC_ReadNew(_handle, (int)channel, dest, dest.Length, ref cursor);

        /// <summary>True once after the device was lost (unplugged / rerouted).</summary>
        public bool PollDeviceLost() => AudioCaptureNative.AC_PollDeviceLost(_handle);

        public void Dispose()
        {
            if (_handle != IntPtr.Zero)
            {
                AudioCaptureNative.AC_DestroyContext(_handle);
                _handle = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        ~AudioCaptureContext() => Dispose();

        private static string ReadUtf8(byte[] buf)
        {
            int len = Array.IndexOf(buf, (byte)0);
            return Encoding.UTF8.GetString(buf, 0, len < 0 ? buf.Length : len);
        }
    }
}
