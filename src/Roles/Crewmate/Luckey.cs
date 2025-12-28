using AmongUs.GameOptions;

namespace TONX.Roles.Crewmate;
public sealed class Luckey : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Luckey),
            player => new Luckey(player),
            CustomRoles.Luckey,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20100,
            SetupOptionItem,
            "lk|幸運兒|幸运",
            "#b8d7a3"
        );
    public Luckey(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionProbability;
    enum OptionName
    {
        LuckeyProbability
    }

    private static void SetupOptionItem()
    {
        OptionProbability = IntegerOptionItem.Create(RoleInfo, 10, OptionName.LuckeyProbability, new(0, 100, 5), 50, false)
            .SetValueFormat(OptionFormat.Percent);
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        if (IRandom.Instance.Next(0, 100) < OptionProbability.GetInt())
        {
            Logger.Info($"幸运儿 {Player.GetNameWithRole()} 触发技能，阻挡了 {info.AttemptKiller.GetNameWithRole()} 的击杀", "Luckey.OnCheckMurderAsTarget");
            info.AttemptKiller.ResetKillCooldown();
            info.AttemptKiller.SetKillCooldownV2(target: Player, forceAnime: true);
            return false;
        }
        return true;
    }
}