using System.Collections.Generic;
using UnityEngine;

public enum EditKind { Move, Rotate, Scale }

public struct MoveRecord
{
    public int chartId;
    public Vector3 prePos;
    public Quaternion preRot;
    public Vector3 preScale;
    public Vector3 postPos;
    public Quaternion postRot;
    public Vector3 postScale;
    public float distance;

    public Vector3 anchorPos;
    public Quaternion anchorRot;

    public bool IsNoChange => SamePose(prePos, preRot, preScale, postPos, postRot, postScale);

    public static bool SamePose(Vector3 aPos, Quaternion aRot, Vector3 aScale,
        Vector3 bPos, Quaternion bRot, Vector3 bScale) =>
        (aPos - bPos).sqrMagnitude < 1e-8f && Quaternion.Angle(aRot, bRot) < 0.01f &&
        (aScale - bScale).sqrMagnitude < 1e-8f;
}

public class Edit
{
    public EditKind kind;
    public MoveRecord move;
    public int group;

    public bool IsPose => true;

    public static string KindName(EditKind kind) => kind.ToString().ToLowerInvariant();
}

public class EditList : List<Edit>
{
    private int _groupSeq;
    private int _group;

    public Edit Peek() => Count > 0 ? this[Count - 1] : null;

    public int TopGroupSize()
    {
        Edit top = Peek();
        if (top == null) return 0;
        if (top.group == 0) return 1;

        int n = 0;
        for (int i = Count - 1; i >= 0 && this[i].group == top.group; i--) n++;
        return n;
    }

    public int UndoStepCount()
    {
        int steps = 0;
        int i = Count - 1;
        while (i >= 0)
        {
            int g = this[i].group;
            steps++;
            if (g == 0) { i--; continue; }
            while (i >= 0 && this[i].group == g) i--;
        }
        return steps;
    }

    public void DropAt(int index)
    {
        if (index < 0 || index >= Count) return;
        RemoveAt(index);
    }

    public int StampGroup(int fromIndex)
    {
        if (fromIndex < 0) fromIndex = 0;
        if (fromIndex >= Count) return 0;

        int id = ++_groupSeq;
        for (int i = fromIndex; i < Count; i++) this[i].group = id;
        return Count - fromIndex;
    }

    public Edit Pop()
    {
        if (Count == 0) return null;
        Edit e = this[Count - 1];
        RemoveAt(Count - 1);
        return e;
    }

    public void PushMove(MoveRecord move, EditKind kind) =>
        Add(new Edit { kind = kind, move = move, group = _group });

    public void DropKind(EditKind kind) => RemoveAll(e => e.kind == kind);
}
