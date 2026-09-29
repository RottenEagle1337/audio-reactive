// AudioLevelTracker.cs
// One analysis "channel": the level of an optionally band-filtered signal, plus a simple
// onset (hit) detector, driving a list of property binders. Several trackers with different
// bands give kick / snare / hat style signals; there are no presets.
//
// Per frame: new samples of the selected device channel -> biquad band filter (continuous
// stream, state kept across frames) -> mean-square envelope follower (sample accurate,
// frame-rate independent) -> dB -> auto gain / fixed gain normalization over the dynamic
// range -> hold-and-fall-down smoothing. Level algorithm after LASP's AudioLevelTracker.
// Onsets: rising energy above running average x threshold, re-armed after the energy falls
// back, limited by a refractory time; each onset sets Envelope to 1 which then decays.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AudioReactive
{
    /// <summary>Band filter of an AudioLevelTracker.</summary>
    public enum FilterType
    {
        Bypass,
        LowPass,
        BandPass,
        HighPass,
    }

    /// <summary>Level of an optionally band-filtered audio signal with onset detection, driving a list of property binders (one analysis "channel").</summary>
    [AddComponentMenu("Audio Reactive/Audio Level Tracker")]
    public sealed class AudioLevelTracker : MonoBehaviour
    {
        private const float EnergyTime = 0.01f;       // envelope follower time constant, s
        private const float AverageTime = 1f;         // onset reference average, s
        private const float RearmRatio = 0.7f;        // re-arm below threshold x ratio
        private const float OnsetFloorDb = -60f;      // no onsets below this input level
        private const float SilenceDb = -80f;
        private const float PeakFallSpeed = 0.6f;     // auto gain peak drift, dB/s

        [SerializeField] private DeviceSelection _device = DeviceSelection.Default;

        [SerializeField] private FilterType _filterType = FilterType.Bypass;
        [SerializeField, Tooltip("High-pass cutoff (HighPass, BandPass), Hz")] private float _lowCutoff = 60f;
        [SerializeField, Tooltip("Low-pass cutoff (LowPass, BandPass), Hz")] private float _highCutoff = 250f;

        [SerializeField, Tooltip("Normalize relative to the recent peak")] private bool _autoGain = true;
        [SerializeField, Range(-10f, 40f), Tooltip("Fixed gain when auto gain is off, dB")] private float _gain = 6f;
        [SerializeField, Range(1f, 60f), Tooltip("Range mapped to 0..1, dB")] private float _dynamicRange = 12f;

        [SerializeField, Tooltip("Instant rise, accelerating fall")] private bool _smoothFall = true;
        [SerializeField, Range(0f, 1f)] private float _fallSpeed = 0.3f;

        [SerializeField, Range(1.1f, 4f), Tooltip("Onset when energy exceeds the running average x threshold")]
        private float _onsetThreshold = 1.6f;
        [SerializeField, Range(0.02f, 0.5f), Tooltip("Minimum time between onsets, s")] private float _onsetRefractory = 0.1f;
        [SerializeField, Range(0.02f, 2f), Tooltip("Envelope decay after an onset, s")] private float _envelopeDecay = 0.2f;
        [SerializeField] private UnityEvent _onOnset = new UnityEvent();

        [SerializeReference] private List<PropertyBinder> _binders = new List<PropertyBinder>();

        private InputStream _stream;
        private DeviceSelection _streamDevice;
        private readonly BiquadFilter _hp = new BiquadFilter(), _lp = new BiquadFilter();
        private FilterType _appliedFilter = (FilterType)(-1);
        private float _appliedLow, _appliedHigh;
        private int _appliedRate;
        private float _meanSquare, _prevEnergy, _avgEnergy = 1e-6f, _energyCoef;
        private float _peakDb = SilenceDb, _fall, _lastOnsetTime = -1f;
        private bool _armed = true;

        // --- Settings (runtime access) ---

        public DeviceSelection Device { get => _device; set => _device = value; }
        public FilterType Filter { get => _filterType; set => _filterType = value; }
        public float LowCutoff { get => _lowCutoff; set => _lowCutoff = value; }
        public float HighCutoff { get => _highCutoff; set => _highCutoff = value; }
        public bool AutoGain { get => _autoGain; set => _autoGain = value; }
        public float Gain { get => _gain; set => _gain = value; }
        public float DynamicRange { get => _dynamicRange; set => _dynamicRange = value; }
        public List<PropertyBinder> Binders => _binders;
        public UnityEvent OnOnset => _onOnset;

        // --- Results (updated in Update) ---

        /// <summary>Filtered input RMS, dBFS (full-scale sine = 0 dB).</summary>
        public float InputLevelDb { get; private set; } = SilenceDb;
        /// <summary>Auto gain reference peak, dBFS.</summary>
        public float PeakDb => _peakDb;
        public float CalculatedGainDb => _autoGain ? -_peakDb : _gain;
        /// <summary>Normalized input level without fall smoothing, 0..1.</summary>
        public float InstantaneousLevel { get; private set; }
        /// <summary>Normalized level with fall smoothing, 0..1 (the main output).</summary>
        public float NormalizedLevel { get; private set; }
        /// <summary>Onset envelope: 1 on an onset, then decays to 0 over Envelope Decay.</summary>
        public float Envelope { get; private set; }
        /// <summary>True only in the frame an onset was detected.</summary>
        public bool IsOnset { get; private set; }
        public int LastUpdateFrame { get; private set; } = -1;

        public bool IsCapturing => _stream != null && _stream.IsOpen;
        public string DeviceName => _stream != null ? _stream.DeviceName : "none";
        public int SampleRate => _stream != null ? _stream.SampleRate : 0;

        /// <summary>Copies the latest raw samples of the selected channel (for waveform displays).</summary>
        public int CopyWaveform(Span<float> dest) => _stream != null ? _stream.CopyLatest(_device.channel, dest) : 0;

        // --- Lifecycle ---

        private void OnEnable() => AcquireStream();

        private void OnDisable()
        {
            AudioSystem.Release(_stream);
            _stream = null;
        }

        private void Update()
        {
            if (!_device.SameDevice(_streamDevice))
            {
                AudioSystem.Release(_stream);
                AcquireStream();
            }

            Analyze(Time.deltaTime, Time.time);
            LastUpdateFrame = Time.frameCount;

            for (int i = 0; i < _binders.Count; i++) _binders[i]?.Apply(this);
            if (IsOnset) _onOnset.Invoke();
        }

        private void AcquireStream()
        {
            _stream = AudioSystem.Acquire(_device);
            _streamDevice = _device;
            _appliedRate = 0;   // reconfigure filters for the new stream
        }

        // --- Analysis ---

        private void Analyze(float dt, float time)
        {
            ReadOnlySpan<float> input = _stream.IsOpen ? _stream.GetNewSamples(_device.channel) : ReadOnlySpan<float>.Empty;
            ConfigureFilter();

            float ms = _meanSquare;
            bool hp = _filterType == FilterType.HighPass || _filterType == FilterType.BandPass;
            bool lp = _filterType == FilterType.LowPass || _filterType == FilterType.BandPass;
            if (!_stream.IsOpen) ms = 0f;
            for (int i = 0; i < input.Length; i++)
            {
                float v = input[i];
                if (hp) v = _hp.Process(v);
                if (lp) v = _lp.Process(v);
                ms += (v * v - ms) * _energyCoef;
            }
            _meanSquare = ms;
            float energy = Mathf.Sqrt(ms);

            // Level: RMS -> dBFS (full-scale sine = 0 dB) -> normalization.
            float inputDb = Mathf.Max(20f * Mathf.Log10(energy / 0.7071f + 1e-9f), SilenceDb);
            InputLevelDb = inputDb;

            if (_autoGain)
            {
                _peakDb = Mathf.Max(_peakDb - PeakFallSpeed * dt, SilenceDb);
                _peakDb = Mathf.Clamp(inputDb - _dynamicRange * 0.05f, _peakDb, 0f);
            }

            float norm = Mathf.Clamp01((inputDb + CalculatedGainDb) / _dynamicRange + 1f);
            InstantaneousLevel = norm;

            if (_smoothFall)
            {
                _fall += Mathf.Pow(10f, 1f + _fallSpeed * 2f) * dt;
                float level = NormalizedLevel - _fall * dt;
                if (level < norm) { level = norm; _fall = 0f; }
                NormalizedLevel = level;
            }
            else
            {
                NormalizedLevel = norm;
            }

            DetectOnset(energy, inputDb, input.Length > 0, dt, time);
        }

        private void DetectOnset(float energy, float inputDb, bool hasInput, float dt, float time)
        {
            IsOnset = false;
            if (hasInput)
            {
                float threshold = _avgEnergy * _onsetThreshold;
                if (energy < threshold * RearmRatio) _armed = true;

                if (_armed && energy > threshold && energy > _prevEnergy && inputDb > OnsetFloorDb &&
                    time - _lastOnsetTime >= _onsetRefractory)
                {
                    IsOnset = true;
                    _armed = false;
                    _lastOnsetTime = time;
                    Envelope = 1f;
                }
                else
                {
                    float k = dt > 0f ? 1f - Mathf.Exp(-dt / AverageTime) : 1f;
                    _avgEnergy = Mathf.Max(_avgEnergy + (energy - _avgEnergy) * k, 1e-6f);
                }
                _prevEnergy = energy;
            }

            if (!IsOnset && Envelope > 0f)
                Envelope = Mathf.Max(0f, Envelope - dt / _envelopeDecay);
        }

        private void ConfigureFilter()
        {
            int rate = _stream.SampleRate;
            if (_filterType == _appliedFilter && rate == _appliedRate &&
                _lowCutoff == _appliedLow && _highCutoff == _appliedHigh) return;

            float nyquist = rate * 0.5f;
            _hp.SetHighPass(Mathf.Clamp(_lowCutoff, 10f, nyquist * 0.95f), rate);
            _lp.SetLowPass(Mathf.Clamp(_highCutoff, 10f, nyquist * 0.95f), rate);
            _hp.Reset();
            _lp.Reset();
            _energyCoef = 1f - Mathf.Exp(-1f / (EnergyTime * rate));

            _appliedFilter = _filterType;
            _appliedRate = rate;
            _appliedLow = _lowCutoff;
            _appliedHigh = _highCutoff;
        }
    }
}
