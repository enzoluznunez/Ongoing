using System.Text;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Text;
using UnityEngine;

namespace AccountingImmersiveAnalytics.Assistant.Voice
{
    public static class Transcripts
    {
        private const int MostLoggedChars = 400;

        private static readonly StringBuilder _heard = new StringBuilder();
        private static readonly StringBuilder _said = new StringBuilder();
        private static readonly StringBuilder _heardThisTurn = new StringBuilder();
        private static string _lastHeard;
        private static bool _heardFlushPending;

        public static void Reset()
        {
            _heard.Clear();
            _heardThisTurn.Clear();
            _said.Clear();
            _heardFlushPending = false;
            _lastHeard = null;
        }

        public static void Heard(string text, bool finished)
        {
            if (!string.IsNullOrEmpty(text)) _heard.Append(text);
            if (finished) Flush(_heard, user: true);
        }

        public static void Said(string text, bool finished)
        {
            if (!string.IsNullOrEmpty(text)) _said.Append(text);
            if (finished) Flush(_said, user: false);
        }

        public static void FlushHeardLater() => _heardFlushPending = true;

        public static void FlushHeardIfPending()
        {
            if (!_heardFlushPending) return;
            _heardFlushPending = false;
            Flush(_heard, user: true);
        }

        public static void Interrupted() => Flush(_said, user: false);

        public static void EndTurn()
        {
            Flush(_heard, user: true);
            Flush(_said, user: false);
            DetectTurnLanguage();
            _heardFlushPending = false;
            _lastHeard = null;
        }

        private static void DetectTurnLanguage()
        {
            if (_heardThisTurn.Length == 0) return;
            string heard = _heardThisTurn.ToString();
            _heardThisTurn.Clear();
            _ = MainThread.Run(() => LanguageDetector.Heard(heard));
        }

        private static void Flush(StringBuilder buffer, bool user)
        {
            if (buffer.Length == 0) return;
            string text = buffer.ToString().Trim();
            buffer.Clear();

            if (text.Length == 0) return;

            if (user)
            {
                if (text == _lastHeard) return;
                _lastHeard = text;
                if (_heardThisTurn.Length > 0) _heardThisTurn.Append(' ');
                _heardThisTurn.Append(text);
            }

            Debug.Log($"[Transcripts][{(user ? "user" : "assistant")}] {Logged(text)}");
        }

        private static string Logged(string text)
        {
            string flat = text.Replace('\n', ' ').Replace('\r', ' ');
            return flat.Length <= MostLoggedChars ? flat : flat.Substring(0, MostLoggedChars) + "...";
        }
    }
}
