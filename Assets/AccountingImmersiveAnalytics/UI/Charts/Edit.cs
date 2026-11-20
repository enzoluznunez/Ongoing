using System;
using System.Collections.Generic;
using AccountingImmersiveAnalytics.Assistant.Context;
using AccountingImmersiveAnalytics.UI.Charts.Spec;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Charts
{
    public enum EditKind { Move, Rotate, Scale, Filter, Sort, Pin, Trend }

    public struct MoveRecord
    {
        public int chartId;
        public Vector3 prePos;
        public Quaternion preRot;
        public Vector3 preScale;

        public static bool SamePose(Vector3 aPos, Quaternion aRot, Vector3 aScale,
            Vector3 bPos, Quaternion bRot, Vector3 bScale) =>
            (aPos - bPos).sqrMagnitude < 1e-8f && Quaternion.Angle(aRot, bRot) < 0.01f &&
            (aScale - bScale).sqrMagnitude < 1e-8f;
    }

    public class Edit
    {
        public EditKind kind;
        public MoveRecord move;
        public int chartId;
        public ChartShaping before;
        public int pinnedBefore = -1;
        public string what;
        public bool byAssistant = ActivityFeed.InAssistantCall;

        public static string KindName(EditKind kind) =>
            kind == EditKind.Rotate ? "turn" : kind == EditKind.Scale ? "resize" : kind.ToString().ToLowerInvariant();
    }

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
                if (this[i].kind != EditKind.Pin && this[i].chartId == chartId) return i;
            return -1;
        }

        public List<Edit> For(int chartId)
        {
            var found = new List<Edit>();
            for (int i = Count - 1; i >= 0; i--)
                if (this[i].kind != EditKind.Pin && this[i].chartId == chartId) found.Add(this[i]);
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
            Push(new Edit { kind = kind, move = move, chartId = move.chartId, what = what });

        public void PushPin(int before) =>
            Push(new Edit { kind = EditKind.Pin, pinnedBefore = before });

        public void PushShape(EditKind kind, int chartId, ChartShaping before, string what) =>
            Push(new Edit { kind = kind, chartId = chartId, before = before, what = what });

        private void Push(Edit edit)
        {
            Add(edit);
            NotifyChanged();
        }
    }
}
