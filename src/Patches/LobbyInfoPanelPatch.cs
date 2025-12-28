using TMPro;
using UnityEngine;

namespace TONX.Patches;

[HarmonyPatch(typeof(HostInfoPanel), nameof(HostInfoPanel.SetUp))]
public static class HostInfoPanelUpdatePatch
{
    private static TextMeshPro HostText;
    public static void Postfix(HostInfoPanel __instance)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            if (HostText == null)
                HostText = __instance.content.transform.FindChild("Name").GetComponent<TextMeshPro>();

            var htmlStringRgb = ColorUtility.ToHtmlStringRGB(Palette.PlayerColors[__instance.player.ColorId]);
            var hostName = Main.HostNickName;
            var youLabel = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.HostYouLabel);

            HostText.text = $"<color=#{htmlStringRgb}>{hostName}</color>  <size=90%><b><font=\"Barlow-BoldItalic SDF\" material=\"Barlow-BoldItalic SDF Outline\">{youLabel}";
        }
    }
}