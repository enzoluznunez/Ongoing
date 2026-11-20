using System;
using System.Collections.Concurrent;

namespace AccountingImmersiveAnalytics.Assistant.Voice
{
    public static class ProtocolLog
    {
        private const int LinesKept = 16;

        private static readonly ConcurrentQueue<string> _lines = new ConcurrentQueue<string>();

        public static void Note(string what)
        {
            _lines.Enqueue($"{DateTime.UtcNow:HH:mm:ss.fff} {what}");
            while (_lines.Count > LinesKept && _lines.TryDequeue(out _)) { }
        }

        public static string Dump() => string.Join("\n  ", _lines.ToArray());
    }
}
