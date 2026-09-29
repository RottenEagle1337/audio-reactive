// VFXAudioLevelBinder.cs
// VFX Property Binder: AudioLevelTracker level or envelope -> exposed float.
// Compiled only when com.unity.visualeffectgraph is installed (AudioReactive.Vfx assembly).
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace AudioReactive.Vfx
{
    /// <summary>VFX Property Binder: AudioLevelTracker level or envelope to an exposed float.</summary>
    [AddComponentMenu("VFX/Property Binders/Audio Reactive/Audio Level Binder")]
    [VFXBinder("Audio Reactive/Audio Level")]
    public sealed class VFXAudioLevelBinder : VFXBinderBase
    {
        [VFXPropertyBinding("System.Single"), SerializeField]
        private ExposedProperty _property = "AudioLevel";

        public AudioLevelTracker target;
        public BinderSource source = BinderSource.Level;

        public string Property { get => (string)_property; set => _property = value; }

        public override bool IsValid(VisualEffect component) => target != null && component.HasFloat(_property);

        public override void UpdateBinding(VisualEffect component) =>
            component.SetFloat(_property, source == BinderSource.Envelope ? target.Envelope : target.NormalizedLevel);

        public override string ToString() => $"Audio Level : '{_property}' -> {(target != null ? target.name : "(null)")}";
    }
}
