namespace TONX;

public static class AprilFoolsModePatch
{
    public static bool FlipSkeld => AprilFoolsMode.ShouldFlipSkeld();
    public static bool HorseMode => AprilFoolsMode.ShouldHorseAround();
    public static bool LongMode => AprilFoolsMode.ShouldLongAround();
}

#region GameManager Patches

[HarmonyPatch(typeof(NormalGameManager), nameof(NormalGameManager.GetBodyType))]
public static class GetBodyTypePatch
{
    public static PlayerBodyTypes LastPlayerBodyType;
    public static void Postfix(ref PlayerBodyTypes __result)
    {
        switch (Main.SwitchOutfitType.Value)
        {
            case OutfitType.HorseMode:
                __result = PlayerBodyTypes.Horse;
                break;
            case OutfitType.LongMode:
                __result = PlayerBodyTypes.Long;
                break;
            default:
                __result = PlayerBodyTypes.Normal;
                break;
        }
        if (__result != LastPlayerBodyType)
        {
            if (LastPlayerBodyType == PlayerBodyTypes.Long)
            {
                var pc = PlayerControl.LocalPlayer;
                pc.cosmetics.SetNamePosition(new(0f, string.IsNullOrEmpty(pc.Data.DefaultOutfit.HatId) ? 0.8f : 1f, -0.5f));
            }
            LastPlayerBodyType = __result;
        }
    }
}

#endregion

#region LongBoi Patches

[HarmonyPatch(typeof(LongBoiPlayerBody))]
public static class LongBoiPatch
{
    [HarmonyPatch(nameof(LongBoiPlayerBody.Awake)), HarmonyPrefix]
    public static bool Awake_Prefix(LongBoiPlayerBody __instance)
    {
        __instance.cosmeticLayer.OnSetBodyAsGhost += (Action)__instance.SetPoolableGhost;
        __instance.cosmeticLayer.OnColorChange += (Action<int>)__instance.SetHeightFromColor;
        __instance.cosmeticLayer.OnCosmeticSet += (Action<string, int, CosmeticsLayer.CosmeticKind>)__instance.OnCosmeticSet;
        __instance.gameObject.layer = 8;
        return false;
    }

    [HarmonyPatch(nameof(LongBoiPlayerBody.Start)), HarmonyPrefix]
    public static bool Start_Prefix(LongBoiPlayerBody __instance)
    {
        __instance.ShouldLongAround = true;
        if (__instance.hideCosmeticsQC) __instance.cosmeticLayer.SetHatVisorVisible(false);

        __instance.SetupNeckGrowth();
        if (__instance.isExiledPlayer)
        {
            var instance = ShipStatus.Instance;
            if (instance == null || instance.Type != ShipStatus.MapType.Fungle)
                __instance.cosmeticLayer.AdjustCosmeticRotations(-17.75f);
        }

        if (!__instance.isPoolablePlayer) __instance.cosmeticLayer.ValidateCosmetics();

        return false;
    }

    // 修复索引为255时超出范围的问题
    [HarmonyPatch(nameof(LongBoiPlayerBody.SetHeightFromColor)), HarmonyPrefix]
    public static bool SetHeightFromColor_Prefix(int colorIndex)
    {
        return colorIndex != byte.MaxValue;
    }

    [HarmonyPatch(typeof(HatManager), nameof(HatManager.CheckLongModeValidCosmetic)), HarmonyPrefix]
    public static bool CheckLongModeValidCosmetic_Prefix(out bool __result, ref string cosmeticID)
    {
        if (AprilFoolsModePatch.HorseMode)
        {
            __result = true;
            return false;
        }

        var flag = AprilFoolsModePatch.LongMode;

        if (flag && string.Equals("skin_rhm", cosmeticID))
        {
            __result = false;
            return false;
        }

        __result = true;
        return false;
    }
}

#endregion