namespace TONX;

[HarmonyPatch(typeof(HttpMatchmakerManager))]
class HttpMatchmakerManagerPatch
{
    public static IRegionInfo CurrentGameRegion;
    [HarmonyPatch(nameof(HttpMatchmakerManager.CoSendRequest), new Type[] { typeof(AmongUs.HTTP.RetryableWebRequest), typeof(string) })]
    [HarmonyPatch(nameof(HttpMatchmakerManager.CoSendRequest), new Type[] { typeof(AmongUs.HTTP.RetryableWebRequest), typeof(string), typeof(int), typeof(Il2CppSystem.Action<HttpMatchmakerManager.MatchmakerFailure>) })]
    [HarmonyPrefix]
    public static void CoSendRequest_Prefix()
    {
        CurrentGameRegion = ServerManager.Instance.CurrentRegion;
    }
}