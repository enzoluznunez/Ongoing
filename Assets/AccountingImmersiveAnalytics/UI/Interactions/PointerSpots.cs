using System.Collections.Generic;
using UnityEngine;

namespace AccountingImmersiveAnalytics.UI.Interactions
{
    public static class PointerSpots
    {
        private static readonly List<Vector3> Spots = new List<Vector3>();

        public static IReadOnlyList<Vector3> All => Spots;

        public static void Clear() => Spots.Clear();

        public static void Add(Vector3 spot) => Spots.Add(spot);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Spots.Clear();
    }
}
