// MaterialPropertyBinder.cs
// Binders that drive a shader property of a Renderer through a MaterialPropertyBlock
// (no material instances are created). The shader property id is cached and refreshed
// only when the property name changes.
using System;
using UnityEngine;

namespace AudioReactive
{
    /// <summary>Binder driving a shader property of a Renderer through a MaterialPropertyBlock.</summary>
    [Serializable]
    public abstract class MaterialPropertyBinder<T> : PropertyBinder
    {
        public Renderer renderer;
        public string propertyName;

        [NonSerialized] private MaterialPropertyBlock _block;
        [NonSerialized] private string _boundProperty;
        [NonSerialized] private Renderer _boundRenderer;
        [NonSerialized] private int _propertyId;

        public static Type ValueType => typeof(T);

        protected override void OnApply(float value)
        {
            if (renderer == null || string.IsNullOrEmpty(propertyName)) return;
            if (!ReferenceEquals(propertyName, _boundProperty) || !ReferenceEquals(renderer, _boundRenderer))
            {
                _boundProperty = propertyName;
                _boundRenderer = renderer;
                _propertyId = Shader.PropertyToID(propertyName);
                Material material = renderer.sharedMaterial;
                if (material != null && !material.HasProperty(_propertyId))
                    Debug.LogWarning($"[AudioReactive] {GetType().Name} on '{OwnerName}': shader '{material.shader.name}' " +
                                     $"of '{renderer.name}' has no property '{propertyName}'.", Owner);
            }
            _block ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_block);
            Write(_block, _propertyId, value);
            renderer.SetPropertyBlock(_block);
        }

        protected abstract void Write(MaterialPropertyBlock block, int id, float value);
    }

    /// <summary>Drives a shader float property: value0 -&gt; value1.</summary>
    [Serializable]
    public sealed class MaterialFloatBinder : MaterialPropertyBinder<float>
    {
        public float value0 = 0f;
        public float value1 = 1f;
        protected override void Write(MaterialPropertyBlock block, int id, float v) =>
            block.SetFloat(id, Mathf.LerpUnclamped(value0, value1, v));
    }

    /// <summary>Drives a shader color property: value0 -&gt; value1.</summary>
    [Serializable]
    public sealed class MaterialColorBinder : MaterialPropertyBinder<Color>
    {
        [ColorUsage(true, true)] public Color value0 = Color.black;
        [ColorUsage(true, true)] public Color value1 = Color.white;
        protected override void Write(MaterialPropertyBlock block, int id, float v) =>
            block.SetColor(id, Color.LerpUnclamped(value0, value1, v));
    }
}
