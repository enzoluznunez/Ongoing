using System;
using System.Collections;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Text;
using AccountingImmersiveAnalytics.UI.Helpers;
using OneText;
using OneText.UGUI;
using UnityEngine;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Panels
{
    public class Tooltip : MonoBehaviour
    {
        private Coroutine _settle;

        private const float UpOffsetFromAnchor = 0.05f;
        private const float NoticeDistanceFromCamera = 0.5f;

        private const float Inset = Style.LargePad;

        private Canvas _canvas;
        private RectTransform _canvasRect;

        private GameObject _hintGroup;
        private OneTextLabel _titleLabel;
        private OneTextLabel _bodyLabel;

        private void Awake()
        {
            _canvas = GetComponentInChildren<Canvas>(true);
            _canvasRect = _canvas != null ? _canvas.transform as RectTransform : null;

            if (_canvas != null) _canvas.sortingOrder = Style.SortNotices;

            if (_canvasRect != null)
            {
                _canvasRect.sizeDelta = new Vector2(Style.TooltipWidth, _canvasRect.sizeDelta.y);
                _canvasRect.pivot = new Vector2(0.5f, 0f);
                _canvasRect.localPosition = Vector3.zero;
            }

            _hintGroup = FindChild("HintGroup");
            _titleLabel = FindComponent<OneTextLabel>("HintTitle");
            _bodyLabel = FindComponent<OneTextLabel>("HintBody");

            Panel.ApplyPanelBorders(transform);
            Panel.ApplyPanelSprites(transform);

            if (_titleLabel != null) _titleLabel.gameObject.SetActive(false);
            EnableWrap(_bodyLabel);
            PlaceBody();

            ApplyFonts();

            if (_canvas != null) _canvas.enabled = false;
        }

        private void ApplyFonts() => Style.ApplyBody(_bodyLabel);

        private void PlaceBody()
        {
            if (_bodyLabel == null) return;
            RectTransform body = _bodyLabel.rectTransform;
            body.anchorMin = new Vector2(0f, 1f);
            body.anchorMax = new Vector2(1f, 1f);
            body.pivot = new Vector2(0.5f, 1f);
            body.offsetMin = new Vector2(Inset, body.offsetMin.y);
            body.offsetMax = new Vector2(-Inset, body.offsetMax.y);
            body.anchoredPosition = new Vector2(0f, -Inset);
        }

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

        private static void EnableWrap(OneTextLabel label)
        {
            if (label == null) return;
            label.Wrap = TextWrap.Wrap;
            label.Overflow = TextOverflow.Overflow;
        }

        public void ShowNotice(string body)
        {
            StopSettle();

            if (_hintGroup != null) _hintGroup.SetActive(true);

            if (_bodyLabel != null)
            {
                Texts.Bind(_bodyLabel, () => Numbers.Text(body));
                PlaceBody();
            }

            Present(NoticeAnchor());
            SetPanelHeight(BodyHeight());

            if (!isActiveAndEnabled) return;
            _settle = StartCoroutine(SettleHeight(_hintGroup, BodyHeight));
        }

        private IEnumerator SettleHeight(GameObject group, Func<float> height)
        {
            yield return LayoutTools.Converge(
                () => _shown && group != null && group.activeInHierarchy,
                () => SetPanelHeight(height()),
                () => _canvasRect != null ? _canvasRect.sizeDelta.y : -1f);

            _settle = null;
        }

        private void StopSettle()
        {
            if (_settle != null) StopCoroutine(_settle);
            _settle = null;
        }

        private void OnDisable() => _settle = null;

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

        private float BodyHeight()
        {
            if (_bodyLabel == null) return Inset * 2f;

            if (_canvasRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(_canvasRect);
            _bodyLabel.rectTransform.sizeDelta = new Vector2(_bodyLabel.rectTransform.sizeDelta.x, _bodyLabel.preferredHeight);
            return Inset + _bodyLabel.preferredHeight + Inset;
        }

        private void SetPanelHeight(float height)
        {
            if (_canvasRect != null)
                _canvasRect.sizeDelta = new Vector2(_canvasRect.sizeDelta.x, height);
        }

        public bool IsVisible => _canvas != null && _shown;


        public void UpdatePosition(Vector3 anchorPoint)
        {
            if (CameraRig.MainTransform == null) return;

            transform.position = anchorPoint + Vector3.up * UpOffsetFromAnchor;
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

        private T FindComponent<T>(string name) where T : Component
        {
            Transform t = FindTransform.FindDeep(transform, name);
            return t != null ? t.GetComponent<T>() : null;
        }

        private GameObject FindChild(string name)
        {
            Transform t = FindTransform.FindDeep(transform, name);
            return t != null ? t.gameObject : null;
        }

    }
}
