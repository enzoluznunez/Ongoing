using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace AccountingImmersiveAnalytics.Data
{
    public static class StreamingFiles
    {

        public static string Url(string fileName)
        {
            string escaped = string.Join("/", fileName.Split('/').Select(Uri.EscapeDataString));
            string path = Application.streamingAssetsPath.TrimEnd('/') + "/" + escaped;
            return path.Contains("://") ? path : "file://" + path;
        }

        public static async Task<List<string>> List(string folder, string extension, string indexFile)
        {
            var names = new List<string>();
            string local = Path.Combine(Application.streamingAssetsPath, folder);
            if (!local.Contains("://") && Directory.Exists(local))
            {
                foreach (string path in Directory.GetFiles(local, "*" + extension)) names.Add(Path.GetFileName(path));
                names.Sort(StringComparer.OrdinalIgnoreCase);
                return names;
            }

            string[] packed = PackedNames(folder);
            if (packed != null)
            {
                names.AddRange(packed.Where(name => name.EndsWith(extension, StringComparison.OrdinalIgnoreCase)));
                names.Sort(StringComparer.OrdinalIgnoreCase);
                return names;
            }

            string json = await ReadText(indexFile);
            if (json == null)
            {
                Debug.LogError($"[StreamingFiles] StreamingAssets/{folder} could not be listed and StreamingAssets/{indexFile} is missing.");
                return names;
            }
            try
            {
                foreach (JToken name in JArray.Parse(json)) names.Add((string)name);
            }
            catch (Exception e)
            {
                Debug.LogError($"[StreamingFiles] StreamingAssets/{indexFile} could not be read: {e.Message}");
            }
            return names;
        }

        private static string[] PackedNames(string folder)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var assets = activity.Call<AndroidJavaObject>("getAssets"))
                    return assets.Call<string[]>("list", folder);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[StreamingFiles] Android could not list StreamingAssets/{folder}: {e.Message}");
                return null;
            }
#else
            return null;
#endif
        }

        public static Task<string> ReadText(string fileName)
        {
            var done = new TaskCompletionSource<string>();
            var req = UnityWebRequest.Get(Url(fileName));
            req.SendWebRequest().completed += _ =>
            {
                done.SetResult(req.result == UnityWebRequest.Result.Success ? req.downloadHandler.text : null);
                req.Dispose();
            };
            return done.Task;
        }

        public static async Task<string> ReadLine(string fileName) => (await ReadText(fileName))?.Trim();

        public static Task<byte[]> ReadBytes(string fileName)
        {
            var done = new TaskCompletionSource<byte[]>();
            var req = UnityWebRequest.Get(Url(fileName));
            req.SendWebRequest().completed += _ =>
            {
                done.SetResult(req.result == UnityWebRequest.Result.Success ? req.downloadHandler.data : null);
                req.Dispose();
            };
            return done.Task;
        }
    }
}
