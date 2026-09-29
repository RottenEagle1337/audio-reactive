// Fft.cs
// Iterative in-place radix-2 FFT (Cooley-Tukey) with bit-reversal permutation.
// Operates on re/im arrays of equal power-of-two length; allocation-free.
using UnityEngine;

namespace AudioReactive
{
    /// <summary>In-place radix-2 FFT helpers.</summary>
    public static class Fft
    {
        public static void Transform(float[] re, float[] im)
        {
            int n = re.Length;

            // Bit-reversal permutation.
            for (int i = 1, j = 0; i < n; i++)
            {
                int bit = n >> 1;
                for (; (j & bit) != 0; bit >>= 1) j ^= bit;
                j ^= bit;

                if (i < j)
                {
                    (re[i], re[j]) = (re[j], re[i]);
                    (im[i], im[j]) = (im[j], im[i]);
                }
            }

            // Butterfly stages.
            for (int len = 2; len <= n; len <<= 1)
            {
                float ang = -2f * Mathf.PI / len;
                float wlenRe = Mathf.Cos(ang);
                float wlenIm = Mathf.Sin(ang);
                int half = len >> 1;

                for (int i = 0; i < n; i += len)
                {
                    float wRe = 1f, wIm = 0f;
                    for (int j = 0; j < half; j++)
                    {
                        int a = i + j;
                        int b = a + half;

                        float vRe = re[b] * wRe - im[b] * wIm;
                        float vIm = re[b] * wIm + im[b] * wRe;

                        re[b] = re[a] - vRe;
                        im[b] = im[a] - vIm;
                        re[a] += vRe;
                        im[a] += vIm;

                        // Rotate the twiddle: w *= wlen (no per-step trig calls).
                        float nwRe = wRe * wlenRe - wIm * wlenIm;
                        wIm = wRe * wlenIm + wIm * wlenRe;
                        wRe = nwRe;
                    }
                }
            }
        }

        /// <summary>Fills a preallocated array with a Hann window of its length.</summary>
        public static void FillHannWindow(float[] window)
        {
            int n = window.Length;
            for (int i = 0; i < n; i++)
                window[i] = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * i / (n - 1));
        }
    }
}
