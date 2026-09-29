// SpectrumAnalyzerEditor.cs
// UI Toolkit inspector for SpectrumAnalyzer: settings (FFT resolution as a power-of-two
// popup, gain field enabled only without auto gain) and the live spectrum graph.
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AudioReactive.EditorTools
{
    [CustomEditor(typeof(SpectrumAnalyzer))]
    public sealed class SpectrumAnalyzerEditor : Editor
    {
        private static readonly List<int> Resolutions = new List<int> { 256, 512, 1024, 2048, 4096, 8192 };

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.Add(new PropertyField(serializedObject.FindProperty("_device")));

            root.Add(Header("Analysis"));
            SerializedProperty resolution = serializedObject.FindProperty("_resolution");
            var resolutionField = new PopupField<int>("Resolution (FFT)", Resolutions,
                Mathf.Max(0, Resolutions.IndexOf(SpectrumFrame.ClampSize(resolution.intValue))));
            resolutionField.AddToClassList(BaseField<int>.alignedFieldUssClassName);
            resolutionField.BindProperty(resolution);
            root.Add(resolutionField);
            root.Add(new PropertyField(serializedObject.FindProperty("_logBandCount"), "Log Bands"));
            root.Add(new PropertyField(serializedObject.FindProperty("_minFrequency"), "Min Frequency (Hz)"));

            root.Add(Header("Level"));
            SerializedProperty autoGain = serializedObject.FindProperty("_autoGain");
            root.Add(new PropertyField(autoGain));
            var gain = new PropertyField(serializedObject.FindProperty("_gain"), "Gain (dB)");
            root.Add(gain);
            root.TrackPropertyValue(autoGain, p => gain.SetEnabled(!p.boolValue));
            gain.SetEnabled(!autoGain.boolValue);
            root.Add(new PropertyField(serializedObject.FindProperty("_dynamicRange"), "Dynamic Range (dB)"));
            root.Add(new PropertyField(serializedObject.FindProperty("_attack"), "Attack (s)"));
            root.Add(new PropertyField(serializedObject.FindProperty("_release"), "Release (s)"));

            root.Add(Header("Monitor"));
            root.Add(new SpectrumGraphElement((SpectrumAnalyzer)target));
            return root;
        }

        private static Label Header(string text) =>
            new Label(text) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 8f, marginBottom = 2f } };
    }
}
