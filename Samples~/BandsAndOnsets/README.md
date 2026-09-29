# Bands & Onsets

Three **Audio Level Trackers** split the signal into bands (there are no presets; a band is just a
filter setting):

| Tracker | Filter | Drives |
|---|---|---|
| Low | LowPass 150 Hz | Low Bar height, Onset Cube emission, Onset Cube rotation |
| Mid | BandPass 300–2000 Hz | Mid Bar height |
| High | HighPass 4 kHz | High Bar height |

Onsets of the Low tracker are used two ways:
- **Material Color Binder** with Source = **Envelope**: the cube flashes and fades (Envelope Decay).
- **On Onset** UnityEvent → `OnsetStepper.Step()` (sample script): the cube turns 90° per hit.

## Try

- Change cutoffs to isolate a kick (LowPass ~100 Hz) or hats (HighPass 8 kHz).
- **Onset Threshold**: lower = more onsets; **Refractory**: minimum time between onsets.
- Watch the meters of all three trackers in Play Mode (select `Audio`'s children).

Requires URP (materials use URP/Lit).
