// MeterScheduler.cs
// One shared timer for all inspector meters. Hooks EditorApplication.update only while at
// least one meter is attached to a panel, ticks at 30 Hz and only in Play Mode, and tells
// meters when their text is due (10 Hz). Meters decide themselves whether to repaint.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace AudioReactive.EditorTools
{
    internal interface IMeter
    {
        // Called at 30 Hz in Play Mode while the meter is attached; textDue at 10 Hz.
        void Tick(bool textDue);
    }

    internal static class MeterScheduler
    {
        private const double Interval = 1.0 / 30.0;
        private const double TextInterval = 0.1;

        private static readonly List<IMeter> s_meters = new List<IMeter>();
        private static double s_nextTick, s_nextText;

        public static void Register(IMeter meter)
        {
            if (s_meters.Contains(meter)) return;
            if (s_meters.Count == 0) EditorApplication.update += Update;
            s_meters.Add(meter);
        }

        public static void Unregister(IMeter meter)
        {
            if (!s_meters.Remove(meter)) return;
            if (s_meters.Count == 0) EditorApplication.update -= Update;
        }

        // True if the element and all its ancestors are displayed (e.g. the component is not folded).
        public static bool IsShown(VisualElement element)
        {
            if (element.panel == null) return false;
            for (VisualElement e = element; e != null; e = e.hierarchy.parent)
                if (e.resolvedStyle.display == DisplayStyle.None || !e.visible) return false;
            return element.contentRect.width > 0f;
        }

        private static void Update()
        {
            if (!EditorApplication.isPlaying) return;
            double now = EditorApplication.timeSinceStartup;
            if (now < s_nextTick) return;
            s_nextTick = now + Interval;

            bool textDue = now >= s_nextText;
            if (textDue) s_nextText = now + TextInterval;

            for (int i = s_meters.Count - 1; i >= 0; i--) s_meters[i].Tick(textDue);
        }
    }
}
