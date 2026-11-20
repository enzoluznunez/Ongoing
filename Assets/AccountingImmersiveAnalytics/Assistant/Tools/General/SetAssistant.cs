using System.Collections.Generic;
using AccountingImmersiveAnalytics.App;
using AccountingImmersiveAnalytics.Assistant.Context;
using AccountingImmersiveAnalytics.Assistant.Live;
using AccountingImmersiveAnalytics.Assistant.Tools.Arguments;
using AccountingImmersiveAnalytics.Assistant.Voice;
using AccountingImmersiveAnalytics.UI.Panels;
using static AccountingImmersiveAnalytics.Assistant.Tools.ToolResult;

namespace AccountingImmersiveAnalytics.Assistant.Tools.General
{
    public sealed class SetAssistant : AssistantTool<SetAssistant.Args>
    {

        public class Args
        {
            [Doc("Whether to turn the assistant on, off, or to the opposite of what it is now.")]
            [Values("on", "off", "toggle")]
            public string state;
        }

        public override FunctionDeclaration Declaration => new FunctionDeclaration
        {
            Name = "SetAssistant",
            Description = "Turn the voice assistant on or off, or toggle it, as the Assistant button on the user's wrist does. " +
                          "Turning it off ends voice control until the user re-enables it by hand, so only turn it off when they have asked; " +
                          "the switch-off happens when the current reply finishes, so you can still say goodbye.",
            Parameters = ParametersFor(typeof(Args))
        };

        protected override void Run(Args args, Dictionary<string, object> result)
        {
            var menu = Scene.Menu;
            if (menu == null) { result["error"] = "The Menu was not found in the scene."; return; }

            bool active;
            switch (args.state)
            {
                case "on": active = true; break;
                case "off": active = false; break;
                default: active = !menu.IsAssistantActive; break;
            }

            if (active)
            {
                VoiceSession.CancelShutdownRequest();
                menu.SetAssistantActive(true, AssistantCause.Assistant);
            }
            else
            {
                if (AssistantTurn.UserTookOver)
                {
                    Refuse(result, "user took control",
                        "The user took over while you were working, so do not turn yourself off on their behalf. " +
                        "Tell them where things stand and ask before carrying on.");
                    return;
                }
                VoiceSession.RequestShutdownAfterTurn();
                result["note"] = "The assistant stays on until this reply finishes, then turns off.";
            }
            result["active"] = active;
        }
    }
}
