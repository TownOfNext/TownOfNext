namespace TONX.Patches;

[HarmonyPatch(typeof(LoadingBarManager))]
public class LoadingBarManagerPatch
{
    [HarmonyPatch(nameof(LoadingBarManager.ToggleLoadingBar))]
    public static void Prefix(LoadingBarManager __instance, ref bool on)
    {
        __instance.loadingBar.crewmate.gameObject.SetActive(false);
        try
        {
            if (!GameStates.IsNotJoined) return;
            on = false;
        }
        catch
        {
            on = false;
        }
    }
}