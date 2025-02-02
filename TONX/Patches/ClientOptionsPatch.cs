using HarmonyLib;
using TONX.Modules.ClientOptions;
using TONX.Modules.NameTagInterface;
using UnityEngine;

namespace TONX;

[HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
public static class OptionsMenuBehaviourStartPatch
{
    private static ClientOptionItem UnlockFPS;
    private static ClientOptionItem LongMode;
    private static ClientOptionItem AutoStartGame;
    private static ClientOptionItem AutoEndGame;
    private static ClientOptionItem ForceOwnLanguage;
    private static ClientOptionItem ForceOwnLanguageRoleName;
    private static ClientOptionItem EnableCustomButton;
    private static ClientOptionItem EnableCustomSoundEffect;
    private static ClientActionItem UnloadMod;
    private static ClientActionItem DumpLog;
    private static ClientOptionItem VersionCheat;
    private static ClientOptionItem GodMode;


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
            UnlockFPS = ClientOptionItem.Create("UnlockFPS", Main.UnlockFPS, __instance, UnlockFPSButtonToggle);
            static void UnlockFPSButtonToggle()
            {
                Application.targetFrameRate = Main.UnlockFPS.Value ? 240 : 60;
                Logger.SendInGame(string.Format(Translator.GetString("FPSSetTo"), Application.targetFrameRate));
            }
        }
        if (LongMode == null || LongMode.ToggleButton == null)
        {
            LongMode = ClientOptionItem.Create("LongMode", Main.LongMode, __instance);
        }
        if (AutoStartGame == null || AutoStartGame.ToggleButton == null)
        {
            AutoStartGame = ClientOptionItem.Create("AutoStartGame", Main.AutoStartGame, __instance, AutoStartButtonToggle);
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
            AutoEndGame = ClientOptionItem.Create("AutoEndGame", Main.AutoEndGame, __instance);
        }
        if (ForceOwnLanguage == null || ForceOwnLanguage.ToggleButton == null)
        {
            ForceOwnLanguage = ClientOptionItem.Create("ForceOwnLanguage", Main.ForceOwnLanguage, __instance);
        }
        if (ForceOwnLanguageRoleName == null || ForceOwnLanguageRoleName.ToggleButton == null)
        {
            ForceOwnLanguageRoleName = ClientOptionItem.Create("ForceOwnLanguageRoleName", Main.ForceOwnLanguageRoleName, __instance);
        }
        if (EnableCustomButton == null || EnableCustomButton.ToggleButton == null)
        {
            EnableCustomButton = ClientOptionItem.Create("EnableCustomButton", Main.EnableCustomButton, __instance);
        }
        if (EnableCustomSoundEffect == null || EnableCustomSoundEffect.ToggleButton == null)
        {
            EnableCustomSoundEffect = ClientOptionItem.Create("EnableCustomSoundEffect", Main.EnableCustomSoundEffect, __instance);
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
            VersionCheat = ClientOptionItem.Create("VersionCheat", Main.VersionCheat, __instance);
        }
        if ((GodMode == null || GodMode.ToggleButton == null) && DebugModeManager.AmDebugger)
        {
            GodMode = ClientOptionItem.Create("GodMode", Main.GodMode, __instance);
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
