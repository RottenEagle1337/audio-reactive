# Spectrum

A **Spectrum Analyzer** (64 log bands) consumed two ways:

- **Octave Bars** — `OctaveBars.cs` reads `SpectrumAnalyzer.OctaveLevels` (10 ISO octave bands, 0..1)
  from code and scales ten cubes. Use this pattern to read analyzer results in your own scripts.
- **Spectrum Quad** — `SpectrumToMaterial.cs` feeds `SpectrumAnalyzer.Texture` (RFloat, one texel per
  band) and the band count into the material; the `AudioReactive/Samples/SpectrumBars` URP shader draws
  the bars on the GPU. Use this pattern for shaders and Shader Graph.

## Try

- **Log Bands**, **Min Frequency**, **Resolution** (FFT size: higher = finer low end, more latency).
- **Auto Gain** / **Dynamic Range**, **Attack** / **Release** smoothing.
- Select **Spectrum Analyzer** in Play Mode to see the live graph in the Inspector.

Requires URP (the bar shader and materials are URP).
