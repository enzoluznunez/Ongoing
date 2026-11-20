using System.Collections.Generic;
using AccountingImmersiveAnalytics.Assistant.Context;
using AccountingImmersiveAnalytics.Assistant.Live;
using AccountingImmersiveAnalytics.Assistant.Tools.Arguments;
using AccountingImmersiveAnalytics.UI.Interactions;

namespace AccountingImmersiveAnalytics.Assistant.Tools.Lists
{
    public sealed class Jump : AssistantTool<Jump.Args>
    {

        public class Args
        {
            [Doc("The scrolling list: 'document' (the open document's pages), 'charts' (the chart column), " +
                 "'edits' (the Edits panel's list), 'filter <column>' (a Filter panel row of categories) or 'key <title>' (a Key panel row).")]
            public string list;
            [Doc("Which item to bring to the start of the view, counting from 1: page 3, the second chart, the fifth category.")]
            [Limits(1, 100000)]
            public int position;
        }

        public override FunctionDeclaration Declaration => new FunctionDeclaration
        {
            Name = "Jump",
            Description = "Glide a scrolling list so one of its items starts the view, as the user does by dragging it. " +
                          "A list only scrolls while it is on screen and longer than its space; the result names the ones that do. " +
                          "Stops a slide started with Scroll, if one is running. Returns what that item is.",
            Parameters = ParametersFor(typeof(Args))
        };

        protected override void Run(Args args, Dictionary<string, object> result)
        {
            ListMotion motion = ListMotion.Find(args.list);
            if (motion == null)
            {
                result["error"] = $"No scrolling list called '{args.list}' is on screen. Scrolling lists now: {ListMotion.Names()}. " +
                                  "A list that is not named fits its space, so all of it is already in view.";
                return;
            }
            int count = motion.Count;
            if (args.position < 1 || args.position > count)
            {
                result["error"] = $"The {motion.Name} list has {count} {motion.ItemWord}s; give a position from 1 to {count}.";
                return;
            }
            motion.JumpTo(args.position - 1);
            result["list"] = motion.Name;
            result["position"] = $"{args.position} of {count}";
            string about = motion.Describe(args.position - 1);
            if (about != null) result["showing"] = about;
            ActivityFeed.Record($"went to {motion.ItemWord} {args.position} of the {motion.Name} list");
        }
    }
}
