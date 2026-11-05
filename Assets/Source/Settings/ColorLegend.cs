using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class ColorLegend
{
    public abstract void Build(RectTransform parent);

    public static ColorLegend For(DataSource data) => DiscreteLegend.Of(data);
}

public sealed class DiscreteLegend : ColorLegend
{
    public readonly List<(Color color, string label)> Entries = new List<(Color, string)>();

    private const int Columns = 2;

    public static DiscreteLegend Of(DataSource data)
    {
        var legend = new DiscreteLegend();
        if (data == null || !data.IsLoaded || !data.HasRowCategories) return legend;

        bool national = ColorModes.IsNational(data);
        bool others = false;
        var seen = new HashSet<string>();

        for (int row = 0; row < data.RowCount; row++)
        {
            Color source = data.RowColorAt(row);
            if (CreateSheet.Uncoloured(source) || ColorModes.IsGrey(source)) { others = true; continue; }

            string label = national ? RegionOf(source) ?? data.RowCategoryAt(row) : data.RowCategoryAt(row);
            if (string.IsNullOrEmpty(label) || !seen.Add(label)) continue;
            legend.Entries.Add((ColorModes.RowColor(data, row), label));
        }

        if (others) legend.Entries.Add((OtherGrey, Strings.Get("colorkey.other")));
        return legend;
    }

    private static string RegionOf(Color source)
    {
        Color32 s = source;
        for (int i = 0; i < ColorModes.RegionCount; i++)
        {
            Color32 r = ColorModes.RegionColor(i);
            if (r.r == s.r && r.g == s.g && r.b == s.b) return ColorModes.RegionName(i);
        }
        return null;
    }

    private static readonly Color OtherGrey = new Color(0.5f, 0.5f, 0.5f);

    public override void Build(RectTransform parent)
    {
        UILayout.Clear(parent);

        if (Entries.Count == 0)
        {
            TextMeshProUGUI none = UILabel.Make(parent, "None", Style.Black,
                TextAlignmentOptions.TopLeft, TextOverflowModes.Overflow, wrap: true);
            none.text = Strings.Get("colorkey.none");
            return;
        }

        RectTransform swatches = UILayout.EnsureChild(parent, "Swatches");
        GridLayoutGroup grid = UILayout.Ensure<GridLayoutGroup>(swatches.gameObject);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Columns;
        grid.spacing = new Vector2(Style.SmallPadding, Style.SmallPadding);
        grid.childAlignment = TextAnchor.UpperLeft;
        float width = Style.Panel.x - Style.PanelInset * 2f - Style.SmallBorder * 2f;
        grid.cellSize = new Vector2((width - Style.SmallPadding * (Columns - 1)) / Columns, Style.Button.y);

        for (int i = 0; i < Entries.Count; i++) AddEntry(swatches, i, Entries[i].color, Entries[i].label);

        int rows = Mathf.CeilToInt(Entries.Count / (float)Columns);
        UILayout.FixedHeight(swatches.gameObject, rows * Style.Button.y + (rows - 1) * Style.SmallPadding);
    }

    private static void AddEntry(RectTransform parent, int index, Color color, string label)
    {
        RectTransform row = UILayout.EnsureChild(parent, $"Entry_{index}");
        HorizontalLayoutGroup layout = UILayout.Ensure<HorizontalLayoutGroup>(row.gameObject);
        layout.spacing = Style.SmallPadding;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        RectTransform swatch = UILayout.EnsureChild(row, "Swatch");
        UILayout.FixedSize(swatch.gameObject, Vector2.one * Style.Subbutton.y);
        Image frame = UILayout.Ensure<Image>(swatch.gameObject);
        RoundedSprite.Apply(frame, Style.ChipRadius);
        frame.color = Style.Black;
        frame.raycastTarget = false;
        RectTransform fillRt = UILayout.EnsureChild(swatch, "Fill");
        UILayout.Stretch(fillRt, Style.SmallBorder);
        Image fill = UILayout.Ensure<Image>(fillRt.gameObject);
        RoundedSprite.Apply(fill, Style.InnerRadius(Style.ChipRadius));
        fill.color = color;
        fill.raycastTarget = false;

        TextMeshProUGUI text = UILabel.Make(row, "Label", Style.Black,
            TextAlignmentOptions.Left, TextOverflowModes.Ellipsis);
        text.text = label;
        LayoutElement le = UILayout.Ensure<LayoutElement>(text.gameObject);
        le.flexibleWidth = 1f;
    }
}
