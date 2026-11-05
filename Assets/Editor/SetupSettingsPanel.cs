using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetupSettingsPanel
{
    private const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Tools/Setup Settings Panel")]
    public static void Run()
    {
        UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath);

        GameObject tool = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Settings Panel")
            {
                Debug.Log("[SetupSettingsPanel] The scene already has a Settings Panel.");
                return;
            }
            if (root.name == "Tool Panel") tool = root;
        }
        if (tool == null) throw new System.Exception("No root 'Tool Panel' in the scene.");

        GameObject copy = Object.Instantiate(tool, tool.transform.parent);
        copy.name = "Settings Panel";
        copy.transform.SetSiblingIndex(tool.transform.GetSiblingIndex() + 1);

        ToolPanelUI old = copy.GetComponent<ToolPanelUI>();
        SettingsPanelUI panel = copy.AddComponent<SettingsPanelUI>();
        var so = new SerializedObject(panel);
        so.FindProperty("grabCollider").objectReferenceValue = copy.GetComponent<BoxCollider>();
        so.FindProperty("zOffsetFromCamera").floatValue = old.zOffsetFromCamera;
        so.ApplyModifiedPropertiesWithoutUndo();
        Object.DestroyImmediate(old);

        Transform grid = Find(copy.transform, "ToolGrid");
        grid.name = "SettingsGrid";
        Clear(grid);
        Clear(Find(copy.transform, "ContentArea"));

        Transform undoAll = Find(copy.transform, "UndoAll_Btn");
        if (undoAll != null) Object.DestroyImmediate(undoAll.gameObject);

        Transform canvas = copy.GetComponentInChildren<Canvas>(true).transform;
        canvas.name = "SettingsCanvas";

        TMP_Text title = Find(copy.transform, "Title").GetComponent<TMP_Text>();
        title.text = "Settings";

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new System.Exception("The scene could not be saved.");
        Debug.Log("[SetupSettingsPanel] Made the Settings Panel.");
    }

    private static Transform Find(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = Find(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    private static void Clear(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--) Object.DestroyImmediate(t.GetChild(i).gameObject);
    }
}
