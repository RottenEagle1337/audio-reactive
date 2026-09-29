// AudioLevelTrackerEditor.cs
// UI Toolkit inspector for AudioLevelTracker: device, filter (cutoff fields shown only when
// used), gain, fall, onset settings, the live level meter and the binder list with an
// "Add Binder" menu of every concrete PropertyBinder type. Fields are regular PropertyFields
// built once; only the meter element repaints during Play Mode.
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AudioReactive.EditorTools
{
    [CustomEditor(typeof(AudioLevelTracker))]
    public sealed class AudioLevelTrackerEditor : Editor
    {
        private SerializedProperty _binders;
        private VisualElement _binderList;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.Add(Field("_device"));

            root.Add(Header("Filter"));
            SerializedProperty filter = serializedObject.FindProperty("_filterType");
            root.Add(new PropertyField(filter, "Type"));
            PropertyField low = Field("_lowCutoff", "Low Cutoff (Hz)");
            PropertyField high = Field("_highCutoff", "High Cutoff (Hz)");
            root.Add(low);
            root.Add(high);
            void UpdateCutoffs()
            {
                var t = (FilterType)filter.enumValueIndex;
                low.style.display = t == FilterType.HighPass || t == FilterType.BandPass ? DisplayStyle.Flex : DisplayStyle.None;
                high.style.display = t == FilterType.LowPass || t == FilterType.BandPass ? DisplayStyle.Flex : DisplayStyle.None;
            }
            root.TrackPropertyValue(filter, _ => UpdateCutoffs());
            UpdateCutoffs();

            root.Add(Header("Level"));
            SerializedProperty autoGain = serializedObject.FindProperty("_autoGain");
            root.Add(new PropertyField(autoGain));
            PropertyField gain = Field("_gain", "Gain (dB)");
            root.Add(gain);
            root.TrackPropertyValue(autoGain, p => gain.SetEnabled(!p.boolValue));
            gain.SetEnabled(!autoGain.boolValue);
            root.Add(Field("_dynamicRange", "Dynamic Range (dB)"));
            root.Add(Field("_smoothFall"));
            root.Add(Field("_fallSpeed"));

            root.Add(Header("Onset"));
            root.Add(Field("_onsetThreshold", "Threshold"));
            root.Add(Field("_onsetRefractory", "Refractory (s)"));
            root.Add(Field("_envelopeDecay", "Envelope Decay (s)"));
            root.Add(Field("_onOnset", "On Onset"));

            root.Add(Header("Monitor"));
            root.Add(new LevelMeterElement((AudioLevelTracker)target));

            root.Add(Header("Binders"));
            _binders = serializedObject.FindProperty("_binders");
            _binderList = new VisualElement();
            root.Add(_binderList);
            var add = new Button(ShowAddMenu) { text = "Add Binder" };
            root.Add(add);
            root.TrackPropertyValue(_binders, _ => RebuildBinders());
            RebuildBinders();

            return root;
        }

        private PropertyField Field(string name, string label = null) =>
            label == null ? new PropertyField(serializedObject.FindProperty(name))
                          : new PropertyField(serializedObject.FindProperty(name), label);

        private static Label Header(string text) =>
            new Label(text) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 8f, marginBottom = 2f } };

        private int _builtCount = -1;

        private void RebuildBinders()
        {
            serializedObject.Update();
            if (_binders.arraySize == _builtCount) return;   // element edits are handled by their own fields
            _builtCount = _binders.arraySize;
            _binderList.Clear();

            for (int i = 0; i < _binders.arraySize; i++)
            {
                int index = i;
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.FlexStart } };
                var field = new PropertyField(_binders.GetArrayElementAtIndex(i)) { style = { flexGrow = 1f } };
                field.Bind(serializedObject);
                var remove = new Button(() => RemoveBinder(index)) { text = "-", tooltip = "Remove binder" };
                row.Add(field);
                row.Add(remove);
                _binderList.Add(row);
            }
        }

        private void ShowAddMenu()
        {
            var menu = new GenericMenu();
            foreach (Type t in TypeCache.GetTypesDerivedFrom<PropertyBinder>())
            {
                if (t.IsAbstract || t.IsGenericType) continue;
                Type type = t;
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(t.Name)), false, () => AddBinder(type));
            }
            menu.ShowAsContext();
        }

        private void AddBinder(Type type)
        {
            serializedObject.Update();
            _binders.arraySize++;
            _binders.GetArrayElementAtIndex(_binders.arraySize - 1).managedReferenceValue = Activator.CreateInstance(type);
            serializedObject.ApplyModifiedProperties();
            _builtCount = -1;
            RebuildBinders();
        }

        private void RemoveBinder(int index)
        {
            serializedObject.Update();
            _binders.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
            _builtCount = -1;
            RebuildBinders();
        }
    }
}
