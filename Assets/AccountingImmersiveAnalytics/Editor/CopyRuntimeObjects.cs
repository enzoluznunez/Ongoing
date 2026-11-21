using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
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
            public string ParentId;
            public int ParentNode = -1;
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
        private class KnownObject
        {
            public string Id;
            public string RootId;
            public string Path;
        }

        [Serializable]
        private class SceneObjects
        {
            public string ScenePath;
            public List<KnownObject> Objects = new List<KnownObject>();
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

            Dictionary<Transform, string> ids = Ids(scene);
            var existing = new HashSet<string>();
            foreach (KnownObject entry in known.Objects) existing.Add(entry.Id);
            var present = new HashSet<string>();
            foreach (string id in ids.Values)
                if (id != null && existing.Contains(id)) present.Add(id);
            if (existing.Count > 0 && present.Count == 0)
            {
                Debug.LogError("[CopyRuntimeObjects] None of the scene's objects could be identified in Play mode, so nothing was copied.");
                return;
            }

            var capture = new Capture { ScenePath = scene.path };
            var owned = new HashSet<string>();
            var skippedRoots = new List<string>();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                string id = ids[root.transform];
                if (id == null || !existing.Contains(id))
                {
                    skippedRoots.Add(root.name);
                    continue;
                }
                if (!HoldsOwnCode(root)) continue;
                owned.Add(id);
                Walk(root.transform, id, -1, ids, existing, capture.Nodes);
            }

            SessionState.SetString(CapturedKey, JsonUtility.ToJson(capture));

            Debug.Log($"[CopyRuntimeObjects] Found {capture.Nodes.Count} objects the code made. Stop Play to add them to {scene.name}.");
            if (skippedRoots.Count > 0)
                Debug.Log("[CopyRuntimeObjects] Left out new root objects: " + string.Join(", ", skippedRoots));

            var gone = new List<string>();
            foreach (KnownObject entry in known.Objects)
                if (owned.Contains(entry.RootId) && !present.Contains(entry.Id)) gone.Add(entry.Path);
            if (gone.Count > 0)
                Debug.LogWarning("[CopyRuntimeObjects] These scene objects were gone at runtime; the code may have removed them:\n" + string.Join("\n", gone));
        }

        private static void Walk(Transform t, string id, int node, Dictionary<Transform, string> ids, HashSet<string> existing, List<Node> nodes)
        {
            bool inPrefab = PrefabUtility.IsPartOfPrefabInstance(t.gameObject);
            for (int i = 0; i < t.childCount; i++)
            {
                Transform child = t.GetChild(i);
                string childId = ids[child];
                int childNode = -1;
                if (childId == null || !existing.Contains(childId))
                {
                    if (inPrefab || IsData(child)) continue;
                    childNode = nodes.Count;
                    nodes.Add(NodeFor(child, node < 0 ? id : null, node));
                }
                Walk(child, childId, childNode, ids, existing, nodes);
            }
        }

        private static Node NodeFor(Transform t, string parentId, int parentNode)
        {
            var node = new Node
            {
                ParentId = parentId,
                ParentNode = parentNode,
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
            if ((t.gameObject.hideFlags & HideFlags.DontSaveInEditor) != 0) return true;
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
            Dictionary<Transform, string> ids = Ids(scene);
            foreach (KeyValuePair<Transform, string> pair in ids)
            {
                if (pair.Value == null) continue;
                known.Objects.Add(new KnownObject { Id = pair.Value, RootId = ids[pair.Key.root], Path = PathOf(pair.Key) });
            }
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

            Dictionary<string, Transform> parents = SceneParents(capture.Nodes);
            var made = new Transform[capture.Nodes.Count];

            int added = 0;
            for (int i = 0; i < capture.Nodes.Count; i++)
            {
                Node node = capture.Nodes[i];
                Transform parent = node.ParentNode >= 0
                    ? made[node.ParentNode]
                    : node.ParentId != null && parents.TryGetValue(node.ParentId, out Transform found) ? found : null;
                if (parent == null || parent.gameObject.scene != scene)
                {
                    Debug.LogWarning($"[CopyRuntimeObjects] The parent of {node.Name} is no longer in the scene; it was not added.");
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
                made[i] = t;
                added++;
            }

            if (added > 0) EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[CopyRuntimeObjects] Added {added} objects to {scene.name}. Save the scene to keep them.");
        }

        private static Dictionary<string, Transform> SceneParents(List<Node> nodes)
        {
            var wanted = new List<string>();
            var parsed = new List<GlobalObjectId>();
            var seen = new HashSet<string>();
            foreach (Node node in nodes)
            {
                if (node.ParentNode >= 0 || string.IsNullOrEmpty(node.ParentId) || !seen.Add(node.ParentId)) continue;
                if (!GlobalObjectId.TryParse(node.ParentId, out GlobalObjectId id)) continue;
                wanted.Add(node.ParentId);
                parsed.Add(id);
            }

            var objects = new Object[parsed.Count];
            GlobalObjectId.GlobalObjectIdentifiersToObjectsSlow(parsed.ToArray(), objects);
            var parents = new Dictionary<string, Transform>();
            for (int i = 0; i < objects.Length; i++)
                if (objects[i] is GameObject go) parents[wanted[i]] = go.transform;
            return parents;
        }

        private static Dictionary<Transform, string> Ids(UnityScene scene)
        {
            var transforms = new List<Transform>();
            foreach (GameObject root in scene.GetRootGameObjects())
                transforms.AddRange(root.GetComponentsInChildren<Transform>(true));

            var objects = new Object[transforms.Count];
            for (int i = 0; i < transforms.Count; i++) objects[i] = transforms[i].gameObject;
            var gids = new GlobalObjectId[objects.Length];
            GlobalObjectId.GetGlobalObjectIdsSlow(objects, gids);

            var ids = new Dictionary<Transform, string>(transforms.Count);
            for (int i = 0; i < transforms.Count; i++)
                ids[transforms[i]] = gids[i].targetObjectId == 0 ? null : gids[i].ToString();
            return ids;
        }

        private static string PathOf(Transform t)
        {
            string path = t.name;
            for (Transform parent = t.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;
            return path;
        }
    }
}
