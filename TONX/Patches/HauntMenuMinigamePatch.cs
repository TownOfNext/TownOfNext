namespace TONX.Patches;

[HarmonyPatch(typeof(HauntMenuMinigame), nameof(HauntMenuMinigame.SetHauntTarget))]
public static class HauntMenuMinigameSetFilterTextPatch
{
    public static void Postfix(HauntMenuMinigame __instance)
    {
        if (__instance.HauntTarget != null && Options.GhostCanSeeOtherRoles.GetBool())
        {
            // 役職表示をカスタムロール名で上書き
            __instance.FilterText.text = Utils.GetDisplayRoleName(PlayerControl.LocalPlayer, __instance.HauntTarget);
        }
    }
}