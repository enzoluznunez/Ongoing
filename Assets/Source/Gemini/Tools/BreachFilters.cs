using System;
using System.Collections.Generic;
using Google.GenAI.Types;

public class BreachFilters {

    [Optional, Limits(CyberContract.FirstYear, CyberContract.LastYear)]
    [Doc("The first year to include, by the year breaches were disclosed.")]
    public int? since;

    [Optional, Limits(CyberContract.FirstYear, CyberContract.LastYear)]
    [Doc("The last year to include, inclusive.")]
    public int? until;

    [Optional]
    [Doc("Industries of the breached companies. A breach in any of them counts.")]
    public string[] industry;

    [Optional]
    [Doc("Attack types. A breach of any of them counts.")]
    public string[] attack;

    [Optional]
    [Doc("The kind of information a breach exposed. A breach of any of these kinds counts.")]
    public string[] information;

    [Optional]
    [Doc("Particular information a breach exposed, such as SSN or Password. A breach that exposed any of them counts.")]
    public string[] accessed;

    [Optional]
    [Doc("Whether the breached entity was the listed company itself ('Parent') or one of its subsidiaries.")]
    public string[] relationship;

    [Optional]
    [Doc("US regions of the companies' headquarters.")]
    public string[] region;

    [Optional]
    [Doc("Stock markets the companies are listed on.")]
    public string[] market;

    [Optional]
    [Doc("Two-letter state codes of the companies' headquarters, such as VA.")]
    public string[] state;

    [Optional]
    [Doc("true for only breaches disclosed to the SEC, false for only those that were not.")]
    public bool? sec;

    [Optional]
    [Doc("Metros, by the names the sheets give them, such as 'New York, NY'.")]
    public string[] metro;

    [Optional]
    [Doc("Cities of the companies' headquarters, such as 'Seattle'.")]
    public string[] city;

    [Optional]
    [Doc("Part of a company's name, or of a breached subsidiary's or brand's, such as 'Equifax' or " +
         "'Sam's Club'. Case does not matter.")]
    public string company;

    [Optional]
    [Doc("A stock ticker, such as WMT.")]
    public string ticker;

    [Optional, Doc("Smallest revenue, in dollars (10 million is 10000000).")] public double? revenue_min;
    [Optional, Doc("Largest revenue, in dollars.")] public double? revenue_max;
    [Optional, Doc("Smallest net income, in dollars; negative is a loss.")] public double? net_income_min;
    [Optional, Doc("Largest net income, in dollars; 0 keeps only losses and break-evens.")] public double? net_income_max;
    [Optional, Doc("Smallest assets, in dollars.")] public double? assets_min;
    [Optional, Doc("Largest assets, in dollars.")] public double? assets_max;
    [Optional, Doc("Smallest book value, in dollars; negative is possible.")] public double? book_value_min;
    [Optional, Doc("Largest book value, in dollars.")] public double? book_value_max;
    [Optional, Doc("Smallest market cap, in dollars.")] public double? market_cap_min;
    [Optional, Doc("Largest market cap, in dollars.")] public double? market_cap_max;
    [Optional, Doc("Smallest stock price, in dollars per share.")] public double? stock_price_min;
    [Optional, Doc("Largest stock price, in dollars per share.")] public double? stock_price_max;
    [Optional, Doc("Fewest records lost. Most breaches do not report it, and those fail this bound.")] public double? records_lost_min;
    [Optional, Doc("Most records lost.")] public double? records_lost_max;
    [Optional, Doc("Smallest cost of the breach, in dollars. Most breaches do not report it, and those fail this bound.")] public double? cost_min;
    [Optional, Doc("Largest cost of the breach, in dollars.")] public double? cost_max;

    private static readonly (string field, string[] names, Func<BreachFilters, string[]> given)[] Vocabulary = {
        ("industry", CyberContract.Industries, f => f.industry),
        ("attack", CyberContract.AttackTypes, f => f.attack),
        ("information", CyberContract.InformationTypes, f => f.information),
        ("accessed", CyberContract.InformationAccessed, f => f.accessed),
        ("relationship", CyberContract.Relationships, f => f.relationship),
        ("region", CyberContract.Regions, f => f.region),
        ("market", CyberContract.Markets, f => f.market),
    };

    public static Schema Named(Schema schema) {
        foreach ((string field, string[] names, _) in Vocabulary)
            if (schema.Properties.TryGetValue(field, out Schema property) && property.Items != null)
                property.Items.Enum = new List<string>(names);
        return schema;
    }

    public static string Canonical(ref string value, string field, string[] names) {
        if (value == null) return null;
        string given = value.Trim();
        string found = Array.Find(names, n => string.Equals(n, given, StringComparison.OrdinalIgnoreCase));
        if (found == null) return $"'{value}' is not one of the {field} names: {string.Join(", ", names)}.";
        value = found;
        return null;
    }

    public string Check() {
        if (since.HasValue && until.HasValue && since > until)
            return $"'since' ({since}) is after 'until' ({until}).";
        foreach (int? year in new[] { since, until })
            if (year.HasValue && (year < CyberContract.FirstYear || year > CyberContract.LastYear))
                return $"Breaches run from {CyberContract.FirstYear} to {CyberContract.LastYear}; {year} is outside them.";

        foreach ((string field, string[] names, var values) in Vocabulary) {
            string[] given = values(this);
            if (given == null) continue;
            for (int i = 0; i < given.Length; i++) {
                given[i] ??= "";
                string bad = Canonical(ref given[i], field, names);
                if (bad != null) return bad;
            }
        }

        if (state != null)
            for (int i = 0; i < state.Length; i++) {
                string code = state[i]?.Trim() ?? "";
                if (code.Length != 2) return $"'{state[i]}' is not a two-letter state code, like VA.";
                state[i] = code.ToUpperInvariant();
            }
        return null;
    }
}
