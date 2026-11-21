using System.Runtime.InteropServices;

namespace AccountingImmersiveAnalytics.Audio
{
    public static class Speaker
    {
#if UNITY_ANDROID && !UNITY_EDITOR

        private static readonly object _gate = new object();
        private static bool _ready;

        [DllImport("spkr", EntryPoint = "init")]
        private static extern void NativeInit();

        [DllImport("spkr", EntryPoint = "destroy")]
        private static extern void NativeDestroy();

        [DllImport("spkr", EntryPoint = "start")]
        public static extern void Start();

        [DllImport("spkr", EntryPoint = "stop")]
        public static extern void Stop();

        [DllImport("spkr", EntryPoint = "clear")]
        private static extern void Clear();

        [DllImport("spkr", EntryPoint = "play")]
        private static extern void Play(
            [MarshalAs(UnmanagedType.LPArray)]
            byte[] data,
            int arrLength
        );

        public static void Init()
        {
            lock (_gate)
            {
                NativeInit();
                _ready = true;
            }
        }

        public static void Destroy()
        {
            lock (_gate)
            {
                _ready = false;
                NativeDestroy();
            }
        }

        public static void Write(byte[] data)
        {
            if (data == null || data.Length == 0) return;
            Play(data, data.Length);
        }

        public static void Flush()
        {
            lock (_gate)
            {
                if (!_ready) return;
                Stop();
                Clear();
                Start();
            }
        }

#else

        public static void Init() { }
        public static void Destroy() { }
        public static void Start() { }
        public static void Stop() { }
        public static void Write(byte[] data) { }
        public static void Flush() { }

#endif

    }
}
