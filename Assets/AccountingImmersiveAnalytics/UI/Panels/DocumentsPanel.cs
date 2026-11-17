using System.Collections.Generic;
using System.Threading.Tasks;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Assistant;
using AccountingImmersiveAnalytics.Assistant.Session;
using AccountingImmersiveAnalytics.Data;
using AccountingImmersiveAnalytics.Documents;
using AccountingImmersiveAnalytics.Text;
using AccountingImmersiveAnalytics.UI.Helpers;
using AccountingImmersiveAnalytics.UI.Interactions;
using OneText;
using OneText.UGUI;
using UnityEngine;
using UnityEngine.UI;

namespace AccountingImmersiveAnalytics.UI.Panels
{
    public class DocumentsPanel : ModularPanel
    {
        private const float LetterWidthPoints = 612f, LetterHeightPoints = 792f;
        private static float UnitsPerPoint => Style.Panel.x / LetterWidthPoints;
        private static float LetterWidth => Style.Panel.x;
        private static float LetterBody => Mathf.Ceil(LetterHeightPoints * UnitsPerPoint);
        private float _bodyWidth = Style.Panel.x;
        private float _bodyHeight = Mathf.Ceil(LetterHeightPoints * Style.Panel.x / LetterWidthPoints);
        private float BodyHeight => _bodyHeight;
        protected override float PanelWidth => _bodyWidth;
        private const float PageGap = Style.SmallPad;
        private const int VisibilityEveryFrames = 8;
        private const float KeepScreens = 2f;

        private enum View { Message, Pages }

        private sealed class Sheet
        {
            public RectTransform Root;
            public RawImage Picture;
            public Texture2D Texture;
            public bool Loading;
            public bool Failed;
        }

        protected override float PanelHeight => Panel.TitleBarHeight + Panel.SplitGap + BodyHeight;

        private OneTextLabel _message;
        private string _messageKey;
        private bool _messageDots;
        private int _shownDots = -1;

        private ScrollRect _scroll;
        private RectTransform _column;
        private RectTransform _messageArea;
        private RectTransform _content;
        private OneTextLabel _meta;
        private OneTextLabel _status;
        private GameObject _summaryBox;
        private OneTextLabel _summaryText;
        private bool _summaryWriting;
        private readonly List<Sheet> _sheets = new List<Sheet>();
        private readonly List<Image> _bodyFills = new List<Image>();

        private DocumentFile _page;
        private DocumentEntry _csv;
        private PdfPages _renderer;
        private DocumentEntry _opening;
        private int _ticket;

        public DocumentFile Page => _page;



        public bool IsLoading => _opening != null;

        private void Awake()
        {
            BuildPanel("panel.documents", "DocumentsGrid");
            Body.gameObject.SetActive(false);
            FindBodyFills();
            BuildMessage();
            BuildColumn();
            _ = DocumentLibrary.Load();
            ShowEmpty();
            if (_canvas != null) StartHidden();
        }

        private void Start()
        {
            ResolveLayoutNow();
            RefreshPanel();
            _ = OpenFirst();
        }

        private async Task OpenFirst()
        {
            await DocumentLibrary.Load();
            if (this == null || _page != null || _opening != null || _csv != null) return;
            IReadOnlyList<DocumentEntry> all = DocumentLibrary.All;
            if (all.Count > 0) await Open(all[0], false);
            else ShowEmpty();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ForgetPages();
        }

        private void FindBodyFills()
        {
            foreach (string name in new[] { "BodyFill", "PanelBackBody" })
                foreach (Transform fill in FindTransform.FindAllDeep(transform, name))
                    if (fill.TryGetComponent(out Image image)) _bodyFills.Add(image);
        }

        private RectTransform BodyRect(string name)
        {
            RectTransform rt = LayoutTools.EnsureChild(PrimaryRect, name);
            LayoutTools.Ensure<LayoutElement>(rt.gameObject).ignoreLayout = true;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(0f, -(Panel.TitleBarHeight + Panel.SplitGap));
            rt.sizeDelta = new Vector2(PanelWidth, BodyHeight);
            rt.SetAsLastSibling();
            return rt;
        }

        private void BuildMessage()
        {
            RectTransform area = _messageArea = BodyRect("Message");
            _message = LabelFactory.Make(area, "Text", Style.Ink, LabelAnchor.Center, TextOverflow.Overflow, bold: true, wrap: true);
            LayoutTools.Stretch(_message.rectTransform, Style.LargePad);
        }

        private void BuildColumn()
        {
            _column = BodyRect("Pages");
            _scroll = GlideScroll.Column(_column, PageGap);
            _content = _scroll.content;
            Panel.ApplyFront(_scroll.viewport.GetComponent<Image>());
            ListMotion.Attach(_scroll, "document", "page", false, false, SheetRoots, DescribePage);

            BuildSummary();
        }

        private void BuildSummary()
        {
            RectTransform card = LayoutTools.EnsureChild(_content, "Summary");
            Image fill = LayoutTools.Ensure<Image>(card.gameObject);
            RoundedSprite.Apply(fill, Style.LargeRadius);
            fill.color = Style.Card;
            fill.raycastTarget = false;
            VerticalLayoutGroup layout = LayoutTools.Ensure<VerticalLayoutGroup>(card.gameObject);
            int pad = (int)Style.LargePad;
            layout.padding = new RectOffset(pad, pad, pad, pad);
            layout.spacing = Style.SmallPad;
            LayoutTools.Stack(layout);

            _meta = Text(card, "Meta", Style.SmallText);
            _meta.color = Style.Muted;
            _status = Text(card, "Status", Style.MediumText);
            _status.color = Style.Muted;

            RectTransform box = LayoutTools.EnsureChild(card, "Ai");
            _summaryBox = box.gameObject;
            Image boxFill = LayoutTools.Ensure<Image>(_summaryBox);
            RoundedSprite.Apply(boxFill, Style.MediumRadius);
            boxFill.color = Style.AiFill;
            boxFill.raycastTarget = false;
            VerticalLayoutGroup boxLayout = LayoutTools.Ensure<VerticalLayoutGroup>(_summaryBox);
            boxLayout.padding = new RectOffset((int)Style.MediumPad, (int)Style.MediumPad, (int)Style.SmallPad, (int)Style.SmallPad);
            LayoutTools.Stack(boxLayout);
            _summaryText = Text(box, "Text", Style.MediumText);
        }

        private static OneTextLabel Text(Transform parent, string name, float size)
        {
            OneTextLabel label = LabelFactory.Make(parent, name, Style.Ink, LabelAnchor.TopStart, TextOverflow.Overflow, wrap: true);
            label.FontSize = size;
            return label;
        }

        private void Show(View view)
        {
            bool pages = view == View.Pages;
            if (pages) _messageDots = false;
            foreach (Image fill in _bodyFills) fill.enabled = !pages;
            _message.transform.parent.gameObject.SetActive(!pages);
            _column.gameObject.SetActive(pages);
        }

        private void ShowMessage(string key, bool dots = false)
        {
            _messageKey = key;
            _messageDots = dots;
            _shownDots = -1;
            BindMessage();
            Show(View.Message);
        }

        private void BindMessage()
        {
            string key = _messageKey;
            if (_messageDots)
            {
                int dots = Style.Dots;
                _shownDots = dots;
                Texts.Bind(_message, () => Style.Counting(Strings.Get(key)));
            }
            else Texts.Bind(_message, () => Strings.Get(key));
        }

        private async Task<DocumentOutcome> OpenCsv(DocumentEntry entry, DocumentOutcome outcome)
        {
            ShowMessage("documents.notShown");
            StateChannel.RecordState("documentsPanel",
                $"the Documents panel has \"{entry.Title}\" open; it is a CSV file, so the panel says it could not be displayed and its charts stand in the column beside it");
            UI.Charts.ChartManager charts = Scene.Charts;
            if (charts == null)
            {
                outcome.Failure = "The chart manager was not found in the scene.";
                return outcome;
            }
            var (made, failure, unreadable) = await charts.OpenCsv(entry);
            outcome.Charts = made;
            outcome.Failure = failure;
            if (unreadable)
            {
                ShowMessage("documents.failed");
                charts.CloseColumn();
                StateChannel.RecordState("documentsPanel",
                    $"the Documents panel has \"{entry.Title}\" open; it is a CSV file with blank or unreadable cells, so the panel says it could not be loaded and no charts were made");
            }
            return outcome;
        }

        public async Task<DocumentOutcome> Open(DocumentEntry entry, bool reveal = true)
        {
            var outcome = new DocumentOutcome();
            if (reveal) ShowPanel();
            Clear();
            int ticket = ++_ticket;
            SetTitle(() => Numbers.Text(entry.Title));
            if (entry.IsCsv)
            {
                _csv = entry;
                return await OpenCsv(entry, outcome);
            }
            _opening = entry;
            ShowMessage("documents.loading", true);
            Scene.Charts?.CloseColumn();

            byte[] bytes = await StreamingFiles.ReadBytes(entry.File);
            DocumentFile page = null;
            PdfPages renderer = null;
            string problem = null;
            string shownKey = "documents.failed";
            if (bytes == null || bytes.Length == 0)
                problem = $"The file StreamingAssets/{entry.File} could not be read from the app.";
            else
            {
                try
                {
                    page = await Task.Run(() => PdfDocumentReader.Read(bytes, entry));
                    if (page == null || page.Pages.Count == 0) problem = "The PDF reader found no pages in this file.";
                }
                catch (System.Exception e)
                {
                    problem = $"The PDF reader could not read this file ({e.GetType().Name}: {e.Message}).";
                    Debug.LogException(e);
                }
            }
            if (problem == null)
            {
                shownKey = "documents.notShown";
                if (!PdfPages.Supported) problem = "PDF pages can only be drawn on the headset, not in the Unity editor.";
                else
                {
                    try
                    {
                        renderer = await PdfPages.Open(bytes);
                        if (renderer == null) problem = "Android's PDF renderer could not open this file.";
                    }
                    catch (System.Exception e)
                    {
                        problem = $"Android's PDF renderer failed ({e.GetType().Name}: {e.Message}).";
                        Debug.LogException(e);
                    }
                }
            }
            if (problem != null) Debug.LogError($"[DocumentsPanel] \"{entry.Title}\" could not be shown: {problem}");
            if (this == null || ticket != _ticket)
            {
                renderer?.Dispose();
                outcome.Failure = "Another document was opened before this one finished loading.";
                return outcome;
            }
            _opening = null;
            if (page == null || renderer == null)
            {
                renderer?.Dispose();
                ShowMessage(shownKey);
                outcome.Failure = $"\"{entry.Title}\" could not be shown: {problem ?? "The reason is unknown."}";
                return outcome;
            }

            outcome.Page = page;
            ShowPages(page, renderer);
            WriteSummary(page, ticket);
            return outcome;
        }

        private void Clear()
        {
            _csv = null;
            _page = null;
            _opening = null;
            ForgetPages();
            SetBodySize(LetterWidth, LetterBody);
            SetTitle(() => "");
            SetSummary(false, null);
        }

        private void ShowEmpty()
        {
            Clear();
            ShowMessage("documents.empty");
        }

        private void ShowPages(DocumentFile page, PdfPages renderer)
        {
            _page = page;
            _renderer = renderer;
            float widest = 0f, tallest = 0f;
            for (int i = 0; i < page.Pages.Count; i++)
            {
                DocumentPage size = page.Pages[i];
                float width = Mathf.Max(1f, (float)size.Width * UnitsPerPoint), height = Mathf.Max(1f, (float)size.Height * UnitsPerPoint);
                widest = Mathf.Max(widest, width);
                tallest = Mathf.Max(tallest, height);
                _sheets.Add(MakeSheet(i, width, height));
            }
            SetBodySize(Mathf.Max(LetterWidth, Mathf.Ceil(widest)), Mathf.Max(LetterBody, Mathf.Ceil(tallest)));
            Texts.Bind(_meta, () => MetaLine(page));
            Show(View.Pages);
            Settle(true);
            StateChannel.RecordState("documentsPanel", $"the Documents panel shows \"{page.Title}\"");
        }

        private void SetBodySize(float width, float height)
        {
            if (Mathf.Approximately(width, _bodyWidth) && Mathf.Approximately(height, _bodyHeight)) return;
            _bodyWidth = width;
            _bodyHeight = height;
            foreach (RectTransform area in new[] { _messageArea, _column })
                if (area != null) area.sizeDelta = new Vector2(width, height);
            QueueSettle();
            PanelLineup.Resize(this);
        }

        private Sheet MakeSheet(int index, float width, float height)
        {
            RectTransform root = LayoutTools.EnsureChild(_content, $"Page_{index + 1}");
            LayoutElement size = LayoutTools.Ensure<LayoutElement>(root.gameObject);
            size.preferredHeight = size.minHeight = height;

            RectTransform sheet = LayoutTools.EnsureChild(root, "Sheet");
            sheet.anchorMin = sheet.anchorMax = sheet.pivot = new Vector2(0.5f, 1f);
            sheet.anchoredPosition = Vector2.zero;
            sheet.sizeDelta = new Vector2(width, height);
            Image card = LayoutTools.Ensure<Image>(sheet.gameObject);
            RoundedSprite.Apply(card, Style.LargeRadius);
            card.color = Style.Card;
            card.raycastTarget = false;
            LayoutTools.Ensure<Mask>(sheet.gameObject).showMaskGraphic = true;

            RectTransform paper = LayoutTools.EnsureChild(sheet, "Paper");
            LayoutTools.Stretch(paper);
            RawImage picture = LayoutTools.Ensure<RawImage>(paper.gameObject);
            picture.color = Style.Card;
            picture.uvRect = new Rect(0f, 1f, 1f, -1f);
            picture.raycastTarget = false;
            if (Panel.FrontMaterial != null) picture.material = Panel.FrontMaterial;
            return new Sheet { Root = root, Picture = picture };
        }

        private void ForgetPages()
        {
            _ticket++;
            foreach (Sheet sheet in _sheets)
            {
                if (sheet.Texture != null) Destroy(sheet.Texture);
                if (sheet.Root != null) Destroy(sheet.Root.gameObject);
            }
            _sheets.Clear();
            _renderer?.Dispose();
            _renderer = null;
        }

        private static string MetaLine(DocumentFile page)
        {
            string pages = page.Pages.Count == 1 ? Strings.Get("documents.page") : Strings.Format("documents.pages", page.Pages.Count);
            var meta = new List<string> { pages };
            if (page.HasText) meta.Add(Strings.Format("documents.minutes", page.Minutes));
            return string.Join(" · ", meta);
        }

        private void LateUpdate()
        {
            if (!IsVisible || _renderer == null || Time.frameCount % VisibilityEveryFrames != 0) return;
            float view = BodyHeight;
            float y = _content.anchoredPosition.y;
            bool loading = false;
            foreach (Sheet sheet in _sheets) loading |= sheet.Loading;

            int next = -1;
            float nearest = float.MaxValue;
            for (int i = 0; i < _sheets.Count; i++)
            {
                Sheet sheet = _sheets[i];
                float top = TopOfRect(sheet.Root), bottom = top + sheet.Root.rect.height;
                float distance = bottom < y ? y - bottom : top > y + view ? top - (y + view) : 0f;
                if (distance > view * KeepScreens)
                {
                    if (sheet.Texture != null) Unload(sheet);
                    continue;
                }
                if (sheet.Texture == null && !sheet.Loading && !sheet.Failed && distance <= view && distance < nearest)
                {
                    nearest = distance;
                    next = i;
                }
            }
            if (!loading && next >= 0) Load(_sheets[next], next, _ticket);
        }

        private async void Load(Sheet sheet, int index, int ticket)
        {
            sheet.Loading = true;
            DocumentPage size = _page.Pages[index];
            Vector2Int pixels = PdfPages.PixelsFor(size.Width, size.Height);
            PagePixels drawn = null;
            try
            {
                drawn = await _renderer.Render(index, pixels.x, pixels.y);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[DocumentsPanel] Page {index + 1} could not be drawn: {e.Message}");
            }
            if (this == null || ticket != _ticket)
            {
                PdfPages.Discard(drawn);
                return;
            }
            sheet.Loading = false;
            if (drawn == null)
            {
                sheet.Failed = true;
                return;
            }
            sheet.Texture = PdfPages.MakeTexture(drawn);
            sheet.Picture.texture = sheet.Texture;
        }

        private static void Unload(Sheet sheet)
        {
            sheet.Picture.texture = null;
            Destroy(sheet.Texture);
            sheet.Texture = null;
        }

        private float TopOfRect(RectTransform rect)
        {
            Vector3 top = _content.InverseTransformPoint(rect.TransformPoint(new Vector3(0f, rect.rect.yMax, 0f)));
            return _content.rect.yMax - top.y;
        }

        private string DescribePage(int index)
        {
            if (_page == null) return null;
            string about = $"page {index + 1} of {_sheets.Count}";
            foreach (DocumentPassage passage in _page.Passages)
            {
                if (passage.Page != index) continue;
                string text = passage.Text.Length > 160 ? passage.Text.Substring(0, 160) + "…" : passage.Text;
                return $"{about}, which begins: {text}";
            }
            return about;
        }

        private List<RectTransform> SheetRoots()
        {
            var roots = new List<RectTransform>();
            foreach (Sheet sheet in _sheets) roots.Add(sheet.Root);
            return roots;
        }

        private async void WriteSummary(DocumentFile page, int ticket)
        {
            if (!page.HasText) return;
            SetSummary(true, null);
            PageSummary summary = await PageSummary.Write(page);
            if (this == null || ticket != _ticket) return;
            SetSummary(false, summary?.Summary);
        }

        private void SetSummary(bool writing, string summary)
        {
            _summaryWriting = writing;
            _shownDots = -1;
            _status.gameObject.SetActive(writing);
            _summaryBox.SetActive(!writing && !string.IsNullOrEmpty(summary));
            if (!string.IsNullOrEmpty(summary)) Texts.Bind(_summaryText, () => Numbers.Text(summary));
            if (writing) ShowWriting();
            Settle(false);
        }

        private void ShowWriting()
        {
            int dots = Style.Dots;
            if (dots == _shownDots) return;
            _shownDots = dots;
            string word = FlashLite.Reached ? "loading" : "assistant.connecting";
            Texts.Bind(_status, () => Style.Counting(Strings.Get(word)));
        }

        private void Update()
        {
            if (!IsVisible) return;
            if (_summaryWriting) ShowWriting();
            if (_messageDots && Style.Dots != _shownDots) BindMessage();
        }

        private void Settle(bool toTop)
        {
            if (toTop && _scroll != null) _scroll.verticalNormalizedPosition = 1f;
            if (_content != null) LayoutRebuilder.MarkLayoutForRebuild(_content);
            QueueSettle();
        }

        protected override void RefreshPanel()
        {
            RefreshPlacementIcons();
            QueueSettle();
        }

        public override void ShowPanel()
        {
            if (_canvas == null) return;
            ShowCanvas();
            Scene.Charts?.SetPanelShown(true);
            RefreshPanel();
            StateChannel.RecordState("documentsPanel", _page != null
                ? $"the Documents panel is open on \"{_page.Title}\""
                : "the Documents panel is open with no document");
        }

        public override void HidePanel()
        {
            if (_canvas == null) return;
            HideCanvas();
            Scene.Charts?.SetPanelShown(false);
            StateChannel.RecordState("documentsPanel", "the Documents panel is closed");
        }
    }
}
