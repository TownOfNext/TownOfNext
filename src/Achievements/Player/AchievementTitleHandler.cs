using Hazel;
using TONX.Achievements.Game;
using TONX.Achievements.Player;
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
        
        if (!AmongUsClient.Instance.AmHost || !GameStates.IsLobby) return;

        var player = Utils.GetPlayerById(playerId);
        if (player == null) return;

        _ = new LateTask(() => NameTagManager.ApplyFor(player),
            0.15f, $"RefreshNameForTitle p{playerId}");
    }
    

    [HarmonyPatch(typeof(NameTagManager), nameof(NameTagManager.ApplyFor))]
    public static class InjectAchievementTitle
    {
        public static void Postfix(PlayerControl player)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (!GameStates.IsLobby) return; 
            if (player == null) return;

            int titleId = PlayerAchievementData.GetEquippedTitle(player.PlayerId);
            if (titleId <= 0) return;

            var achievement = AchievementRegistry.GetById(titleId);
            if (achievement == null) return;

            string currentName = player.name;
            string prefix      = $"<size=75%><color={achievement.TitleColorHex}>《{achievement.TitleDisplay}》</color></size>";

            // 防止重复叠加（名字里已有该头衔字符串则跳过）
            if (currentName.Contains(achievement.TitleDisplay)) return;

            string newName = prefix + "\r\n" + currentName;
            if (player.CurrentOutfitType == PlayerOutfitType.Default)
                player.RpcSetName(newName);
        }
    }

    // ────────────────────────────────────────────
    //  Harmony Patch #2
    //  LobbyBehaviour.Start → Postfix
    //  每次进入大厅时重置头衔佩戴状态
    // ────────────────────────────────────────────

    [HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
    public static class ResetTitlesOnLobbyStart
    {
        public static void Postfix()
        {
            PlayerAchievementData.ResetTitles();
            Logger.Info("[Achievement] 大厅开始，头衔佩戴状态已重置。", "TitleHandler");
        }
    }
}
