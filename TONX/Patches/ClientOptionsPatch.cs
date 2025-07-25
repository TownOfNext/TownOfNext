using HarmonyLib;
using TONX.Modules.ClientOptions;
using TONX.Modules.NameTagInterface;
using UnityEngine;

namespace TONX;

[HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
public static class OptionsMenuBehaviourStartPatch
{
    private static ClientOptionItem<bool> UnlockFPS;
    private static ClientOptionItem<OutfitType> SwitchOutfitType;
    private static ClientOptionItem<bool> AutoStartGame;
    private static ClientOptionItem<bool> AutoEndGame;
    private static ClientOptionItem<bool> ForceOwnLanguage;
    private static ClientOptionItem<bool> ForceOwnLanguageRoleName;
    private static ClientOptionItem<bool> EnableCustomButton;
    private static ClientOptionItem<bool> EnableCustomSoundEffect;
    private static ClientActionItem UnloadMod;
    private static ClientActionItem DumpLog;
    private static ClientOptionItem<bool> VersionCheat;
    private static ClientOptionItem<bool> GodMode;


    private static bool reseted = false;
    public static void Postfix(OptionsMenuBehaviour __instance)
    {
        if (__instance.DisableMouseMovement == null) return;

        NameTagPanel.Init(__instance);

        if (!reseted || !DebugModeManager.AmDebugger)
        {
            reseted = true;
            Main.VersionCheat.Value = false;
            Main.GodMode.Value = false;
        }

        if (UnlockFPS == null || UnlockFPS.ToggleButton == null)
        {
            UnlockFPS = ClientOptionItem<bool>.Create("UnlockFPS", Main.UnlockFPS, __instance, UnlockFPSButtonToggle);
            static void UnlockFPSButtonToggle()
            {
                Application.targetFrameRate = Main.UnlockFPS.Value ? 240 : 60;
                Logger.SendInGame(string.Format(Translator.GetString("FPSSetTo"), Application.targetFrameRate));
            }
        }
        if (SwitchOutfitType == null || SwitchOutfitType.ToggleButton == null)
        {
            SwitchOutfitType = ClientOptionItem<OutfitType>.Create("SwitchOutfitType", Main.SwitchOutfitType, __instance, SwitchMode);
            static void SwitchMode()
            {
                foreach (var pc in Main.AllPlayerControls)
                {
                    pc.MyPhysics.SetBodyType(pc.BodyType);
                    if (pc.BodyType == PlayerBodyTypes.Normal)
                        pc.cosmetics.currentBodySprite.BodySprite.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                }
            }
        }
        if (AutoStartGame == null || AutoStartGame.ToggleButton == null)
        {
            AutoStartGame = ClientOptionItem<bool>.Create("AutoStartGame", Main.AutoStartGame, __instance, AutoStartButtonToggle);
            static void AutoStartButtonToggle()
            {
                if (Main.AutoStartGame.Value == false && GameStates.IsCountDown)
                {
                    GameStartManager.Instance.ResetStartState();
                }
            }
        }
        if (AutoEndGame == null || AutoEndGame.ToggleButton == null)
        {
            AutoEndGame = ClientOptionItem<bool>.Create("AutoEndGame", Main.AutoEndGame, __instance);
        }
        if (ForceOwnLanguage == null || ForceOwnLanguage.ToggleButton == null)
        {
            ForceOwnLanguage = ClientOptionItem<bool>.Create("ForceOwnLanguage", Main.ForceOwnLanguage, __instance);
        }
        if (ForceOwnLanguageRoleName == null || ForceOwnLanguageRoleName.ToggleButton == null)
        {
            ForceOwnLanguageRoleName = ClientOptionItem<bool>.Create("ForceOwnLanguageRoleName", Main.ForceOwnLanguageRoleName, __instance);
        }
        if (EnableCustomButton == null || EnableCustomButton.ToggleButton == null)
        {
            EnableCustomButton = ClientOptionItem<bool>.Create("EnableCustomButton", Main.EnableCustomButton, __instance);
        }
        if (EnableCustomSoundEffect == null || EnableCustomSoundEffect.ToggleButton == null)
        {
            EnableCustomSoundEffect = ClientOptionItem<bool>.Create("EnableCustomSoundEffect", Main.EnableCustomSoundEffect, __instance);
        }
        if (UnloadMod == null || UnloadMod.ToggleButton == null)
        {
            UnloadMod = ClientActionItem.Create("UnloadMod", ModUnloaderScreen.Show, __instance);
        }
        if (DumpLog == null || DumpLog.ToggleButton == null)
        {
            DumpLog = ClientActionItem.Create("DumpLog", () => Utils.DumpLog(), __instance);
        }
        if ((VersionCheat == null || VersionCheat.ToggleButton == null) && DebugModeManager.AmDebugger)
        {
            VersionCheat = ClientOptionItem<bool>.Create("VersionCheat", Main.VersionCheat, __instance);
        }
        if ((GodMode == null || GodMode.ToggleButton == null) && DebugModeManager.AmDebugger)
        {
            GodMode = ClientOptionItem<bool>.Create("GodMode", Main.GodMode, __instance);
        }

        if (ModUnloaderScreen.Popup == null)
            ModUnloaderScreen.Init(__instance);
    }
}

[HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Close))]
public static class OptionsMenuBehaviourClosePatch
{
    public static void Postfix()
    {
        ClientActionItem.CustomBackground?.gameObject?.SetActive(false);
        NameTagPanel.Hide();
        NameTagEditMenu.Hide();
        ModUnloaderScreen.Hide();
    }
}
