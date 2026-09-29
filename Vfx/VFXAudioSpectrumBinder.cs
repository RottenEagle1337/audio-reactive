// VFXAudioSpectrumBinder.cs
// VFX Property Binder: SpectrumAnalyzer log bands -> exposed Texture2D + uint band count.
// Compiled only when com.unity.visualeffectgraph is installed (AudioReactive.Vfx assembly).
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace AudioReactive.Vfx
{
    /// <summary>VFX Property Binder: SpectrumAnalyzer texture and band count to exposed Texture2D / uint.</summary>
    [AddComponentMenu("VFX/Property Binders/Audio Reactive/Audio Spectrum Binder")]
    [VFXBinder("Audio Reactive/Audio Spectrum")]
    public sealed class VFXAudioSpectrumBinder : VFXBinderBase
    {
        [VFXPropertyBinding("UnityEngine.Texture2D"), SerializeField]
        private ExposedProperty _textureProperty = "SpectrumTexture";
        [VFXPropertyBinding("System.UInt32"), SerializeField]
        private ExposedProperty _sizeProperty = "SpectrumSize";

        public SpectrumAnalyzer target;

        public string TextureProperty { get => (string)_textureProperty; set => _textureProperty = value; }
        public string SizeProperty { get => (string)_sizeProperty; set => _sizeProperty = value; }

        public override bool IsValid(VisualEffect component) =>
            target != null && component.HasTexture(_textureProperty) && component.HasUInt(_sizeProperty);

        public override void UpdateBinding(VisualEffect component)
        {
            Texture2D texture = target.Texture;
            if (texture == null) return;
            component.SetTexture(_textureProperty, texture);
            component.SetUInt(_sizeProperty, (uint)texture.width);
        }

        public override string ToString() => $"Audio Spectrum : '{_textureProperty}' -> {(target != null ? target.name : "(null)")}";
    }
}
