using Hazel;
using TONX.Achievements.Game;
using TONX.Achievements.Player;
using TONX.Attributes;
using UnityEngine;

namespace TONX.Achievements.Player;

public static class AchievementTitleHandler
{
    public static void SendTitleSyncRpc(byte playerId, int achievementId)
    {
        _ = new LateTask(() =>
        {
            ApplyTitleLocally(playerId, achievementId);
            
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                PlayerControl.LocalPlayer.NetId,
                (byte)CustomRPC.SyncAchievementTitle,
                SendOption.Reliable,
                -1);
            writer.Write(playerId);
            writer.Write(achievementId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);

        }, 0f, $"AchievementTitleSyncRpc p{playerId}");
    }
    
    public static void ReceiveTitleSyncRpc(MessageReader reader)
    {
        byte playerId = reader.ReadByte();
        int achievementId = reader.ReadInt32();
        ApplyTitleLocally(playerId, achievementId);
    }

    private static void ApplyTitleLocally(byte playerId, int achievementId)
    {
        PlayerAchievementData.SetEquippedTitle(playerId, achievementId);
        Logger.Info($"Player {playerId} Title change → ID={achievementId}", "TitleHandler");
        
        if (!GameStates.IsLobby) return;

        var player = Utils.GetPlayerById(playerId);
        if (player == null) return;

        _ = new LateTask(() => NameTagManager.ApplyFor(player),
            0.15f, $"RefreshNameForTitle p{playerId}");
    }
    
    // 实现接口
    public class AchievementTitleProvider : NameTagManager.INameTagProvider
    {
        public string GetPrefix(PlayerControl player)
        {
            int titleId = PlayerAchievementData.GetEquippedTitle(player.PlayerId);
            if (titleId <= 0) return null;

            var achievement = AchievementRegistry.GetById(titleId);
            if (achievement == null) return null;

            return $"<size=75%><color={achievement.TitleColorHex}>[{achievement.TitleDisplay}]</color></size>";
        }
    }
}
