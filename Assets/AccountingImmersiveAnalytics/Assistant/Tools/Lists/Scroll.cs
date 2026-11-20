using System.Collections.Generic;
using AccountingImmersiveAnalytics.Assistant.Context;
using AccountingImmersiveAnalytics.Assistant.Live;
using AccountingImmersiveAnalytics.Assistant.Tools.Arguments;
using AccountingImmersiveAnalytics.UI.Interactions;

namespace AccountingImmersiveAnalytics.Assistant.Tools.Lists
{
    public sealed class Scroll : AssistantTool<Scroll.Args>
    {

        public class Args
        {
            [Doc("The scrolling list, named as for Jump: 'document', 'charts', 'edits', 'filter <column>' or 'key <title>'.")]
            public string list;
            [Doc("True to start sliding down the list, false to stop it. Defaults to true."), DefaultsTo(true), Optional]
            public bool on;
        }

        public override FunctionDeclaration Declaration => new FunctionDeclaration
        {
            Name = "Scroll",
            Description = "Turn on or off a slow, steady slide down a scrolling list, at a pace the user can read along with, " +
                          "from wherever the list is now. It stops by itself at the end of the list, when the user takes hold " +
                          "of the list, or when the list leaves the screen, and you are told which.",
            Parameters = ParametersFor(typeof(Args))
        };

        protected override void Run(Args args, Dictionary<string, object> result)
        {
            ListMotion motion = ListMotion.Find(args.list);
            if (motion == null)
            {
                result["error"] = $"No scrolling list called '{args.list}' is on screen. Scrolling lists now: {ListMotion.Names()}.";
                return;
            }
            result["list"] = motion.Name;
            if (args.on)
            {
                motion.StartAuto();
                int at = motion.Current;
                if (at >= 0) result["from"] = $"{motion.ItemWord} {at + 1} of {motion.Count}";
                ActivityFeed.Record($"started the auto-scroll of the {motion.Name} list");
            }
            else
            {
                bool was = motion.Auto;
                motion.StopAuto(null);
                int at = motion.Current;
                if (at >= 0) result["stoppedAt"] = $"{motion.ItemWord} {at + 1} of {motion.Count}";
                if (!was) result["note"] = "It was not auto-scrolling.";
                else ActivityFeed.Record($"stopped the auto-scroll of the {motion.Name} list");
            }
            result["auto"] = args.on;
        }
    }
}
