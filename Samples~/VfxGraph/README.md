# VFX Graph

Requires the **Visual Effect Graph** package (`com.unity.visualeffectgraph`) and URP.

Three small effects, each with a standard **VFX Property Binder** component and one Audio Reactive binder:

| Effect | Binder | Exposed properties | What the graph does |
|---|---|---|---|
| `AudioLevel.vfx` | Audio Level | `AudioLevel` (float) | spawn rate = AudioLevel x 400 |
| `AudioSpectrum.vfx` | Audio Spectrum | `SpectrumTexture` (Texture2D), `SpectrumSize` (uint) | one particle per band on a line (Position Sequential); *Set Scale.Y from Map* (Sequential) reads the band |
| `AudioWaveform.vfx` | Audio Waveform | `WaveformTexture` (Texture2D), `WaveformSize` (uint) | one particle per sample; *Set Position from Map* puts the sample on local X, *Position Sequential* spreads particles along local Y; the object is rotated 90° around Z |

The level and waveform binders read the **Audio Level Tracker**, the spectrum binder reads the
**Spectrum Analyzer**. Open the graphs to see how the exposed properties are wired; binders only need the
exposed property names.
