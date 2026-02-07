using UnityEngine.SceneManagement;

namespace TONX.Patches;

[HarmonyPatch(typeof(LoadingBarManager))]
public class LoadingBarManagerPatch
{
    [HarmonyPatch(nameof(LoadingBarManager.ToggleLoadingBar))]
    public static void Prefix(LoadingBarManager __instance, ref bool on)
    {
        if (SceneManager.GetActiveScene().name != "SplashIntro") return;
        on = false;
    }
}