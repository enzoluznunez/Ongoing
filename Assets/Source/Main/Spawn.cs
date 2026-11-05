using UnityEngine;

public static class Spawn
{
    public static void Once<T>(ref T instance) where T : MonoBehaviour
    {
        if (instance != null) return;
        var go = new GameObject(typeof(T).Name);
        Object.DontDestroyOnLoad(go);
        instance = go.AddComponent<T>();
    }
}
