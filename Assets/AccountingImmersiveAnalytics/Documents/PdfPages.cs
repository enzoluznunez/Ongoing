using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Documents
{
    public sealed class PdfPages : IDisposable
    {
        private const int LongestSide = 2048;

    #if UNITY_ANDROID && !UNITY_EDITOR
        public static bool Supported => true;

        private const int ReadOnly = 0x10000000;
        private const int RenderForDisplay = 1;
        private const int White = -1;

        private AndroidJavaObject _descriptor;
        private AndroidJavaObject _renderer;
        private string _scratch;

        private bool OpenHere(byte[] bytes, string scratch)
        {
            _scratch = scratch;
            System.IO.File.WriteAllBytes(scratch, bytes);
            using (var file = new AndroidJavaObject("java.io.File", scratch))
            using (var descriptors = new AndroidJavaClass("android.os.ParcelFileDescriptor"))
                _descriptor = descriptors.CallStatic<AndroidJavaObject>("open", file, ReadOnly);
            _renderer = new AndroidJavaObject("android.graphics.pdf.PdfRenderer", _descriptor);
            Count = _renderer.Call<int>("getPageCount");
            return true;
        }

        private PagePixels RenderHere(int index, int width, int height)
        {
            using (AndroidJavaObject page = _renderer.Call<AndroidJavaObject>("openPage", index))
            using (var configs = new AndroidJavaClass("android.graphics.Bitmap$Config"))
            using (AndroidJavaObject config = configs.GetStatic<AndroidJavaObject>("ARGB_8888"))
            using (var bitmaps = new AndroidJavaClass("android.graphics.Bitmap"))
            using (AndroidJavaObject bitmap = bitmaps.CallStatic<AndroidJavaObject>("createBitmap", width, height, config))
            {
                try
                {
                    bitmap.Call("eraseColor", White);
                    CallVoid(page, "render", "(Landroid/graphics/Bitmap;Landroid/graphics/Rect;Landroid/graphics/Matrix;I)V",
                        new jvalue { l = bitmap.GetRawObject() }, new jvalue { l = IntPtr.Zero }, new jvalue { l = IntPtr.Zero },
                        new jvalue { i = RenderForDisplay });
                    var pixels = new PagePixels
                    {
                        Width = width, Height = height,
                        Native = new NativeArray<byte>(width * height * 4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory)
                    };
                    IntPtr buffer = IntPtr.Zero;
                    try
                    {
                        buffer = AndroidJNI.NewDirectByteBuffer(pixels.Native);
                        CallVoid(bitmap, "copyPixelsToBuffer", "(Ljava/nio/Buffer;)V", new jvalue { l = buffer });
                    }
                    catch
                    {
                        pixels.Dispose();
                        throw;
                    }
                    finally
                    {
                        if (buffer != IntPtr.Zero) AndroidJNI.DeleteLocalRef(buffer);
                    }
                    return pixels;
                }
                finally
                {
                    bitmap.Call("recycle");
                    page.Call("close");
                }
            }
        }

        private static void CallVoid(AndroidJavaObject target, string name, string signature, params jvalue[] args)
        {
            IntPtr type = AndroidJNI.GetObjectClass(target.GetRawObject());
            try
            {
                IntPtr method = AndroidJNI.GetMethodID(type, name, signature);
                AndroidJNI.CallVoidMethod(target.GetRawObject(), method, args);
                IntPtr thrown = AndroidJNI.ExceptionOccurred();
                if (thrown == IntPtr.Zero) return;
                AndroidJNI.ExceptionClear();
                AndroidJNI.DeleteLocalRef(thrown);
                throw new InvalidOperationException($"Android's PdfRenderer failed in {name}.");
            }
            finally
            {
                AndroidJNI.DeleteLocalRef(type);
            }
        }

        private void CloseHere()
        {
            _renderer?.Call("close");
            _renderer?.Dispose();
            _descriptor?.Call("close");
            _descriptor?.Dispose();
            _renderer = null;
            _descriptor = null;
            if (_scratch != null && System.IO.File.Exists(_scratch)) System.IO.File.Delete(_scratch);
            _scratch = null;
        }
    #else
        public static bool Supported => false;

        private bool OpenHere(byte[] bytes, string scratch) => false;

        private PagePixels RenderHere(int index, int width, int height) => null;

        private void CloseHere() { }
    #endif

        private static BlockingCollection<Action> _jobs;
        private static Thread _thread;
        private bool _closed;

        public int Count { get; private set; }

        public static async Task<PdfPages> Open(byte[] bytes)
        {
            if (!Supported || bytes == null) return null;
            string scratch = System.IO.Path.Combine(Application.temporaryCachePath, $"document-{Guid.NewGuid():N}.pdf");
            var pages = new PdfPages();
            bool opened = false;
            try
            {
                opened = await Run(() => pages.OpenHere(bytes, scratch));
                return opened ? pages : null;
            }
            finally
            {
                if (!opened) pages.Dispose();
            }
        }

        public Task<PagePixels> Render(int index, int width, int height) =>
            Run(() => _closed || index < 0 || index >= Count ? null : RenderHere(index, width, height));

        public static Vector2Int PixelsFor(double pageWidth, double pageHeight, double share = 1)
        {
            double scale = LongestSide * share / Math.Max(1.0, Math.Max(pageWidth, pageHeight));
            return new Vector2Int(Math.Max(1, (int)Math.Round(pageWidth * scale)), Math.Max(1, (int)Math.Round(pageHeight * scale)));
        }

        public static Texture2D MakeTexture(PagePixels pixels, float cornerPixels = 0f)
        {
            if (pixels == null) return null;
            using (pixels)
            {
                RoundCorners(pixels, cornerPixels);
                var texture = new Texture2D(pixels.Width, pixels.Height, TextureFormat.RGBA32, true, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 4
                };
                texture.SetPixelData(pixels.Native, 0);
                texture.Apply(true, true);
                return texture;
            }
        }

        private static void RoundCorners(PagePixels pixels, float radius)
        {
            int width = pixels.Width, height = pixels.Height;
            radius = Mathf.Min(radius, Mathf.Min(width, height) * 0.5f);
            int reach = Mathf.CeilToInt(radius);
            if (reach < 1 || pixels.Native.Length < width * height * 4) return;
            NativeArray<byte> bytes = pixels.Native;
            for (int y = 0; y < reach; y++)
            {
                float dy = radius - (y + 0.5f);
                for (int x = 0; x < reach; x++)
                {
                    float dx = radius - (x + 0.5f);
                    if (dx <= 0f || dy <= 0f) continue;
                    float cover = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    if (cover >= 1f) continue;
                    Fade(bytes, width, x, y, cover);
                    Fade(bytes, width, width - 1 - x, y, cover);
                    Fade(bytes, width, x, height - 1 - y, cover);
                    Fade(bytes, width, width - 1 - x, height - 1 - y, cover);
                }
            }
        }

        private static void Fade(NativeArray<byte> bytes, int width, int x, int y, float cover)
        {
            int at = (y * width + x) * 4 + 3;
            bytes[at] = (byte)Mathf.RoundToInt(bytes[at] * cover);
        }

        public void Dispose()
        {
            if (_closed) return;
            _closed = true;
            _ = Run(() =>
            {
                CloseHere();
                return true;
            });
        }

        private static Task<T> Run<T>(Func<T> work)
        {
            var done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            EnsureThread();
            _jobs.Add(() =>
            {
                try
                {
                    done.SetResult(work());
                }
                catch (Exception e)
                {
                    done.SetException(e);
                }
            });
            return done.Task;
        }

        private static void EnsureThread()
        {
            if (_thread != null && _thread.IsAlive) return;
            _jobs = new BlockingCollection<Action>();
            BlockingCollection<Action> jobs = _jobs;
            _thread = new Thread(() => Work(jobs)) { IsBackground = true, Name = "PdfPages" };
            _thread.Start();
    #if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= Stop;
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Stop;
    #endif
            Application.quitting -= Stop;
            Application.quitting += Stop;
        }

        private static void Work(BlockingCollection<Action> jobs)
        {
    #if UNITY_ANDROID && !UNITY_EDITOR
            AndroidJNI.AttachCurrentThread();
    #endif
            foreach (Action job in jobs.GetConsumingEnumerable()) job();
    #if UNITY_ANDROID && !UNITY_EDITOR
            AndroidJNI.DetachCurrentThread();
    #endif
        }

        private static void Stop()
        {
            _jobs?.CompleteAdding();
            _thread = null;
        }
    }
}
