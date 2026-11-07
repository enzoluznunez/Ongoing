using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class ColorLegend
{
    public abstract void Build(RectTransform parent);

    public static ColorLegend For(ChartGeometry geometry) => DiscreteLegend.Of(geometry);
}

public sealed class DiscreteLegend : ColorLegend
{
    public readonly List<(Color color, string label)> Entries = new List<(Color, string)>();

    private const int Columns = 2;

    public float Width;
    public string Title;

    public static DiscreteLegend Of(ChartGeometry geometry)
    {
        var legend = new DiscreteLegend();
        if (geometry == null) return legend;
        legend.Title = geometry.LegendTitle;
        foreach (LegendEntry entry in geometry.Legend)
        {
            Color top = entry.slot < 0 ? Color.white : ColorModes.ColorAt(entry.slot);
            legend.Entries.Add((Color.Lerp(Color.black, top, entry.shade), entry.label));
        }
        return legend;
    }

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

        if (!string.IsNullOrEmpty(Title))
        {
            TextMeshProUGUI title = UILabel.Make(parent, "KeyTitle", Style.Black,
                TextAlignmentOptions.Left, TextOverflowModes.Overflow, wrap: true);
            title.text = Title;
        }

        RectTransform swatches = UILayout.EnsureChild(parent, "Swatches");
        GridLayoutGroup grid = UILayout.Ensure<GridLayoutGroup>(swatches.gameObject);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Columns;
        grid.spacing = new Vector2(Style.SmallPadding, Style.SmallPadding);
        grid.childAlignment = TextAnchor.UpperLeft;
        float width = Width > 0f ? Width : Style.Panel.x - Style.PanelInset * 2f - Style.SmallBorder * 2f;
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
