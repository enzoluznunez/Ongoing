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

        public static async Task<List<string>> List(string folder, string indexFile)
        {
            string[] found = LocalNames(folder) ?? PackedNames(folder);
            if (found != null && found.Length > 0)
            {
                Array.Sort(found, StringComparer.OrdinalIgnoreCase);
                return found.ToList();
            }

            var names = new List<string>();
            string json = await ReadText(indexFile);
            if (json == null)
            {
                Debug.LogError($"[StreamingFiles] StreamingAssets/{folder} could not be listed and StreamingAssets/{indexFile} is missing.");
                return names;
            }
            try
            {
                foreach (JToken name in JArray.Parse(json))
                    if (name.Type == JTokenType.String) names.Add((string)name);
            }
            catch (Exception e)
            {
                Debug.LogError($"[StreamingFiles] StreamingAssets/{indexFile} could not be read: {e.Message}");
            }
            return names;
        }

        private static string[] LocalNames(string folder)
        {
            string local = Path.Combine(Application.streamingAssetsPath, folder);
            if (local.Contains("://") || !Directory.Exists(local)) return null;
            try
            {
                return Directory.GetFiles(local).Select(Path.GetFileName).ToArray();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[StreamingFiles] Could not list StreamingAssets/{folder}: {e.Message}");
                return null;
            }
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

        public static Task<string> ReadText(string fileName) => Fetch(fileName, handler => handler.text);

        public static async Task<string> ReadLine(string fileName)
        {
            string text = (await ReadText(fileName))?.TrimStart('﻿');
            if (text == null) return null;
            foreach (string line in text.Split('\n'))
                if (!string.IsNullOrWhiteSpace(line)) return line.Trim();
            return "";
        }

        public static Task<byte[]> ReadBytes(string fileName) => Fetch(fileName, handler => handler.data);

        private static Task<T> Fetch<T>(string fileName, Func<DownloadHandler, T> read)
        {
            var done = new TaskCompletionSource<T>();
            var req = UnityWebRequest.Get(Url(fileName));
            req.SendWebRequest().completed += _ =>
            {
                try
                {
                    done.TrySetResult(req.result == UnityWebRequest.Result.Success ? read(req.downloadHandler) : default);
                }
                catch (Exception e)
                {
                    done.TrySetException(e);
                }
                finally
                {
                    req.Dispose();
                }
            };
            return done.Task;
        }
    }
}
