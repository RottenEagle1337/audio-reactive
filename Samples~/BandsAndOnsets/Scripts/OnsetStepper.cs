// OnsetStepper.cs
// Sample script: wire an AudioLevelTracker's "On Onset" event to Step() and the object turns by a
// fixed angle on every detected hit, easing towards the new orientation. Allocation-free.
using UnityEngine;

namespace AudioReactive.Samples
{
    public sealed class OnsetStepper : MonoBehaviour
    {
        public Vector3 axis = Vector3.up;
        public float stepAngle = 90f;
        [Tooltip("Easing time towards the target orientation, s")]
        public float smoothTime = 0.08f;

        private Quaternion _target;

        private void Awake() => _target = transform.localRotation;

        // Hook this to AudioLevelTracker.On Onset.
        public void Step() => _target = Quaternion.AngleAxis(stepAngle, axis) * _target;

        private void Update()
        {
            float k = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(smoothTime, 1e-3f));
            transform.localRotation = Quaternion.Slerp(transform.localRotation, _target, k);
        }
    }
}
