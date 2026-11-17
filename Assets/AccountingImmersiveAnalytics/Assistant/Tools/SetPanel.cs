using System.Collections.Generic;
using AccountingImmersiveAnalytics.Assistant.Live;
using AccountingImmersiveAnalytics.Assistant.Session;
using AccountingImmersiveAnalytics.UI.Panels;

namespace AccountingImmersiveAnalytics.Assistant.Tools
{
    public sealed class SetPanel : AgenticTool<SetPanel.Args> {

        public class Args {
            [Doc("What to do with the panel. Prefer 'open' or 'close' over 'toggle' when you know the state you want.")]
            [Values("open", "close", "toggle")]
            public string state;
        }

        public override FunctionDeclaration Declaration => new FunctionDeclaration {
            Name = "SetPanel",
            Description = "Open, close, or toggle the Documents panel, as the Documents button on the user's wrist does. " +
                          "The panel opens ahead of the user and neither moves nor hides the chart.",
            Parameters = ParametersFor(typeof(Args))
        };

        protected override void Run(Args args, Dictionary<string, object> result) {
            if (!TryDocumentsPanel(result, out ModularPanel panel, out string name)) return;
            switch (args.state) {
                case "open": if (!panel.IsVisible) panel.ShowPanel(); break;
                case "close": if (panel.IsVisible) panel.HidePanel(); break;
                case "toggle": panel.TogglePanel(); break;
                default: result["error"] = "Say 'open', 'close' or 'toggle'."; return;
            }
            result["visible"] = panel.IsVisible;
            result["did"] = panel.IsVisible ? $"the {name} is open" : $"the {name} is closed";
        }
    }
}
