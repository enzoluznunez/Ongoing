using System.Collections.Generic;
using AccountingImmersiveAnalytics.UI.Charts.Data;

namespace AccountingImmersiveAnalytics.UI.Charts.Spec
{
    public enum Aggregate { Sum, Mean, Count }

    public sealed class ChartSpec
    {
        public ChartKind Kind;
        public bool Horizontal;
        public string Title;
        public string Insight;
        public Aggregate Aggregate = Aggregate.Sum;
        public bool LogX;
        public bool LogY;
        public readonly Dictionary<Role, string> Fields = new Dictionary<Role, string>();
        public readonly Dictionary<Role, string> Titles = new Dictionary<Role, string>();

        public string FieldFor(Role role) => Fields.TryGetValue(role, out string name) ? name : null;

        public string TitleFor(Role role)
        {
            if (Titles.TryGetValue(role, out string title) && !string.IsNullOrWhiteSpace(title)) return title;
            if (role == Role.Value && FieldFor(Role.Value) == null) return "Count";
            return FieldFor(role) ?? "";
        }

        public string Describe() => ChartKinds.Name(Kind, Horizontal);

        public string Validate(CsvTable table)
        {
            foreach (RoleRule rule in ChartKinds.RulesFor(Kind))
            {
                string name = FieldFor(rule.Role);
                if (name == null)
                {
                    if (rule.Required) return $"The {Describe()} '{Title}' needs a {rule.Role.ToString().ToLowerInvariant()} column.";
                    continue;
                }
                CsvField field = table.Field(name);
                if (field == null) return $"'{Title}' names a column '{name}' that the file does not have.";
                bool number = rule.Type == FieldType.Number;
                if (number && !field.IsNumber) return $"The {rule.Role.ToString().ToLowerInvariant()} of '{Title}' needs numbers, and '{field.Name}' holds text.";
                if (!number && field.IsNumber) return $"The {rule.Role.ToString().ToLowerInvariant()} of '{Title}' needs text, and '{field.Name}' holds numbers.";
            }
            return null;
        }

        public List<int> Rows(CsvTable table, ChartShaping shaping)
        {
            var used = new List<CsvField>();
            foreach (string name in UsedFields())
            {
                CsvField field = table.Field(name);
                if (field != null) used.Add(field);
            }
            System.Func<int, bool> keeps = shaping?.Keeper(table);
            var rows = new List<int>(table.RowCount);
            for (int r = 0; r < table.RowCount; r++)
            {
                bool complete = true;
                foreach (CsvField field in used)
                    if (!field.Has(r)) { complete = false; break; }
                if (!complete) continue;
                if (keeps != null && !keeps(r)) continue;
                rows.Add(r);
            }
            return rows;
        }

        public IEnumerable<string> UsedFields()
        {
            foreach (RoleRule rule in ChartKinds.RulesFor(Kind))
            {
                string name = FieldFor(rule.Role);
                if (name != null) yield return name;
            }
        }
    }
}
