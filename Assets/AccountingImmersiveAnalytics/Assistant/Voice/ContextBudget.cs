using System;
using System.Collections.Generic;
using System.Threading;
using AccountingImmersiveAnalytics.Assistant.Live;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Assistant.Voice
{
    public static class ContextBudget
    {
        public const long WindowTokens = 131072;
        public const long ServerTrimTrigger = 48000;
        public const long ServerTrimTarget = 24000;
        public const long CeilingTokens = WindowTokens / 2;
        public const long WarningTokens = CeilingTokens - 6000;

        private const long TrimNoticedTokens = 2000;

        private static int _usageConnection;
        private static long _contextTokens;
        private static long _sessionPromptTokens;
        private static long _sessionUncachedTokens;
        private static long _lastExactContext;
        private static volatile bool _resumed;
        private static volatile bool _exhausted;
        private static volatile bool _warned;
        private static int _warningPending;
        private static int _endedPending;

        public static bool Exhausted => _exhausted;
        public static long ContextTokens => _contextTokens;

        public static bool ConsumeWarning() => Interlocked.Exchange(ref _warningPending, 0) == 1;
        public static bool ConsumeEnded() => Interlocked.Exchange(ref _endedPending, 0) == 1;

        public static void MarkEnded() => Interlocked.Exchange(ref _endedPending, 1);

        public static void StartConnection(bool resumed) => _resumed = resumed;

        public static void Reset()
        {
            ClearExhaustion();
            _usageConnection = 0;
            _contextTokens = 0;
            _sessionPromptTokens = 0;
            _sessionUncachedTokens = 0;
            _lastExactContext = 0;
            ToolCallTracker.ResetRounds();
        }

        public static void ClearExhaustion()
        {
            _exhausted = false;
            _warned = false;
        }

        public static void Observe(UsageMetadata usage, int connection)
        {
            if (usage == null) return;

            long promptTokens = (long?)usage.PromptTokenCount ?? 0;
            long cachedTokens = (long?)usage.CachedContentTokenCount ?? 0;
            long thoughtTokens = (long?)usage.ThoughtsTokenCount ?? 0;
            long uncachedTokens = promptTokens > cachedTokens ? promptTokens - cachedTokens : 0;
            int rounds = ToolCallTracker.RoundsThisTurn;
            bool usable = Track(connection, promptTokens, uncachedTokens, rounds, out long context, out bool exact);
            string how = exact ? "exact" : $"estimated over {rounds} tool rounds";

            Debug.Log($"[ContextBudget] conn={connection} context={context} ({how}) prompt={promptTokens} response={usage.ResponseTokenCount} " +
                      $"total={usage.TotalTokenCount} session={_sessionPromptTokens} cached={cachedTokens} uncached={uncachedTokens} " +
                      $"thoughts={thoughtTokens} sessionUncached={_sessionUncachedTokens} promptByModality=[{ByModality(usage)}] " +
                      $"at {DateTime.UtcNow:HH:mm:ss.fff}");

            if (exact && _lastExactContext > 0 && context + TrimNoticedTokens < _lastExactContext)
                Debug.Log($"[ContextBudget] Context fell {_lastExactContext} -> {context}; the server trimmed it.");
            if (exact) _lastExactContext = context;

            if (usable) CheckCeiling(context);
        }

        private static string ByModality(UsageMetadata usage)
        {
            if (usage.PromptTokensDetails == null) return "";
            var parts = new List<string>();
            foreach (ModalityTokenCount count in usage.PromptTokensDetails)
                if (count != null) parts.Add($"{count.Modality}:{count.TokenCount}");
            return string.Join(", ", parts);
        }

        private static bool Track(int connection, long promptTokens, long uncachedTokens, int rounds, out long context, out bool exact)
        {
            if (connection != _usageConnection)
            {
                _usageConnection = connection;
                if (!_resumed) _contextTokens = 0;
            }

            if (promptTokens > 0) _sessionPromptTokens += promptTokens;
            if (uncachedTokens > 0) _sessionUncachedTokens += uncachedTokens;

            exact = rounds <= 0;
            context = _contextTokens;
            if (promptTokens <= 0) return false;

            long reading = promptTokens / (rounds + 1);
            if (exact || reading > _contextTokens) _contextTokens = reading;

            context = _contextTokens;
            return true;
        }

        private static void CheckCeiling(long context)
        {
            if (context >= CeilingTokens) { MarkExhausted(context); return; }
            if (context < WarningTokens || _warned) return;

            _warned = true;
            Debug.LogWarning($"[ContextBudget] Approaching the context ceiling: {context} of {CeilingTokens}.");
            Interlocked.Exchange(ref _warningPending, 1);
        }

        private static void MarkExhausted(long context)
        {
            if (_exhausted) return;
            _exhausted = true;
            Debug.LogWarning($"[ContextBudget] Context ceiling reached: {context} >= {CeilingTokens}.");
            VoiceSession.DropConnection();
        }
    }
}
