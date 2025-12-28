namespace TONX;

[HarmonyPatch(typeof(TextBoxTMP))]
public class TextBoxPatch
{
    static Dictionary<string, string> replaceDic = new()
    {
        { "（", " (" },
        { "）", ") " },
        { "，", ", " },
        { "：", ": " },
        { "[", "【" },
        { "]", "】" },
        { "‘", " '" },
        { "’", "' " },
        { "“", " ''" },
        { "”", "'' " },
        { "！", "! " },
        { "\r\n", " "},
    };
    [HarmonyPatch(nameof(TextBoxTMP.SetText)), HarmonyPrefix]
    public static bool ModifyCharacterLimit(TextBoxTMP __instance, [HarmonyArgument(0)] string input, [HarmonyArgument(1)] string inputCompo = "")
    {
        if (input.Length < 1) return true;
        string before = input[^1..];
        if (replaceDic.TryGetValue(before, out var after))
        {
            __instance.SetText(input.Replace(before, after));
            return false;
        }
        return true;
    }
}