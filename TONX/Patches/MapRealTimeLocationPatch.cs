namespace TONX;

[HarmonyPatch]
public class MapRealTimeLocationPatch
{
    private static bool ShouldShowRealTime => !PlayerControl.LocalPlayer.IsAlive() || PlayerControl.LocalPlayer.Is(Roles.Core.CustomRoles.GM) || Main.GodMode.Value;
    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowNormalMap)), HarmonyPrefix]
    public static bool ShowNormalMap(MapBehaviour __instance)
    {
        if (!ShouldShowRealTime) return true;
        __instance.ShowCountOverlay(true, true, true);
        return false;
    }
    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowSabotageMap)), HarmonyPrefix]
    public static bool ShowSabotageMap(MapBehaviour __instance)
    {
        if (!ShouldShowRealTime || PlayerControl.LocalPlayer.Is(Roles.Core.CustomRoleTypes.Impostor)) return true;
        __instance.ShowCountOverlay(true, true, true);
        return false;
    }
}