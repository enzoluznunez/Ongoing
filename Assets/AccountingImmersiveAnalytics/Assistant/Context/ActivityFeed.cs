using System.Collections.Generic;
using AccountingImmersiveAnalytics.Assistant.Voice;

namespace AccountingImmersiveAnalytics.Assistant.Context
{
    public static class ActivityFeed
    {
        private const int MaxUserQueue = 40;

        private static int _assistantDepth;

        public static bool InAssistantCall
        {
            get => _assistantDepth > 0;
            set
            {
                if (value) _assistantDepth++;
                else if (_assistantDepth > 0) _assistantDepth--;
            }
        }

        public static bool UserDriven => !InAssistantCall;

        private static readonly List<string> _assistantBatch = new List<string>();
        private static readonly List<string> _userQueue = new List<string>();
        private static readonly Dictionary<string, string> _state = new Dictionary<string, string>();
        private static readonly List<string> _stateOrder = new List<string>();
        private static bool _snapshotWanted;
        private static string _pointing;

        public static void Point(string what)
        {
            if (string.IsNullOrEmpty(what)) return;
            _pointing = what;
            ScenePush.Request();
        }

        public static void Record(string what)
        {
            if (string.IsNullOrEmpty(what)) return;

            if (InAssistantCall)
            {
                _assistantBatch.Add(what);
                return;
            }


            _userQueue.Add(what);
            while (_userQueue.Count > MaxUserQueue) _userQueue.RemoveAt(0);
            ScenePush.Request();
        }

        public static string TakeAssistantBatch()
        {
            if (_assistantBatch.Count == 0) return null;
            string text = string.Join("; ", _assistantBatch);
            _assistantBatch.Clear();
            return text;
        }

        public static void SetState(string key, string what)
        {
            if (string.IsNullOrEmpty(what)) return;
            if (!_state.ContainsKey(key)) _stateOrder.Add(key);
            _state[key] = what;
        }

        public static void RecordState(string key, string what)
        {
            SetState(key, what);
            Record(what);
        }

        public static void ClearPending()
        {
            _userQueue.Clear();
            _assistantBatch.Clear();
            _assistantDepth = 0;
            _snapshotWanted = false;
            _pointing = null;
        }

        public static void RequestSnapshot()
        {
            _snapshotWanted = true;
            ScenePush.Request();
        }

        public static bool HasPending => _snapshotWanted || _userQueue.Count > 0 || _pointing != null;

        public static bool TryTakeBatch(out string text)
        {
            text = null;
            if (!HasPending) return false;

            var lines = new List<string>(2);

            if (_snapshotWanted)
            {
                _snapshotWanted = false;
                if (_stateOrder.Count > 0)
                {
                    var facts = new List<string>(_stateOrder.Count);
                    for (int i = 0; i < _stateOrder.Count; i++) facts.Add(_state[_stateOrder[i]]);
                    lines.Add("[state] " + string.Join("; ", facts));
                }
            }

            if (_userQueue.Count > 0)
            {
                lines.Add("[tool] User: " + string.Join("; ", _userQueue));
                _userQueue.Clear();
            }

            if (_pointing != null)
            {
                lines.Add("[pointing] The user pinched " + _pointing);
                _pointing = null;
            }

            if (lines.Count == 0) return false;
            text = string.Join("\n", lines);
            return true;
        }
    }
}
