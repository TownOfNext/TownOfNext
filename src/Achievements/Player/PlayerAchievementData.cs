namespace TONX.Achievements.Player;

public sealed class AchievementRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string UnlockedAt { get; set; } = "";
}
/// <summary>
/// 成就系统最核心的类，网络请求数据同步叽里呱啦的都在里面。
/// </summary>
public static class PlayerAchievementData
{
    // FriendCode → 已解锁成就列表
    private static readonly Dictionary<string, List<AchievementRecord>> AchievementCache = new();

    // PlayerId → 已佩戴成就 ID（0 = 未佩戴）
    private static readonly Dictionary<byte, int> EquippedTitles = new();
    
    public static void UpdateCache(string friendCode, List<AchievementRecord> achievements)
    {
        if (string.IsNullOrEmpty(friendCode)) return;
        AchievementCache[friendCode] = achievements ?? new();
        Logger.Info($"缓存更新：{friendCode} 拥有 {AchievementCache[friendCode].Count} 个成就", "AchievementData");
    }
    
    public static void AddToCache(string friendCode, AchievementRecord record)
    {
        if (string.IsNullOrEmpty(friendCode) || record == null) return;
        if (!AchievementCache.ContainsKey(friendCode))
            AchievementCache[friendCode] = new();

        if (!AchievementCache[friendCode].Any(r => r.Id == record.Id))
            AchievementCache[friendCode].Add(record);
    }
    
    public static List<AchievementRecord> GetCached(string friendCode)
    {
        if (string.IsNullOrEmpty(friendCode)) return new();
        return AchievementCache.TryGetValue(friendCode, out var list) ? list : new();
    }

    public static bool HasAchievement(string friendCode, int achievementId)
    {
        var list = GetCached(friendCode);
        return list.Any(r => r.Id == achievementId);
    }
    
    /// <summary>
    /// 设置指定玩家佩戴的成就头衔 ID。
    /// 传入 0 或负数表示卸下头衔。
    /// 由 RPC 同步到所有客户端后调用。
    /// </summary>
    public static void SetEquippedTitle(byte playerId, int achievementId)
    {
        if (achievementId <= 0)
            EquippedTitles.Remove(playerId);
        else
            EquippedTitles[playerId] = achievementId;
    }
    
    public static int GetEquippedTitle(byte playerId)
        => EquippedTitles.TryGetValue(playerId, out var id) ? id : 0;
    
    public static void ResetTitles()
    {
        EquippedTitles.Clear();
        Logger.Info("[Achievement] 头衔佩戴状态已重置（新大厅）", "AchievementData");
    }
    
    public static void ClearAll()
    {
        AchievementCache.Clear();
        EquippedTitles.Clear();
    }
}
