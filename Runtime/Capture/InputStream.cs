// InputStream.cs
// One open capture device shared by every tracker that selects it (see AudioSystem).
// Updates lazily, at most once per frame, on the first access: pulls the samples captured
// since the previous frame for each channel somebody asked for (mono / left / right) and
// appends them to a per-channel history. Consumers read either the new samples (continuous
// stream for filters and envelope followers) or the latest window (FFT, waveform).
// Also polls for a lost device once per second and reopens it.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AudioReactive
{
    /// <summary>One open capture device shared by all trackers that select it. Obtain with AudioSystem.Acquire.</summary>
    public sealed class InputStream : IDisposable
    {
        /// <summary>~340 ms at 48 kHz: covers FFT windows up to 8192 samples.</summary>
        public const int HistorySize = 16384;
        private const int HistoryMask = HistorySize - 1;
        private const float DeviceCheckInterval = 1f;

        private sealed class ChannelData
        {
            public readonly float[] History = new float[HistorySize];
            public readonly float[] NewSamples = new float[HistorySize];
            public int NewCount;
            public long Written;
            public ulong Cursor;
            public bool Active;
        }

        private readonly ChannelData[] _channels = { new ChannelData(), new ChannelData(), new ChannelData() };
        private readonly List<SpectrumFrame> _spectra = new List<SpectrumFrame>();
        private AudioCaptureContext _ctx;
        private int _lastFrame = -1;
        private float _nextDeviceCheck;

        internal int RefCount;

        public DeviceSelection Device { get; }
        public bool IsOpen { get; private set; }
        public int SampleRate { get; private set; } = 48000;
        public int ChannelCount { get; private set; }
        public string DeviceName { get; private set; } = "none";

        internal InputStream(DeviceSelection device)
        {
            Device = device;
            _ctx = AudioCaptureContext.Create();
            if (_ctx != null) Open();
        }

        // --- Consumer API (all calls are cheap after the first one in a frame) ---

        /// <summary>Samples of `channel` captured since the previous frame, oldest first.</summary>
        public ReadOnlySpan<float> GetNewSamples(AudioChannel channel)
        {
            ChannelData c = Prepare(channel);
            return new ReadOnlySpan<float>(c.NewSamples, 0, c.NewCount);
        }

        /// <summary>Copies the newest dest.Length samples of `channel` (oldest first). Returns the count copied.</summary>
        public int CopyLatest(AudioChannel channel, Span<float> dest)
        {
            ChannelData c = Prepare(channel);
            int n = Math.Min(dest.Length, HistorySize);
            long start = c.Written - n;
            for (int i = 0; i < n; i++)
                dest[i] = c.History[(int)((start + i) & HistoryMask)];
            return n;
        }

        /// <summary>Shared spectrum of `channel` at the given FFT size (computed once per frame).</summary>
        public SpectrumFrame GetSpectrum(AudioChannel channel, int size)
        {
            size = SpectrumFrame.ClampSize(size);
            foreach (SpectrumFrame f in _spectra)
                if (f.Channel == channel && f.Size == size) return f;
            var frame = new SpectrumFrame(this, channel, size);
            _spectra.Add(frame);
            return frame;
        }

        /// <summary>Forces a reopen (e.g. after the device list changed).</summary>
        public void Reopen()
        {
            if (_ctx == null) return;
            _ctx.Close();
            Open();
        }

        public void Dispose()
        {
            _ctx?.Dispose();
            _ctx = null;
            IsOpen = false;
        }

        // --- Internals ---

        private ChannelData Prepare(AudioChannel channel)
        {
            ChannelData c = _channels[(int)channel + 1];
            if (!c.Active)
            {
                c.Active = true;
                SkipToNow(channel, c);
            }
            Update();
            return c;
        }

        private void Update()
        {
            int frame = Time.frameCount;
            if (frame == _lastFrame) return;
            _lastFrame = frame;

            for (int i = 0; i < _channels.Length; i++)
            {
                ChannelData c = _channels[i];
                if (!c.Active) continue;
                int n = IsOpen ? _ctx.ReadNew((AudioChannel)(i - 1), c.NewSamples, ref c.Cursor) : 0;
                c.NewCount = n;
                for (int k = 0; k < n; k++)
                    c.History[(int)((c.Written + k) & HistoryMask)] = c.NewSamples[k];
                c.Written += n;
            }

            if (_ctx != null && Time.unscaledTime >= _nextDeviceCheck)
            {
                _nextDeviceCheck = Time.unscaledTime + DeviceCheckInterval;
                if (_ctx.PollDeviceLost())
                {
                    Debug.LogWarning($"[AudioReactive] Device '{DeviceName}' lost, reopening.");
                    Reopen();
                }
            }
        }

        private void Open()
        {
            IsOpen = false;
            bool loopback = Device.loopback;
            if (loopback && !_ctx.IsLoopbackSupported)
            {
                Debug.LogWarning($"[AudioReactive] Loopback capture is not supported by {_ctx.BackendName}; " +
                                 "using an input device instead (select a monitor source or a virtual loopback device).");
                loopback = false;
            }

            string id = Device.deviceId;
            string name = "default";
            if (!string.IsNullOrEmpty(id))
            {
                name = null;
                foreach (AudioDeviceInfo d in _ctx.GetDevices(loopback))
                    if (d.Id == id) { name = d.Name; break; }

                if (name != null && _ctx.Open(id, loopback)) IsOpen = true;
                else
                {
                    Debug.LogWarning($"[AudioReactive] Device '{id}' is unavailable, using the default device.");
                    name = "default (fallback)";
                }
            }
            if (!IsOpen) IsOpen = _ctx.Open(null, loopback);

            DeviceName = IsOpen ? name : "none";
            SampleRate = IsOpen ? Mathf.Max(1, (int)_ctx.SampleRate) : SampleRate;
            ChannelCount = IsOpen ? _ctx.ChannelCount : 0;
            _nextDeviceCheck = Time.unscaledTime + DeviceCheckInterval;

            for (int i = 0; i < _channels.Length; i++)
            {
                ChannelData c = _channels[i];
                Array.Clear(c.History, 0, HistorySize);
                c.NewCount = 0;
                if (c.Active) SkipToNow((AudioChannel)(i - 1), c);
            }

            if (IsOpen)
                Debug.Log($"[AudioReactive] Stream opened: {_ctx.BackendName}, {(loopback ? "loopback" : "input")}, '{DeviceName}', {SampleRate} Hz, {ChannelCount} ch.");
            else
                Debug.LogWarning("[AudioReactive] Failed to open the audio stream.");
        }

        // Discards whatever the native ring buffered before now.
        private void SkipToNow(AudioChannel channel, ChannelData c)
        {
            c.Cursor = 0;
            if (IsOpen) _ctx.ReadNew(channel, c.NewSamples, ref c.Cursor);
        }
    }
}
