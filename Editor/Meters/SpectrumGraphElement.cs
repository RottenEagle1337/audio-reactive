// SpectrumGraphElement.cs
// Inspector graph for SpectrumAnalyzer: log bands as bars and the ten octave levels below,
// all in one mesh (up to 512 + 10 bars = one draw call). Repainted by MeterScheduler only
// when the analyzer produced a new frame; the info text refreshes at 10 Hz when it changed.
using UnityEngine;
using UnityEngine.UIElements;

namespace AudioReactive.EditorTools
{
    internal sealed class SpectrumGraphElement : VisualElement, IMeter
    {
        private const float BandsHeight = 90f;
        private const float OctavesHeight = 36f;
        private const float Gap = 6f;

        private readonly SpectrumAnalyzer _analyzer;
        private readonly VisualElement _canvas;
        private readonly Label _label;
        private int _drawnFrame = -1;
        private int _shownSize, _shownBands, _shownPeak = int.MinValue;

        public SpectrumGraphElement(SpectrumAnalyzer analyzer)
        {
            _analyzer = analyzer;
            style.marginTop = 4f;
            style.marginBottom = 4f;

            _canvas = new VisualElement { style = { height = BandsHeight + Gap + OctavesHeight } };
            _canvas.generateVisualContent += Draw;
            Add(_canvas);

            _label = new Label("Enter Play Mode to see the spectrum.") { style = { opacity = 0.8f } };
            Add(_label);

            RegisterCallback<AttachToPanelEvent>(_ => MeterScheduler.Register(this));
            RegisterCallback<DetachFromPanelEvent>(_ => MeterScheduler.Unregister(this));
        }

        public void Tick(bool textDue)
        {
            if (_analyzer == null || !MeterScheduler.IsShown(this)) return;

            if (_analyzer.LastUpdateFrame != _drawnFrame)
            {
                _drawnFrame = _analyzer.LastUpdateFrame;
                _canvas.MarkDirtyRepaint();
            }

            if (!textDue) return;
            int size = _analyzer.FftSize, bands = _analyzer.LogBands.Length;
            int peak = Mathf.RoundToInt(_analyzer.PeakDb);
            if (size == _shownSize && bands == _shownBands && peak == _shownPeak) return;
            _shownSize = size;
            _shownBands = bands;
            _shownPeak = peak;
            float binHz = size > 0 ? (float)_analyzer.SampleRate / size : 0f;
            _label.text = $"FFT {size}   {binHz:F1} Hz/bin   {bands} bands   Peak {peak} dB";
        }

        private void Draw(MeshGenerationContext context)
        {
            Rect r = _canvas.contentRect;
            if (r.width <= 0f || _analyzer == null) return;

            var bands = _analyzer.LogBands;
            var octaves = _analyzer.OctaveLevels;
            var mesh = new QuadMesh(context, 2 + bands.Length + octaves.Length);

            float bandsBottom = r.yMin + BandsHeight;
            float octTop = bandsBottom + Gap, octBottom = octTop + OctavesHeight;
            mesh.Rect(r.xMin, r.yMin, r.xMax, bandsBottom, MeterColors.Background);
            mesh.Rect(r.xMin, octTop, r.xMax, octBottom, MeterColors.Background);

            float bw = r.width / Mathf.Max(1, bands.Length);
            float gap = bw > 3f ? 1f : 0f;
            for (int i = 0; i < bands.Length; i++)
            {
                float x = r.xMin + i * bw;
                mesh.Rect(x, bandsBottom - BandsHeight * bands[i], x + bw - gap, bandsBottom, MeterColors.Fill);
            }

            float ow = r.width / octaves.Length;
            for (int i = 0; i < octaves.Length; i++)
            {
                float x = r.xMin + i * ow;
                mesh.Rect(x + 1f, octBottom - OctavesHeight * octaves[i], x + ow - 1f, octBottom, MeterColors.Octave);
            }
        }
    }
}
