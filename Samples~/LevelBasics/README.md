# Level Basics

The smallest setup: one **Audio Level Tracker** drives the scale of a sphere through a
**Vector3 Property Binder**.

## Run

1. Play music on the default output device (Windows loopback) or pick another device in the tracker.
2. Enter Play Mode and select **Audio Level Tracker** to watch the live meter in the Inspector.

## Try

- **Auto Gain** off + **Gain**: fixed sensitivity instead of following the recent peak.
- **Dynamic Range**: how many dB below the peak map to 0 (smaller = punchier).
- **Smooth Fall / Fall Speed**: how fast the level drops after a hit.
- **Channel**: Mono, Left or Right.
- In the binder: **Source** Level / Envelope, the **Curve**, **Value 0 / Value 1**.

Requires URP (the sphere material uses URP/Lit).
