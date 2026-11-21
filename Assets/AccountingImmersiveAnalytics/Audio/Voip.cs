using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Audio
{
    public static class Voip
    {
#if UNITY_ANDROID && !UNITY_EDITOR

        public static event Action<byte[]> FrameCaptured;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void FrameCallback(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
            byte[] data,
            int length
        );

        [MonoPInvokeCallback(typeof(FrameCallback))]
        private static void OnFrame(byte[] data, int length)
        {
            try { FrameCaptured?.Invoke(data); }
            catch (Exception e) { Debug.LogException(e); }
        }

        private static readonly FrameCallback _onFrame = OnFrame;

        [DllImport("voip", EntryPoint = "init")]
        private static extern void NativeInit(FrameCallback onFrame);

        public static void Init()
        {
            NativeInit(_onFrame);
        }

        [DllImport("voip", EntryPoint = "destroy")]
        public static extern void Destroy();

        [DllImport("voip", EntryPoint = "start")]
        public static extern void Start();

        [DllImport("voip", EntryPoint = "stop")]
        public static extern void Stop();

#else

        public static event Action<byte[]> FrameCaptured { add { } remove { } }

        public static void Init() { }
        public static void Destroy() { }
        public static void Start() { }
        public static void Stop() { }

#endif
    }
}
