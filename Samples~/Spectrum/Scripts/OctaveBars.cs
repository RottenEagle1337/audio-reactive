// OctaveBars.cs
// Sample script: reads SpectrumAnalyzer.OctaveLevels from code and scales one bar per octave
// band (bars grow upwards from their base). Shows how to consume analyzer results directly.
using UnityEngine;

namespace AudioReactive.Samples
{
    public sealed class OctaveBars : MonoBehaviour
    {
        public SpectrumAnalyzer analyzer;
        [Tooltip("One transform per octave band (up to 10), e.g. unit cubes")]
        public Transform[] bars = new Transform[SpectrumAnalyzer.OctaveBandCount];
        public float minHeight = 0.05f;
        public float maxHeight = 3f;

        private void LateUpdate()
        {
            if (analyzer == null) return;
            var levels = analyzer.OctaveLevels;
            for (int i = 0; i < bars.Length && i < levels.Length; i++)
            {
                Transform bar = bars[i];
                if (bar == null) continue;
                float h = Mathf.Lerp(minHeight, maxHeight, levels[i]);
                Vector3 s = bar.localScale;
                bar.localScale = new Vector3(s.x, h, s.z);
                Vector3 p = bar.localPosition;
                bar.localPosition = new Vector3(p.x, h * 0.5f, p.z);
            }
        }
    }
}
