using UnityEngine;
using Object = UnityEngine.Object;

namespace TONX;

[HarmonyPatch]
public class MapBehaviourPatch
{
    private static Dictionary<PlayerControl, SpriteRenderer> herePoints = new Dictionary<PlayerControl, SpriteRenderer>();
    private static Dictionary<PlayerControl, Vector3> preMeetingPostions = new Dictionary<PlayerControl, Vector3>();
    private static bool ShouldShowRealTime => !PlayerControl.LocalPlayer.IsAlive() || PlayerControl.LocalPlayer.Is(CustomRoles.GM) || Main.GodMode.Value;
    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowNormalMap)), HarmonyPostfix]
    public static void ShowNormalMap_Postfix(MapBehaviour __instance)
    {
        InitializeCustomHerePoints(__instance);
    }
    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowSabotageMap)), HarmonyPostfix]
    public static void ShowSabotageMap_Postfix(MapBehaviour __instance)
    {
        InitializeCustomHerePoints(__instance);
    }

    private static void InitializeCustomHerePoints(MapBehaviour __instance)
    {
        __instance.DisableTrackerOverlays();
        // 删除旧图标
        foreach (var oldHerePoint in herePoints)
        {
            if (oldHerePoint.Value == null) continue;
            Object.Destroy(oldHerePoint.Value.gameObject);
        }
        herePoints.Clear();

        // 创建新图标
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (!pc.AmOwner && pc != null)
            {
                var herePoint = Object.Instantiate(__instance.HerePoint, __instance.HerePoint.transform.parent);
                herePoint.gameObject.SetActive(false);
                herePoints.Add(pc, herePoint);
            }
        }
    }

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.FixedUpdate)), HarmonyPostfix]
    public static void FixedUpdate_Postfix(MapBehaviour __instance)
    {
        if (!ShouldShowRealTime) return;
        foreach (var kvp in herePoints)
        {
            var pc = kvp.Key;
            var herePoint = kvp.Value;
            if (herePoint == null) continue;
            herePoint.gameObject.SetActive(false);
            if (pc == null || __instance.countOverlay.gameObject.active) continue;
            herePoint.gameObject.SetActive(true);

            // 设置图标颜色
            herePoint.material.SetColor(PlayerMaterial.BodyColor, pc.Data.Color);
            herePoint.material.SetColor(PlayerMaterial.BackColor, pc.Data.ShadowColor);
            herePoint.material.SetColor(PlayerMaterial.VisorColor, Palette.VisorColor);

            // 设置图标位置
            var vector = GameStates.IsMeeting && preMeetingPostions.TryGetValue(pc, out var pmp) ? pmp : pc.transform.position;
            vector /= ShipStatus.Instance.MapScale;
            vector.x *= Mathf.Sign(ShipStatus.Instance.transform.localScale.x);
            vector.z = -1f;
            herePoint.transform.localPosition = vector;
        }
    }

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.Close)), HarmonyPostfix]
    public static void Close_Postfix(MapBehaviour __instance)
    {
        if (!ShouldShowRealTime) return;
        foreach (var kvp in herePoints)
        {
            var herePoint = kvp.Value;
            if (herePoint == null) continue;
            herePoint.gameObject.SetActive(false);
        }
    }

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.SetPreMeetingPosition)), HarmonyPrefix]
    public static void SetPreMeetingPosition_Prefix()
    {
        preMeetingPostions.Clear();
        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (!pc.AmOwner && pc != null)
            {
                // 记录玩家在开会前的位置
                preMeetingPostions.Add(pc, pc.transform.position);
            }
        }
    }
}
