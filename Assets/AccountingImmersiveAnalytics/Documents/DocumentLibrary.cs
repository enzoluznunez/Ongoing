using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AccountingImmersiveAnalytics.Data;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Documents
{
    public static class DocumentLibrary
    {
        public const string Folder = "Documents";
        public const string IndexFile = Folder + "/index.json";
        public const string Extension = ".pdf";
        public const string CsvExtension = DocumentEntry.CsvExtension;

        public static bool Listed(string name) =>
            name.EndsWith(Extension, StringComparison.OrdinalIgnoreCase) || name.EndsWith(CsvExtension, StringComparison.OrdinalIgnoreCase);

        private static readonly List<DocumentEntry> Entries = new List<DocumentEntry>();
        private static Task _loading;

        public static IReadOnlyList<DocumentEntry> All => Entries;

        public static bool Ready { get; private set; }

        public static event Action Changed;

        public static Task Load() => _loading ??= LoadAsync();

        private static async Task LoadAsync()
        {
            Entries.Clear();
            var reads = new List<Task<DocumentEntry>>();
            foreach (string name in await FileNames())
                if (Listed(name)) reads.Add(EntryFor(name));
            Entries.AddRange(await Task.WhenAll(reads));
            if (Entries.Count == 0) Debug.LogWarning($"[DocumentLibrary] No PDF or CSV files were found in StreamingAssets/{Folder}.");
            Ready = true;
            Changed?.Invoke();
        }

        private static async Task<DocumentEntry> EntryFor(string name)
        {
            string id = System.IO.Path.GetFileNameWithoutExtension(name);
            string file = $"{Folder}/{name}";
            if (name.EndsWith(CsvExtension, StringComparison.OrdinalIgnoreCase))
                return new DocumentEntry { Id = id, Title = id, File = file };
            byte[] bytes = await StreamingFiles.ReadBytes(file);
            var peek = bytes != null ? await Task.Run(() => PdfDocumentReader.Peek(bytes)) : null;
            string title = peek?.Title;
            var entry = new DocumentEntry { Id = id, Title = string.IsNullOrWhiteSpace(title) ? id : title.Trim(), File = file };
            if (peek.HasValue && peek.Value.Width > 0 && peek.Value.Height > 0)
            {
                entry.PageWidth = peek.Value.Width;
                entry.PageHeight = peek.Value.Height;
                entry.Showable = true;
            }
            return entry;
        }

        private static Task<List<string>> FileNames() => StreamingFiles.List(Folder, "", IndexFile);

        public static DocumentEntry Find(string wanted)
        {
            if (string.IsNullOrWhiteSpace(wanted)) return null;
            string key = wanted.Trim();
            foreach (DocumentEntry entry in Entries)
                if (string.Equals(entry.Title, key, StringComparison.OrdinalIgnoreCase) || string.Equals(entry.Id, key, StringComparison.OrdinalIgnoreCase))
                    return entry;
            DocumentEntry only = null;
            int hits = 0;
            foreach (DocumentEntry entry in Entries)
            {
                if (entry.Title.IndexOf(key, StringComparison.OrdinalIgnoreCase) < 0 && entry.Id.IndexOf(key, StringComparison.OrdinalIgnoreCase) < 0) continue;
                only = entry;
                hits++;
            }
            return hits == 1 ? only : null;
        }

        public static int IndexOf(DocumentEntry entry) => entry == null ? -1 : Entries.IndexOf(entry);

        public static string ForPrompt()
        {
            if (Entries.Count == 0) return "none yet";
            var text = new StringBuilder();
            for (int i = 0; i < Entries.Count; i++)
            {
                if (i > 0) text.Append(i == Entries.Count - 1 ? ", and " : ", ");
                text.Append('"').Append(Entries[i].Title).Append('"');
            }
            return text.ToString();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Entries.Clear();
            _loading = null;
            Ready = false;
            Changed = null;
        }
    }
}
