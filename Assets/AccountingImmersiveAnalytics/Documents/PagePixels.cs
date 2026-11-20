using System;
using Unity.Collections;

namespace AccountingImmersiveAnalytics.Documents
{
    public sealed class PagePixels : IDisposable
    {
        public int Width;
        public int Height;
        public NativeArray<byte> Native;

        public void Dispose()
        {
            if (Native.IsCreated) Native.Dispose();
        }
    }
}
