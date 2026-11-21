using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Assistant.FlashLite
{
    public static class AnswerCache
    {
        private const string FileName = "answers-v1.json";
        private const int SaveDelayMilliseconds = 2000;

        private static readonly Dictionary<string, string> Answers = new Dictionary<string, string>();
        private static Task _loading;
        private static bool _saving;
        private static bool _dirty;
        private static string _file;

        public static string KeyFor(string request)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(request));
                var key = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) key.Append(b.ToString("x2"));
                return key.ToString();
            }
        }

        public static async Task<string> Find(string key)
        {
            await Load();
            return Answers.TryGetValue(key, out string answer) ? answer : null;
        }

        public static void Store(string key, string answer)
        {
            if (string.IsNullOrEmpty(answer)) return;
            Answers[key] = answer;
            _dirty = true;
            SaveSoon();
        }

        private static Task Load()
        {
            if (_loading != null) return _loading;
            _file = Path.Combine(Application.persistentDataPath, FileName);
            _loading = ReadInto(_file);
            return _loading;
        }

        private static async Task ReadInto(string file)
        {
            Dictionary<string, string> saved = await Task.Run(() =>
            {
                try
                {
                    return File.Exists(file) ? JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(file)) : null;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AnswerCache] Could not read saved answers from {file}: {e.Message}");
                    return null;
                }
            });
            if (saved == null) return;
            foreach (KeyValuePair<string, string> pair in saved)
                if (!Answers.ContainsKey(pair.Key)) Answers[pair.Key] = pair.Value;
        }

        private static async void SaveSoon()
        {
            if (_saving || _file == null) return;
            _saving = true;
            try
            {
                while (_dirty)
                {
                    await Task.Delay(SaveDelayMilliseconds);
                    _dirty = false;
                    var snapshot = new Dictionary<string, string>(Answers);
                    string file = _file;
                    await Task.Run(() =>
                    {
                        try
                        {
                            File.WriteAllText(file, JsonConvert.SerializeObject(snapshot));
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"[AnswerCache] Could not save answers to {file}: {e.Message}");
                        }
                    });
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AnswerCache] Saving answers failed: {e.Message}");
            }
            finally
            {
                _saving = false;
            }
        }
    }
}
