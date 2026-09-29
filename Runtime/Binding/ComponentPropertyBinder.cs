// ComponentPropertyBinder.cs
// Binder that drives a public writable property of type T on any component
// (Light.intensity, Transform.localScale, a custom script's property...).
// The setter is bound once through Delegate.CreateDelegate and re-bound only when the
// target or the property name changes, so applying a value does not allocate.
// Fields are not supported (setting them through reflection would box every frame).
using System;
using System.Reflection;
using UnityEngine;

namespace AudioReactive
{
    /// <summary>Binder driving a public writable property of type T on any component; the setter is bound once as a delegate.</summary>
    [Serializable]
    public abstract class ComponentPropertyBinder<T> : PropertyBinder
    {
        public Component target;
        public string propertyName;

        [NonSerialized] private Action<T> _setter;
        [NonSerialized] private Component _boundTarget;
        [NonSerialized] private string _boundProperty;

        public static Type ValueType => typeof(T);

        /// <summary>True if `property` can be driven by this binder type.</summary>
        public static bool IsBindable(PropertyInfo property) =>
            property.PropertyType == typeof(T) && property.CanWrite &&
            property.GetSetMethod() != null && property.GetIndexParameters().Length == 0;

        protected void SetValue(T value)
        {
            if (!ReferenceEquals(target, _boundTarget) || !ReferenceEquals(propertyName, _boundProperty))
                Bind();
            if (_setter != null && target != null) _setter(value);
        }

        private void Bind()
        {
            _boundTarget = target;
            _boundProperty = propertyName;
            _setter = null;
            if (target == null || string.IsNullOrEmpty(propertyName)) return;

            PropertyInfo p = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (p != null && IsBindable(p))
                _setter = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), target, p.GetSetMethod());
            else
                // Logged once per target / property change. In players this also catches setters
                // removed by managed code stripping (see the package README).
                Debug.LogWarning($"[AudioReactive] {GetType().Name} on '{OwnerName}': {target.GetType().Name} has no " +
                                 $"public writable {typeof(T).Name} property '{propertyName}'.", Owner);
        }
    }

    /// <summary>Drives a float property: value0 -&gt; value1.</summary>
    [Serializable]
    public sealed class FloatPropertyBinder : ComponentPropertyBinder<float>
    {
        public float value0 = 0f;
        public float value1 = 1f;
        protected override void OnApply(float v) => SetValue(Mathf.LerpUnclamped(value0, value1, v));
    }

    /// <summary>Drives a Vector3 property (e.g. Transform.localScale): value0 -&gt; value1.</summary>
    [Serializable]
    public sealed class Vector3PropertyBinder : ComponentPropertyBinder<Vector3>
    {
        public Vector3 value0 = Vector3.one;
        public Vector3 value1 = Vector3.one * 2f;
        protected override void OnApply(float v) => SetValue(Vector3.LerpUnclamped(value0, value1, v));
    }

    /// <summary>Drives a Quaternion property (default Transform.localRotation) from Euler angles value0 -&gt; value1.</summary>
    [Serializable]
    public sealed class EulerRotationPropertyBinder : ComponentPropertyBinder<Quaternion>
    {
        public Vector3 value0 = Vector3.zero;
        public Vector3 value1 = new Vector3(0f, 90f, 0f);
        public EulerRotationPropertyBinder() => propertyName = "localRotation";
        protected override void OnApply(float v) => SetValue(Quaternion.Euler(Vector3.LerpUnclamped(value0, value1, v)));
    }

    /// <summary>Drives a Color property (e.g. Light.color): value0 -&gt; value1.</summary>
    [Serializable]
    public sealed class ColorPropertyBinder : ComponentPropertyBinder<Color>
    {
        [ColorUsage(true, true)] public Color value0 = Color.black;
        [ColorUsage(true, true)] public Color value1 = Color.white;
        protected override void OnApply(float v) => SetValue(Color.LerpUnclamped(value0, value1, v));
    }
}
