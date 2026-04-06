using TONX.Achievements.Core.Base;
using UnityEngine;

namespace TONX.Achievements.Common;

public sealed class TestAchievement2 : AchievementBase
{
    public const int AchievementId = 2;
    public override int Id => AchievementId;
    public override string Name => "测试成就2";
    public override string Description => "测试作用。2";
    public override string TitleDisplay => "测试成就2";
    public override Color TitleColor => new Color(0.235f, 0.357f, 0.3f, 1f);
}