// BiquadFilter.cs
// Biquad filter (RBJ Audio EQ Cookbook), direct form I. A high-pass -> low-pass cascade
// forms a band; a single HP / LP gives the top / bottom region. Used by ChannelAnalyzer
// in the time domain, which is more accurate at low frequencies than FFT bins
// (at 2048 points a bin is ~23 Hz, so a 40-100 Hz kick spans only 3-4 bins).
// State persists between calls: feed it a continuous stream (IAudioSource.NewSamples).
using UnityEngine;

namespace AudioReactive
{
    /// <summary>RBJ biquad low-pass / high-pass filter with persistent state.</summary>
    public sealed class BiquadFilter
    {
        private double _b0, _b1, _b2, _a1, _a2;
        private float _x1, _x2, _y1, _y2;

        public void SetLowPass(float freq, float sampleRate, float q = 0.7071f)
        {
            float w0 = 2f * Mathf.PI * freq / sampleRate;
            float cosw0 = Mathf.Cos(w0);
            float alpha = Mathf.Sin(w0) / (2f * q);
            double a0 = 1.0 + alpha;
            _b0 = (1.0 - cosw0) * 0.5 / a0;
            _b1 = (1.0 - cosw0) / a0;
            _b2 = _b0;
            _a1 = -2.0 * cosw0 / a0;
            _a2 = (1.0 - alpha) / a0;
        }

        public void SetHighPass(float freq, float sampleRate, float q = 0.7071f)
        {
            float w0 = 2f * Mathf.PI * freq / sampleRate;
            float cosw0 = Mathf.Cos(w0);
            float alpha = Mathf.Sin(w0) / (2f * q);
            double a0 = 1.0 + alpha;
            _b0 = (1.0 + cosw0) * 0.5 / a0;
            _b1 = -(1.0 + cosw0) / a0;
            _b2 = _b0;
            _a1 = -2.0 * cosw0 / a0;
            _a2 = (1.0 - alpha) / a0;
        }

        public float Process(float x)
        {
            float y = (float)(_b0 * x + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2);
            _x2 = _x1; _x1 = x;
            _y2 = _y1; _y1 = y;
            return y;
        }

        public void Reset() => _x1 = _x2 = _y1 = _y2 = 0f;
    }
}
