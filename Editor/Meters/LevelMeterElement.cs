// LevelMeterElement.cs
// Inspector level meter for AudioLevelTracker (after LASP's level meter): 6 dB grid over the
// dynamic range, smoothed level fill, instantaneous input marker, onset envelope strip and
// onset flash, plus a dB readout. Drawn as one mesh; repainted by MeterScheduler only when
// the quantized values changed, text refreshed at 10 Hz only when its numbers changed.
using UnityEngine;
using UnityEngine.UIElements;

namespace AudioReactive.EditorTools
{
    internal sealed class LevelMeterElement : VisualElement, IMeter
    {
        private const float BarHeight = 22f;
        private const float EnvelopeHeight = 4f;
        private const float FlashWidth = 18f;
        private const int MaxGridLines = 10;
        private const int QuadCount = 1 + MaxGridLines + 4;

        private readonly AudioLevelTracker _tracker;
        private readonly VisualElement _canvas;
        private readonly Label _label;
        private int _drawnLevel = -1, _drawnInstant = -1, _drawnEnvelope = -1, _drawnRange = -1;
        private int _shownDb = int.MinValue, _shownPeak = int.MinValue, _shownGain = int.MinValue;

        public LevelMeterElement(AudioLevelTracker tracker)
        {
            _tracker = tracker;
            style.marginTop = 4f;
            style.marginBottom = 4f;

            _canvas = new VisualElement { style = { height = BarHeight + EnvelopeHeight + 2f } };
            _canvas.generateVisualContent += Draw;
            Add(_canvas);

            _label = new Label("Enter Play Mode to see the level.") { style = { unityTextAlign = TextAnchor.MiddleLeft, opacity = 0.8f } };
            Add(_label);

            RegisterCallback<AttachToPanelEvent>(_ => MeterScheduler.Register(this));
            RegisterCallback<DetachFromPanelEvent>(_ => MeterScheduler.Unregister(this));
        }

        public void Tick(bool textDue)
        {
            if (_tracker == null || !MeterScheduler.IsShown(this)) return;

            int level = Quantize(_tracker.NormalizedLevel);
            int instant = Quantize(_tracker.InstantaneousLevel);
            int envelope = Quantize(_tracker.Envelope);
            int range = Mathf.RoundToInt(_tracker.DynamicRange);
            if (level != _drawnLevel || instant != _drawnInstant || envelope != _drawnEnvelope || range != _drawnRange)
            {
                _drawnLevel = level;
                _drawnInstant = instant;
                _drawnEnvelope = envelope;
                _drawnRange = range;
                _canvas.MarkDirtyRepaint();
            }

            if (!textDue) return;
            int db = Mathf.RoundToInt(_tracker.InputLevelDb * 10f);
            int peak = Mathf.RoundToInt(_tracker.PeakDb * 10f);
            int gain = Mathf.RoundToInt(_tracker.CalculatedGainDb * 10f);
            if (db == _shownDb && peak == _shownPeak && gain == _shownGain) return;
            _shownDb = db;
            _shownPeak = peak;
            _shownGain = gain;
            _label.text = _tracker.IsCapturing
                ? $"Input {db * 0.1f:F1} dB   Peak {peak * 0.1f:F1} dB   Gain {gain * 0.1f:F1} dB   ({_tracker.DeviceName})"
                : "Not capturing";
        }

        private static int Quantize(float v) => Mathf.RoundToInt(Mathf.Clamp01(v) * 500f);

        private void Draw(MeshGenerationContext context)
        {
            Rect r = _canvas.contentRect;
            if (r.width <= FlashWidth + 4f) return;

            var mesh = new QuadMesh(context, QuadCount);
            float barRight = r.xMax - FlashWidth - 4f;
            float width = barRight - r.xMin;
            float top = r.yMin, bottom = r.yMin + BarHeight;

            mesh.Rect(r.xMin, top, barRight, bottom, MeterColors.Background);

            // 6 dB grid lines, counted from the full-scale (right) end.
            float range = Mathf.Max(1f, _tracker != null ? _tracker.DynamicRange : 12f);
            int lines = Mathf.Min(MaxGridLines, Mathf.FloorToInt(range / 6f));
            for (int i = 0; i < MaxGridLines; i++)
            {
                if (i < lines)
                {
                    float x = barRight - width * (6f * (i + 1) / range);
                    mesh.Rect(x, top, x + 1f, bottom, MeterColors.Grid);
                }
                else mesh.Rect(0f, 0f, 0f, 0f, MeterColors.Grid);   // unused slot
            }

            float level = _drawnLevel / 500f, instant = _drawnInstant / 500f, envelope = _drawnEnvelope / 500f;
            mesh.Rect(r.xMin, top + 3f, r.xMin + width * level, bottom - 3f, MeterColors.Fill);
            float mx = r.xMin + width * instant;
            mesh.Rect(mx - 1f, top, mx + 1f, bottom, MeterColors.Marker);
            mesh.Rect(r.xMin, bottom + 2f, r.xMin + width * envelope, bottom + 2f + EnvelopeHeight, MeterColors.Envelope);

            Color32 flash = MeterColors.Envelope;
            flash.a = (byte)(255 * envelope);
            mesh.Rect(barRight + 4f, top, r.xMax, bottom, envelope > 0f ? flash : MeterColors.Background);
        }
    }
}
