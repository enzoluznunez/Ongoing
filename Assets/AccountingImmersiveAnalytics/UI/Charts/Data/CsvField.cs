using System.Collections.Generic;
using System.Globalization;

namespace AccountingImmersiveAnalytics.UI.Charts.Data
{
    public sealed class CsvField
    {
        public string Name;
        public bool IsNumber;
        public double[] Numbers;
        public int[] Codes;
        public List<string> Categories;
        public List<string> Folded;
        public double Min = double.NaN;
        public double Max = double.NaN;

        public bool Has(int row) => IsNumber ? !double.IsNaN(Numbers[row]) : Codes[row] >= 0;

        public string TextAt(int row) =>
            IsNumber
                ? (double.IsNaN(Numbers[row]) ? null : Numbers[row].ToString(CultureInfo.InvariantCulture))
                : (Codes[row] < 0 ? null : Categories[Codes[row]]);

        public int Distinct
        {
            get
            {
                if (!IsNumber) return Categories.Count;
                var seen = new HashSet<double>();
                foreach (double v in Numbers)
                    if (!double.IsNaN(v)) seen.Add(v);
                return seen.Count;
            }
        }
    }
}
