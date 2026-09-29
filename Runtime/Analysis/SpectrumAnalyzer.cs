// SpectrumAnalyzer.cs
// Spectrum of the selected device channel on a logarithmic frequency scale, plus the ten
// ISO octave band levels and an RFloat texture of the log bands for shaders / VFX Graph.
//
// Per frame: shared FFT (InputStream.GetSpectrum) -> average magnitude per band (bands
// narrower than one FFT bin interpolate between bins) -> dB -> auto gain / fixed gain
// normalization over the dynamic range -> attack / release smoothing. All outputs are 0..1.
using System;
using UnityEngine;

namespace AudioReactive
{
    /// <summary>Log-scale spectrum, ten ISO octave levels and a spectrum texture of the selected device channel.</summary>
    [AddComponentMenu("Audio Reactive/Spectrum Analyzer")]
    public sealed class SpectrumAnalyzer : MonoBehaviour
    {
        public const int MaxLogBands = 512;
        public const int OctaveBandCount = 10;

        // ISO octave band centers, Hz.
        private static readonly float[] OctaveCenters =
            { 31.5f, 63f, 125f, 250f, 500f, 1000f, 2000f, 4000f, 8000f, 16000f };

        private const float SilenceDb = -100f;
        private const float PeakFallSpeed = 1f;   // auto gain peak drift, dB/s

        [SerializeField] private DeviceSelection _device = DeviceSelection.Default;

        [SerializeField, Tooltip("FFT size (power of two)")] private int _resolution = 2048;
        [SerializeField, Range(8, MaxLogBands)] private int _logBandCount = 64;
        [SerializeField, Range(10f, 200f), Tooltip("Lower edge of the log scale, Hz")] private float _minFrequency = 20f;

        [SerializeField, Tooltip("Normalize relative to the loudest band's recent peak")] private bool _autoGain = true;
        [SerializeField, Range(-20f, 60f), Tooltip("Fixed gain when auto gain is off, dB")] private float _gain = 0f;
        [SerializeField, Range(6f, 100f), Tooltip("Range mapped to 0..1, dB")] private float _dynamicRange = 60f;

        [SerializeField, Range(0.001f, 0.2f), Tooltip("Rise time, s")] private float _attack = 0.02f;
        [SerializeField, Range(0.01f, 1f), Tooltip("Fall time, s")] private float _release = 0.25f;

        private InputStream _stream;
        private DeviceSelection _streamDevice;
        private float[] _logBands = Array.Empty<float>();
        private readonly float[] _octaves = new float[OctaveBandCount];
        private float[] _bandLo, _bandHi;                     // fractional bin edges per log band
        private readonly float[] _octLo = new float[OctaveBandCount], _octHi = new float[OctaveBandCount];
        private int _layoutSize, _layoutRate, _layoutBands;
        private float _layoutMinFreq;
        private float _peakDb = SilenceDb;
        private Texture2D _texture;

        // --- Settings (runtime access) ---

        public DeviceSelection Device { get => _device; set => _device = value; }
        public int Resolution { get => _resolution; set => _resolution = value; }
        public int LogBandCount { get => _logBandCount; set => _logBandCount = value; }
        public bool AutoGain { get => _autoGain; set => _autoGain = value; }
        public float DynamicRange { get => _dynamicRange; set => _dynamicRange = value; }

        // --- Results ---

        public ReadOnlySpan<float> LogBands => _logBands;
        public ReadOnlySpan<float> OctaveLevels => _octaves;
        public float PeakDb => _peakDb;
        public int FftSize => _layoutSize;
        public int SampleRate => _stream != null ? _stream.SampleRate : 0;
        public bool IsCapturing => _stream != null && _stream.IsOpen;
        public int LastUpdateFrame { get; private set; } = -1;

        /// <summary>Log bands as an RFloat texture (width = log band count, height 1); null before the first frame.</summary>
        public Texture2D Texture => _texture;

        // --- Lifecycle ---

        private void OnEnable()
        {
            _stream = AudioSystem.Acquire(_device);
            _streamDevice = _device;
        }

        private void OnDisable()
        {
            AudioSystem.Release(_stream);
            _stream = null;
        }

        private void OnDestroy()
        {
            if (_texture != null) Destroy(_texture);
        }

        private void Update()
        {
            if (!_device.SameDevice(_streamDevice))
            {
                AudioSystem.Release(_stream);
                _stream = AudioSystem.Acquire(_device);
                _streamDevice = _device;
            }

            SpectrumFrame frame = _stream.GetSpectrum(_device.channel, _resolution);
            EnsureLayout(frame.Size);
            Analyze(frame.Magnitudes, Time.deltaTime);
            UpdateTexture();
            LastUpdateFrame = Time.frameCount;
        }

        // --- Analysis ---

        private void Analyze(float[] mag, float dt)
        {
            float ka = dt > 0f ? 1f - Mathf.Exp(-dt / _attack) : 1f;
            float kr = dt > 0f ? 1f - Mathf.Exp(-dt / _release) : 1f;
            float gain = _autoGain ? -_peakDb : _gain;
            float loudest = SilenceDb;

            for (int b = 0; b < _logBands.Length; b++)
            {
                float db = ToDb(BandAverage(mag, _bandLo[b], _bandHi[b]));
                loudest = Mathf.Max(loudest, db);
                _logBands[b] = Smooth(_logBands[b], Normalize(db, gain), ka, kr);
            }

            for (int b = 0; b < OctaveBandCount; b++)
                _octaves[b] = Smooth(_octaves[b], Normalize(ToDb(BandAverage(mag, _octLo[b], _octHi[b])), gain), ka, kr);

            if (_autoGain)
            {
                _peakDb = Mathf.Max(_peakDb - PeakFallSpeed * dt, SilenceDb);
                _peakDb = Mathf.Clamp(loudest, _peakDb, 0f);
            }
        }

        private float Normalize(float db, float gain) => Mathf.Clamp01((db + gain) / _dynamicRange + 1f);

        private static float Smooth(float current, float target, float ka, float kr) =>
            current + (target - current) * (target > current ? ka : kr);

        private static float ToDb(float magnitude) => Mathf.Max(20f * Mathf.Log10(magnitude + 1e-9f), SilenceDb);

        // Average magnitude over fractional bin range [lo, hi); narrow bands interpolate.
        private static float BandAverage(float[] mag, float lo, float hi)
        {
            int last = mag.Length - 1;
            if (hi - lo < 1f)
            {
                float c = Mathf.Clamp((lo + hi) * 0.5f, 0f, last);
                int i = Mathf.Min((int)c, last - 1);
                return Mathf.Lerp(mag[i], mag[i + 1], c - i);
            }
            int from = Mathf.Clamp(Mathf.FloorToInt(lo), 0, last);
            int to = Mathf.Clamp(Mathf.CeilToInt(hi), from + 1, mag.Length);
            float sum = 0f;
            for (int k = from; k < to; k++) sum += mag[k];
            return sum / (to - from);
        }

        // Rebuilds band edges when the FFT size, sample rate, band count or min frequency changes.
        private void EnsureLayout(int fftSize)
        {
            int bands = Mathf.Clamp(_logBandCount, 1, MaxLogBands);
            int rate = _stream.SampleRate;
            if (fftSize == _layoutSize && rate == _layoutRate && bands == _layoutBands && _minFrequency == _layoutMinFreq)
                return;

            if (bands != _logBands.Length)
            {
                _logBands = new float[bands];
                _bandLo = new float[bands];
                _bandHi = new float[bands];
            }
            _layoutSize = fftSize;
            _layoutRate = rate;
            _layoutBands = bands;
            _layoutMinFreq = _minFrequency;

            float binHz = (float)rate / fftSize;
            float minF = Mathf.Max(1f, _minFrequency);
            float maxF = Mathf.Max(minF * 2f, rate * 0.5f);
            for (int b = 0; b < bands; b++)
            {
                _bandLo[b] = minF * Mathf.Pow(maxF / minF, (float)b / bands) / binHz;
                _bandHi[b] = minF * Mathf.Pow(maxF / minF, (float)(b + 1) / bands) / binHz;
            }

            // Octave edges: geometric means between centers, half an octave beyond the ends.
            for (int b = 0; b < OctaveBandCount; b++)
            {
                float lo = b == 0 ? OctaveCenters[0] / Mathf.Sqrt(2f) : Mathf.Sqrt(OctaveCenters[b - 1] * OctaveCenters[b]);
                float hi = b == OctaveBandCount - 1 ? OctaveCenters[b] * Mathf.Sqrt(2f) : Mathf.Sqrt(OctaveCenters[b] * OctaveCenters[b + 1]);
                _octLo[b] = lo / binHz;
                _octHi[b] = Mathf.Min(hi, rate * 0.5f) / binHz;
            }
        }

        private void UpdateTexture()
        {
            if (_texture == null || _texture.width != _logBands.Length)
            {
                if (_texture != null) Destroy(_texture);
                _texture = new Texture2D(_logBands.Length, 1, TextureFormat.RFloat, false, true)
                {
                    name = "Audio Reactive Spectrum",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.DontSave,
                };
            }
            _texture.SetPixelData(_logBands, 0);
            _texture.Apply(false);
        }
    }
}
