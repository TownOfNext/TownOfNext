using UnityEngine;
using Object = UnityEngine.Object;

namespace TONX;

[HarmonyPatch]
public class SwitchShipStyleButtonPatch
{
    public static GameObject SwitchShipStyleButton;
    private static int ShipStyle = -1;
    private static List<GameObject> ShipStyles = new();
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Awake)), HarmonyPostfix]
    public static void ShipStatusFixedUpdate(ShipStatus __instance)
    {
        if (SwitchShipStyleButton != null) return;
        InitializeShipStyles();
        if (ShipStyles.Count == 0) return;
        var pos = Main.NormalOptions.MapId switch
        {
            0 => AprilFoolsModePatch.FlipSkeld ? new Vector3(9.57f, -5.36f, -1.00f) : new Vector3(-9.57f, -5.36f, -1.00f), // 食堂
            1 => new Vector3(3.10f, 0.60f, 0.00f), // Mira HQ 食堂
            2 => new Vector3(1.00f, 0.10f, 0.00f), // Polus 办公室
            4 => new Vector3(-10.08f, -21.53f, 0.00f), // The Airship 引擎室
            5 => new Vector3(0.50f, 0.50f, -1.00f), // The Fungle 会议室
            _ => new Vector3(0f, 0f, 0f)
        };
        SwitchShipStyleButton = CreateSwitchShipStyleButton(__instance, new Vector3(0.65f, 0.65f, 1f), pos);
    }
    private static GameObject CreateSwitchShipStyleButton(ShipStatus shipStatus, Vector3 scale, Vector3 pos)
    {
        var template = shipStatus.EmergencyButton.gameObject;
        var button = Object.Instantiate(template, template.transform.parent);
        button.name = "Switch Ship Style Button";
        button.transform.localScale = scale;
        button.transform.localPosition = pos;
        CreateSwitchShipStyleConsole(button);
        return button;
    }
    private static SystemConsole CreateSwitchShipStyleConsole(GameObject btn)
    {
        var console = btn.GetComponent<SystemConsole>();
        console.Image.color = new Color32(80, 255, 255, byte.MaxValue);
        console.usableDistance /= 2;
        console.name = "Switch Ship Style Console";
        return console;
    }
    private static void InitializeShipStyles()
    {
        ShipStyle = -1;
        ShipStyles = new();
        for (var i = 0; i < ShipStatus.Instance.gameObject.transform.GetChildCount(); i++)
        {
            var obj = ShipStatus.Instance.gameObject.transform.GetChild(i).gameObject;
            if (obj && obj.name.Contains("Decor")) ShipStyles.Add(obj);
        }
        for (var j = 0; j < ShipStyles.Count; j++)
        {
            if (ShipStyles[j]?.active ?? false)
            {
                ShipStyle = j;
                break;
            }
        }
    }

    [HarmonyPatch(typeof(SystemConsole), nameof(SystemConsole.Use))]
    public static class SystemConsole_Use_Patch
    {
        [HarmonyPrefix]
        public static bool UseConsole(SystemConsole __instance)
        {
            if (__instance.name != "Switch Ship Style Console") return true;

            if (ShipStyle + 1 == ShipStyles.Count) ShipStyle = -1;
            else ShipStyle++;

            foreach (var style in ShipStyles) style?.SetActive(false);
            if (ShipStyle != -1) ShipStyles[ShipStyle]?.SetActive(true);
            RPC.PlaySound(PlayerControl.LocalPlayer.PlayerId, ShipStyle == -1 ? Sounds.TaskComplete : Sounds.TaskUpdateSound);

            return false;
        }
    }
}