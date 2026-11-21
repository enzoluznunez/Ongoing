using System;
using System.Collections.Generic;
using AccountingImmersiveAnalytics.Assistant.Context;
using AccountingImmersiveAnalytics.Assistant.Live;
using AccountingImmersiveAnalytics.Assistant.Tools.Arguments;
using AccountingImmersiveAnalytics.UI.Charts;
using AccountingImmersiveAnalytics.UI.Charts.Tools;
using AccountingImmersiveAnalytics.UI.Interactions;
using AccountingImmersiveAnalytics.UI.Panels;
using UnityEngine;
using static AccountingImmersiveAnalytics.Assistant.Tools.ToolResult;
using static AccountingImmersiveAnalytics.Assistant.Tools.ToolTargets;

namespace AccountingImmersiveAnalytics.Assistant.Tools.Chart
{
    public sealed class CallPlace : AssistantTool<CallPlace.Args>
    {

        private const float MinDistance = 0.001f;
        private const float MaxDistance = 100f;
        private const float MaxDegrees = 3600f;
        private const float PanelMinDistance = 0.02f;
        private const float PanelMaxDistance = 1f;

        public class Args
        {
            [Doc("What to place: 'chart', the pinned chart (the default), or 'documents', the Documents panel, by the Place " +
                 "button in its title bar."), Optional]
            [Values("chart", "documents")]
            public string target;
            [Doc("Which way to slide it, from the user's point of view; 'forward' is away from them. Leave it out to only turn it."), Optional]
            [Values("left", "right", "forward", "back", "up", "down",
                    "forward-left", "forward-right", "back-left", "back-right")]
            public string direction;
            [Doc("How far to slide, in meters: from a millimetre for the chart; from 0.02 to 1 for the panel, which also stays within reach."),
             Limits(MinDistance, MaxDistance), DefaultsTo(0.15f), Optional]
            public float distance;
            [Doc("Which way to turn it about the upright axis, seen from above from the user's point of view; " +
                 "'right' is clockwise. Leave it out to only slide it."), Optional]
            [Values("left", "right")]
            public string turn;
            [Doc("How far to turn, in degrees. More than a full turn is reduced to the equivalent turn, so 730 becomes 10."),
             Limits(1, MaxDegrees), DefaultsTo(90f), Optional]
            public float degrees;
            [Doc("false switches Place off, as pressing the lit button again does. Defaults to true."), DefaultsTo(true), Optional]
            public bool on;
        }

        protected override bool EditsAreOutcome => true;

        public override FunctionDeclaration Declaration => new FunctionDeclaration
        {
            Name = "CallPlace",
            Description = "Press a Place button and move what it places as the user does with it on: slide it a distance " +
                          "in a direction (diagonals too), turn it about the upright axis, or both. The pinned chart's Place " +
                          "is the default; target 'documents' presses the Documents panel's, which must be open. The chart and " +
                          "the panel move separately. Moving the chart is an edit Undo takes back; moving the panel is not, " +
                          "and it leaves the row of panels. With neither slide nor turn, it only switches Place on.",
            Parameters = ParametersFor(typeof(Args))
        };

        protected override void Run(Args args, Dictionary<string, object> result)
        {
            bool panel = args.target == "documents";
            if (!panel && SwitchedOff(ToolType.Place, args.on, result)) return;

            Vector3 way = Vector3.zero;
            if (args.direction != null && !TryWorldDirection(args.direction, out way))
            {
                result["error"] = "The user's head was not found, so there is no way to tell which way that is.";
                return;
            }
            string turn = args.turn;
            float sign = turn == "right" ? 1f : -1f;

            if (panel)
            {
                PlacePanel(args, way, turn, sign, result);
                return;
            }

            if (!TryPinnedChart(result, "Only the pinned chart can be placed.", out ChartManager mgr)) return;
            if (!EnsureToolSelected(ToolType.Place, result)) return;

            if (args.direction == null && turn == null) { result["ok"] = true; return; }

            Transform chart = mgr.PinnedTransform;
            float asked = args.distance;
            float distance = Mathf.Clamp(asked, MinDistance, MaxDistance);
            Vector3 delta = args.direction != null ? way * distance : Vector3.zero;
            float askedDegrees = AskedDegrees(args);
            float degrees = turn != null ? askedDegrees % 360f : 0f;
            bool turns = degrees >= 0.05f;
            if (turn != null && !turns)
            {
                result["turnedDegrees"] = 0;
                result["turnNote"] = askedDegrees >= 360f
                    ? $"{Round(askedDegrees)} degrees is a whole number of full turns, so the chart ends facing as it was."
                    : "That is too small a turn to see.";
                if (args.direction == null) return;
            }

            var pre = ChangeChartPose(mgr, chart, args.direction != null ? EditKind.Move : EditKind.Rotate, t =>
            {
                if (args.direction != null) t.localPosition += t.parent.InverseTransformVector(delta);
                if (turns) t.rotation = Quaternion.AngleAxis(sign * degrees, Vector3.up) * t.rotation;
            });

            if (args.direction != null)
            {
                mgr.GetCommittedPose(chart, out Vector3 nowPos, out _, out _);
                result["moved"] = args.direction;
                result["actualMeters"] = Round(chart.parent.TransformVector(nowPos - pre.Position).magnitude);
                if (distance != asked)
                    result["note"] = $"The distance was limited to the {MinDistance} to {MaxDistance} meter range; {distance} m was used.";
            }
            if (turns)
            {
                result["turned"] = turn;
                result["turnedDegrees"] = Round(degrees);
            }
        }

        private static float AskedDegrees(Args args) => Mathf.Clamp(Math.Abs(args.degrees), 0f, MaxDegrees);

        private static void PlacePanel(Args args, Vector3 way, string turn, float sign, Dictionary<string, object> result)
        {
            if (!TryDocumentsPanel(result, out DocumentsPanel panel)) return;
            const string name = DocumentsPanelName;
            if (!args.on)
            {
                panel.SetPlace(false);
                result["on"] = false;
                result["ok"] = true;
                return;
            }
            if (!panel.IsVisible)
            {
                Refuse(result, $"open {name}",
                    $"The {name} is closed, so there is nothing to place. Show it with SetPanel, then call this again.");
                return;
            }
            panel.SetPlace(true);
            result["on"] = true;
            result["ok"] = true;
            if (args.direction == null && turn == null) return;

            PanelLineup.Undock(panel);
            Transform t = panel.transform;
            if (args.direction != null)
            {
                float distance = Mathf.Clamp(args.distance, PanelMinDistance, PanelMaxDistance);
                Vector3 before = t.position;
                t.position = PanelPlacement.Reachable(before + way * distance, out bool limited);
                float actual = (t.position - before).magnitude;
                result["moved"] = args.direction;
                result["actualMeters"] = Round(actual);
                if (actual > 0f) result["changed"] = true;
                if (limited) result["note"] = "The panel was kept within the user's reach and moved less than asked.";
                ActivityFeed.Record($"moved the {name} {actual:0.00}m {args.direction}");
            }
            if (turn != null)
            {
                float degrees = AskedDegrees(args) % 360f;
                RectTransform body = panel.PrimaryRect;
                Vector3 centre = body != null ? body.TransformPoint(body.rect.center) : t.position;
                t.RotateAround(centre, Vector3.up, sign * degrees);
                result["turned"] = turn;
                result["turnedDegrees"] = Round(degrees);
                if (degrees > 0f) result["changed"] = true;
                ActivityFeed.Record($"turned the {name} {degrees:0} degrees {turn}");
            }
        }
    }
}
