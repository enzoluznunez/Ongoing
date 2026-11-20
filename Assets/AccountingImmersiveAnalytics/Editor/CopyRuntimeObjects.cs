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
        private const string OwnAssembly = "Study.Runtime";

        private static readonly string[] DataPrefixes =
        {
            "Slot ", "Plank_", "Dataset_", "Option_", "Step_", "Paragraph_", "TMP SubMesh", "PrewarmRig", "PanelCapture"
        };

        [Serializable]
        private class Node
        {
            public string path;
            public string parentPath;
            public string name;
            public int sibling;
            public int layer;
            public bool active;
            public bool rect;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
            public Vector2 anchorMin;
            public Vector2 anchorMax;
            public Vector2 pivot;
            public Vector2 anchoredPosition;
            public Vector2 sizeDelta;
        }

        [Serializable]
        private class Capture
        {
            public string scenePath;
            public List<Node> nodes = new List<Node>();
        }

        [Serializable]
        private class SceneObjects
        {
            public string scenePath;
            public List<string> paths = new List<string>();
        }

        static CopyRuntimeObjects() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        [MenuItem(MenuPath, true)]
        private static bool CanCopy() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        private static void Copy()
        {
            var known = JsonUtility.FromJson<SceneObjects>(SessionState.GetString(SceneObjectsKey, "{}"));
            UnityScene scene = SceneManager.GetActiveScene();
            if (known == null || known.scenePath != scene.path)
            {
                Debug.LogError("[CopyRuntimeObjects] The scene was not recorded when Play began. Stop, then press Play again.");
                return;
            }

            var existing = new HashSet<string>(known.paths);
            var capture = new Capture { scenePath = scene.path };
            var present = new HashSet<string>();
            var skippedRoots = new List<string>();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                string path = PathOf(root.transform);
                if (!existing.Contains(path))
                {
                    skippedRoots.Add(root.name);
                    continue;
                }
                if (!HoldsOwnCode(root)) continue;
                Walk(root.transform, path, existing, present, capture.nodes);
            }

            SessionState.SetString(CapturedKey, JsonUtility.ToJson(capture));

            Debug.Log($"[CopyRuntimeObjects] Found {capture.nodes.Count} objects the code made. Stop Play to add them to {scene.name}.");
            if (skippedRoots.Count > 0)
                Debug.Log("[CopyRuntimeObjects] Left out new root objects: " + string.Join(", ", skippedRoots));

            var gone = new List<string>();
            foreach (string path in known.paths)
                if (!present.Contains(path) && OwnedRootOf(path, scene)) gone.Add(path);
            if (gone.Count > 0)
                Debug.LogWarning("[CopyRuntimeObjects] These scene objects were gone at runtime; the code may have removed them:\n" + string.Join("\n", gone));
        }

        private static void Walk(Transform t, string path, HashSet<string> existing, HashSet<string> present, List<Node> nodes)
        {
            present.Add(path);
            for (int i = 0; i < t.childCount; i++)
            {
                Transform child = t.GetChild(i);
                string childPath = path + "/" + PartOf(child);
                if (!existing.Contains(childPath))
                {
                    if (IsData(child) || PrefabUtility.IsPartOfPrefabInstance(t.gameObject)) continue;
                    nodes.Add(NodeFor(child, path, childPath));
                }
                Walk(child, childPath, existing, present, nodes);
            }
        }

        private static Node NodeFor(Transform t, string parentPath, string path)
        {
            var node = new Node
            {
                path = path,
                parentPath = parentPath,
                name = t.name,
                sibling = t.GetSiblingIndex(),
                layer = t.gameObject.layer,
                active = t.gameObject.activeSelf,
                localPosition = t.localPosition,
                localRotation = t.localRotation,
                localScale = t.localScale
            };
            if (t is RectTransform rt)
            {
                node.rect = true;
                node.anchorMin = rt.anchorMin;
                node.anchorMax = rt.anchorMax;
                node.pivot = rt.pivot;
                node.anchoredPosition = rt.anchoredPosition;
                node.sizeDelta = rt.sizeDelta;
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
                if (behaviour != null && behaviour.GetType().Assembly.GetName().Name == OwnAssembly) return true;
            return false;
        }

        private static bool OwnedRootOf(string path, UnityScene scene)
        {
            string rootPart = path.Split('/')[0];
            foreach (GameObject root in scene.GetRootGameObjects())
                if (PathOf(root.transform) == rootPart) return HoldsOwnCode(root);
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
            var known = new SceneObjects { scenePath = scene.path };
            foreach (GameObject root in scene.GetRootGameObjects()) Collect(root.transform, PathOf(root.transform), known.paths);
            SessionState.SetString(SceneObjectsKey, JsonUtility.ToJson(known));
            SessionState.EraseString(CapturedKey);
        }

        private static void Collect(Transform t, string path, List<string> paths)
        {
            paths.Add(path);
            for (int i = 0; i < t.childCount; i++)
            {
                Transform child = t.GetChild(i);
                Collect(child, path + "/" + PartOf(child), paths);
            }
        }

        private static void ApplyCapture()
        {
            string json = SessionState.GetString(CapturedKey, "");
            SessionState.EraseString(CapturedKey);
            if (string.IsNullOrEmpty(json)) return;

            var capture = JsonUtility.FromJson<Capture>(json);
            UnityScene scene = SceneManager.GetSceneByPath(capture.scenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError($"[CopyRuntimeObjects] {capture.scenePath} is not open, so nothing was added.");
                return;
            }

            var byPath = new Dictionary<string, Transform>();
            foreach (GameObject root in scene.GetRootGameObjects()) Index(root.transform, PathOf(root.transform), byPath);

            int added = 0;
            foreach (Node node in capture.nodes)
            {
                if (byPath.ContainsKey(node.path)) continue;
                if (!byPath.TryGetValue(node.parentPath, out Transform parent))
                {
                    Debug.LogWarning($"[CopyRuntimeObjects] No parent at {node.parentPath}; {node.name} was not added.");
                    continue;
                }

                GameObject go = node.rect ? new GameObject(node.name, typeof(RectTransform)) : new GameObject(node.name);
                Transform t = go.transform;
                t.SetParent(parent, false);
                t.SetSiblingIndex(Mathf.Min(node.sibling, parent.childCount - 1));
                t.SetLocalPositionAndRotation(node.localPosition, node.localRotation);
                t.localScale = node.localScale;
                if (t is RectTransform rt)
                {
                    rt.anchorMin = node.anchorMin;
                    rt.anchorMax = node.anchorMax;
                    rt.pivot = node.pivot;
                    rt.anchoredPosition = node.anchoredPosition;
                    rt.sizeDelta = node.sizeDelta;
                }
                go.layer = node.layer;
                go.SetActive(node.active);
                UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Copy Runtime Objects Into Scene");
                byPath[node.path] = t;
                added++;
            }

            if (added > 0) EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[CopyRuntimeObjects] Added {added} objects to {scene.name}. Save the scene to keep them.");
        }

        private static void Index(Transform t, string path, Dictionary<string, Transform> byPath)
        {
            byPath[path] = t;
            for (int i = 0; i < t.childCount; i++)
            {
                Transform child = t.GetChild(i);
                Index(child, path + "/" + PartOf(child), byPath);
            }
        }

        private static string PathOf(Transform t)
        {
            if (t.parent != null) return PathOf(t.parent) + "/" + PartOf(t);
            int twin = 0;
            foreach (GameObject root in t.gameObject.scene.GetRootGameObjects())
            {
                if (root.transform == t) break;
                if (root.name == t.name) twin++;
            }
            return twin == 0 ? t.name : $"{t.name}#{twin}";
        }

        private static string PartOf(Transform t)
        {
            int twin = 0;
            Transform parent = t.parent;
            for (int i = 0; i < t.GetSiblingIndex(); i++)
                if (parent.GetChild(i).name == t.name) twin++;
            return twin == 0 ? t.name : $"{t.name}#{twin}";
        }
    }
}
