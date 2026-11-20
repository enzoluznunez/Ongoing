using System.Collections.Generic;
using UnityEngine;

namespace AccountingImmersiveAnalytics.App
{
    public static class FindTransform
    {
        public static List<Transform> FindAllDeep(Transform parent, string name)
        {
            var results = new List<Transform>();
            if (parent != null) CollectDeep(parent, name, results);
            return results;
        }

        private static void CollectDeep(Transform t, string name, List<Transform> into)
        {
            if (t.name == name) into.Add(t);
            for (int i = 0; i < t.childCount; i++)
                CollectDeep(t.GetChild(i), name, into);
        }

        public static Transform EnsureChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing;
            Transform child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        public static Transform FindDeep(Transform parent, string name)
        {
            if (parent == null) return null;
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform result = FindDeep(parent.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
