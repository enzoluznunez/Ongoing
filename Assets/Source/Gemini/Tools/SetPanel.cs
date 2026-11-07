using System.Collections.Generic;
using GeminiLive;

public sealed class SetPanel : AgenticTool<SetPanel.Args> {

    public class Args {
        [Doc("What to do with it. Prefer 'open' or 'close' over 'toggle' when you know the state you want.")]
        [Values("open", "close", "toggle")]
        public string state;
        [Doc("Which panel: 'tool', the tool panel, or 'settings', the settings panel. Defaults to 'tool'.")]
        [Values("tool", "settings"), Optional]
        public string panel;
    }

    public override FunctionDeclaration Declaration => new FunctionDeclaration {
        Name = "SetPanel",
        Description = "Open, close, or toggle the tool panel (the tool buttons and your speed setting) or the " +
                      "settings panel (the user's own settings, which only they change), as the user's wrist " +
                      "buttons do. A panel opens ahead of the user, beside the other when that one is open; " +
                      "neither moves nor hides the chart.",
        Parameters = ParametersFor(typeof(Args))
    };

    protected override void Run(Args args, Dictionary<string, object> result) {
        string state = args.state;

        if (args.panel == "settings") {
            var settings = Scene.SettingsPanel;
            if (settings == null) { result["error"] = "Settings panel not found in scene."; return; }
            Apply(state, settings.IsVisible, settings.ShowPanel, settings.HidePanel, settings.TogglePanel);
            result["visible"] = settings.IsVisible;
            result["did"] = settings.IsVisible ? "the settings panel is open" : "the settings panel is closed";
            return;
        }

        var tool = Scene.ToolPanel;
        if (tool == null) { result["error"] = "Tool panel not found in scene."; return; }

        Apply(state, tool.IsVisible, tool.ShowPanel, tool.HidePanel, tool.TogglePanel);
        result["visible"] = tool.IsVisible;
        result["did"] = tool.IsVisible ? "the tool panel is open" : "the tool panel is closed";
    }

    private static void Apply(string state, bool visible, System.Action show, System.Action hide, System.Action toggle) {
        switch (state) {
            case "open": if (!visible) show(); break;
            case "close": if (visible) hide(); break;
            default: toggle(); break;
        }
    }
}
