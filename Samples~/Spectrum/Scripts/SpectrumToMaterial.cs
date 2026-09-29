// SpectrumToMaterial.cs
// Sample script: feeds SpectrumAnalyzer.Texture (RFloat, one texel per log band) into a
// renderer's material through a MaterialPropertyBlock, together with the band count.
// Pair with the "AudioReactive/Samples/SpectrumBars" shader or any shader sampling the texture.
using UnityEngine;

namespace AudioReactive.Samples
{
    public sealed class SpectrumToMaterial : MonoBehaviour
    {
        public SpectrumAnalyzer analyzer;
        public Renderer target;
        public string textureProperty = "_SpectrumTex";
        public string bandCountProperty = "_Bars";

        private MaterialPropertyBlock _block;
        private int _textureId, _countId;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _textureId = Shader.PropertyToID(textureProperty);
            _countId = Shader.PropertyToID(bandCountProperty);
        }

        private void LateUpdate()
        {
            if (analyzer == null || target == null || analyzer.Texture == null) return;
            target.GetPropertyBlock(_block);
            _block.SetTexture(_textureId, analyzer.Texture);
            _block.SetFloat(_countId, analyzer.Texture.width);
            target.SetPropertyBlock(_block);
        }
    }
}
