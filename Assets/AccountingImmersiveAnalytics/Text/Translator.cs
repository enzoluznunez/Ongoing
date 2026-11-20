using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Assistant.FlashLite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Text
{
    public static class Translator
    {
        private const int MostPerRequest = 40;
        private const int MostCharactersPerRequest = 6000;
        private const int TimeoutSeconds = 20;
        private const float RetrySeconds = 10f;
        private const int PumpDelayMilliseconds = 250;
        private const int SaveDelayMilliseconds = 2000;
        private const string Folder = "translations-v2";
        private const string OldFolder = "translations";

        public static event Action Changed;

        private readonly struct Item
        {
            public readonly string Original;
            public readonly string Source;
            public readonly string Context;

            public Item(string original, string source, string context)
            {
                Original = original;
                Source = source;
                Context = context;
            }
        }

        private sealed class Queue
        {
            public readonly List<Item> Order = new List<Item>();
            public readonly HashSet<(string, string, string)> Members = new HashSet<(string, string, string)>();

            public bool Add(Item item)
            {
                if (!Members.Add((item.Original, item.Source, item.Context))) return false;
                Order.Add(item);
                return true;
            }

            public List<Item> TakeAll()
            {
                var taken = new List<Item>(Order);
                Order.Clear();
                Members.Clear();
                return taken;
            }

            public int Count => Order.Count;
        }

        private static readonly Dictionary<string, Dictionary<string, string>> Cache =
            new Dictionary<string, Dictionary<string, string>>();
        private static readonly Dictionary<string, Queue> Wanted = new Dictionary<string, Queue>();
        private static readonly HashSet<(string target, string original)> Asking = new HashSet<(string, string)>();
        private static readonly Dictionary<string, float> RetryAt = new Dictionary<string, float>();
        private static readonly HashSet<string> Fetching = new HashSet<string>();
        private static readonly HashSet<string> LoadedFromDisk = new HashSet<string>();
        private static readonly HashSet<string> Saving = new HashSet<string>();
        private static readonly HashSet<(string target, string original)> Retried = new HashSet<(string, string)>();
        private static string _folder;
        private static bool _pumping;

        public static int Misses { get; private set; }

        public static int Lookups { get; private set; }

        public static string Get(string original, string source = Language.English, string context = null)
        {
            string target = Language.Current;
            if (!string.IsNullOrEmpty(original) && HasLetters(original)) Lookups++;
            if (TryCached(original, source, target, out string translated)) return translated;

            Misses++;
            if (!Asking.Contains((target, original)) && QueueFor(target).Add(new Item(original, source, context))) StartPump();
            return original;
        }

        public static bool TryCached(string original, string source, string target, out string translated)
        {
            translated = original;
            if (!NeedsTranslation(original, source, target)) return true;
            return Cache.TryGetValue(target, out Dictionary<string, string> known) &&
                   known.TryGetValue(original, out translated);
        }

        private static Queue QueueFor(string target)
        {
            if (!Wanted.TryGetValue(target, out Queue queue)) Wanted[target] = queue = new Queue();
            return queue;
        }

        private static Dictionary<string, string> CacheFor(string target)
        {
            if (!Cache.TryGetValue(target, out Dictionary<string, string> known)) Cache[target] = known = new Dictionary<string, string>();
            return known;
        }

        public static async Task Prioritize(string target, string original, string source = Language.English)
        {
            if (TryCached(original, source, target, out _) || !Asking.Add((target, original))) return;
            await Request(target, source, null, new List<string> { original });
        }

        private static async Task Fetch(string target)
        {
            if (!Fetching.Add(target)) return;
            try
            {
                await LoadFromDisk(target);
                while (Wanted.TryGetValue(target, out Queue queue) && queue.Count > 0)
                {
                    if (RetryAt.TryGetValue(target, out float retry) && Time.realtimeSinceStartup < retry) return;

                    var requests = new List<Task>();
                    List<Item> items = queue.TakeAll();
                    foreach (IGrouping<(string, string), Item> group in items.GroupBy(item => (item.Source, item.Context)).ToList())
                    foreach (List<Item> batch in Batches(group))
                    {
                        var originals = new List<string>();
                        foreach (Item item in batch)
                        {
                            if (TryCached(item.Original, item.Source, target, out _) || !Asking.Add((target, item.Original))) continue;
                            originals.Add(item.Original);
                        }
                        if (originals.Count > 0) requests.Add(Request(target, group.Key.Item1, group.Key.Item2, originals));
                    }
                    await Task.WhenAll(requests);
                }
            }
            finally
            {
                Fetching.Remove(target);
            }
        }

        private static async Task Request(string target, string source, string context, List<string> batch)
        {
            JObject answer = await FlashLiteClient.Ask(Instructions(target, source, context, batch.Count),
                new JArray(batch).ToString(Formatting.None),
                FlashLiteClient.Schema(("translations", "STRING_LIST")), TimeoutSeconds);
            foreach (string original in batch) Asking.Remove((target, original));

            List<string> translations = (answer?["translations"] as JArray)?.Select(token => (string)token).ToList();
            if (translations == null || translations.Count != batch.Count)
            {
                Debug.LogWarning($"[Translator] {batch.Count} texts into {Language.NameOf(target)} did not come back " +
                                 $"({(translations == null ? "no answer" : $"{translations.Count} answers")}); retrying in {RetrySeconds:F0} s.");
                RetryAt[target] = Time.realtimeSinceStartup + RetrySeconds;
                Queue queue = QueueFor(target);
                foreach (string original in batch) queue.Add(new Item(original, source, context));
                return;
            }

            var found = new Dictionary<string, string>();
            for (int i = 0; i < batch.Count; i++)
            {
                string original = batch[i], translated = translations[i];
                if (Markers.Signature(original) == Markers.Signature(translated))
                {
                    found[original] = translated;
                    continue;
                }
                if (Retried.Add((target, original)))
                {
                    QueueFor(target).Add(new Item(original, source, context));
                    StartPump();
                    continue;
                }
                found[original] = Markers.KeepSealed(translated);
            }
            Store(target, found);
        }

        private static IEnumerable<List<Item>> Batches(IEnumerable<Item> items)
        {
            var batch = new List<Item>();
            int characters = 0;
            foreach (Item item in items)
            {
                if (batch.Count > 0 && (batch.Count >= MostPerRequest || characters + item.Original.Length > MostCharactersPerRequest))
                {
                    yield return batch;
                    batch = new List<Item>();
                    characters = 0;
                }
                batch.Add(item);
                characters += item.Original.Length;
            }
            if (batch.Count > 0) yield return batch;
        }

        private static string Instructions(string target, string source, string context, int count) =>
            (context == null
                ? "You translate the on-screen text of a mixed-reality data app for people who do not read data or " +
                  $"science well. Translate each item of the JSON array into {Language.NameOf(target)}. "
                : $"You translate a web page, titled \"{context}\", that a reader opened in a mixed-reality app. " +
                  $"Translate each item of the JSON array into {Language.NameOf(target)}. The items are the page's " +
                  "headings, paragraphs, list items, table cells, captions and buttons, in page order; use the ones " +
                  "around each item to understand it. ") +
            Language.WritingOf(target) + " " +
            (source == Language.Unknown
                ? "The items may be in any language; an item already in the target language comes back unchanged. "
                : $"The items are in {Language.NameOf(source)}. ") +
            (context == null
                ? "Keep each one as short and plain as the original; these are labels, buttons, chart titles and short paragraphs. "
                : "Translate faithfully and completely, keeping the meaning, tone and level of detail; do not summarise, explain or skip anything. ") +
            "Keep placeholders such as {0} and {1} exactly as they are. " +
            (context == null
                ? "Keep numbers, units and symbols. "
                : $"Keep every number's value, unit and symbol, but write numbers the way {Language.NameOf(target)} readers usually do, " +
                  "with that language's decimal and thousands separators" +
                  (Numbers.StyleOf(target).Digits is string digits ? $" and its own digits ({digits})" : "") + ". ") +
            "Some items carry markers written like ⟦5⟧ and ⟦/⟧: keep every marker, unchanged, around the " +
            "words that translate the words it surrounds, and add none. " +
            "Markers written like ⟦#2⟧ stand for text that must not change, such as code, a web address or a formula: " +
            "keep each one exactly once, where it belongs in the translated sentence. " +
            "Keep line breaks where they are. " +
            "Use the usual local name for countries, places and organisations, and leave brand names as they are. " +
            $"Return 'translations' with exactly {count} items, in the same order as the input.";

        private static void StartPump()
        {
            if (_pumping) return;
            _pumping = true;
            Pump();
        }

        private static async void Pump()
        {
            try
            {
                await Task.Delay(PumpDelayMilliseconds);
                while (Wanted.TryGetValue(Language.Current, out Queue queue) && queue.Count > 0)
                {
                    string target = Language.Current;
                    float wait = RetryAt.TryGetValue(target, out float retry) ? retry - Time.realtimeSinceStartup : 0f;
                    if (wait > 0f || Fetching.Contains(target))
                        await Task.Delay(Mathf.Max(PumpDelayMilliseconds, Mathf.CeilToInt(wait * 1000f)));
                    else
                        await Fetch(target);
                }
            }
            finally
            {
                _pumping = false;
            }
        }

        private static bool NeedsTranslation(string original, string source, string target) =>
            !string.IsNullOrEmpty(original) && !Language.Same(source, target) && HasLetters(original) && !Numbers.IsNumber(original);

        private static void Store(string target, IReadOnlyDictionary<string, string> translations)
        {
            if (translations == null || translations.Count == 0) return;
            Dictionary<string, string> known = CacheFor(target);
            foreach (KeyValuePair<string, string> pair in translations)
                if (!string.IsNullOrEmpty(pair.Value)) known[pair.Key] = pair.Value;
            if (target == Language.Current) AnnounceChange();
            SaveSoon(target);
        }

        private static string FolderPath => _folder ??= PrepareFolder();

        private static string PrepareFolder()
        {
            string old = Path.Combine(Application.persistentDataPath, OldFolder);
            try
            {
                if (Directory.Exists(old)) Directory.Delete(old, true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Translator] Could not delete the old translation cache {old}: {e.Message}");
            }
            return Path.Combine(Application.persistentDataPath, Folder);
        }

        private static string FileOf(string target) => Path.Combine(FolderPath, target + ".json");

        private static async Task LoadFromDisk(string target)
        {
            if (!LoadedFromDisk.Add(target)) return;
            string file = FileOf(target);
            Dictionary<string, string> saved = await Task.Run(() =>
            {
                try
                {
                    return File.Exists(file) ? JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(file)) : null;
                }
                catch (Exception)
                {
                    return null;
                }
            });
            if (saved == null || saved.Count == 0) return;
            Dictionary<string, string> known = CacheFor(target);
            foreach (KeyValuePair<string, string> pair in saved)
                if (!known.ContainsKey(pair.Key)) known[pair.Key] = pair.Value;
            if (target == Language.Current) AnnounceChange();
        }

        private static async void SaveSoon(string target)
        {
            if (!Saving.Add(target)) return;
            string file = FileOf(target);
            try
            {
                await Task.Delay(SaveDelayMilliseconds);
                if (!Cache.TryGetValue(target, out Dictionary<string, string> known)) return;
                var snapshot = new Dictionary<string, string>(known);
                await Task.Run(() =>
                {
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(file));
                        File.WriteAllText(file, JsonConvert.SerializeObject(snapshot));
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[Translator] could not save translations for {target}: {e.Message}");
                    }
                });
            }
            finally
            {
                Saving.Remove(target);
            }
        }

        private static bool _announcing;

        private static void AnnounceChange()
        {
            if (_announcing) return;
            _announcing = true;
            MainThread.Run(() =>
            {
                _announcing = false;
                Changed?.Invoke();
            });
        }

        private static bool HasLetters(string text)
        {
            for (int i = 0; i < text.Length; i++)
                if (char.IsLetter(text[i])) return true;
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Cache.Clear();
            Wanted.Clear();
            Asking.Clear();
            RetryAt.Clear();
            Fetching.Clear();
            LoadedFromDisk.Clear();
            Saving.Clear();
            Retried.Clear();
            _folder = null;
            _pumping = false;
            Misses = 0;
            Lookups = 0;
            Changed = null;
            _announcing = false;
        }
    }
}
