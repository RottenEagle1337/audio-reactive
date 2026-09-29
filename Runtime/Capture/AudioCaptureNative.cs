// AudioCaptureNative.cs
// P/Invoke bindings for the AudioCapture native plugin (miniaudio): AudioCapture.dll on
// Windows, libAudioCapture.so on Linux, AudioCapture.bundle on macOS. Internal layer:
// the public API lives in AudioCaptureContext / AudioSystem.
//
// ABI notes: native bool is 1 byte (UnmanagedType.U1); device ids are UTF-8.
// Only the entry points used by the package are imported (the library exports more).
using System;
using System.Runtime.InteropServices;

namespace AudioReactive
{
    internal static class AudioCaptureNative
    {
        private const string Library = "AudioCapture";

        /// <summary>ABI version this code was written against; must match AC_GetApiVersion().</summary>
        public const int ApiVersion = 3;

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AC_GetApiVersion();

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AC_CreateContext();

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void AC_DestroyContext(IntPtr context);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool AC_IsLoopbackSupported(IntPtr context);

        /// <summary>Static, null-terminated ASCII string owned by the library.</summary>
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AC_GetBackendName(IntPtr context);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AC_GetDeviceCount(IntPtr context,
            [MarshalAs(UnmanagedType.U1)] bool loopback);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool AC_GetDeviceInfo(IntPtr context,
            [MarshalAs(UnmanagedType.U1)] bool loopback, int index,
            byte[] idBuf, int idBufLen,
            byte[] nameBuf, int nameBufLen);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool AC_Open(IntPtr context,
            [MarshalAs(UnmanagedType.U1)] bool loopback,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string deviceId);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void AC_Close(IntPtr context);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern float AC_GetSampleRate(IntPtr context);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AC_GetChannelCount(IntPtr context);

        /// <summary>
        /// Samples of `channel` (-1 mono, 0 left, 1 right) written since `cursor`
        /// (at most maxLength, newest win); advances the cursor.
        /// </summary>
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AC_ReadNew(IntPtr context, int channel, float[] dest, int maxLength, ref ulong cursor);

        /// <summary>True once after the device stopped or was rerouted (the flag clears on read).</summary>
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool AC_PollDeviceLost(IntPtr context);
    }
}
