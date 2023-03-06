using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SignalRInterRegex;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        TxtIn.Text = File.ReadAllText("scratch.txt");
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        var txt = TxtIn.Text;

        var nTxt = "";
        var methodName = new Regex(@"Task (?<Method>\w+)\((?<Signature>.*?)\)");
        var interfaces = new Regex(@"(?<interface>public interface \w+)");

        nTxt += interfaces.Matches(txt).ToList()[0].Groups["interface"].Value.Contains("ToClient")
            ? "// --- hub server --> client"
            : "// --- hub client --> server";
        nTxt += "\nprivate void RegisterHubCallbacks()\n{\n";

        var matches = methodName.Matches(txt).ToList();
        matches.Take(10).ToList().ForEach(m => nTxt += GetHubRegister(m));

        nTxt += "}\n\n";
        nTxt += interfaces.Matches(txt).ToList()[1].Groups["interface"].Value.Contains("ToClient")
            ? "// --- hub server --> client"
            : "// --- hub client --> server";

        nTxt += GetMethod(methodName.Matches(txt).ToList()[matches.Count - 2]);
        nTxt += GetMethod(methodName.Matches(txt).ToList()[matches.Count - 1]);

        TxtOut.Text = nTxt;
    }

    private string GetHubRegister(Match match)
    {
        var signature = match.Groups["Signature"].Value;
        var method = match.Groups["Method"].Value;
        var l = signature.Split(",");
        var ret = "";
        if (l.Length > 1)
        {
            ret = $"\t_hub.On<{l[0].Split(" ")[0]}, {l[1].Split(" ")[1]}>(nameof({method}), {method});\n";
        }
        else
        {
            ret = $"\t_hub.On<{l[0].Split(" ")[0]}>(nameof({method}), {method});\n";
        }

        return ret;
    }

    private string GetMethod(Match match)
    {
        var signature = match.Groups["Signature"].Value;
        var method = match.Groups["Method"].Value;
        var l = signature.Split(",");

        var ret = $"\npublic Task {method}({signature}) " +"{\n";
        ret += $"\treturn _hub.State != HubConnectionState.Connected ?\n\t\t Task.CompletedTask : _hub.InvokeAsync(nameof({method}), {l[0].Split(" ")[1]}, {l[1].Split(" ")[1]});";
        ret += "\n}\n";

        return ret;
    }
}