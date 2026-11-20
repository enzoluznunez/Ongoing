using UnityEngine;

namespace AccountingImmersiveAnalytics.App
{
    [DefaultExecutionOrder(-10000)]
    public sealed class AssetLibraryLink : MonoBehaviour
    {
        [SerializeField] private AssetLibrary _library;

        private void Awake()
        {
            if (_library == null) Debug.LogError("[AssetLibraryLink] No asset library is assigned, so icons and materials won't load.");
            AssetLibrary.Use(_library);
        }
    }
}
