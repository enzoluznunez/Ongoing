using System;
using System.Collections.Generic;
using System.Linq;
using AccountingImmersiveAnalytics.Text;
using AccountingImmersiveAnalytics.UI.Charts.Data;
using AccountingImmersiveAnalytics.UI.Charts.Scales;

namespace AccountingImmersiveAnalytics.UI.Charts.Spec
{
    public static class TrendFitter
    {
        private const int CurvePoints = 64;
        private const double SmoothSpan = 0.5d;
        private const int SmoothFewest = 5;

        public static readonly TrendKind[] Order =
        {
            TrendKind.None, TrendKind.Linear, TrendKind.Exponential, TrendKind.Logarithmic,
            TrendKind.Polynomial, TrendKind.Power, TrendKind.Smooth
        };

        public static bool Offered(ChartSpec spec) =>
            spec != null && (spec.Kind == ChartKind.Scatter || spec.Kind == ChartKind.Bubble);

        public static TrendFit Fit(ChartSpec spec, CsvTable table, ChartShaping shaping, TrendKind kind)
        {
            if (!Offered(spec) || table == null || kind == TrendKind.None) return null;
            List<(double X, double Y)> points = Points(spec, table, shaping);
            if (!Fits(kind, points)) return null;

            double low = points.Min(p => p.X), high = points.Max(p => p.X);
            string x = Prose(spec.TitleFor(Role.X)), y = Prose(spec.TitleFor(Role.Y));
            var fit = new TrendFit { Kind = kind };
            Func<double, double> at;
            switch (kind)
            {
                case TrendKind.Linear:
                {
                    (double a, double b) = Line(points);
                    at = v => a + b * v;
                    double step = Step(low, high);
                    fit.Meaning = $"{Capital(y)} {(b >= 0 ? "rises" : "falls")} about {AxisFormat.Value(Math.Abs(b * step))} for every {AxisFormat.Value(step)} more {x}";
                    break;
                }
                case TrendKind.Exponential:
                {
                    (double a, double b) = Line(points.Select(p => (p.X, Math.Log(p.Y))).ToList());
                    at = v => Math.Exp(a + b * v);
                    double step = Step(low, high);
                    double change = Math.Exp(b * step) - 1d;
                    fit.Meaning = $"{Capital(y)} {(change >= 0 ? "grows" : "shrinks")} about {Percent(change)} for every {AxisFormat.Value(step)} more {x}";
                    break;
                }
                case TrendKind.Logarithmic:
                {
                    (double a, double b) = Line(points.Select(p => (Math.Log(p.X), p.Y)).ToList());
                    at = v => a + b * Math.Log(Math.Max(v, 1e-12));
                    double doubling = b * Math.Log(2d);
                    fit.Meaning = doubling >= 0
                        ? $"Each doubling of {x} adds about {AxisFormat.Value(doubling)} to {y}"
                        : $"Each doubling of {x} takes about {AxisFormat.Value(-doubling)} off {y}";
                    break;
                }
                case TrendKind.Polynomial:
                {
                    (double a, double b, double c) = Quadratic(points);
                    at = v => a + b * v + c * v * v;
                    double turn = Math.Abs(c) > 1e-12 ? -b / (2d * c) : double.NaN;
                    if (!double.IsNaN(turn) && turn > low && turn < high)
                        fit.Meaning = c < 0
                            ? $"{Capital(y)} rises until about {AxisFormat.Value(turn)} {x}, then falls"
                            : $"{Capital(y)} falls until about {AxisFormat.Value(turn)} {x}, then rises";
                    else
                        fit.Meaning = $"{Capital(y)} {(at(high) >= at(low) ? "rises" : "falls")} along a curve as {x} grows";
                    break;
                }
                case TrendKind.Power:
                {
                    (double a, double b) = Line(points.Select(p => (Math.Log(p.X), Math.Log(p.Y))).ToList());
                    at = v => Math.Exp(a) * Math.Pow(Math.Max(v, 1e-12), b);
                    double change = Math.Pow(2d, b) - 1d;
                    fit.Meaning = $"Doubling {x} {(change >= 0 ? "raises" : "lowers")} {y} about {Percent(change)}";
                    break;
                }
                default:
                {
                    List<(double X, double Y)> sorted = points.OrderBy(p => p.X).ToList();
                    at = v => Lowess(sorted, v);
                    fit.Meaning = $"Follows the local average of {y}";
                    break;
                }
            }

            for (int i = 0; i < CurvePoints; i++)
            {
                double v = low + (high - low) * i / (CurvePoints - 1);
                double w = at(v);
                if (!double.IsNaN(w) && !double.IsInfinity(w)) fit.Curve.Add((v, w));
            }
            return fit.Curve.Count > 1 ? fit : null;
        }

        public static bool Fits(ChartSpec spec, CsvTable table, ChartShaping shaping, TrendKind kind) =>
            kind == TrendKind.None ? Offered(spec) : Fit(spec, table, shaping, kind) != null;

        public static string WhyNot(ChartSpec spec, CsvTable table, ChartShaping shaping, TrendKind kind)
        {
            if (!Offered(spec) || table == null || kind == TrendKind.None) return null;
            List<(double X, double Y)> points = Points(spec, table, shaping);
            string name = Strings.Get("trend." + kind), x = spec.TitleFor(Role.X), y = spec.TitleFor(Role.Y);
            if (Fits(kind, points)) return Fit(spec, table, shaping, kind) != null ? null : Strings.Format("trend.noCurve", name);
            int needed = kind == TrendKind.Polynomial ? 3 : 2;
            int distinct = DistinctX(points, needed);
            if (kind == TrendKind.Smooth && points.Count < SmoothFewest) return Strings.Format("trend.needsPoints", name, SmoothFewest);
            if (distinct < needed) return Strings.Format("trend.needsMore", name, needed, Numbers.Text(x));
            switch (kind)
            {
                case TrendKind.Exponential: return Strings.Format("trend.needsAbove", name, Numbers.Text(y));
                case TrendKind.Logarithmic: return Strings.Format("trend.needsAbove", name, Numbers.Text(x));
                case TrendKind.Power: return Strings.Format("trend.needsBoth", name, Numbers.Text(x), Numbers.Text(y));
                default: return null;
            }
        }

        private static int DistinctX(List<(double X, double Y)> points, int enough)
        {
            var seen = new HashSet<double>();
            foreach (var point in points)
                if (seen.Add(point.X) && seen.Count >= enough) break;
            return seen.Count;
        }

        private static bool Fits(TrendKind kind, List<(double X, double Y)> points)
        {
            int distinct = DistinctX(points, 3);
            switch (kind)
            {
                case TrendKind.Linear: return distinct >= 2;
                case TrendKind.Exponential: return distinct >= 2 && points.All(p => p.Y > 0d);
                case TrendKind.Logarithmic: return distinct >= 2 && points.All(p => p.X > 0d);
                case TrendKind.Polynomial: return distinct >= 3;
                case TrendKind.Power: return distinct >= 2 && points.All(p => p.X > 0d && p.Y > 0d);
                case TrendKind.Smooth: return points.Count >= SmoothFewest && distinct >= 2;
                default: return true;
            }
        }

        private static List<(double X, double Y)> Points(ChartSpec spec, CsvTable table, ChartShaping shaping)
        {
            CsvField x = table.Field(spec.FieldFor(Role.X));
            CsvField y = table.Field(spec.FieldFor(Role.Y));
            var points = new List<(double X, double Y)>();
            if (x == null || y == null || !x.IsNumber || !y.IsNumber) return points;
            foreach (int r in spec.Rows(table, shaping)) points.Add((x.Numbers[r], y.Numbers[r]));
            return points;
        }

        private static (double A, double B) Line(List<(double X, double Y)> points)
        {
            double mx = points.Average(p => p.X), my = points.Average(p => p.Y);
            double sxy = 0d, sxx = 0d;
            foreach ((double px, double py) in points)
            {
                sxy += (px - mx) * (py - my);
                sxx += (px - mx) * (px - mx);
            }
            double b = sxx > 0d ? sxy / sxx : 0d;
            return (my - b * mx, b);
        }

        private static (double A, double B, double C) Quadratic(List<(double X, double Y)> points)
        {
            double centre = points.Average(p => p.X);
            double s0 = 0, s1 = 0, s2 = 0, s3 = 0, s4 = 0, t0 = 0, t1 = 0, t2 = 0;
            foreach ((double px, double py) in points)
            {
                double u = px - centre, u2 = u * u;
                s0 += 1; s1 += u; s2 += u2; s3 += u2 * u; s4 += u2 * u2;
                t0 += py; t1 += py * u; t2 += py * u2;
            }
            double[,] m = { { s0, s1, s2 }, { s1, s2, s3 }, { s2, s3, s4 } };
            double[] v = { t0, t1, t2 };
            double[] k = Solve(m, v);
            double a = k[0] - k[1] * centre + k[2] * centre * centre;
            double b = k[1] - 2d * k[2] * centre;
            return (a, b, k[2]);
        }

        private static double[] Solve(double[,] m, double[] v)
        {
            int n = v.Length;
            for (int col = 0; col < n; col++)
            {
                int pivot = col;
                for (int row = col + 1; row < n; row++)
                    if (Math.Abs(m[row, col]) > Math.Abs(m[pivot, col])) pivot = row;
                for (int c = 0; c < n; c++) (m[col, c], m[pivot, c]) = (m[pivot, c], m[col, c]);
                (v[col], v[pivot]) = (v[pivot], v[col]);
                double d = m[col, col];
                if (Math.Abs(d) < 1e-15) return new double[n];
                for (int row = 0; row < n; row++)
                {
                    if (row == col) continue;
                    double f = m[row, col] / d;
                    for (int c = col; c < n; c++) m[row, c] -= f * m[col, c];
                    v[row] -= f * v[col];
                }
            }
            var result = new double[n];
            for (int i = 0; i < n; i++) result[i] = v[i] / m[i, i];
            return result;
        }

        private static double Lowess(List<(double X, double Y)> sorted, double at)
        {
            int count = Math.Max(2, (int)Math.Ceiling(sorted.Count * SmoothSpan));
            List<(double X, double Y, double D)> near = sorted
                .Select(p => (p.X, p.Y, D: Math.Abs(p.X - at)))
                .OrderBy(p => p.D).Take(count).ToList();
            double reach = Math.Max(near[near.Count - 1].D, 1e-12) * 1.000001d;
            double sw = 0, sx = 0, sy = 0, sxx = 0, sxy = 0;
            foreach ((double px, double py, double pd) in near)
            {
                double u = pd / reach;
                double w = Math.Pow(1d - u * u * u, 3d);
                sw += w; sx += w * px; sy += w * py; sxx += w * px * px; sxy += w * px * py;
            }
            if (sw <= 0d) return double.NaN;
            double mx = sx / sw, my = sy / sw;
            double vx = sxx / sw - mx * mx;
            double b = vx > 1e-12 ? (sxy / sw - mx * my) / vx : 0d;
            return my + b * (at - mx);
        }

        private static double Step(double low, double high)
        {
            double raw = Math.Max((high - low) / 10d, 1e-12);
            double magnitude = Math.Pow(10d, Math.Floor(Math.Log10(raw)));
            foreach (double s in new[] { 1d, 2d, 5d, 10d })
                if (s * magnitude >= raw) return s * magnitude;
            return 10d * magnitude;
        }

        private static string Percent(double change)
        {
            double p = Math.Abs(change) * 100d;
            return (p >= 10d ? Math.Round(p).ToString("0") : p.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)) + "%";
        }

        private static string Prose(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return "";
            string plain = title;
            int open = plain.IndexOf(" (", StringComparison.Ordinal);
            if (open > 0 && plain.EndsWith(")")) plain = plain.Substring(0, open);
            string[] words = plain.Trim().Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                string w = words[i];
                bool titled = w.Length > 1 && char.IsUpper(w[0]) && w.Substring(1).ToLowerInvariant() == w.Substring(1);
                if (titled) words[i] = char.ToLowerInvariant(w[0]) + w.Substring(1);
            }
            return string.Join(" ", words);
        }

        private static string Capital(string text) =>
            string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}
