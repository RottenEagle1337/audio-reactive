// DeviceSelectionDrawer.cs
// UI Toolkit drawer for DeviceSelection: loopback toggle (disabled with a hint where the
// backend has no loopback), a device dropdown from AudioSystem's cached list with a refresh
// button, and the channel. Trackers pick up the change on their next Update.
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace AudioReactive.EditorTools
{
    [CustomPropertyDrawer(typeof(DeviceSelection))]
    public sealed class DeviceSelectionDrawer : PropertyDrawer
    {
        private const string LoopbackHint =
            "Loopback capture is Windows-only. To analyze system output here, select an input device " +
            "that carries it: a PulseAudio / PipeWire \"Monitor of ...\" source on Linux, or a virtual " +
            "loopback device such as BlackHole on macOS.";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty loopback = property.FindPropertyRelative(nameof(DeviceSelection.loopback));
            SerializedProperty deviceId = property.FindPropertyRelative(nameof(DeviceSelection.deviceId));
            SerializedProperty channel = property.FindPropertyRelative(nameof(DeviceSelection.channel));

            var root = new VisualElement();

            var loopbackField = new PropertyField(loopback, "Loopback");
            root.Add(loopbackField);
            if (!AudioSystem.IsLoopbackSupported)
            {
                loopbackField.SetEnabled(false);
                root.Add(new HelpBox(LoopbackHint, HelpBoxMessageType.Info));
            }

            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var dropdown = new DropdownField("Device") { style = { flexGrow = 1f } };
            dropdown.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            var refresh = new Button { text = "Refresh", tooltip = "Re-enumerate audio devices" };
            row.Add(dropdown);
            row.Add(refresh);
            root.Add(row);

            root.Add(new PropertyField(channel));

            var ids = new List<string>();

            void Rebuild()
            {
                bool lb = loopback.boolValue && AudioSystem.IsLoopbackSupported;
                IReadOnlyList<AudioDeviceInfo> devices = AudioSystem.GetDevices(lb);
                var names = new List<string> { "Default" };
                ids.Clear();
                ids.Add("");
                foreach (AudioDeviceInfo d in devices) { names.Add(d.Name); ids.Add(d.Id); }

                string current = deviceId.stringValue ?? "";
                int index = ids.IndexOf(current);
                if (index < 0) { names.Add($"{current} (not found)"); ids.Add(current); index = ids.Count - 1; }
                dropdown.choices = names;
                dropdown.SetValueWithoutNotify(names[index]);
            }

            dropdown.RegisterValueChangedCallback(_ =>
            {
                int i = dropdown.index;
                if (i < 0 || i >= ids.Count || ids[i] == deviceId.stringValue) return;
                deviceId.stringValue = ids[i];
                deviceId.serializedObject.ApplyModifiedProperties();
            });
            refresh.clicked += () => { AudioSystem.RefreshDevices(); Rebuild(); };

            root.TrackPropertyValue(loopback, _ =>
            {
                // Ids differ between playback and input devices.
                deviceId.stringValue = "";
                deviceId.serializedObject.ApplyModifiedProperties();
                Rebuild();
            });
            root.TrackPropertyValue(deviceId, _ => Rebuild());

            Rebuild();
            return root;
        }
    }
}
