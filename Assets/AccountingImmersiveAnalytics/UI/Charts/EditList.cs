using System;
using System.Collections.Generic;
using AccountingImmersiveAnalytics.UI.Charts.Spec;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Charts
{
    public class EditList : List<Edit>
    {
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Changed = null;

        public void NotifyChanged() => Changed?.Invoke();

        public Edit Peek() => Count > 0 ? this[Count - 1] : null;

        public int NewestFor(int chartId)
        {
            for (int i = Count - 1; i >= 0; i--)
                if (this[i].Kind != EditKind.Pin && this[i].ChartId == chartId) return i;
            return -1;
        }

        public List<Edit> For(int chartId)
        {
            var found = new List<Edit>();
            for (int i = Count - 1; i >= 0; i--)
                if (this[i].Kind != EditKind.Pin && this[i].ChartId == chartId) found.Add(this[i]);
            return found;
        }

        public void DropAt(int index)
        {
            if (index < 0 || index >= Count) return;
            RemoveAt(index);
            NotifyChanged();
        }

        public Edit Pop()
        {
            if (Count == 0) return null;
            Edit e = this[Count - 1];
            RemoveAt(Count - 1);
            NotifyChanged();
            return e;
        }

        public void PushMove(MoveRecord move, EditKind kind, string what) =>
            Push(new Edit { Kind = kind, Move = move, ChartId = move.ChartId, What = what });

        public void PushPin(int before) =>
            Push(new Edit { Kind = EditKind.Pin, PinnedBefore = before });

        public void PushShape(EditKind kind, int chartId, ChartShaping before, string what) =>
            Push(new Edit { Kind = kind, ChartId = chartId, Before = before, What = what });

        private void Push(Edit edit)
        {
            Add(edit);
            NotifyChanged();
        }
    }
}
