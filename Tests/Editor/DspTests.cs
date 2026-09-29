// DspTests.cs
// EditMode tests of the signal processing building blocks: FFT peak placement and biquad
// low-pass / high-pass attenuation.
using NUnit.Framework;
using UnityEngine;

namespace AudioReactive.Tests
{
    public sealed class DspTests
    {
        private const float SampleRate = 48000f;

        [Test]
        public void Fft_SinePeaksAtItsBin()
        {
            const int n = 1024, bin = 37;
            var re = new float[n];
            var im = new float[n];
            for (int i = 0; i < n; i++) re[i] = Mathf.Sin(2f * Mathf.PI * bin * i / n);

            Fft.Transform(re, im);

            int peak = 0;
            float best = 0f;
            for (int k = 0; k < n / 2; k++)
            {
                float m = re[k] * re[k] + im[k] * im[k];
                if (m > best) { best = m; peak = k; }
            }
            Assert.AreEqual(bin, peak);
        }

        [Test]
        public void Fft_HannWindowIsSymmetricAndZeroAtEdges()
        {
            var w = new float[256];
            Fft.FillHannWindow(w);
            Assert.AreEqual(0f, w[0], 1e-6f);
            Assert.AreEqual(0f, w[255], 1e-6f);
            Assert.AreEqual(w[10], w[245], 1e-5f);
        }

        [Test]
        public void LowPass_PassesLowAndCutsHigh()
        {
            Assert.Greater(Gain(f => f.SetLowPass(200f, SampleRate), 50f), 0.9f);
            Assert.Less(Gain(f => f.SetLowPass(200f, SampleRate), 5000f), 0.01f);
        }

        [Test]
        public void HighPass_PassesHighAndCutsLow()
        {
            Assert.Greater(Gain(f => f.SetHighPass(4000f, SampleRate), 12000f), 0.9f);
            Assert.Less(Gain(f => f.SetHighPass(4000f, SampleRate), 100f), 0.01f);
        }

        // Steady-state RMS gain of the filter for a sine at `frequency`.
        private static float Gain(System.Action<BiquadFilter> setup, float frequency)
        {
            var filter = new BiquadFilter();
            setup(filter);
            const int settle = 4800, measure = 9600;
            double inSum = 0, outSum = 0;
            for (int i = 0; i < settle + measure; i++)
            {
                float x = Mathf.Sin(2f * Mathf.PI * frequency * i / SampleRate);
                float y = filter.Process(x);
                if (i < settle) continue;
                inSum += x * x;
                outSum += y * y;
            }
            return (float)System.Math.Sqrt(outSum / inSum);
        }
    }
}
