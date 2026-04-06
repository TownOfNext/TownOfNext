using TONX.Achievements.Core.Interfaces;
using TONX.Achievements.Game;
using TONX.Achievements.Player;
using UnityEngine;

namespace TONX.Achievements.Core.Base;

public abstract class AchievementBase : IAchievement
{
    private static readonly List<(string friendCode, int id, string name, string description)> PendingUnlocks = new();
    
    /// <summary>
    /// 成就的唯一ID
    /// </summary>
    public abstract int Id { get; }

    /// <summary>
    /// 成就名称
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// 成就获取介绍
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// 头衔里显示的名称
    /// </summary>
    public abstract string TitleDisplay { get; }

    /// <summary>
    /// 成就颜色
    /// </summary>
    public abstract Color TitleColor { get; }
    
    /// <summary>
    /// 成就颜色Hex码
    /// </summary>
    public string TitleColorHex =>
        $"#{(int)(TitleColor.r * 255):X2}{(int)(TitleColor.g * 255):X2}{(int)(TitleColor.b * 255):X2}";
    
    
    public void TryUnlock(PlayerControl player)
    {
        if (GameStates.IsLocalGame) return;
        if (player == null || string.IsNullOrEmpty(player.FriendCode)) return;
        if (PlayerAchievementData.HasAchievement(player.FriendCode, Id)) return;

        Logger.Info($"{player.GetRealName()} unlocked [{Id}]{Name}", "Achievement");
        
        PlayerAchievementData.AddToCache(player.FriendCode, new AchievementRecord
        {
            Id = Id,
            Name = Name,
            Description = Description,
            UnlockedAt = DateTime.UtcNow.ToString("o")
        });
        
        PendingUnlocks.Add((player.FriendCode, Id, Name, Description));
    }

    public static async Task FlushPendingUnlocks()
    {
        if (PendingUnlocks.Count == 0) return;
        
        var toSend = new List<(string friendCode, int id, string name, string description)>(PendingUnlocks);
        PendingUnlocks.Clear();

        foreach (var (friendCode, id, name, description) in toSend)
        {
            await Game.AchievementManager.TriggerUnlockAsync(friendCode, id, name, description);

            var player = Main.AllPlayerControls.FirstOrDefault(p => p.FriendCode == friendCode);
            if (player == null) continue;

            var achievement = AchievementRegistry.GetById(id);
            string colorHex = achievement?.TitleColorHex ?? "#FFD700";
            string msg = string.Format(GetString("Achievement.Unlocked"),colorHex,name);
            Utils.SendMessage(msg, player.PlayerId, $"<color=#FFD700>{GetString("AchievementMsgTitle")}</color>");
        }
    }
}
