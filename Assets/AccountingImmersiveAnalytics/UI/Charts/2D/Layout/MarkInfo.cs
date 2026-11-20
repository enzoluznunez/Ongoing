using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Charts.TwoD.Layout
{
    public enum HitShape { Box, Circle, Ring }

    public sealed class MarkInfo
    {
        public int Id;
        public int Shape = -1;
        public HitShape Area;
        public Rect Box;
        public Vector2 Centre;
        public float Outer;
        public float Inner;
        public float From;
        public float To;
        public string Title;
        public string TitleField;
        public readonly List<KeyValuePair<string, string>> Values = new List<KeyValuePair<string, string>>();
        public readonly List<string> Members = new List<string>();

        public MarkInfo Includes(IEnumerable<string> members)
        {
            Members.AddRange(members);
            if (Members.Count > 0) With("Includes", $"{Members.Count} more");
            return this;
        }

        public MarkInfo With(string label, string value)
        {
            Values.Add(new KeyValuePair<string, string>(label, value));
            return this;
        }

        public float Distance(Vector2 p)
        {
            switch (Area)
            {
                case HitShape.Circle:
                    return Mathf.Max(0f, Vector2.Distance(p, Centre) - Outer);
                case HitShape.Ring:
                {
                    Vector2 d = p - Centre;
                    float r = d.magnitude;
                    float radial = Mathf.Max(0f, Mathf.Max(r - Outer, Inner - r));
                    float angle = Mathf.Atan2(d.y, d.x);
                    while (angle < From) angle += Mathf.PI * 2f;
                    bool inside = angle <= To;
                    return inside ? radial : radial + 1000f;
                }
                default:
                {
                    float dx = Mathf.Max(Box.xMin - p.x, 0f, p.x - Box.xMax);
                    float dy = Mathf.Max(Box.yMin - p.y, 0f, p.y - Box.yMax);
                    return Mathf.Sqrt(dx * dx + dy * dy);
                }
            }
        }

        public string Body => string.Join("; ", Values.Select(pair => $"{pair.Key}: {pair.Value}"));

        public List<KeyValuePair<string, string>> Rows
        {
            get
            {
                var rows = new List<KeyValuePair<string, string>>();
                bool listed = Values.Exists(pair => pair.Value == Title);
                if (!string.IsNullOrEmpty(TitleField) && !string.IsNullOrEmpty(Title) && !listed)
                    rows.Add(new KeyValuePair<string, string>(TitleField, Title));
                rows.AddRange(Values);
                return rows;
            }
        }

        public string Describe()
        {
            string body = Body;
            if (string.IsNullOrEmpty(Title)) return body;
            return body.Length > 0 ? $"{Title}; {body}" : Title;
        }
    }
}
