namespace TONX.Achievements.Core.Interfaces;


public interface IAchievementTracker
{
    /// <summary>
    /// 返回该职业所有的全部成就列表
    /// </summary>
    IEnumerable<IAchievement> TrackedAchievements { get; }
}
