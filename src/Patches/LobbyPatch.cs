using TONX.Achievements.Core.Base;
using TONX.Achievements.Player;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TONX;

[HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
public class LobbyStartPatch
{
    private static GameObject Paint;
    
    private static bool _wasInGame = false;

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.ExitGame))]
    public static class MarkGameExit
    {
        public static void Prefix() => _wasInGame = GameStates.IsInGame;
    }
    public static void Postfix(LobbyBehaviour __instance)
    {
        SetPaint(__instance);
        ResetTitles();
    }

    public static void SetPaint(LobbyBehaviour __instance)
    {
        if (Paint != null) return;
        Paint = Object.Instantiate(__instance.transform.FindChild("Leftbox").gameObject, __instance.transform);
        Paint.name = "TONX Lobby Paint";
        Paint.transform.localPosition = new Vector3(0.042f, -2.59f, -10.5f);
        SpriteRenderer renderer = Paint.GetComponent<SpriteRenderer>();
        renderer.sprite = Utils.LoadSprite("TONX.Resources.Images.LobbyPaint.png", 290f);
    }
    public static void ResetTitles()
    {
        if (_wasInGame)
        {
            PlayerAchievementData.ResetTitles();
            _wasInGame = false;
        }
        _ = AchievementBase.FlushPendingUnlocks();
    }
}