using TMPro;
using TONX.Modules;
using TONX.Modules.OptionItems;
using TONX.Modules.OptionItems.Interfaces;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TONX;

[HarmonyPatch(typeof(GameSettingMenu))]
public static class GameSettingMenuPatch
{
    private static List<GameOptionsMenu> tonxSettingsTab = new List<GameOptionsMenu>();
    private static List<PassiveButton> tonxSettingsButton = new List<PassiveButton>();
    public static List<string> TONXMenuName = new List<string>();
    public static List<CategoryHeaderMasked> CategoryHeaders = new List<CategoryHeaderMasked>();
    // 左侧按钮坐标
    private static Vector3 buttonPosition = new(-2.55f, -0.1f, 0f);
    // 本体按钮大小
    private static Vector3 buttonSize = new(0.45f, 0.6f, 1f);

    [HarmonyPatch(nameof(GameSettingMenu.Start)), HarmonyPostfix]
    public static void Start_Postfix(GameSettingMenu __instance)
    {
        // 重置列表
        tonxSettingsTab = new List<GameOptionsMenu>();
        tonxSettingsButton = new List<PassiveButton>();
        TONXMenuName = new List<string>();
        CategoryHeaders = new List<CategoryHeaderMasked>();

        __instance.GamePresetsButton.transform.parent.localPosition = buttonPosition;
        __instance.GamePresetsButton.transform.parent.localScale = buttonSize;

        // TONX设置按钮
        foreach (var tab in Enum.GetValues(typeof(TabGroup)))
        {
            Vector3 offset_left = new (0f, 0.64f * ((int)tab + 3) - 0.64f, 0f);
            Vector3 offset_right = new (-3f, 0.64f * ((int)tab - 2) - 0.64f, 0f);
            Color32 buttonColor = tab switch
            {
                TabGroup.SystemSettings => Main.ModColor32,
                TabGroup.GameSettings => new Color32(89, 239, 131, 255),
                TabGroup.ImpostorRoles => Utils.GetCustomRoleTypeColor(CustomRoleTypes.Impostor),
                TabGroup.CrewmateRoles => Utils.GetCustomRoleTypeColor(CustomRoleTypes.Crewmate),
                TabGroup.NeutralRoles => Utils.GetCustomRoleTypeColor(CustomRoleTypes.Neutral),
                TabGroup.Addons => Utils.GetCustomRoleTypeColor(CustomRoleTypes.Addon),
                TabGroup.OtherRoles => new Color32(118, 184, 224, 255),
                _ => Color.white,
            };

            var settingsTab = Object.Instantiate(__instance.GameSettingsTab, __instance.GameSettingsTab.transform.parent);
            settingsTab.name = tab.ToString() + " TAB";
            TONXMenuName.Add(settingsTab.name);
            var vanillaOptions = settingsTab.GetComponentsInChildren<OptionBehaviour>();
            foreach (var vanillaOption in vanillaOptions)
            {
                Object.Destroy(vanillaOption.gameObject);
            }

            var settingsButton = Object.Instantiate(__instance.GameSettingsButton, __instance.GameSettingsButton.transform.parent);
            settingsButton.name = tab.ToString() + " BUTTON";
            settingsButton.transform.localPosition -= ((int)tab < 2) ? offset_left : offset_right;
            settingsButton.buttonText.DestroyTranslator();
            settingsButton.buttonText.text = Utils.ColorString(buttonColor, GetString($"TabGroup.{tab}"));
            var activeSprite = settingsButton.activeSprites.GetComponent<SpriteRenderer>();
            var selectedSprite = settingsButton.selectedSprites.GetComponent<SpriteRenderer>();
            var inactiveSprite = settingsButton.inactiveSprites.GetComponent<SpriteRenderer>();
            activeSprite.color = selectedSprite.color = inactiveSprite.color = buttonColor;
            settingsButton.OnClick.AddListener((Action)(() =>
            {
                ControllerManager.Instance.OpenOverlayMenu(settingsTab.name, GameSettingMenu.Instance.BackButton);
                __instance.ChangeTab((int)tab + 3, false); // 关闭原版标签页
                settingsTab.gameObject.SetActive(true);
                __instance.MenuDescriptionText.text = GetString($"MenuDescriptionText.{tab}");
                settingsButton.SelectButton(true);
            }));

            // 生成Tab图标
            var settingsImage = CreateTabImage(__instance, settingsTab, $"TONX.Resources.Images.TabIcon_{tab}.png");

            // 创建每个设置项的设置按钮
            var template = __instance.GameSettingsTab.stringOptionOrigin;
            var scOptions = new Il2CppSystem.Collections.Generic.List<OptionBehaviour>();
            foreach (var option in OptionItem.AllOptions)
            {
                if (option.Tab != (TabGroup)tab) continue;
                if (option.OptionBehaviour == null)
                {
                    if (option.IsText)
                    {
                        CategoryHeaders.Add(CreateCategoryHeader(__instance, settingsTab, option));
                        continue;
                    }
                    var stringOption = Object.Instantiate(template, settingsTab.settingsContainer);
                    scOptions.Add(stringOption);
                    stringOption.SetClickMask(__instance.GameSettingsButton.ClickMask);
                    stringOption.SetUpFromData(stringOption.data, GameOptionsMenu.MASK_LAYER);
                    stringOption.OnValueChanged = new Action<OptionBehaviour>((o) => { });
                    stringOption.TitleText.text = option.Name;
                    stringOption.Value = stringOption.oldValue = option.CurrentValue;
                    stringOption.ValueText.text = option.GetString();
                    stringOption.name = option.Name;

                    // 放大标题框
                    var indent = 0f; // 如果有父项则使边框左侧缩进
                    var parent = option.Parent;
                    while (parent != null)
                    {
                        indent += 0.15f;
                        parent = parent.Parent;
                    }
                    stringOption.LabelBackground.size += new Vector2(2f - indent * 2, 0f);
                    stringOption.LabelBackground.transform.localPosition += new Vector3(-1f + indent, 0f, 0f);
                    stringOption.TitleText.rectTransform.sizeDelta += new Vector2(2f - indent * 2, 0f);
                    stringOption.TitleText.transform.localPosition += new Vector3(-1f + indent, 0f, 0f);

                    option.OptionBehaviour = stringOption;
                }
                option.OptionBehaviour.gameObject.SetActive(true);
            }
            settingsTab.Children = scOptions;
            settingsTab.gameObject.SetActive(false);

            // 存储模组设置按钮
            tonxSettingsTab.Add(settingsTab);
            tonxSettingsButton.Add(settingsButton);
        }
    }
    private static MapSelectButton CreateTabImage(GameSettingMenu __instance, GameOptionsMenu tonxTab, string resourcePath)
    {
        var image = Utils.LoadSprite(resourcePath, 100f);
        var tabImage = Object.Instantiate(__instance.GameSettingsTab.MapPicker.MapButtonOrigin, Vector3.zero, Quaternion.identity, tonxTab.transform);
        tabImage.SetImage(image, GameOptionsMenu.MASK_LAYER);
        tabImage.transform.localPosition = new(7.3f, -0.6f, -10f);
        Object.Destroy(tabImage.Button.GetComponentInChildren<BoxCollider2D>());
        tabImage.Button.activeSprites.transform.GetChild(0).gameObject.SetActive(false);
        tabImage.Button.activeSprites.GetComponent<SpriteRenderer>().sprite = tabImage.Button.inactiveSprites.GetComponent<SpriteRenderer>().sprite = null;
        return tabImage;
    }
    private static CategoryHeaderMasked CreateCategoryHeader(GameSettingMenu __instance, GameOptionsMenu tonxTab, OptionItem option)
    {
        var categoryHeader = Object.Instantiate(__instance.GameSettingsTab.categoryHeaderOrigin, Vector3.zero, Quaternion.identity, tonxTab.settingsContainer);
        categoryHeader.name = option.Name;
        categoryHeader.Title.text = option.GetName();
        var maskLayer = GameOptionsMenu.MASK_LAYER;
        categoryHeader.Background.material.SetInt(PlayerMaterial.MaskLayer, maskLayer);
        if (categoryHeader.Divider != null)
        {
            categoryHeader.Divider.material.SetInt(PlayerMaterial.MaskLayer, maskLayer);
        }
        categoryHeader.Title.fontMaterial.SetFloat("_StencilComp", 3f);
        categoryHeader.Title.fontMaterial.SetFloat("_Stencil", (float)maskLayer);
        categoryHeader.transform.localScale = Vector3.one * GameOptionsMenu.HEADER_SCALE;
        return categoryHeader;
    }

    // 为了避免首次显示角色设置时出现的异常（原版Bug）的Prefix
    [HarmonyPatch(nameof(GameSettingMenu.ChangeTab)), HarmonyPrefix]
    public static bool ChangeTab_Prefix(bool previewOnly)
    {
        if (!previewOnly)
        {
            foreach (var tab in tonxSettingsTab)
            {
                if (tab)
                {
                    tab.gameObject.SetActive(false);
                }
            }
            foreach (var button in tonxSettingsButton)
            {
                if (button)
                {
                    button.SelectButton(false);
                }
            }
        }
        return true;
    }
}

[HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Initialize))]
public static class GameOptionsMenuInitializePatch
{
    public static void Postfix(GameOptionsMenu __instance)
    {
        foreach (var ob in __instance.Children)
        {
            switch (ob.Title)
            {
                case StringNames.GameShortTasks:
                case StringNames.GameLongTasks:
                case StringNames.GameCommonTasks:
                    ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 99);
                    break;
                case StringNames.GameKillCooldown:
                    ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                    break;
                case StringNames.GameNumImpostors:
                    if (DebugModeManager.IsDebugMode)
                    {
                        ob.Cast<NumberOption>().ValidRange.min = 0;
                    }
                    break;
                default:
                    break;
            }
        }
    }
}

[HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
public class GameOptionsMenuUpdatePatch
{
    private static float _timer = 1f;

    public static void Postfix(GameOptionsMenu __instance)
    {
        if (!GameSettingMenuPatch.TONXMenuName.Contains(__instance.name)) return;

        foreach (var tab in Enum.GetValues(typeof(TabGroup)))
        {
            if (__instance.name != tab.ToString() + " TAB") continue;

            _timer += Time.deltaTime;
            if (_timer < 0.1f) return;
            _timer = 0f;

            var offset = 2.4f;
            var isFirst = true;

            foreach (var option in OptionItem.AllOptions)
            {
                if ((TabGroup)tab != option.Tab) continue;
                if (option.IsText)
                {
                    if (isFirst)
                    {
                        offset += 0.3f;
                        isFirst = false;
                    }
                    foreach (var categoryHeader in GameSettingMenuPatch.CategoryHeaders)
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
                UpdateOption(option, ref offset);
            }

            __instance.scrollBar.ContentYBounds.max = (-offset) - 1.5f;
        }
    }
    private static void UpdateCategoryHeader(CategoryHeaderMasked categoryHeader, OptionItem item, ref float offset)
    {
        var enabled = true;
        enabled = AmongUsClient.Instance.AmHost && !item.IsHiddenOn(Options.CurrentGameMode);
        categoryHeader.gameObject.SetActive(enabled);
        if (enabled)
        {
            offset -= GameOptionsMenu.HEADER_HEIGHT;
            categoryHeader.transform.localPosition = new(GameOptionsMenu.HEADER_X, offset, -2f);
        }
    }
    private static void UpdateOption(OptionItem item, ref float offset)
    {
        if (item?.OptionBehaviour == null || item.OptionBehaviour.gameObject == null) return;

        var enabled = true;
        var parent = item.Parent;

        // 查看父选项值并决定是否显示
        enabled = AmongUsClient.Instance.AmHost && !item.IsHiddenOn(Options.CurrentGameMode);
        var stringOption = item.OptionBehaviour;
        while (parent != null && enabled)
        {
            enabled = parent.GetBool();
            parent = parent.Parent;
        }
        
        item.OptionBehaviour.gameObject.SetActive(enabled);

        if (enabled)
        {
            // 交替改变颜色以方便查看
            stringOption.LabelBackground.color = item is IRoleOptionItem roleOption ? roleOption.RoleColor : Color.white;

            offset -= GameOptionsMenu.SPACING_Y;
            if (item.IsHeader)
            {
                // Header间隙增大
                offset -= HeaderSpacingY;
            }
            item.OptionBehaviour.transform.localPosition = new Vector3(
                GameOptionsMenu.START_POS_X,
                offset,
                -2f);
        }
    }

    private const float HeaderSpacingY = 0.2f;
}

[HarmonyPatch(typeof(StringOption), nameof(StringOption.Initialize))]
public class StringOptionInitializePatch
{
    public static bool Prefix(StringOption __instance)
    {
        var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
        if (option == null) return true;

        __instance.OnValueChanged = new Action<OptionBehaviour>((o) => { });
        __instance.TitleText.text = option.GetName(option is RoleSpawnChanceOptionItem);
        __instance.Value = __instance.oldValue = option.CurrentValue;
        __instance.ValueText.text = option.GetString();
        if (option is RoleSpawnChanceOptionItem item && !GameObject.Find(option.Name + "CustomRoleInfo"))
        {
            var infoButton = Object.Instantiate(__instance.PlusBtn, __instance.PlusBtn.transform.parent);
            infoButton.name = option.Name + "CustomRoleInfo";
            infoButton.transform.localPosition += new Vector3(0.7f, 0f, 0f);
            infoButton.GetComponentInChildren<TextMeshPro>().text = "?";
            infoButton.interactableHoveredColor = Main.ModColor32;
            infoButton.interactableClickColor = new Color32(161, 121, 128, 255);
            infoButton.OnClick = new();
            infoButton.OnClick.AddListener((Action)(() =>
            {
                InGameRoleInfoMenu.SetRoleInfoRefByRole(item.RoleId);
                InGameRoleInfoMenu.Show();
            }));
            infoButton.gameObject.SetActive(true);
        }

        return false;
    }
    public static void Postfix(StringOption __instance)
    {
        __instance.PlusBtn.interactableHoveredColor = __instance.MinusBtn.interactableHoveredColor = Main.ModColor32;
        __instance.PlusBtn.interactableClickColor = __instance.MinusBtn.interactableClickColor = new Color32(161, 121, 128, 255);
    }
}
[HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Initialize))]
public class NumberOptionInitializePatch
{
    public static void Postfix(NumberOption __instance)
    {
        __instance.PlusBtn.interactableHoveredColor = __instance.MinusBtn.interactableHoveredColor = Main.ModColor32;
        __instance.PlusBtn.interactableClickColor = __instance.MinusBtn.interactableClickColor = new Color32(161, 121, 128, 255);
    }
}

[HarmonyPatch(typeof(StringOption), nameof(StringOption.Increase))]
public class StringOptionIncreasePatch
{
    public static bool Prefix(StringOption __instance)
    {
        var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
        if (option == null) return true;

        option.SetValue(option.CurrentValue + (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 5 : 1));
        return false;
    }
}

[HarmonyPatch(typeof(StringOption), nameof(StringOption.Decrease))]
public class StringOptionDecreasePatch
{
    public static bool Prefix(StringOption __instance)
    {
        var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
        if (option == null) return true;

        option.SetValue(option.CurrentValue - (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 5 : 1));
        return false;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSyncSettings))]
public class RpcSyncSettingsPatch
{
    public static void Postfix()
    {
        OptionItem.SyncAllOptions();
    }
}
[HarmonyPatch(typeof(RolesSettingsMenu), nameof(RolesSettingsMenu.InitialSetup))]
public static class RolesSettingsMenuPatch
{
    public static void Postfix(RolesSettingsMenu __instance)
    {
        foreach (var ob in __instance.advancedSettingChildren)
        {
            switch (ob.Title)
            {
                case StringNames.ScientistCooldown:
                    ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                    break;
                case StringNames.EngineerCooldown:
                    ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                    break;
                case StringNames.TrackerCooldown:
                    ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                    break;
                case StringNames.ShapeshifterCooldown:
                    ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                    break;
                case StringNames.PhantomCooldown:
                    ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                    break;
                default:
                    break;
            }
        }
    }
}
