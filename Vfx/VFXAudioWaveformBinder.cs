// VFXAudioWaveformBinder.cs
// VFX Property Binder: raw samples of an AudioLevelTracker channel -> exposed Texture2D + uint sample count.
// Compiled only when com.unity.visualeffectgraph is installed (AudioReactive.Vfx assembly).
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace AudioReactive.Vfx
{
    /// <summary>VFX Property Binder: latest raw samples of an AudioLevelTracker channel to exposed Texture2D / uint.</summary>
    [AddComponentMenu("VFX/Property Binders/Audio Reactive/Audio Waveform Binder")]
    [VFXBinder("Audio Reactive/Audio Waveform")]
    public sealed class VFXAudioWaveformBinder : VFXBinderBase
    {
        [VFXPropertyBinding("UnityEngine.Texture2D"), SerializeField]
        private ExposedProperty _textureProperty = "WaveformTexture";
        [VFXPropertyBinding("System.UInt32"), SerializeField]
        private ExposedProperty _sizeProperty = "WaveformSize";

        public AudioLevelTracker target;
        [Range(64, 4096)] public int sampleCount = 512;

        private Texture2D _texture;
        private float[] _samples;

        public string TextureProperty { get => (string)_textureProperty; set => _textureProperty = value; }
        public string SizeProperty { get => (string)_sizeProperty; set => _sizeProperty = value; }

        public override bool IsValid(VisualEffect component) =>
            target != null && component.HasTexture(_textureProperty) && component.HasUInt(_sizeProperty);

        public override void UpdateBinding(VisualEffect component)
        {
            if (_texture == null || _texture.width != sampleCount)
            {
                if (_texture != null) DestroyTexture(_texture);
                _texture = new Texture2D(sampleCount, 1, TextureFormat.RFloat, false, true)
                {
                    name = "Audio Reactive Waveform",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.DontSave,
                };
                _samples = new float[sampleCount];
            }

            target.CopyWaveform(_samples);
            _texture.SetPixelData(_samples, 0);
            _texture.Apply(false);
            component.SetTexture(_textureProperty, _texture);
            component.SetUInt(_sizeProperty, (uint)sampleCount);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_texture != null) DestroyTexture(_texture);
            _texture = null;
        }

        // VFX Property Binder also runs in edit mode, where Destroy is not allowed.
        private static void DestroyTexture(Texture2D texture)
        {
            if (Application.isPlaying) Destroy(texture);
            else DestroyImmediate(texture);
        }

        public override string ToString() => $"Audio Waveform : '{_textureProperty}' -> {(target != null ? target.name : "(null)")}";
    }
}
