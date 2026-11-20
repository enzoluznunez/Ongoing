using System.Threading.Tasks;
using AccountingImmersiveAnalytics.Data;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Assistant
{
    public static class GeminiKey
    {
        private const string FileName = "gemini.key";

        private static Task<string> _loading;

        public static Task<string> Load()
        {
            if (_loading == null || _loading.IsFaulted || _loading.IsCanceled) _loading = Read();
            return _loading;
        }

        private static async Task<string> Read()
        {
            string key = await StreamingFiles.ReadLine(FileName);
            if (key != null) return key;
            Debug.LogError($"[GeminiKey] Failed to load the API key from {StreamingFiles.Url(FileName)}.");
            return "";
        }
    }
}
