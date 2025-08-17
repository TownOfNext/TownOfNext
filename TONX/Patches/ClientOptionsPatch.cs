using BepInEx.Configuration;
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

    private static bool reseted;

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

        InitializeOptions(__instance);
        InitializeActions(__instance);
        InitializeDebugOptions(__instance);

        if (ModUnloaderScreen.Popup == null)
            ModUnloaderScreen.Init(__instance);
    }

    private static void InitializeOptions(OptionsMenuBehaviour instance)
    {
        CreateIfNull(ref UnlockFPS, "UnlockFPS", Main.UnlockFPS, instance, Unlock);
        CreateIfNull(ref SwitchOutfitType, "SwitchOutfitType", Main.SwitchOutfitType, instance, SwitchType);
        CreateIfNull(ref AutoStartGame, "AutoStartGame", Main.AutoStartGame, instance, StartGame);
        CreateIfNull(ref AutoEndGame, "AutoEndGame", Main.AutoEndGame, instance);
        CreateIfNull(ref ForceOwnLanguage, "ForceOwnLanguage", Main.ForceOwnLanguage, instance);
        CreateIfNull(ref ForceOwnLanguageRoleName, "ForceOwnLanguageRoleName", Main.ForceOwnLanguageRoleName, instance);
        CreateIfNull(ref EnableCustomButton, "EnableCustomButton", Main.EnableCustomButton, instance);
        CreateIfNull(ref EnableCustomSoundEffect, "EnableCustomSoundEffect", Main.EnableCustomSoundEffect, instance);
        return;

        static void StartGame()
        {
            if (!Main.AutoStartGame.Value && GameStates.IsCountDown)
                GameStartManager.Instance.ResetStartState();
        }
        
        static void SwitchType()
        {
            foreach (var pc in Main.AllPlayerControls)
            {
                pc.MyPhysics.SetBodyType(pc.BodyType);
                if (pc.BodyType == PlayerBodyTypes.Normal)
                    pc.cosmetics.currentBodySprite.BodySprite.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            }
        }
        
        static void Unlock()
        {
            Application.targetFrameRate = Main.UnlockFPS.Value ? 240 : 60;
            Logger.SendInGame(string.Format(GetString("FPSSetTo"), Application.targetFrameRate));
        }
    }

    private static void InitializeActions(OptionsMenuBehaviour instance)
    {
        if (UnloadMod == null || UnloadMod.ToggleButton == null)
            UnloadMod = ClientActionItem.Create("UnloadMod", ModUnloaderScreen.Show, instance);
        
        if (DumpLog == null || DumpLog.ToggleButton == null)
            DumpLog = ClientActionItem.Create("DumpLog", () => Utils.DumpLog(), instance);
    }

    private static void InitializeDebugOptions(OptionsMenuBehaviour instance)
    {
        if (!DebugModeManager.AmDebugger) return;
        
        CreateIfNull(ref VersionCheat, "VersionCheat", Main.VersionCheat, instance);
        CreateIfNull(ref GodMode, "GodMode", Main.GodMode, instance);
    }

    private static void CreateIfNull<T>(ref ClientOptionItem<T> option, string name, ConfigEntry<T> config, OptionsMenuBehaviour instance, Action clickAction = null)
    {
        if (option == null || option.ToggleButton == null)
            option = ClientOptionItem<T>.Create(name, config, instance, clickAction);
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
