// BinderTests.cs
// EditMode tests of the property binders: component properties through cached delegates,
// material properties through a MaterialPropertyBlock, and warnings instead of exceptions
// for invalid bindings.
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AudioReactive.Tests
{
    public sealed class BinderTests
    {
        private GameObject _go;

        [SetUp]
        public void SetUp() => _go = new GameObject("BinderTest");

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        [Test]
        public void FloatBinder_SetsLightIntensity()
        {
            var light = _go.AddComponent<Light>();
            var binder = new FloatPropertyBinder { target = light, propertyName = "intensity", value0 = 0f, value1 = 4f };
            binder.ApplyValue(0.5f);
            Assert.AreEqual(2f, light.intensity, 1e-5f);
        }

        [Test]
        public void Vector3Binder_SetsLocalScale()
        {
            var binder = new Vector3PropertyBinder
            {
                target = _go.transform, propertyName = "localScale", value0 = Vector3.one, value1 = Vector3.one * 3f,
            };
            binder.ApplyValue(1f);
            Assert.AreEqual(Vector3.one * 3f, _go.transform.localScale);
        }

        [Test]
        public void EulerRotationBinder_SetsLocalRotation()
        {
            var binder = new EulerRotationPropertyBinder
            {
                target = _go.transform, value0 = Vector3.zero, value1 = new Vector3(0f, 90f, 0f),
            };
            binder.ApplyValue(1f);
            Assert.AreEqual(90f, _go.transform.localEulerAngles.y, 1e-3f);
        }

        [Test]
        public void ColorBinder_SetsLightColor()
        {
            var light = _go.AddComponent<Light>();
            var binder = new ColorPropertyBinder { target = light, propertyName = "color", value0 = Color.black, value1 = Color.red };
            binder.ApplyValue(1f);
            Assert.AreEqual(Color.red, light.color);
        }

        [Test]
        public void InvalidProperty_WarnsOnceWithoutThrowing()
        {
            var light = _go.AddComponent<Light>();
            var binder = new FloatPropertyBinder { target = light, propertyName = "noSuchProperty" };
            LogAssert.Expect(LogType.Warning, new Regex("no public writable Single property 'noSuchProperty'"));
            binder.ApplyValue(0.5f);
            binder.ApplyValue(0.7f);   // no second warning: binding is cached until target / name change
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void MissingTarget_IsIgnored()
        {
            var binder = new FloatPropertyBinder { propertyName = "intensity" };
            Assert.DoesNotThrow(() => binder.ApplyValue(1f));
        }

        [Test]
        public void MaterialColorBinder_WritesPropertyBlock()
        {
            var renderer = _go.AddComponent<MeshRenderer>();
            var material = new Material(Shader.Find("Unlit/Color"));
            renderer.sharedMaterial = material;
            var binder = new MaterialColorBinder { renderer = renderer, propertyName = "_Color", value0 = Color.black, value1 = Color.green };

            binder.ApplyValue(1f);

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            Assert.AreEqual(Color.green, block.GetColor("_Color"));
            Object.DestroyImmediate(material);
        }
    }
}
