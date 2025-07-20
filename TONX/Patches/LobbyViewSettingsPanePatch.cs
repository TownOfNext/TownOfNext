using System;
using System.Collections.Generic;
using AmongUs.GameOptions;
using HarmonyLib;
using TONX.Modules.OptionItems;
using TONX.Modules.OptionItems.Interfaces;
using UnityEngine;
using static TONX.Translator;
using Object = UnityEngine.Object;

namespace TONX;

[HarmonyPatch(typeof(LobbyViewSettingsPane))]
public static class LobbyViewSettingsPanePatch
{
    private static List<PassiveButton> tonxSettingsButton = new List<PassiveButton>();
    public static List<CategoryHeaderMasked> CategoryHeaders = new List<CategoryHeaderMasked>();
    private static Vector3 buttonPosition = new(-6f, 1.4f, 0f);
    private static Vector3 buttonSize = new(0.45f, 0.45f, 1f);

    [HarmonyPatch(nameof(LobbyViewSettingsPane.Awake)), HarmonyPostfix]
    public static void Awake_Postfix(LobbyViewSettingsPane __instance)
    {
        // 调整原版按钮
        var OverviewTab = GameObject.Find("OverviewTab");
        OverviewTab.transform.localScale = buttonSize;
        OverviewTab.transform.localPosition = buttonPosition + new Vector3(0f, 0.18f, 0f);
        var RolesTab = GameObject.Find("RolesTabs");
        RolesTab.transform.localScale = buttonSize;
        RolesTab.transform.localPosition = buttonPosition + new Vector3(1.6f, 0.18f, 0f);

        // 模组按钮
        tonxSettingsButton = new List<PassiveButton>();
        foreach (var tab in Enum.GetValues(typeof(TabGroup)))
        {
            Vector3 offset_up = new (1.6f * ((int)tab + 2), 0.18f, 0f);
            Vector3 offset_down = new (1.6f * ((int)tab - 2), -0.2f, 0f);
            tonxSettingsButton.Add(CreateButton(__instance, tab.ToString() + " VIEWBUTTON", ((int)tab < 2) ? offset_up : offset_down, GetString($"TabGroup.{tab}"), (int)tab + 3551));
        }
        tonxSettingsButton.Add(CreateButton(__instance, "RolesOverview VIEWBUTTON", new Vector3(1.6f * 4, 0.18f, 0f), GetString("ActiveRolesList"), 3558));
    }

    private static PassiveButton CreateButton(LobbyViewSettingsPane __instance, string buttonName, Vector3 offset, string buttonText, int targetMenu)
    {
        var settingsButton = Object.Instantiate(__instance.taskTabButton, __instance.taskTabButton.transform.parent);
        settingsButton.name = buttonName;
        settingsButton.transform.localPosition = buttonPosition + offset;
        settingsButton.transform.localScale = buttonSize;
        settingsButton.buttonText.DestroyTranslator();
        settingsButton.buttonText.text = buttonText;
        settingsButton.OnClick.RemoveAllListeners();
        settingsButton.OnClick.AddListener((Action)(() => 
        {
            __instance.ChangeTab((StringNames)targetMenu);
            settingsButton.SelectButton(true);
        }));
        settingsButton.OnMouseOut.RemoveAllListeners();
        settingsButton.OnMouseOver.RemoveAllListeners();
        return settingsButton;
    }

    [HarmonyPatch(nameof(LobbyViewSettingsPane.ChangeTab)), HarmonyPostfix]
    public static void ChangeTab_Postfix(LobbyViewSettingsPane __instance, StringNames category)
    {
        foreach (var button in tonxSettingsButton)
        {
            button.SelectButton(false);
        }
        if ((int)category < 3551) return;
        __instance.taskTabButton.SelectButton(false);
        CreateCustomOptions(__instance, (int)category == 3558);
    }

    private static void CreateCustomOptions(LobbyViewSettingsPane __instance, bool isRolesOverview)
    {
        // 删除原版gameobject
        foreach (var vanillaOption in __instance.settingsInfo)
        {
            Object.Destroy(vanillaOption.gameObject);
        }
        __instance.settingsInfo.Clear();

        // 模组设置
        CategoryHeaders = new List<CategoryHeaderMasked>();
        var template = __instance.infoPanelOrigin;
        if (isRolesOverview)
        {
            foreach (var kvp in Options.CustomRoleSpawnChances)
            {
                var option = kvp.Value;
                var infoPanelOption = CreateOption(__instance, option, template, option.GetString() + " x " + kvp.Key.GetCount());
                __instance.settingsInfo.Add(infoPanelOption.gameObject);
                option.ViewOptionBehaviour = infoPanelOption;
            }
        }
        else
        {
            foreach (var option in OptionItem.AllOptions)
            {
                if ((int)option.Tab != ((int)__instance.currentTab - 3551)) continue;  
                
                if (option.IsText)
                {
                    var categoryHeader = CreateCategoryHeader(__instance, option);
                    CategoryHeaders.Add(categoryHeader);
                    __instance.settingsInfo.Add(categoryHeader.gameObject);
                    continue;
                }

                var infoPanelOption = CreateOption(__instance, option, template, option.GetString());
                __instance.settingsInfo.Add(infoPanelOption.gameObject);
                option.ViewOptionBehaviour = infoPanelOption;
            }
        }
    }

    private static CategoryHeaderMasked CreateCategoryHeader(LobbyViewSettingsPane __instance, OptionItem option)
    {
        var categoryHeader = Object.Instantiate(__instance.categoryHeaderOrigin, Vector3.zero, Quaternion.identity, __instance.settingsContainer);
        categoryHeader.name = option.Name;
        categoryHeader.Title.text = option.GetName();
        var maskLayer = LobbyViewSettingsPane.MASK_LAYER;
        categoryHeader.Background.material.SetInt(PlayerMaterial.MaskLayer, maskLayer);
        if (categoryHeader.Divider != null)
        {
            categoryHeader.Divider.material.SetInt(PlayerMaterial.MaskLayer, maskLayer);
        }
        categoryHeader.Title.fontMaterial.SetFloat("_StencilComp", 3f);
        categoryHeader.Title.fontMaterial.SetFloat("_Stencil", (float)maskLayer);
        return categoryHeader;
    }

    private static ViewSettingsInfoPanel CreateOption(LobbyViewSettingsPane __instance, OptionItem option, ViewSettingsInfoPanel template, string settingText)
    {
        var infoPanelOption = Object.Instantiate(template, __instance.settingsContainer);
        infoPanelOption.SetMaskLayer(LobbyViewSettingsPane.MASK_LAYER);
        infoPanelOption.titleText.text = option.Name;
        infoPanelOption.settingText.text = settingText;
        infoPanelOption.name = option.Name;

        var indent = 0f;
        var parent = option.Parent;
        while (parent != null)
        {
            indent += 0.15f;
            parent = parent.Parent;
        }

        infoPanelOption.labelBackground.size += new Vector2(2f - indent * 2, 0f);
        infoPanelOption.labelBackground.transform.localPosition += new Vector3(-1f + indent, 0f, 0f);
        infoPanelOption.titleText.rectTransform.sizeDelta += new Vector2(2f - indent * 2, 0f);
        infoPanelOption.titleText.transform.localPosition += new Vector3(-1f + indent, 0f, 0f);

        return infoPanelOption;
    }

    [HarmonyPatch(nameof(LobbyViewSettingsPane.Update)), HarmonyPostfix]
    public static void Update_Postfix(LobbyViewSettingsPane __instance)
    {
        if ((int)__instance.currentTab < 3551) return;

        var isOdd = true;
        var offset = 2f;
        var isFirst = true;

        if ((int)__instance.currentTab == 3558)
        {
            foreach (var kvp in Options.CustomRoleSpawnChances)
            {
                if (isFirst) isFirst = false;
                var option = kvp.Value;
                UpdateOption(ref isOdd, option, ref offset, option.GetString() + " x " + kvp.Key.GetCount());
            }
        }
        else
        {
            foreach (var option in OptionItem.AllOptions)
            {
                if ((int)option.Tab != ((int)__instance.currentTab - 3551)) continue; 
                if (option.IsText)
                {
                    if (isFirst)
                    {
                        offset += 0.3f;
                        isFirst = false;
                    }
                    foreach (var categoryHeader in CategoryHeaders)
                    {
                        if (option.Name == categoryHeader.name)
                        {
                            UpdateCategoryHeader(categoryHeader, option, ref offset);
                            continue;
                        }
                    }
                    continue;
                }
                if (isFirst) isFirst = false;
                UpdateOption(ref isOdd, option, ref offset, option.GetString());
            }
        }
        __instance.scrollBar.ContentYBounds.max = (-offset) - 1.5f;
    }

    private static void UpdateCategoryHeader(CategoryHeaderMasked categoryHeader, OptionItem item, ref float offset)
    {
        var enabled = true;
        // 检测是否隐藏设置
        enabled = (!Options.HideGameSettings.GetBool() || AmongUsClient.Instance.AmHost) && GameStates.IsModHost && !item.IsHiddenOn(Options.CurrentGameMode);
        categoryHeader.gameObject.SetActive(enabled);
        if (enabled)
        {
            offset -= LobbyViewSettingsPane.HEADER_SPACING_Y;
            categoryHeader.transform.localPosition = new(LobbyViewSettingsPane.HEADER_START_X, offset, -2f);
        }
    }

    private static void UpdateOption(ref bool isOdd, OptionItem option, ref float offset, string settingText)
    {
        if (option?.ViewOptionBehaviour == null || option.ViewOptionBehaviour.gameObject == null) return;

        var enabled = true;
        var parent = option.Parent;

        // 检测是否隐藏设置
        enabled = !option.IsHiddenOn(Options.CurrentGameMode) && (!Options.HideGameSettings.GetBool() || AmongUsClient.Instance.AmHost) && GameStates.IsModHost;
        var infoPanelOption = option.ViewOptionBehaviour;
        while (parent != null && enabled)
        {
            enabled = parent.GetBool();
            parent = parent.Parent;
        }

        infoPanelOption.gameObject.SetActive(enabled);
        
        if (enabled)
        {
            infoPanelOption.labelBackground.color = option is IRoleOptionItem roleOption ? roleOption.RoleColor : (isOdd ? Color.cyan : Color.white);
            infoPanelOption.titleText.text = option.GetName(option is RoleSpawnChanceOptionItem);
            infoPanelOption.settingText.text = settingText;

            offset -= LobbyViewSettingsPane.SPACING_Y;
            if (option.IsHeader)
            {
                offset -= HeaderSpacingY;
            }
            infoPanelOption.transform.localPosition = new Vector3(
                LobbyViewSettingsPane.START_POS_X + 2f,
                offset,
                -2f);

            isOdd = !isOdd;
        }
    }
    private const float HeaderSpacingY = 0.2f;
}