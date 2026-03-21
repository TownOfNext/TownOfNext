using UnityEngine;
using Object = UnityEngine.Object;

namespace TONX;

[HarmonyPatch(typeof(AprilFoolsMode))]
public static class AprilFoolsModePatch
{
    public static bool FlipSkeld => AprilFoolsMode.ShouldFlipSkeld();
    public static bool HorseMode => AprilFoolsMode.ShouldHorseAround();
    public static bool LongMode => AprilFoolsMode.ShouldLongAround();
    public static bool EnableFlipSkeld = false;

    [HarmonyPatch(nameof(AprilFoolsMode.ShouldFlipSkeld)), HarmonyPrefix]
    public static bool ShouldFlipSkeld_Prefix(ref bool __result)
    {
        __result = EnableFlipSkeld
            && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "FindAGame"
            && (!GameObject.Find("FreeplayPopover")?.active ?? true);
        return false;
    }

    /*
    [HarmonyPatch(nameof(AprilFoolsMode.ShouldHorseAround)), HarmonyPrefix]
    public static bool ShouldHorseAround_Prefix(ref bool __result)
    {
        __result = Main.SwitchOutfitType.Value == OutfitType.HorseMode;
        return false;
    }
    [HarmonyPatch(nameof(AprilFoolsMode.ShouldLongAround)), HarmonyPrefix]
    public static bool ShouldLongAround_Prefix(ref bool __result)
    {
        __result = Main.SwitchOutfitType.Value == OutfitType.LongMode;
        return false;
    }
    */
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
                foreach (var pc in Main.AllPlayerControls)
                    pc?.cosmetics?.SetNamePosition(new(0f, string.IsNullOrEmpty(pc.Data.DefaultOutfit.HatId) ? 0.8f : 1f, -0.5f));
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

#region Dleks Patches

[HarmonyPatch(typeof(GameOptionsMapPicker))]
public static class GameOptionsMapPickerPatch
{
    [HarmonyPatch(typeof(CreateGameMapPicker), nameof(CreateGameMapPicker.Initialize))]
    [HarmonyPatch(typeof(GameOptionsMapPicker), nameof(GameOptionsMapPicker.Initialize))]
    [HarmonyPrefix]
    public static void Initialize_Prefix(GameOptionsMapPicker __instance)
    {
        if (__instance.AllMapIcons.ToArray().Any(m => m.Name == MapNames.Dleks)) return;
        MapIconByName thisVal = new MapIconByName
        {
            Name = MapNames.Dleks,
            MapIcon = __instance.AllMapIcons[0].MapIcon,
            MapImage = __instance.AllMapIcons[0].MapImage,
            NameImage = __instance.AllMapIcons[0].NameImage
        };
        __instance.AllMapIcons.Insert(3, thisVal);
    }

    [HarmonyPatch(nameof(GameOptionsMapPicker.SetupMapButtons)), HarmonyPostfix]
    public static void SetupMapButtons_Postfix(GameOptionsMapPicker __instance)
    {
        for (var i = 0; i < __instance.mapButtons.Count; i++)
        {
            var btn = __instance.mapButtons[i];
            MapIconByName thisVal = __instance.AllMapIcons[i];
            btn.Button.OnClick.RemoveAllListeners();
            btn.Button.OnClick.AddListener((Action)(() =>
            {
                if (__instance.selectedButton)
                {
                    __instance.selectedButton.Button.SelectButton(isSelected: false);
                }
                __instance.selectedButton = btn;
                __instance.selectedButton.Button.SelectButton(isSelected: true);
                AprilFoolsModePatch.EnableFlipSkeld = (int)thisVal.Name == 3;
                __instance.SelectMap((int)thisVal.Name == 3 ? __instance.AllMapIcons[0] : thisVal);
            }));
        }

        if (AprilFoolsModePatch.EnableFlipSkeld && __instance.selectedMapId == 0)
        {
            __instance.mapButtons[0].Button.SelectButton(isSelected: false);
            __instance.mapButtons[3].Button.SelectButton(isSelected: true);
            __instance.selectedButton = __instance.mapButtons[3];
        }
    }

    [HarmonyPatch(nameof(GameOptionsMapPicker.SelectMap), new Type[] { typeof(int) }), HarmonyPrefix]
    public static bool SelectMapTypeOfInt_Prefix(GameOptionsMapPicker __instance, [HarmonyArgument(0)] int mapId)
    {
        if (mapId != 3) return true;
        AprilFoolsModePatch.EnableFlipSkeld = true;
        __instance.SelectMap(0);
        return false;
    }
}

[HarmonyPatch(typeof(FreeplayPopover))] 
public static class FreeplayPopoverPatch
{
    [HarmonyPatch(nameof(FreeplayPopover.Awake)), HarmonyPrefix]
    public static void FreeplayPopover_Awake_Prefix(FreeplayPopover __instance)
    {
        FreeplayPopoverButton prefab = __instance.buttons[0];
        FreeplayPopoverButton thisBtn = Object.Instantiate(prefab, prefab.transform.parent);
        thisBtn.map = MapNames.Dleks;
        thisBtn.button.GetComponent<SpriteRenderer>().flipX = true;
        var btns = __instance.buttons.ToList();
        btns.Insert(3, thisBtn);
        __instance.buttons = btns.ToArray();
        __instance.buttons[3].transform.localPosition = new Vector3(1.25f, 0.00f, 0.00f);
        __instance.buttons[4].transform.localPosition = new Vector3(-1.25f, -0.75f, 0.00f);
        __instance.buttons[5].transform.localPosition = new Vector3(1.25f, -0.75f, 0.00f);
    }
}

#endregion