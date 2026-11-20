using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace AccountingImmersiveAnalytics.Editor
{
    [InitializeOnLoad]
    public static class CopyRuntimeObjects
    {
        private const string MenuPath = "Tools/Scene/Copy Runtime Objects Into Scene";
        private const string SceneObjectsKey = "CopyRuntimeObjects.SceneObjects";
        private const string CapturedKey = "CopyRuntimeObjects.Captured";

        private static readonly string[] DataPrefixes =
        {
            "Slot ", "Plank_", "Dataset_", "Option_", "Step_", "Paragraph_", "TMP SubMesh", "PrewarmRig"
        };

        private static readonly System.Reflection.Assembly OwnAssembly = typeof(App.Scene).Assembly;

        [Serializable]
        private class Node
        {
            public string Path;
            public string ParentPath;
            public string Name;
            public int Sibling;
            public int Layer;
            public bool Active;
            public bool Rect;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public Vector3 LocalScale;
            public Vector2 AnchorMin;
            public Vector2 AnchorMax;
            public Vector2 Pivot;
            public Vector2 AnchoredPosition;
            public Vector2 SizeDelta;
        }

        [Serializable]
        private class Capture
        {
            public string ScenePath;
            public List<Node> Nodes = new List<Node>();
        }

        [Serializable]
        private class SceneObjects
        {
            public string ScenePath;
            public List<string> Paths = new List<string>();
        }

        static CopyRuntimeObjects() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        [MenuItem(MenuPath, true)]
        private static bool CanCopy() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        private static void Copy()
        {
            var known = JsonUtility.FromJson<SceneObjects>(SessionState.GetString(SceneObjectsKey, "{}"));
            UnityScene scene = SceneManager.GetActiveScene();
            if (known.ScenePath != scene.path)
            {
                Debug.LogError("[CopyRuntimeObjects] The scene was not recorded when Play began. Stop, then press Play again.");
                return;
            }

            var existing = new HashSet<string>(known.Paths);
            var capture = new Capture { ScenePath = scene.path };
            var present = new HashSet<string>();
            var owned = new HashSet<string>();
            var skippedRoots = new List<string>();

            foreach ((Transform root, string path) in Roots(scene))
            {
                if (!existing.Contains(path))
                {
                    skippedRoots.Add(root.name);
                    continue;
                }
                if (!HoldsOwnCode(root.gameObject)) continue;
                owned.Add(path);
                Walk(root, path, existing, present, capture.Nodes);
            }

            SessionState.SetString(CapturedKey, JsonUtility.ToJson(capture));

            Debug.Log($"[CopyRuntimeObjects] Found {capture.Nodes.Count} objects the code made. Stop Play to add them to {scene.name}.");
            if (skippedRoots.Count > 0)
                Debug.Log("[CopyRuntimeObjects] Left out new root objects: " + string.Join(", ", skippedRoots));

            var gone = new List<string>();
            foreach (string path in known.Paths)
                if (!present.Contains(path) && owned.Contains(path.Split('/')[0])) gone.Add(path);
            if (gone.Count > 0)
                Debug.LogWarning("[CopyRuntimeObjects] These scene objects were gone at runtime; the code may have removed them:\n" + string.Join("\n", gone));
        }

        private static void Walk(Transform t, string path, HashSet<string> existing, HashSet<string> present, List<Node> nodes)
        {
            present.Add(path);
            bool inPrefab = PrefabUtility.IsPartOfPrefabInstance(t.gameObject);
            foreach ((Transform child, string childPath) in Children(t, path))
            {
                if (!existing.Contains(childPath))
                {
                    if (inPrefab || IsData(child)) continue;
                    nodes.Add(NodeFor(child, path, childPath));
                }
                Walk(child, childPath, existing, present, nodes);
            }
        }

        private static Node NodeFor(Transform t, string parentPath, string path)
        {
            var node = new Node
            {
                Path = path,
                ParentPath = parentPath,
                Name = t.name,
                Sibling = t.GetSiblingIndex(),
                Layer = t.gameObject.layer,
                Active = t.gameObject.activeSelf,
                LocalPosition = t.localPosition,
                LocalRotation = t.localRotation,
                LocalScale = t.localScale
            };
            if (t is RectTransform rt)
            {
                node.Rect = true;
                node.AnchorMin = rt.anchorMin;
                node.AnchorMax = rt.anchorMax;
                node.Pivot = rt.pivot;
                node.AnchoredPosition = rt.anchoredPosition;
                node.SizeDelta = rt.sizeDelta;
            }
            return node;
        }

        private static bool IsData(Transform t)
        {
            if ((t.gameObject.hideFlags & HideFlags.DontSave) != 0) return true;
            foreach (string prefix in DataPrefixes)
                if (t.name.StartsWith(prefix, StringComparison.Ordinal)) return true;
            return false;
        }

        private static bool HoldsOwnCode(GameObject root)
        {
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour != null && behaviour.GetType().Assembly == OwnAssembly) return true;
            return false;
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode) RememberScene();
            else if (change == PlayModeStateChange.EnteredEditMode) ApplyCapture();
        }

        private static void RememberScene()
        {
            UnityScene scene = SceneManager.GetActiveScene();
            var known = new SceneObjects { ScenePath = scene.path };
            known.Paths.AddRange(Index(scene).Keys);
            SessionState.SetString(SceneObjectsKey, JsonUtility.ToJson(known));
            SessionState.EraseString(CapturedKey);
        }

        private static void ApplyCapture()
        {
            string json = SessionState.GetString(CapturedKey, "");
            SessionState.EraseString(CapturedKey);
            if (string.IsNullOrEmpty(json)) return;

            var capture = JsonUtility.FromJson<Capture>(json);
            UnityScene scene = SceneManager.GetSceneByPath(capture.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError($"[CopyRuntimeObjects] {capture.ScenePath} is not open, so nothing was added.");
                return;
            }

            Dictionary<string, Transform> byPath = Index(scene);

            int added = 0;
            foreach (Node node in capture.Nodes)
            {
                if (byPath.ContainsKey(node.Path)) continue;
                if (!byPath.TryGetValue(node.ParentPath, out Transform parent))
                {
                    Debug.LogWarning($"[CopyRuntimeObjects] No parent at {node.ParentPath}; {node.Name} was not added.");
                    continue;
                }

                GameObject go = node.Rect ? new GameObject(node.Name, typeof(RectTransform)) : new GameObject(node.Name);
                Transform t = go.transform;
                t.SetParent(parent, false);
                t.SetSiblingIndex(Mathf.Min(node.Sibling, parent.childCount - 1));
                t.SetLocalPositionAndRotation(node.LocalPosition, node.LocalRotation);
                t.localScale = node.LocalScale;
                if (t is RectTransform rt)
                {
                    rt.anchorMin = node.AnchorMin;
                    rt.anchorMax = node.AnchorMax;
                    rt.pivot = node.Pivot;
                    rt.anchoredPosition = node.AnchoredPosition;
                    rt.sizeDelta = node.SizeDelta;
                }
                go.layer = node.Layer;
                go.SetActive(node.Active);
                UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Copy Runtime Objects Into Scene");
                byPath[node.Path] = t;
                added++;
            }

            if (added > 0) EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[CopyRuntimeObjects] Added {added} objects to {scene.name}. Save the scene to keep them.");
        }

        private static Dictionary<string, Transform> Index(UnityScene scene)
        {
            var byPath = new Dictionary<string, Transform>();
            foreach ((Transform root, string path) in Roots(scene)) Index(root, path, byPath);
            return byPath;
        }

        private static void Index(Transform t, string path, Dictionary<string, Transform> byPath)
        {
            byPath[path] = t;
            foreach ((Transform child, string childPath) in Children(t, path)) Index(child, childPath, byPath);
        }

        // A path part is the object's name, with #n added for the nth earlier sibling of the same name.
        private static List<(Transform, string)> Roots(UnityScene scene)
        {
            var twins = new Dictionary<string, int>();
            var roots = new List<(Transform, string)>();
            foreach (GameObject root in scene.GetRootGameObjects()) roots.Add((root.transform, PartOf(root.name, twins)));
            return roots;
        }

        private static List<(Transform, string)> Children(Transform t, string path)
        {
            var twins = new Dictionary<string, int>();
            var children = new List<(Transform, string)>(t.childCount);
            for (int i = 0; i < t.childCount; i++)
            {
                Transform child = t.GetChild(i);
                children.Add((child, path + "/" + PartOf(child.name, twins)));
            }
            return children;
        }

        private static string PartOf(string name, Dictionary<string, int> twins)
        {
            twins.TryGetValue(name, out int twin);
            twins[name] = twin + 1;
            return twin == 0 ? name : $"{name}#{twin}";
        }
    }
}
