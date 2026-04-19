using TONX.Achievements.Core.Base;
using UnityEngine;

namespace TONX.Achievements.Roles.Crewmate.Criminologist;

public sealed class SelfVerify : AchievementBase
{
    public const int AchievementId = 1001;

    public override int Id => AchievementId;
    public override string Name => "终极犯罪";
    public override string Description => "犯罪学家将自己与一位死去的人匹配并匹配成功，将自己致死。";
    public override string TitleDisplay => "终极犯罪";
    public override Color TitleColor => new Color(0.235f, 0.357f, 0.639f, 1f);
}
