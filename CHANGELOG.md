# Changelog

All notable changes to this package are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the package uses
[Semantic Versioning](https://semver.org/).

## [1.0.0-preview.1] - 2026-09-29

First public preview.

### Added
- `LICENSE.md` (Unlicense), package metadata (`documentationUrl`, `changelogUrl`, `licensesUrl`), README for
  installation by git URL with a roadmap and links to the native plugin and development repositories.
- Native plugin: audio-reactive-native v1.0.0 (C API 3), Windows x64 binary from its CI release.
- EditMode tests (`Tests/Editor`): FFT, biquad filters, property binders, device selection, stream sharing.
- `AudioSystem.IsAvailable`; XML documentation of the public API.
- Binders log one warning when the target property / shader property cannot be found.

### Changed
- Minimum Unity version lowered to 6000.0 (developed and tested on 6000.6; 6000.0 not verified yet).
- `AudioCaptureContext` is internal; `AudioSystem` is the public entry point of the capture layer.
  `AudioDeviceInfo` moved next to `DeviceSelection`.

### Fixed
- Static state of `AudioSystem` and the plugin check is reset on every play session
  (Enter Play Mode Options with domain reload disabled).

### Known limitations
- Native plugin shipped for Windows x64 only; Linux / macOS binaries are built by the native CI but not
  shipped until verified.
- Verified in the Editor and in a Windows Mono player; IL2CPP players not tested yet.

## [0.2.1] - 2026-09-28

### Added
- Samples (Package Manager, importable separately): Level Basics, Bands & Onsets, Property Binders,
  Spectrum, VFX Graph.

### Fixed
- VFX binders split into one file per class (`VFXAudioLevelBinder`, `VFXAudioSpectrumBinder`,
  `VFXAudioWaveformBinder`); with all three in one file, Unity could not serialize the components.

## [0.2.0] - 2026-09-28

Minimal redesign after LASP.

### Added
- `AudioLevelTracker`: device / channel selection, LowPass / BandPass / HighPass filter with editable cutoffs,
  auto gain, dynamic range, fall smoothing, onset detection (`Envelope`, `IsOnset`, `On Onset`), binder list.
- `SpectrumAnalyzer` component: log bands, ten octave levels, auto gain, RFloat texture.
- `AudioSystem` / `InputStream`: one shared stream per device, lazy per-frame update, per-channel reads.
- Property binders (`[SerializeReference]`): Float, Vector3, Euler Rotation, Color, Material Float, Material Color.
- VFX Graph property binders (`AudioReactive.Vfx` assembly): Audio Level, Audio Spectrum, Audio Waveform.
- UI Toolkit inspectors with live meters (single-mesh drawing, shared 30 Hz timer).
- Cross-platform groundwork: native API v3 (`AC_IsLoopbackSupported`, `AC_GetBackendName`), Core Audio /
  PulseAudio / ALSA backends, CMake build, per-platform plugin folders, graceful fallback without a plugin.

### Removed
- `AudioCaptureSource`, `IAudioSource`, `AudioStream`, `AudioTrackerBase`.
- `ChannelTracker` / `ChannelAnalyzer` / `ChannelConfig` and genre presets, flatness gate, `TempoHistogram`.
- `LevelTracker` / `LevelAnalyzer`, `SpectrumTracker`, peaks / means octave outputs.
- `AudioBinder`, `SpectrumBinder`, `BinderTargetBinding`, `ValueMapping` and all runtime OnGUI monitors.

## [0.1.0] - 2026-09-28

First packaged version (previously `Assets/AudioReactive`).

### Added
- Native API v2: `AC_GetApiVersion`, cursor-based `AC_ReadNew`; native sample rate (no resampler),
  low-latency period, 16384-frame ring, silence watchdog for WASAPI loopback.
- `AudioStream`: per-frame new samples + history; `SpectrumFrame`: one shared FFT per frame.
- `AudioTrackerBase`, `TempoHistogram`, `ValueMapping`, `DebugGui`, `CaptureMonitor`.

### Changed
- Channel analysis filters the continuous sample stream (valid biquad state) with a sample-accurate energy follower.
- `ChannelBinder` + `LevelBinder` merged into `AudioBinder` (`AudioBinderSource` replaces `BinderSource`).
- The capture overlay moved from `AudioCaptureSource` to `CaptureMonitor`.
- Spectrum texture is a `Texture2D` (RFloat) instead of a RenderTexture.
- All comments, tooltips and logs in English.

### Fixed
- `SpectrumTracker`: changing the log band count no longer throws or leaves a stale band mapping.
- `ChannelTracker`: BPM range, floor and tempo channel now apply at runtime.
- Trackers survive source disable / enable and device switches.
- Native bool marshalling (`U1`) and UTF-8 device ids.
- `BinderTargetBindingDrawer` height, `AudioCaptureSourceEditor` undo / saving and device enumeration per repaint.
- Per-frame allocations in overlays and binders.

### Removed
- Legacy `BandAnalyzer` / `InstrumentAnalyzer` and their trackers / monitors, `WaveformDebugger`,
  unused `AudioReactiveEditorUtils`.
