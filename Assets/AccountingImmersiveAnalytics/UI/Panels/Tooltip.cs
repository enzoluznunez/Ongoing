using System;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Text;
using AccountingImmersiveAnalytics.UI.Helpers;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Panels
{
    public class Tooltip : MonoBehaviour
    {
        private const float NoticeDistanceFromCamera = 0.5f;

        private Canvas _canvas;
        private RectTransform _canvasRect;
        private PanelCard _card;
        private string _body;

        private void Awake()
        {
            _canvas = GetComponentInChildren<Canvas>(true);
            _canvasRect = _canvas != null ? _canvas.transform as RectTransform : null;
            if (_canvas == null || _canvasRect == null) return;

            _canvas.sortingOrder = Style.SortNotices;
            _canvasRect.pivot = new Vector2(0.5f, 0f);
            _canvasRect.localPosition = Vector3.zero;

            Transform legacy = FindTransform.FindDeep(transform, "PanelBorder");
            if (legacy != null) legacy.gameObject.SetActive(false);

            _card = new PanelCard(_canvasRect, "Card", Style.NoticeWidth);
            _card.Root.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            TranslationSurface.Add(_card.Root, 0f, Style.LargeRadius);
            _card.Resized += () => _canvasRect.sizeDelta = new Vector2(Style.NoticeWidth, _card.Height);
            Texts.Refreshed += OnTextsRefreshed;

            _canvas.enabled = false;
        }

        private void OnDestroy() => Texts.Refreshed -= OnTextsRefreshed;

        private bool _shown;
        private bool _warming;
        private CanvasGroup _warmGroup;

        public void PrewarmCanvas(bool on)
        {
            if (_canvas == null || on == _warming) return;
            _warming = on;
            if (_warmGroup == null) _warmGroup = LayoutTools.Ensure<CanvasGroup>(_canvas.gameObject);
            _warmGroup.alpha = on && !_shown ? 0f : 1f;
            ApplyShown();
        }

        private void ApplyShown()
        {
            if (_canvas != null) _canvas.enabled = _shown || _warming;
        }

        public void ShowNotice(string body)
        {
            _body = body;
            Build();
            Present(NoticeAnchor());
        }

        private void OnTextsRefreshed()
        {
            if (_shown) Build();
        }

        private void Build()
        {
            if (_card == null) return;
            string body = _body;
            Func<string> text = () => Numbers.Text(body);
            _card.Begin(Style.NoticeWidth);
            RectTransform row = _card.Row(_card.RowFor(text, _card.InnerWidth));
            _card.Label(row, text, 0f, _card.InnerWidth, LabelAnchor.Start, wrap: true);
            _card.End();
            _card.Root.anchoredPosition = Vector2.zero;
            _canvasRect.sizeDelta = new Vector2(Style.NoticeWidth, _card.Height);
        }

        private Vector3 NoticeAnchor()
        {
            Transform cam = CameraRig.MainTransform;
            if (cam == null) return transform.position;

            return cam.position + CameraRig.FlatForward * NoticeDistanceFromCamera;
        }

        public void DismissNotice()
        {
            _shown = false;
            ApplyShown();
        }

        public bool IsVisible => _canvas != null && _shown;

        public void UpdatePosition(Vector3 anchorPoint)
        {
            if (CameraRig.MainTransform == null) return;

            transform.position = anchorPoint + Vector3.up * Style.AnchorOffset;
            FaceViewer();
        }

        private void FaceViewer()
        {
            if (CameraRig.TryFaceViewer(transform.position, out Quaternion rotation))
                transform.rotation = rotation;
        }

        private void LateUpdate()
        {
            if (IsVisible) FaceViewer();
        }

        private void Present(Vector3 worldHitPoint)
        {
            UpdatePosition(worldHitPoint);
            _shown = true;
            if (_warmGroup != null) _warmGroup.alpha = 1f;
            ApplyShown();
        }
    }
}
