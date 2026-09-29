// PropertyBinder.cs
// Base of the property binders stored in an AudioLevelTracker (LASP-style, via
// [SerializeReference]). A binder reads the tracker's normalized level or onset envelope,
// shapes it with an optional curve and applies it to a target property every frame.
// Concrete binders only map a 0..1 value to their output type.
using System;
using UnityEngine;

namespace AudioReactive
{
    /// <summary>Tracker signal a binder reads.</summary>
    public enum BinderSource
    {
        /// <summary>AudioLevelTracker.NormalizedLevel.</summary>
        Level,
        /// <summary>AudioLevelTracker.Envelope (onset flash with decay).</summary>
        Envelope,
    }

    /// <summary>Base of the property binders stored in an AudioLevelTracker; maps Level or Envelope (0..1) to a target property.</summary>
    [Serializable]
    public abstract class PropertyBinder
    {
        [NonSerialized] private AudioLevelTracker _owner;

        public bool enabled = true;
        public BinderSource source = BinderSource.Level;
        [Tooltip("Response curve (x = input 0..1). Empty = linear.")]
        public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        /// <summary>Called by the tracker once per frame.</summary>
        public void Apply(AudioLevelTracker tracker)
        {
            if (!enabled) return;
            _owner = tracker;
            float v = source == BinderSource.Envelope ? tracker.Envelope : tracker.NormalizedLevel;
            if (curve != null && curve.length > 0) v = curve.Evaluate(v);
            OnApply(v);
        }

        /// <summary>Applies a shaped 0..1 value to the target.</summary>
        protected abstract void OnApply(float value);

        // Test hook: applies a value without a tracker.
        internal void ApplyValue(float value) => OnApply(value);

        /// <summary>Tracker that applied this binder last (context for warnings).</summary>
        protected AudioLevelTracker Owner => _owner;

        protected string OwnerName => _owner != null ? _owner.name : "(no tracker)";
    }
}
