using System.Text;
using UnityEngine;

namespace TONX;

public static class OptionShower
{
    public static void ShowChildren(OptionItem option, ref StringBuilder sb, Color color, int deep = 0)
    {
        foreach (var opt in option.Children.Select((v, i) => new { Value = v, Index = i + 1 }))
        {
            if (opt.Value.Name == "Maximum") continue; //Maximumの項目は飛ばす
            if (deep > 0)
            {
                sb.Append(string.Concat(Enumerable.Repeat(Utils.ColorString(color, "┃"), deep - 1)));
                sb.Append(Utils.ColorString(color, opt.Index == option.Children.Count ? "┗ " : "┣ "));
            }
            sb.Append($"{opt.Value.GetName()}: {opt.Value.GetString()}\n");
            if (opt.Value.GetBool()) ShowChildren(opt.Value, ref sb, color, deep + 1);
        }
    }
}