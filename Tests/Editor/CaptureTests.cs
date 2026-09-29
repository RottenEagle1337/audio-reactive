// CaptureTests.cs
// EditMode tests of the capture layer: device selection equality, plugin availability on the
// platforms that ship a native plugin, and shared stream reference counting.
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AudioReactive.Tests
{
    public sealed class CaptureTests
    {
        [Test]
        public void DeviceSelection_SameDeviceIgnoresChannel()
        {
            var a = new DeviceSelection { loopback = true, deviceId = "", channel = AudioChannel.Left };
            var b = new DeviceSelection { loopback = true, deviceId = null, channel = AudioChannel.Right };
            var c = new DeviceSelection { loopback = false, deviceId = "", channel = AudioChannel.Left };
            Assert.IsTrue(a.SameDevice(b));
            Assert.IsFalse(a.SameDevice(c));
        }

        [Test]
        [UnityPlatform(RuntimePlatform.WindowsEditor)]
        public void NativePlugin_IsAvailableOnWindows()
        {
            Assert.IsTrue(AudioSystem.IsAvailable);
            Assert.AreEqual("WASAPI", AudioSystem.BackendName);
            Assert.IsTrue(AudioSystem.IsLoopbackSupported);
        }

        [Test]
        public void AudioSystem_SharesOneStreamPerDevice()
        {
            var left = new DeviceSelection { loopback = false, deviceId = "", channel = AudioChannel.Left };
            var right = new DeviceSelection { loopback = false, deviceId = "", channel = AudioChannel.Right };

            InputStream a = AudioSystem.Acquire(left);
            InputStream b = AudioSystem.Acquire(right);
            try
            {
                Assert.AreSame(a, b);
                Assert.AreEqual(2, a.RefCount);
            }
            finally
            {
                AudioSystem.Release(b);
                Assert.AreEqual(1, a.RefCount);
                AudioSystem.Release(a);
            }
            Assert.IsFalse(a.IsOpen);
        }
    }
}
