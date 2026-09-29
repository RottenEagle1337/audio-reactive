// PropertyBinderDrawer.cs
// UI Toolkit drawer for PropertyBinder subclasses (managed references in a tracker's binder
// list). Draws a foldout titled with the binder type and its fields; for component binders
// the target gets a component picker (all components on the target's GameObject) and the
// property name becomes a dropdown of writable public properties of the binder's value type;
// for material binders the property name lists matching shader properties.
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace AudioReactive.EditorTools
{
    [CustomPropertyDrawer(typeof(PropertyBinder), true)]
    public sealed class PropertyBinderDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            object binder = property.managedReferenceValue;
            Type type = binder?.GetType();
            var foldout = new Foldout
            {
                text = type != null ? ObjectNames.NicifyVariableName(type.Name) : "(missing binder)",
                value = true,
            };
            if (type == null) return foldout;

            Type componentValueType = GetGenericArgument(type, typeof(ComponentPropertyBinder<>));
            Type materialValueType = GetGenericArgument(type, typeof(MaterialPropertyBinder<>));

            SerializedProperty it = property.Copy();
            SerializedProperty end = it.GetEndProperty();
            for (bool enter = true; it.NextVisible(enter) && !SerializedProperty.EqualContents(it, end); enter = false)
            {
                SerializedProperty p = it.Copy();
                if (p.name == "propertyName" && componentValueType != null)
                    foldout.Add(ComponentPropertyDropdown(property, p, componentValueType));
                else if (p.name == "propertyName" && materialValueType != null)
                    foldout.Add(ShaderPropertyDropdown(property, p, materialValueType));
                else if (p.name == "target" && componentValueType != null)
                    foldout.Add(TargetField(p));
                else
                    foldout.Add(new PropertyField(p));
            }
            return foldout;
        }

        private static Type GetGenericArgument(Type type, Type genericDefinition)
        {
            for (Type t = type; t != null; t = t.BaseType)
                if (t.IsGenericType && t.GetGenericTypeDefinition() == genericDefinition)
                    return t.GetGenericArguments()[0];
            return null;
        }

        // Target object field + dropdown to switch between components of the same GameObject.
        private static VisualElement TargetField(SerializedProperty target)
        {
            var root = new VisualElement();
            var field = new ObjectField("Target") { objectType = typeof(Component), allowSceneObjects = true };
            field.AddToClassList(BaseField<UnityEngine.Object>.alignedFieldUssClassName);
            field.BindProperty(target);
            var picker = new DropdownField("Component");
            picker.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            root.Add(field);
            root.Add(picker);

            var components = new List<Component>();
            void Rebuild()
            {
                var current = target.objectReferenceValue as Component;
                picker.style.display = current != null ? DisplayStyle.Flex : DisplayStyle.None;
                if (current == null) return;
                current.GetComponents(components);
                var names = new List<string>();
                foreach (Component c in components) names.Add(c != null ? c.GetType().Name : "(missing)");
                picker.choices = names;
                picker.SetValueWithoutNotify(names[Mathf.Max(0, components.IndexOf(current))]);
            }

            picker.RegisterValueChangedCallback(_ =>
            {
                int i = picker.index;
                if (i < 0 || i >= components.Count || components[i] == target.objectReferenceValue) return;
                target.objectReferenceValue = components[i];
                target.serializedObject.ApplyModifiedProperties();
            });
            root.TrackPropertyValue(target, _ => Rebuild());
            Rebuild();
            return root;
        }

        private static VisualElement ComponentPropertyDropdown(SerializedProperty binder, SerializedProperty name, Type valueType)
        {
            SerializedProperty target = binder.FindPropertyRelative("target");
            return NameDropdown(name, target, () =>
            {
                var names = new List<string>();
                if (target.objectReferenceValue is Component c)
                    foreach (PropertyInfo p in c.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        if (p.PropertyType == valueType && p.CanWrite && p.GetSetMethod() != null &&
                            p.GetIndexParameters().Length == 0 && p.GetCustomAttribute<ObsoleteAttribute>() == null)
                            names.Add(p.Name);
                names.Sort(StringComparer.Ordinal);
                return names;
            });
        }

        private static VisualElement ShaderPropertyDropdown(SerializedProperty binder, SerializedProperty name, Type valueType)
        {
            SerializedProperty renderer = binder.FindPropertyRelative("renderer");
            return NameDropdown(name, renderer, () =>
            {
                var names = new List<string>();
                if (renderer.objectReferenceValue is Renderer r && r.sharedMaterial != null)
                {
                    Shader shader = r.sharedMaterial.shader;
                    for (int i = 0; i < shader.GetPropertyCount(); i++)
                    {
                        ShaderPropertyType t = shader.GetPropertyType(i);
                        bool match = valueType == typeof(Color)
                            ? t == ShaderPropertyType.Color
                            : t == ShaderPropertyType.Float || t == ShaderPropertyType.Range;
                        if (match) names.Add(shader.GetPropertyName(i));
                    }
                }
                return names;
            });
        }

        // Dropdown bound to a string property; choices rebuilt when `source` changes.
        private static VisualElement NameDropdown(SerializedProperty name, SerializedProperty source, Func<List<string>> choices)
        {
            var dropdown = new DropdownField("Property");
            dropdown.AddToClassList(BaseField<string>.alignedFieldUssClassName);

            void Rebuild()
            {
                List<string> names = choices();
                string current = name.stringValue;
                if (!string.IsNullOrEmpty(current) && !names.Contains(current)) names.Insert(0, current);
                dropdown.choices = names;
                dropdown.SetValueWithoutNotify(string.IsNullOrEmpty(current) ? "(none)" : current);
            }

            dropdown.RegisterValueChangedCallback(e =>
            {
                if (e.newValue == name.stringValue) return;
                name.stringValue = e.newValue;
                name.serializedObject.ApplyModifiedProperties();
            });
            dropdown.TrackPropertyValue(source, _ => Rebuild());
            dropdown.TrackPropertyValue(name, _ => Rebuild());
            Rebuild();
            return dropdown;
        }
    }
}
