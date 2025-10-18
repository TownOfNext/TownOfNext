using AmongUs.GameOptions;
using UnityEngine;

namespace TONX;

[HarmonyPatch(typeof(RoleOptionSetting), nameof(RoleOptionSetting.UpdateValuesAndText))]
class ChanceChangePatch
{
    public static void Postfix(RoleOptionSetting __instance)
    {
        __instance.CountPlusBtn.interactableHoveredColor = __instance.CountMinusBtn.interactableHoveredColor =
        __instance.ChancePlusBtn.interactableHoveredColor = __instance.ChanceMinusBtn.interactableHoveredColor = Main.ModColor32;
        __instance.CountPlusBtn.interactableClickColor = __instance.CountMinusBtn.interactableClickColor =
        __instance.ChancePlusBtn.interactableClickColor = __instance.ChanceMinusBtn.interactableClickColor = new Color32(161, 121, 128, 255);

        // The Phantom does not work together with desynchronized impostor roles e.g. Sheriff so we need to disable it.
        // This may be removed in the future when we have implemented changing vanilla role or some other stuff.
        if (__instance.Role.Role is RoleTypes.GuardianAngel /*|| (__instance.Role.Role is RoleTypes.Phantom && !DebugModeManager.IsDebugMode)*/)
        {
            string disableText = $" ({GetString("Disabled")})";
            // 隐藏+-按钮
            foreach (var button in __instance.GetComponentsInChildren<PassiveButton>())
            {
                button.gameObject.SetActive(false);
            }

            if (!__instance.titleText.text.Contains(disableText))
                __instance.titleText.text += disableText;
            if (__instance.roleChance != 0 || __instance.roleMaxCount != 0)
            {
                __instance.roleChance = 0;
                __instance.roleMaxCount = 0;
                __instance.OnValueChanged.Invoke(__instance);
            }
        }
    }
}

[HarmonyPatch(typeof(GameOptionsManager), nameof(GameOptionsManager.SwitchGameMode))]
class SwitchGameModePatch
{
    public static void Postfix(AmongUs.GameOptions.GameModes gameMode)
    {
        if (gameMode == AmongUs.GameOptions.GameModes.HideNSeek)
        {
            ErrorText.Instance.HnSFlag = true;
            ErrorText.Instance.AddError(ErrorCode.HnsUnload);
            Harmony.UnpatchAll();
            Main.Instance.Unload();
        }
    }
}