# Property Binders

One **Audio Level Tracker** with every binder type. Object names tell which binder drives them.

| Binder | Target | Source |
|---|---|---|
| Float | Point Light `intensity` | Level |
| Color | Point Light `color` | Envelope |
| Vector3 | Cube `localScale` | Level |
| Euler Rotation | Cube `localRotation` | Level |
| Material Float | Sphere `_Smoothness` (MaterialPropertyBlock) | Level |
| Material Color | Sphere `_EmissionColor` (MaterialPropertyBlock) | Envelope |

Component binders work with any public writable property of the value type on any component
(pick the component and property from the dropdowns). Material binders list the shader properties
of the renderer's material. Add binders with **Add Binder** at the bottom of the tracker inspector.

Requires URP (materials use URP/Lit; the sphere material has emission enabled).
