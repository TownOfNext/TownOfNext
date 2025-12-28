using UnityEngine;

namespace TONX.Patches;

[HarmonyPatch(typeof(ButtonRolloverHandler))]
class ButtonRolloverHandlerPatch
{
    [HarmonyPatch(nameof(ButtonRolloverHandler.DoMouseOver)), HarmonyPrefix]
    public static void DoMouseOver_Prefix(ButtonRolloverHandler __instance)
    {
        if (__instance.OverColor == new Color(0, 1, 0, 1) || __instance.OverColor == Palette.AcceptedGreen)
            __instance.OverColor = new Color32(255, 192, 203, 255);
    }
    [HarmonyPatch(nameof(ButtonRolloverHandler.ChangeOutColor)), HarmonyPrefix]
    public static void ChangeOutColor_Prefix(ButtonRolloverHandler __instance, ref Color color)
    {
        if (color is { r: 0, g: 1, b: > 0.163f and < 0.165f, a: 1 })
            color = new Color32(255, 129, 166, 255);
    }
}
[HarmonyPatch(typeof(Palette))]
class PalettePath
{
    [HarmonyPatch(nameof(Palette.AcceptedGreen), MethodType.Getter), HarmonyPrefix]
    public static bool Get_AcceptedGreen_Prefix(ref Color __result)
    {
        __result = new Color32(255, 129, 166, (byte)(__result.a * 255));
        return false;
    }
}