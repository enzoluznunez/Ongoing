using System.Collections.Generic;
using AccountingImmersiveAnalytics.Assistant.Live;
using AccountingImmersiveAnalytics.Hands;

namespace AccountingImmersiveAnalytics.Assistant.Tools.General
{
    public sealed class ReadPointer : AssistantTool
    {

        public override FunctionDeclaration Declaration => new FunctionDeclaration
        {
            Name = "ReadPointer",
            Description = "Read what the user is pointing at or hovering a fingertip over right now: the chart, the " +
                          "shape's tooltip and, if they pinched it, the explanation on it. Use it whenever the user " +
                          "says 'this', 'that', 'here' or 'this one' about the data."
        };

        protected override void Run(Dictionary<string, object> args, Dictionary<string, object> result)
        {
            DataInspector inspector = DataInspector.Current;
            string pointing = inspector != null ? inspector.Describe() : null;
            if (pointing == null)
            {
                result["pointing"] = false;
                result["message"] = "The user is not pointing at any shape right now. Use the last '[pointing]' message, or ask which one they mean.";
                return;
            }
            result["pointing"] = true;
            result["at"] = "The user is pointing " + pointing;
        }
    }
}
