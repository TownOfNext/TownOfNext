using TONX.Achievements.Core.Base;
using UnityEngine;

namespace TONX.Achievements.Common;

public sealed class TestAchievement1 : AchievementBase
{
    public const int AchievementId = 1;
    public override int Id => AchievementId;
    public override string Name => "测试成就1";
    public override string Description => "测试作用。1";
    public override string TitleDisplay => "测试成就1";
    public override Color TitleColor => new Color(0.235f, 0.357f, 0.639f, 1f);
}