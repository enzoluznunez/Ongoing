using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

public enum ChannelRole { Category, Number, Any }

public sealed class ChannelRule
{
    public string Name;
    public ChannelRole Role;
    public bool Required;
}

public sealed class Blueprint
{
    public string Kind;
    public string Name;
    public readonly List<ChannelRule> Channels = new List<ChannelRule>();
    public readonly Dictionary<string, double> Layout = new Dictionary<string, double>();

    public double LayoutValue(string key, double fallback) =>
        Layout.TryGetValue(key, out double value) ? value : fallback;

    public static Blueprint Parse(string json)
    {
        JObject root = JObject.Parse(json);
        var blueprint = new Blueprint { Kind = (string)root["kind"], Name = (string)root["name"] };
        if (root["channels"] is JObject channels)
            foreach (JProperty channel in channels.Properties())
                blueprint.Channels.Add(new ChannelRule
                {
                    Name = channel.Name,
                    Role = ParseRole((string)channel.Value["role"]),
                    Required = (bool?)channel.Value["required"] ?? false
                });
        if (root["layout"] is JObject layout)
            foreach (JProperty setting in layout.Properties())
                blueprint.Layout[setting.Name] = (double)setting.Value;
        return blueprint;
    }

    private static ChannelRole ParseRole(string role)
    {
        switch (role)
        {
            case "category": return ChannelRole.Category;
            case "number": return ChannelRole.Number;
            default: return ChannelRole.Any;
        }
    }
}

public sealed class ChartSpec
{
    public string Id;
    public string Title;
    public string Description;
    public string Summary;
    public string BlueprintKind;
    public string Dataset;
    public readonly Dictionary<string, string> Encoding = new Dictionary<string, string>();
    public readonly Dictionary<string, string> Titles = new Dictionary<string, string>();

    public string TitleFor(string channel, string fallback) =>
        Titles.TryGetValue(channel, out string title) ? title : fallback;

    public string ColumnFor(string channel) => Encoding.TryGetValue(channel, out string column) ? column : null;

    public string Validate(Blueprint blueprint, DataTable table)
    {
        foreach (ChannelRule rule in blueprint.Channels)
        {
            string name = ColumnFor(rule.Name);
            if (name == null)
            {
                if (rule.Required) return $"'{Title}' does not say which column is its {rule.Name}.";
                continue;
            }

            DataColumn column = table.Column(name);
            if (column == null) return $"'{Title}' has no column called '{name}'.";
            if (rule.Role == ChannelRole.Number && !column.IsNumber)
                return $"The {rule.Name} of '{Title}' needs numbers, and '{name}' holds text.";
        }
        return null;
    }
}

public static class BlueprintStore
{
    private const string Folder = "Blueprints";
    private const string CatalogFile = "Charts/catalog.json";

    public static async Task<Blueprint> Fetch(string kind)
    {
        string json = await StreamingFiles.ReadText($"{Folder}/{kind}.json");
        if (json == null) throw new InvalidOperationException($"There is no blueprint for '{kind}'.");
        return Blueprint.Parse(json);
    }

    public static async Task<(List<ChartSpec> charts, string start)> FetchCatalog()
    {
        string json = await StreamingFiles.ReadText(CatalogFile);
        if (json == null) throw new InvalidOperationException($"There is no {CatalogFile} in StreamingAssets.");

        var specs = new List<ChartSpec>();
        JObject root = JObject.Parse(json);
        foreach (JToken item in root["charts"])
        {
            var spec = new ChartSpec
            {
                Id = (string)item["id"],
                Title = (string)item["title"],
                Description = (string)item["description"],
                Summary = (string)item["summary"],
                BlueprintKind = (string)item["blueprint"],
                Dataset = (string)item["dataset"]
            };
            foreach (JProperty channel in ((JObject)item["encoding"]).Properties())
                spec.Encoding[channel.Name] = (string)channel.Value;
            if (item["titles"] is JObject titles)
                foreach (JProperty title in titles.Properties())
                    spec.Titles[title.Name] = (string)title.Value;
            specs.Add(spec);
        }
        return (specs, (string)root["start"]);
    }
}
