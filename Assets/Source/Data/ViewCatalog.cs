using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class ViewCatalog : MonoBehaviour
{
    public ManageDatasets manageDatasets;

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
            Notices.Show(this, Strings.Get("notice.noSheets"), Strings.Format("notice.noSheets.body", BreachMap.File));
            Debug.LogWarning($"[ViewCatalog] No metros to list: {map.Exception?.GetBaseException().Message}");
            Settled = true;
            yield break;
        }
        ColorModes.Learn(map.Result);
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
        Debug.Log($"[ViewCatalog] Listed {entries.Count} sheets; reading them all before the app opens.");

        manageDatasets.SwitchDataset(manageDatasets.IndexOf(UsSheetFile()));

        foreach (var entry in entries)
            manageDatasets.Preload(manageDatasets.IndexOf(entry.Item1));
    }

    public const string National = "United States";

    public static string Title(string region) =>
        region != null && region.StartsWith("US ", StringComparison.Ordinal) ? region.Substring(3) : region;

    public const string SheetFolder = "Sheets";

    public static string SheetFile(string metro) => FileFor(metro);

    public static string RegionSheetFile(string region) => FileFor(region);

    public static string UsSheetFile() => FileFor(National);

    private static string FileFor(string place) =>
        $"{SheetFolder}/{place.Replace('/', '-').Replace('\\', '-')}.csv";
}
