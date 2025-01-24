using System;
using System.Collections.Immutable;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using TONX.Modules.OptionItems;
using TONX.Modules.OptionItems.Interfaces;
using UnityEngine;
using UnityEngine.UI;
using static TONX.Translator;
using Object = UnityEngine.Object;

namespace TONX
{
    [HarmonyPatch(typeof(GameSettingMenu))]
    public static class GameSettingMenuPatch
    {
        private static GameOptionsMenu tonxSettingsTab;
        private static PassiveButton tonxSettingsButton;
        public static CategoryHeaderMasked SystemSettingsCategoryHeader { get; private set; }
        public static CategoryHeaderMasked GameSettingsCategoryHeader { get; private set; }
        public static CategoryHeaderMasked ImpostorRoleCategoryHeader { get; private set; }
        public static CategoryHeaderMasked CrewmateRoleCategoryHeader { get; private set; }
        public static CategoryHeaderMasked NeutralRoleCategoryHeader { get; private set; }
        public static CategoryHeaderMasked AddOnCategoryHeader { get; private set; }
        public static CategoryHeaderMasked OtherRoleCategoryHeader { get; private set; }

        [HarmonyPatch(nameof(GameSettingMenu.Start)), HarmonyPostfix]
        public static void StartPostfix(GameSettingMenu __instance)
        {
            tonxSettingsTab = Object.Instantiate(__instance.GameSettingsTab, __instance.GameSettingsTab.transform.parent);
            tonxSettingsTab.name = TONXMenuName;
            var vanillaOptions = tonxSettingsTab.GetComponentsInChildren<OptionBehaviour>();
            foreach (var vanillaOption in vanillaOptions)
            {
                Object.Destroy(vanillaOption.gameObject);
            }

            // TONX設定ボタンのスペースを作るため，左側の要素を上に詰める
            var gameSettingsLabel = __instance.transform.Find("GameSettingsLabel");
            if (gameSettingsLabel)
            {
                gameSettingsLabel.localPosition += Vector3.up * 0.2f;
            }
            __instance.MenuDescriptionText.transform.parent.localPosition += Vector3.up * 0.4f;
            __instance.GamePresetsButton.transform.parent.localPosition += Vector3.up * 0.5f;

            // TONX設定ボタン
            tonxSettingsButton = Object.Instantiate(__instance.GameSettingsButton, __instance.GameSettingsButton.transform.parent);
            tonxSettingsButton.name = "TONXSettingsButton";
            tonxSettingsButton.transform.localPosition = __instance.RoleSettingsButton.transform.localPosition + (__instance.RoleSettingsButton.transform.localPosition - __instance.GameSettingsButton.transform.localPosition);
            tonxSettingsButton.buttonText.DestroyTranslator();
            tonxSettingsButton.buttonText.text = GetString("TONXSettingsButtonLabel");
            var activeSprite = tonxSettingsButton.activeSprites.GetComponent<SpriteRenderer>();
            var selectedSprite = tonxSettingsButton.selectedSprites.GetComponent<SpriteRenderer>();
            activeSprite.color = selectedSprite.color = Main.UnityModColor;
            tonxSettingsButton.OnClick.AddListener((Action)(() =>
            {
                __instance.ChangeTab(-1, false);  // バニラタブを閉じる
                tonxSettingsTab.gameObject.SetActive(true);
                __instance.MenuDescriptionText.text = GetString("TONXSettingsDescription");
                tonxSettingsButton.SelectButton(true);
            }));

            // 各カテゴリの見出しを作成
            SystemSettingsCategoryHeader = CreateCategoryHeader(__instance, tonxSettingsTab, "TabGroup.SystemSettings");
            GameSettingsCategoryHeader = CreateCategoryHeader(__instance, tonxSettingsTab, "TabGroup.GameSettings");
            ImpostorRoleCategoryHeader = CreateCategoryHeader(__instance, tonxSettingsTab, "TabGroup.ImpostorRoles");
            CrewmateRoleCategoryHeader = CreateCategoryHeader(__instance, tonxSettingsTab, "TabGroup.CrewmateRoles");
            NeutralRoleCategoryHeader = CreateCategoryHeader(__instance, tonxSettingsTab, "TabGroup.NeutralRoles");
            AddOnCategoryHeader = CreateCategoryHeader(__instance, tonxSettingsTab, "TabGroup.Addons");
            OtherRoleCategoryHeader = CreateCategoryHeader(__instance, tonxSettingsTab, "TabGroup.OtherRoles");

            // 各設定スイッチを作成
            var template = __instance.GameSettingsTab.stringOptionOrigin;
            var scOptions = new Il2CppSystem.Collections.Generic.List<OptionBehaviour>();
            foreach (var option in OptionItem.AllOptions)
            {
                if (option.OptionBehaviour == null)
                {
                    var stringOption = Object.Instantiate(template, tonxSettingsTab.settingsContainer);
                    scOptions.Add(stringOption);
                    if (option is not TextOptionItem)
                    {
                        stringOption.SetClickMask(__instance.GameSettingsButton.ClickMask);
                        stringOption.SetUpFromData(stringOption.data, GameOptionsMenu.MASK_LAYER);
                    }
                    stringOption.OnValueChanged = new Action<OptionBehaviour>((o) => { });
                    stringOption.TitleText.text = option.Name;
                    stringOption.Value = stringOption.oldValue = option.CurrentValue;
                    stringOption.ValueText.text = option.GetString();
                    stringOption.name = option.Name;

                    // タイトルの枠をデカくする
                    var indent = 0f;  // 親オプションがある場合枠の左を削ってインデントに見せる
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
            tonxSettingsTab.Children = scOptions;
            tonxSettingsTab.gameObject.SetActive(false);

            // 各カテゴリまでスクロールするボタンを作成
            var jumpButtonY = -0.45f;
            var jumpToSysButton = CreateJumpToCategoryButton(__instance, tonxSettingsTab, "TONX.Resources.Images.TabIcon_SystemSettings.png", ref jumpButtonY, SystemSettingsCategoryHeader);
            var jumpToGameButton = CreateJumpToCategoryButton(__instance, tonxSettingsTab, "TONX.Resources.Images.TabIcon_GameSettings.png", ref jumpButtonY, GameSettingsCategoryHeader);
            var jumpToImpButton = CreateJumpToCategoryButton(__instance, tonxSettingsTab, "TONX.Resources.Images.TabIcon_ImpostorRoles.png", ref jumpButtonY, ImpostorRoleCategoryHeader);
            var jumpToCrewButton = CreateJumpToCategoryButton(__instance, tonxSettingsTab, "TONX.Resources.Images.TabIcon_CrewmateRoles.png", ref jumpButtonY, CrewmateRoleCategoryHeader);
            var jumpToNeutralButton = CreateJumpToCategoryButton(__instance, tonxSettingsTab, "TONX.Resources.Images.TabIcon_NeutralRoles.png", ref jumpButtonY, NeutralRoleCategoryHeader);
            var jumpToAddOnButton = CreateJumpToCategoryButton(__instance, tonxSettingsTab, "TONX.Resources.Images.TabIcon_Addons.png", ref jumpButtonY, AddOnCategoryHeader);
            var jumpToOtherButton = CreateJumpToCategoryButton(__instance, tonxSettingsTab, "TONX.Resources.Images.TabIcon_OtherRoles.png", ref jumpButtonY, OtherRoleCategoryHeader);
        }
        private static MapSelectButton CreateJumpToCategoryButton(GameSettingMenu __instance, GameOptionsMenu tonxTab, string resourcePath, ref float localY, CategoryHeaderMasked jumpTo)
        {
            var image = Utils.LoadSprite(resourcePath, 100f);
            var button = Object.Instantiate(__instance.GameSettingsTab.MapPicker.MapButtonOrigin, Vector3.zero, Quaternion.identity, tonxTab.transform);
            button.SetImage(image, GameOptionsMenu.MASK_LAYER);
            button.transform.localPosition = new(7.1f, localY, -10f);
            button.Button.ClickMask = tonxTab.ButtonClickMask;
            button.Button.OnClick.AddListener((Action)(() =>
            {
                tonxTab.scrollBar.velocity = Vector2.zero;  // ドラッグの慣性によるスクロールを止める
                var relativePosition = tonxTab.scrollBar.transform.InverseTransformPoint(jumpTo.transform.position);  // Scrollerのローカル空間における座標に変換
                var scrollAmount = CategoryJumpY - relativePosition.y;
                tonxTab.scrollBar.Inner.localPosition = tonxTab.scrollBar.Inner.localPosition + Vector3.up * scrollAmount;  // 強制スクロール
                tonxTab.scrollBar.ScrollRelative(Vector2.zero);  // スクロール範囲内に収め，スクロールバーを更新する
            }));
            button.Button.activeSprites.transform.GetChild(0).gameObject.SetActive(false);  // チェックボックスを消す
            localY -= JumpButtonSpacing;
            return button;
        }
        private const float JumpButtonSpacing = 0.6f;
        // ジャンプしたカテゴリヘッダのScrollerとの相対Y座標がこの値になる
        private const float CategoryJumpY = 2f;
        private static CategoryHeaderMasked CreateCategoryHeader(GameSettingMenu __instance, GameOptionsMenu tonxTab, string translationKey)
        {
            var categoryHeader = Object.Instantiate(__instance.GameSettingsTab.categoryHeaderOrigin, Vector3.zero, Quaternion.identity, tonxTab.settingsContainer);
            categoryHeader.name = translationKey;
            categoryHeader.Title.text = GetString(translationKey);
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

        // 初めてロール設定を表示したときに発生する例外(バニラバグ)の影響を回避するためPrefix
        [HarmonyPatch(nameof(GameSettingMenu.ChangeTab)), HarmonyPrefix]
        public static void ChangeTabPrefix(bool previewOnly)
        {
            if (!previewOnly)
            {
                if (tonxSettingsTab)
                {
                    tonxSettingsTab.gameObject.SetActive(false);
                }
                if (tonxSettingsButton)
                {
                    tonxSettingsButton.SelectButton(false);
                }
            }
        }

        public const string TONXMenuName = "TownOfNextTab";
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
            if (__instance.name != GameSettingMenuPatch.TONXMenuName) return;

            _timer += Time.deltaTime;
            if (_timer < 0.1f) return;
            _timer = 0f;

            var offset = 2.7f;
            var isOdd = true;

            UpdateCategoryHeader(GameSettingMenuPatch.SystemSettingsCategoryHeader, ref offset);
            foreach (var option in OptionItem.SystemSettingsOptions)
            {
                UpdateOption(ref isOdd, option, ref offset);
            }
            UpdateCategoryHeader(GameSettingMenuPatch.GameSettingsCategoryHeader, ref offset);
            foreach (var option in OptionItem.GameSettingsOptions)
            {
                UpdateOption(ref isOdd, option, ref offset);
            }
            UpdateCategoryHeader(GameSettingMenuPatch.ImpostorRoleCategoryHeader, ref offset);
            foreach (var option in OptionItem.ImpostorRoleOptions)
            {
                UpdateOption(ref isOdd, option, ref offset);
            }
            UpdateCategoryHeader(GameSettingMenuPatch.CrewmateRoleCategoryHeader, ref offset);
            foreach (var option in OptionItem.CrewmateRoleOptions)
            {
                UpdateOption(ref isOdd, option, ref offset);
            }
            UpdateCategoryHeader(GameSettingMenuPatch.NeutralRoleCategoryHeader, ref offset);
            foreach (var option in OptionItem.NeutralRoleOptions)
            {
                UpdateOption(ref isOdd, option, ref offset);
            }
            UpdateCategoryHeader(GameSettingMenuPatch.AddOnCategoryHeader, ref offset);
            foreach (var option in OptionItem.AddOnOptions)
            {
                UpdateOption(ref isOdd, option, ref offset);
            }
            UpdateCategoryHeader(GameSettingMenuPatch.OtherRoleCategoryHeader, ref offset);
            foreach (var option in OptionItem.OtherRoleOptions)
            {
                UpdateOption(ref isOdd, option, ref offset);
            }

            __instance.scrollBar.ContentYBounds.max = (-offset) - 1.5f;
        }
        private static void UpdateCategoryHeader(CategoryHeaderMasked categoryHeader, ref float offset)
        {
            offset -= GameOptionsMenu.HEADER_HEIGHT;
            categoryHeader.transform.localPosition = new(GameOptionsMenu.HEADER_X, offset, -2f);
        }
        private static void UpdateOption(ref bool isOdd, OptionItem item, ref float offset)
        {
            if (item?.OptionBehaviour == null || item.OptionBehaviour.gameObject == null) return;

            var enabled = true;
            var parent = item.Parent;

            // 親オプションの値を見て表示するか決める
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
                // 見やすさのため交互に色を変える  
                stringOption.LabelBackground.color = item is IRoleOptionItem roleOption ? roleOption.RoleColor : (isOdd ? Color.cyan : Color.white);

                offset -= GameOptionsMenu.SPACING_Y;
                if (item.IsHeader)
                {
                    // IsHeaderなら隙間を広くする
                    offset -= HeaderSpacingY;
                }
                item.OptionBehaviour.transform.localPosition = new Vector3(
                    GameOptionsMenu.START_POS_X,
                    offset,
                    -2f);

                stringOption.ValueText.text = item.GetString();

                isOdd = !isOdd;
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

            if (option is TextOptionItem)
            {
                foreach (var button in option.OptionBehaviour.GetComponentsInChildren<PassiveButton>())
                {
                    button.gameObject.SetActive(false);
                }
                option.OptionBehaviour.LabelBackground.gameObject.SetActive(false);
                option.OptionBehaviour.ValueText.gameObject.SetActive(false);
                // stringOption.TitleText.gameObject.SetActive(true);
            }

            __instance.OnValueChanged = new Action<OptionBehaviour>((o) => { });
            __instance.TitleText.text = option.GetName(option is RoleSpawnChanceOptionItem);
            __instance.Value = __instance.oldValue = option.CurrentValue;
            __instance.ValueText.text = option.GetString();

            return false;
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
                    case StringNames.EngineerCooldown:
                        ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                        break;
                    case StringNames.ShapeshifterCooldown:
                        ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}