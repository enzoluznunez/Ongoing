using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

// Lists the sheets at startup, from the regions and metros map.json names:
// one for the whole United States, one per region, and one per metro, leaving
// out a metro none of whose breaches reports a figure, as it has no rows. Each
// becomes a listed dataset whose payload is the CSV in StreamingAssets/Sheets
// that draws it. Nothing is opened here; the map opens the national sheet once
// it is drawn. The national sheet and each region's, the sheets a map opens
// on, are read behind it, so sliding to a map never waits on a read; every
// metro's is read after them, before the app is used.
public class ViewCatalog : MonoBehaviour
{
    public ManageDatasets manageDatasets;

    // Whether the sheets are listed and queued to be read, or never will be
    // (no map to list them from), so the loading phase knows what it waits on.
    public static bool Settled { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Settled = false;

    private void Start()
    {
        if (manageDatasets == null) manageDatasets = GetComponent<ManageDatasets>();
        if (manageDatasets == null) manageDatasets = Scene.Datasets;

        StartCoroutine(Bootstrap());
    }

    private IEnumerator Bootstrap()
    {
        if (manageDatasets == null)
        {
            Debug.LogError("[ViewCatalog] No ManageDatasets in the scene; no sheet will be listed.");
            Settled = true;
            yield break;
        }

        Task<BreachMap.Answer> map = BreachMap.Fetch();
        while (!map.IsCompleted) yield return null;
        if (map.Status != TaskStatus.RanToCompletion)
        {
            // The map says so in its own notice.
            Debug.LogWarning($"[ViewCatalog] No metros to list: {map.Exception?.GetBaseException().Message}");
            Settled = true;
            yield break;
        }
        ListMetros(map.Result);
        Settled = true;
    }

    private void ListMetros(BreachMap.Answer map)
    {
        const string Rows = "a row each: its revenue, net income, assets, book value, market cap and stock " +
                            "price, each bar as tall as its place among every US breach.";

        var entries = new List<(string, string, string)>();
        entries.Add((UsSheetFile(), National,
            $"Every breach at a company headquartered in the United States, every region together, {Rows}"));
        foreach (BreachMap.Region region in map.regions)
            entries.Add((RegionSheetFile(region.region), Title(region.region),
                $"Every breach at a company headquartered in the {Title(region.region)} region, metro by " +
                $"metro, {Rows}"));

        List<BreachMap.Metro> metros = map.Metros();
        foreach (BreachMap.Metro metro in metros)
        {
            if (metro.charted == 0) continue;
            entries.Add((SheetFile(metro.name), metro.name,
                $"Every breach at a company headquartered in the {metro.name} metro ({Title(metro.region)}), {Rows}"));
        }
        manageDatasets.AddCatalogEntries(entries);
        Debug.Log($"[ViewCatalog] Listed {entries.Count} sheets; reading them all before the map opens.");

        // The US sheet, the regions', then every metro's, all while the app is
        // loading (LoadingPhase), so poking a pin never waits on a read or a
        // parse.
        foreach (var entry in entries)
            manageDatasets.Preload(manageDatasets.IndexOf(entry.Item1));
    }

    // What the national sheet and map are called.
    public const string National = "United States";

    // 'US West' -> 'West', as the map and the rail name it.
    public static string Title(string region) =>
        region != null && region.StartsWith("US ", StringComparison.Ordinal) ? region.Substring(3) : region;

    // Where the sheets are, inside StreamingAssets: one CSV per place, named
    // for it ('United States.csv', 'US West.csv', 'Boston.csv').
    public const string SheetFolder = "Sheets";

    // The file that draws one metro's sheet.
    public static string SheetFile(string metro) => FileFor(metro);

    // The file that draws one region's sheet ('US West').
    public static string RegionSheetFile(string region) => FileFor(region);

    // The file that draws the whole country's sheet.
    public static string UsSheetFile() => FileFor(National);

    // A slash in a name would read as a folder.
    private static string FileFor(string place) =>
        $"{SheetFolder}/{place.Replace('/', '-').Replace('\\', '-')}.csv";
}
