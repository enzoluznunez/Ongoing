using System;

namespace AccountingImmersiveAnalytics.UI.Charts.Scales
{
    public sealed class NumberAxis
    {
        private static readonly double[] Steps = { 1d, 2d, 2.5d, 5d, 10d };

        public double Low { get; private set; }
        public double High { get; private set; }
        public double Step { get; private set; }
        public bool Log { get; private set; }
        public bool Reversed { get; set; }

        public static NumberAxis For(double min, double max, bool includeZero = false, bool log = false, bool whole = false)
        {
            if (double.IsNaN(min) || double.IsNaN(max)) { min = 0d; max = 1d; }
            if (log && min > 0d) return LogAxis(min, max);
            if (includeZero)
            {
                min = Math.Min(min, 0d);
                max = Math.Max(max, 0d);
            }
            if (max - min < 1e-12)
            {
                double pad = whole ? 2d : Math.Abs(max) > 0d ? Math.Abs(max) * 0.5d : 1d;
                if (includeZero && min >= 0d) max += pad;
                else { min -= pad; max += pad; }
            }
            int intervals = ChartStyle.Ticks - 1;
            double raw = (max - min) / intervals;
            double magnitude = Math.Pow(10d, Math.Floor(Math.Log10(raw)));
            for (int k = 0; k < 40; k++)
            {
                foreach (double s in Steps)
                {
                    double step = s * magnitude;
                    if (step < raw * 0.999) continue;
                    if (whole && (step < 1d || Math.Abs(step - Math.Round(step)) > 1e-9)) continue;
                    double low = Math.Floor(min / step + 1e-9) * step;
                    double high = low + step * intervals;
                    if (high + step * 1e-9 >= max) return new NumberAxis { Low = Clean(low), High = Clean(high), Step = step };
                }
                magnitude *= 10d;
            }
            return new NumberAxis { Low = min, High = max, Step = (max - min) / intervals };
        }

        private static NumberAxis LogAxis(double min, double max)
        {
            int intervals = ChartStyle.Ticks - 1;
            double low = Math.Floor(Math.Log10(min));
            double high = Math.Ceiling(Math.Log10(max));
            double step = Math.Max(1d, Math.Ceiling((high - low) / intervals));
            return new NumberAxis { Low = low, High = low + step * intervals, Step = step, Log = true };
        }

        private static double Clean(double v) => Math.Abs(v) < 1e-12 ? 0d : v;

        public double TickValue(int i)
        {
            double at = Low + Step * i;
            return Log ? Math.Pow(10d, at) : at;
        }

        public float Fraction(double v)
        {
            double at = Log ? (v > 0d ? Math.Log10(v) : Low) : v;
            double t = (at - Low) / (High - Low);
            if (Reversed) t = 1d - t;
            return (float)t;
        }

        public string Label(int i) => AxisFormat.Number(TickValue(i), Log ? TickValue(i) : Step);

        public string ShownLabel(int i) => AccountingImmersiveAnalytics.Text.Numbers.Shown(TickValue(i), Log ? TickValue(i) : Step);
    }
}
