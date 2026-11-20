using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Assistant.Voice
{
    public static class ToolCallTracker
    {
        private const int StallMilliseconds = 20000;

        private sealed class PendingCall
        {
            public string Name;
            public DateTime SentUtc;
        }

        private static readonly ConcurrentDictionary<string, PendingCall> _pending =
            new ConcurrentDictionary<string, PendingCall>();
        private static readonly HashSet<string> _cancelled = new HashSet<string>();
        private static readonly object _cancelGate = new object();
        private static int _roundsThisTurn;

        public static int RoundsThisTurn => Volatile.Read(ref _roundsThisTurn);

        public static int StartRound() => Interlocked.Increment(ref _roundsThisTurn);

        public static void ResetRounds() => Interlocked.Exchange(ref _roundsThisTurn, 0);

        public static void Dispatched(string id, string name)
        {
            if (string.IsNullOrEmpty(id)) return;
            _pending[id] = new PendingCall { Name = name, SentUtc = DateTime.UtcNow };
        }

        public static void Settled(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            _pending.TryRemove(id, out _);
        }

        public static void Clear() => _pending.Clear();

        public static void CheckStalled()
        {
            if (_pending.IsEmpty) return;
            DateTime now = DateTime.UtcNow;

            foreach (var pair in _pending)
            {
                PendingCall call = pair.Value;
                if ((now - call.SentUtc).TotalMilliseconds < StallMilliseconds) continue;
                if (!_pending.TryRemove(pair.Key, out _)) continue;

                Debug.LogError($"[ToolCallTracker] No tool response was ever sent for {call.Name} " +
                               $"(id={pair.Key}) after {StallMilliseconds / 1000}s; the turn cannot complete.");
            }
        }

        public static void Cancel(IEnumerable<string> ids)
        {
            if (ids == null) return;
            lock (_cancelGate)
                foreach (string id in ids)
                    if (!string.IsNullOrEmpty(id)) _cancelled.Add(id);
        }

        public static bool ConsumeCancelled(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            lock (_cancelGate) return _cancelled.Remove(id);
        }
    }
}
