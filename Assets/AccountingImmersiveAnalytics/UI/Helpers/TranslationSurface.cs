using System.Collections.Generic;
using AccountingImmersiveAnalytics.Text;
using OneText;
using OneText.UGUI;
using UnityEngine;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Helpers
{
    public sealed class TranslationSurface : MonoBehaviour
    {
        private const string CoverName = "TranslationCover";

        private readonly HashSet<Component> _waiting = new HashSet<Component>();
        private float _inset;
        private float _radius;
        private RectTransform _cover;
        private OneTextLabel _label;
        private string _shown;

        public static void Add(RectTransform surface, float inset = 0f, float radius = 0f)
        {
            if (surface == null) return;
            TranslationSurface added = LayoutTools.Ensure<TranslationSurface>(surface.gameObject);
            added._inset = inset;
            added._radius = radius;
        }

        public void SetWaiting(Component label, bool waiting)
        {
            if (waiting) _waiting.Add(label);
            else _waiting.Remove(label);
            Apply();
        }

        private void LateUpdate() => Apply();

        private void Apply()
        {
            _waiting.RemoveWhere(label => label == null);
            bool show = false;
            foreach (Component label in _waiting)
                if (label.gameObject.activeInHierarchy) { show = true; break; }

            if (show && _cover == null) Build();
            if (_cover == null) return;
            if (_cover.gameObject.activeSelf != show)
            {
                _cover.gameObject.SetActive(show);
                _shown = null;
            }
            if (!show) return;
            if (_cover.GetSiblingIndex() != transform.childCount - 1) _cover.SetAsLastSibling();

            (string text, string language) = Loading.Steady();
            if (text == _shown) return;
            _shown = text;
            _label.Language = language;
            _label.Text = text;
        }

        private void Build()
        {
            _cover = LayoutTools.EnsureChild(transform, CoverName);
            LayoutTools.Stretch(_cover, _inset);
            LayoutTools.Ensure<LayoutElement>(_cover.gameObject).ignoreLayout = true;

            Image fill = LayoutTools.Ensure<Image>(_cover.gameObject);
            if (_radius > 0f) RoundedSprite.Apply(fill, _radius);
            fill.color = Style.Card;
            fill.raycastTarget = true;
            Image own = GetComponent<Image>();
            if (own != null) fill.material = own.material;

            _label = LabelFactory.Make(_cover, "Loading", Style.Ink, LabelAnchor.Center, TextOverflow.Overflow);
            Style.ApplyBody(_label);
            _label.RichText = true;
            _label.raycastTarget = false;
            LayoutTools.Stretch(_label.rectTransform);
            _cover.gameObject.SetActive(false);
        }
    }
}
