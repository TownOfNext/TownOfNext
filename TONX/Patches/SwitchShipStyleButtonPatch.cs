using UnityEngine;
using Object = UnityEngine.Object;

namespace TONX;

[HarmonyPatch]
public class SwitchShipStyleButtonPatch
{
    public static GameObject SwitchShipStyleButton;
    static ShipStyles ShipStyle;
    enum ShipStyles
    {
        Normal,
        Helloween,
        BirthdayDecorSkeld
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Awake)), HarmonyPostfix]
    public static void ShipStatusFixedUpdate(ShipStatus __instance)
    {
        if (Main.NormalOptions.MapId != 0) return;
        if (SwitchShipStyleButton != null) return;
        var template = __instance.EmergencyButton.gameObject;
        SwitchShipStyleButton = Object.Instantiate(template, template.transform.parent);
        SwitchShipStyleButton.name = "Switch Ship Style Button";
        SwitchShipStyleButton.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
        SwitchShipStyleButton.transform.localPosition = new Vector3(-9.57f, -5.36f, -10f);
        var console = SwitchShipStyleButton.GetComponent<SystemConsole>();
        console.Image.color = new Color32(80, 255, 255, byte.MaxValue);
        console.usableDistance /= 2;
        console.name = "Switch Ship Style Console";
    }

    [HarmonyPatch(typeof(SystemConsole), nameof(SystemConsole.Use))]
    public static class SystemConsole_Use_Patch
    {
        [HarmonyPrefix]
        public static bool UseConsole(SystemConsole __instance)
        {
            if (__instance.name != "Switch Ship Style Console") return true;

            var allStyles = EnumHelper.GetAllValues<ShipStyles>();
            var currentIndex = Array.IndexOf(allStyles, ShipStyle);
            int nextIndex;
            if (currentIndex == allStyles.Length - 1)
            {
                nextIndex = 0;
            }
            else
            {
                nextIndex = currentIndex + 1;
            }
            ShipStyle = allStyles[nextIndex];

            foreach (var style in allStyles)
            {
                if (style != ShipStyles.Normal)
                    ShipStatus.Instance.gameObject.transform.FindChild(style.ToString())?.gameObject.SetActive(false);
            }
            if (ShipStyle != ShipStyles.Normal)
                ShipStatus.Instance.gameObject.transform.FindChild(ShipStyle.ToString())?.gameObject.SetActive(true);
            RPC.PlaySound(PlayerControl.LocalPlayer.PlayerId, ShipStyle != ShipStyles.Normal ? Sounds.ImpTransform : Sounds.TaskUpdateSound);

            return false; 
        }
    }
}