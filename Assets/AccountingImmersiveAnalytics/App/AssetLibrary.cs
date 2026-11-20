using System.Collections.Generic;
using UnityEngine;

namespace AccountingImmersiveAnalytics.App
{
    [CreateAssetMenu(menuName = "Accounting Immersive Analytics/Asset Library", fileName = "AssetLibrary")]
    public sealed class AssetLibrary : ScriptableObject
    {
        [SerializeField] private Sprite[] _icons = new Sprite[0];
        [SerializeField] private Material _panelFront;
        [SerializeField] private Material _panelBack;
        [SerializeField] private Material _chartMarks;
        [SerializeField] private Material _rayLine;
        [SerializeField] private GameObject _pokeCanvas;

        private Dictionary<string, Sprite> _iconsByName;

        public static AssetLibrary Current { get; private set; }

        public Material PanelFront => _panelFront;
        public Material PanelBack => _panelBack;
        public Material ChartMarks => _chartMarks;
        public Material RayLine => _rayLine;
        public GameObject PokeCanvas => _pokeCanvas;

        public static void Use(AssetLibrary library) => Current = library;

        public static Sprite Icon(string name)
        {
            if (Current == null)
            {
                Debug.LogError($"[AssetLibrary] No asset library is in use, so the {name} icon can't be shown. Check the AssetLibraryLink on Systems in Main.unity.");
                return null;
            }
            return Current.FindIcon(name);
        }

        private Sprite FindIcon(string name)
        {
            if (_iconsByName == null)
            {
                _iconsByName = new Dictionary<string, Sprite>();
                foreach (Sprite icon in _icons)
                    if (icon != null) _iconsByName[icon.name] = icon;
            }
            if (_iconsByName.TryGetValue(name, out Sprite found)) return found;
            Debug.LogError($"[AssetLibrary] The asset library has no icon called {name}.");
            return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Current = null;
    }
}
