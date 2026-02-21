using UnityEngine;

namespace TONX;

[HarmonyPatch]
public class SwitchShipStyleButtonPatch
{
    public static List<GameObject> ShipStyles = new();
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Awake)), HarmonyPostfix]
    public static void Awake_Postfix()
    {
        ShipStyles = new();
        for (var i = 0; i < ShipStatus.Instance.gameObject.transform.GetChildCount(); i++)
        {
            var obj = ShipStatus.Instance.gameObject.transform.GetChild(i).gameObject;
            if (obj && obj.name.Contains("Decor")) ShipStyles.Add(obj);
        }
    }
    [HarmonyPatch(typeof(SystemConsole), nameof(SystemConsole.Start)), HarmonyPrefix]
    public static void Start_Prefix(SystemConsole __instance)
    {
        if (__instance.MinigamePrefab.TryCast<TaskAdderGame>()) __instance.FreeplayOnly = false; // 阻止TaskAdder被销毁
        __instance.MinigamePrefab.TryCast<VitalsMinigame>()?.BatteryText?.gameObject?.SetActive(false); // 树懒写了SetActive(true)，却忘了默认的应该设为false
        if (Main.NormalOptions.MapId == 4) // 树懒不修的bug我来修hhh
        {
            if (__instance.MinigamePrefab.TryCast<PlanetSurveillanceMinigame>()) __instance.useIcon = ImageNames.CamsButton;
            if (__instance.MinigamePrefab.TryCast<VitalsMinigame>()) __instance.useIcon = ImageNames.VitalsButton;
        }
    }
}