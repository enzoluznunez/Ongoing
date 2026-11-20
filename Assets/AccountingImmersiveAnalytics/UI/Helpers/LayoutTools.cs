using System;
using System.Collections;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using UnityEngine;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Helpers
{
    public static class LayoutTools
    {
        public const int SettlePasses = 4;

        public static void Clear(Transform root)
        {
            if (root == null) return;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                child.SetParent(null, false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        public static void ShowCanvas(Canvas canvas, PokeInteractable[] pokes, bool on, bool drawn = false)
        {
            if (!canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
            canvas.enabled = on || drawn;
            for (int i = 0; i < pokes.Length; i++)
                if (pokes[i] != null) pokes[i].enabled = on;
        }

        public static bool PartShown(GameObject part) =>
            part != null && part.activeInHierarchy &&
            !(part.TryGetComponent(out CanvasGroup group) && group.alpha <= 0f && !group.blocksRaycasts);

        public static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        public static void FitPokeBounds(RectTransform canvas)
        {
            if (canvas == null) return;
            Rect r = canvas.rect;
            foreach (BoundsClipper clip in canvas.GetComponentsInChildren<BoundsClipper>(true))
            {
                Transform t = clip.transform;
                Vector3 centre = t.InverseTransformPoint(canvas.TransformPoint(r.center));
                Vector3 size = t.InverseTransformVector(canvas.TransformVector(new Vector3(r.width, r.height, 0f)));
                clip.Position = new Vector3(centre.x, centre.y, clip.Position.z);
                clip.Size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), clip.Size.z);
            }
        }

        public static T Ensure<T>(GameObject go) where T : Component
        {
            T c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        public static RectTransform EnsureChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing as RectTransform ?? existing.gameObject.AddComponent<RectTransform>();
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.AddComponent<RectTransform>();
        }

        public static void Stack(HorizontalOrVerticalLayoutGroup layout)
        {
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        public static LayoutElement FixedHeight(GameObject go, float height)
        {
            LayoutElement le = Ensure<LayoutElement>(go);
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleHeight = 0f;
            return le;
        }

        public static LayoutElement FixedSize(GameObject go, Vector2 size)
        {
            LayoutElement le = FixedHeight(go, size.y);
            le.minWidth = size.x;
            le.preferredWidth = size.x;
            le.flexibleWidth = 0f;
            return le;
        }

        public static IEnumerator Converge(Func<bool> alive, Action apply, Func<float> measure)
        {
            float previous = measure();

            for (int pass = 0; pass < SettlePasses; pass++)
            {
                yield return null;
                if (!alive()) break;

                apply();

                float current = measure();
                if (Mathf.Approximately(current, previous)) break;
                previous = current;
            }
        }
    }
}
