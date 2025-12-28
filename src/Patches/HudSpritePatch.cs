using TONX.Roles.Core.Interfaces;
using UnityEngine;

#pragma warning disable CS8602 // 解引用可能出现空引用。

namespace TONX;

public static class CustomButton
{
    public static Sprite GetSprite(string name) => Utils.LoadSprite($"TONX.Resources.Images.Skills.{name}.png", 115f);
}

#nullable enable
[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update)), HarmonyPriority(Priority.LowerThanNormal)]
public static class HudSpritePatch
{
    private static Sprite? Defalt_Kill => DestroyableSingleton<HudManager>.Instance?.KillButton?.graphic?.sprite;
    private static Sprite? Defalt_Ability => DestroyableSingleton<HudManager>.Instance?.AbilityButton?.graphic?.sprite;
    private static Sprite? Defalt_Vent => DestroyableSingleton<HudManager>.Instance?.ImpostorVentButton?.graphic?.sprite;
    private static Sprite? Defalt_Report => DestroyableSingleton<HudManager>.Instance?.ReportButton?.graphic?.sprite;
    public static void Postfix(HudManager __instance)
    {
        var player = PlayerControl.LocalPlayer;
        if (__instance == null || player == null || !GameStates.IsModHost || !GameStates.IsInTask) return;
        if (!SetHudActivePatch.IsActive || !player.IsAlive()) return;

        Sprite newKillButton = Defalt_Kill ?? __instance.KillButton.graphic.sprite;
        Sprite newAbilityButton = Defalt_Ability ?? __instance.AbilityButton.graphic.sprite;
        Sprite newVentButton = Defalt_Vent ?? __instance.ImpostorVentButton.graphic.sprite;
        Sprite newReportButton = Defalt_Report ?? __instance.ReportButton.graphic.sprite;

        if (Main.EnableCustomButton.Value)
        {
            if (player.GetRoleClass() is IKiller killer)
            {
                if (killer.OverrideKillButtonSprite(out var newKillButtonName))
                    newKillButton = CustomButton.GetSprite(newKillButtonName);
                if (killer.OverrideVentButtonSprite(out var newVentButtonName))
                    newVentButton = CustomButton.GetSprite(newVentButtonName);
            }
            if (player.GetRoleClass()?.GetAbilityButtonSprite(out var newAbilityButtonName) ?? false)
                newAbilityButton = CustomButton.GetSprite(newAbilityButtonName);
            if (player.GetRoleClass()?.GetReportButtonSprite(out var newReportButtonName) ?? false)
                newReportButton = CustomButton.GetSprite(newReportButtonName);
        }

        if (player.GetRoleClass() is IKiller)
        {
            if (__instance.KillButton.graphic.sprite != newKillButton && newKillButton != null)
            {
                __instance.KillButton.graphic.sprite = newKillButton;
                __instance.KillButton.graphic.material = __instance.ReportButton.graphic.material;
            }
            if (__instance.ImpostorVentButton.graphic.sprite != newVentButton && newVentButton != null)
            {
                __instance.ImpostorVentButton.graphic.sprite = newVentButton;
            }
            __instance.KillButton?.graphic?.material?.SetFloat("_Desat", __instance?.KillButton?.isCoolingDown ?? true ? 1f : 0f);
        }
        if (__instance.AbilityButton.graphic.sprite != newAbilityButton && newAbilityButton != null)
        {
            __instance.AbilityButton.graphic.sprite = newAbilityButton;
            __instance.AbilityButton.graphic.material = __instance.ReportButton.graphic.material;
        }
        if (__instance.ReportButton.graphic.sprite != newReportButton && newReportButton != null)
        {
            __instance.ReportButton.graphic.sprite = newReportButton;
        }
        __instance.AbilityButton?.graphic?.material?.SetFloat("_Desat", __instance?.AbilityButton?.isCoolingDown ?? true ? 1f : 0f);
    }
}
#nullable disable