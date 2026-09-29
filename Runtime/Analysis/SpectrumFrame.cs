// SpectrumFrame.cs
// Shared spectrum of one stream channel at one FFT size: Hann window + radix-2 FFT of the
// latest samples, computed lazily at most once per frame no matter how many analyzers read it.
// Obtain through InputStream.GetSpectrum.
//
// Magnitudes are single-sided and scaled by 2/N (a full-scale sine peaks near 0.5
// after the Hann window's coherent gain).
using UnityEngine;

namespace AudioReactive
{
    /// <summary>Shared FFT of one stream channel at one size, computed at most once per frame. Obtain with InputStream.GetSpectrum.</summary>
    public sealed class SpectrumFrame
    {
        public const int MinSize = 256;
        public const int MaxSize = 8192;

        private readonly InputStream _stream;
        private readonly float[] _samples, _re, _im, _window, _magnitudes;
        private int _lastFrame = -1;

        internal SpectrumFrame(InputStream stream, AudioChannel channel, int size)
        {
            _stream = stream;
            Channel = channel;
            Size = size;
            _samples = new float[size];
            _re = new float[size];
            _im = new float[size];
            _window = new float[size];
            _magnitudes = new float[size / 2];
            Fft.FillHannWindow(_window);
        }

        public AudioChannel Channel { get; }
        public int Size { get; }

        /// <summary>Frequency resolution, Hz per bin.</summary>
        public float BinHz => (float)_stream.SampleRate / Size;

        public static int ClampSize(int size) => Mathf.ClosestPowerOfTwo(Mathf.Clamp(size, MinSize, MaxSize));

        /// <summary>Single-sided magnitudes of the current frame (Size / 2 values).</summary>
        public float[] Magnitudes
        {
            get { Update(); return _magnitudes; }
        }

        private void Update()
        {
            if (_lastFrame == Time.frameCount) return;
            _lastFrame = Time.frameCount;

            int n = Size;
            _stream.CopyLatest(Channel, _samples);
            for (int i = 0; i < n; i++)
            {
                _re[i] = _samples[i] * _window[i];
                _im[i] = 0f;
            }

            Fft.Transform(_re, _im);

            float scale = 2f / n;
            for (int k = 0; k < n / 2; k++)
                _magnitudes[k] = Mathf.Sqrt(_re[k] * _re[k] + _im[k] * _im[k]) * scale;
        }
    }
}
