using System.Collections.Generic;
using UnityEngine;

public static class ColorModes
{
    private static readonly Color[] RedGreen = Parse(
        "#8000ff", "#d9d921", "#8c5115", "#4cd9d9", "#008c7e",
        "#988cff", "#62b272", "#b27d00", "#b23e61", "#794d8c");

    private static readonly Color[] BlueYellow = Parse(
        "#8000ff", "#00e5ff", "#ffcc00", "#b22a1b", "#1c8c00",
        "#ff26e9", "#988cff", "#8c7e00", "#bbd977", "#0057d9");

    private static readonly Dictionary<string, int> _regionSlot = new Dictionary<string, int>();
    private static readonly List<Color> _regionColors = new List<Color>();
    private static readonly List<string> _regionNames = new List<string>();

    public static int RegionCount => _regionNames.Count;
    public static string RegionName(int i) => _regionNames[i];
    public static Color RegionColor(int i) => _regionColors[i];
    private static readonly Dictionary<string, int> _metroSlot = new Dictionary<string, int>();
    private static readonly Dictionary<Color32, int> _bySeen = new Dictionary<Color32, int>();

    public static void Learn(BreachMap.Answer map)
    {
        _regionSlot.Clear();
        _regionColors.Clear();
        _regionNames.Clear();
        _metroSlot.Clear();
        _bySeen.Clear();
        if (map == null) return;

        for (int i = 0; i < map.regions.Count; i++)
        {
            BreachMap.Region region = map.regions[i];
            _regionSlot[region.region] = i;
            _regionSlot[ViewCatalog.Title(region.region)] = i;
            _regionColors.Add(region.color);
            _regionNames.Add(ViewCatalog.Title(region.region));
        }

        var coloured = new HashSet<string>();
        foreach (BreachMap.City city in map.cities)
            if (!string.IsNullOrEmpty(city.metro) && !CreateSheet.Uncoloured(city.color) && !IsGrey(city.color))
                coloured.Add(city.metro);

        var rankInRegion = new Dictionary<string, int>();
        foreach (BreachMap.Metro metro in map.Metros())
        {
            if (!coloured.Contains(metro.name)) continue;
            rankInRegion.TryGetValue(metro.region, out int rank);
            _metroSlot[metro.name] = rank;
            rankInRegion[metro.region] = rank + 1;
        }
    }

    public static Color RowColor(DataSource data, int dataRow)
    {
        Color source = data.RowColorAt(dataRow);
        return Map(source, data.RowCategoryAt(dataRow), IsNational(data));
    }

    public static Color Map(Color source, string category, bool national)
    {
        Color[] palette = Palette(Settings.ColorMode);
        if (palette == null || CreateSheet.Uncoloured(source) || IsGrey(source)) return source;
        return palette[Slot(source, category, national) % palette.Length];
    }

    public static bool IsNational(DataSource data)
    {
        ManageDatasets datasets = ManageDatasets.Instance;
        return datasets != null && datasets.PayloadOf(data) == ViewCatalog.UsSheetFile();
    }

    private static int Slot(Color source, string category, bool national)
    {
        if (national)
        {
            for (int i = 0; i < _regionColors.Count; i++)
                if (Same(_regionColors[i], source)) return i;
            if (category != null && _regionSlot.TryGetValue(category, out int region)) return region;
        }
        else if (category != null && _metroSlot.TryGetValue(category, out int metro)) return metro;

        Color32 key = source;
        if (!_bySeen.TryGetValue(key, out int seen))
        {
            seen = _bySeen.Count;
            _bySeen[key] = seen;
        }
        return seen;
    }

    private static Color[] Palette(ColorMode mode) =>
        mode == ColorMode.RedGreen ? RedGreen : mode == ColorMode.BlueYellow ? BlueYellow : null;

    public static bool IsGrey(Color c) =>
        Mathf.Abs(c.r - c.g) < 0.02f && Mathf.Abs(c.g - c.b) < 0.02f;

    private static bool Same(Color a, Color b) => (Color32)a is Color32 x && (Color32)b is Color32 y &&
        x.r == y.r && x.g == y.g && x.b == y.b;

    private static Color[] Parse(params string[] hex)
    {
        var colors = new Color[hex.Length];
        for (int i = 0; i < hex.Length; i++)
            colors[i] = ColorUtility.TryParseHtmlString(hex[i], out Color c) ? c : Color.magenta;
        return colors;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _regionSlot.Clear();
        _regionColors.Clear();
        _regionNames.Clear();
        _metroSlot.Clear();
        _bySeen.Clear();
    }
}
