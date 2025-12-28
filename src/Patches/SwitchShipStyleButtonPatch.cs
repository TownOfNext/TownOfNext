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
    public static bool Start_Prefix()
    {
        return false; // 阻止TaskAdder被销毁
    }
}
