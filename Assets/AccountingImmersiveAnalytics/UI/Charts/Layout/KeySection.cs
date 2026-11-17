using AccountingImmersiveAnalytics.UI.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Charts.Layout
{
    public enum KeyShape { Box, Line, Dot, Size }

    public sealed class KeySection
    {
        public string Title;
        public KeyShape Shape = KeyShape.Box;
        public readonly List<KeyEntry> Entries = new List<KeyEntry>();
        public List<string> RampLabels;
        public float LargestRadius;

        public bool IsRamp => RampLabels != null;

        public static KeySection Groups(string title, IReadOnlyList<string> names, KeyShape shape, Func<int, Color> color)
        {
            var section = new KeySection { Title = title, Shape = shape };
            for (int i = 0; i < names.Count; i++) section.Entries.Add(new KeyEntry { Name = names[i], Color = color(i) });
            return section;
        }
    }
}
