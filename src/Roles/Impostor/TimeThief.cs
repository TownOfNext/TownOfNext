using AmongUs.GameOptions;
using TONX.Roles.Core.Interfaces;

namespace TONX.Roles.Impostor;

public sealed class TimeThief : RoleBase, IMeetingTimeAlterable, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(TimeThief),
            player => new TimeThief(player),
            CustomRoles.TimeThief,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            2700,
            SetupOptionItem,
            "tt|蚀时者|蚀时"
        );
    public TimeThief(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        DecreaseMeetingTime = OptionDecreaseMeetingTime.GetInt();
        LowerLimitVotingTime = OptionLowerLimitVotingTime.GetInt();
        ReturnStolenTimeUponDeath = OptionReturnStolenTimeUponDeath.GetBool();
    }
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionDecreaseMeetingTime;
    private static OptionItem OptionLowerLimitVotingTime;
    private static OptionItem OptionReturnStolenTimeUponDeath;
    enum OptionName
    {
        TimeThiefDecreaseMeetingTime,
        TimeThiefLowerLimitVotingTime,
        TimeThiefReturnStolenTimeUponDeath
    }
    public static float KillCooldown;
    public static int DecreaseMeetingTime;
    public static int LowerLimitVotingTime;
    public static bool ReturnStolenTimeUponDeath;

    public bool RevertOnDie => ReturnStolenTimeUponDeath;

    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionDecreaseMeetingTime = IntegerOptionItem.Create(RoleInfo, 11, OptionName.TimeThiefDecreaseMeetingTime, new(0, 100, 1), 25, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionLowerLimitVotingTime = IntegerOptionItem.Create(RoleInfo, 12, OptionName.TimeThiefLowerLimitVotingTime, new(1, 300, 1), 10, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionReturnStolenTimeUponDeath = BooleanOptionItem.Create(RoleInfo, 13, OptionName.TimeThiefReturnStolenTimeUponDeath, true, false);
    }
    public float CalculateKillCooldown() => KillCooldown;
    public int CalculateMeetingTimeDelta()
    {
        var sec = -(DecreaseMeetingTime * MyState.GetKillCount(true));
        return sec;
    }
    public override string GetProgressText(bool comms = false)
    {
        var time = CalculateMeetingTimeDelta();
        return time < 0 ? Utils.ColorString(Palette.ImpostorRed.ShadeColor(0.5f), $"{time}s") : "";
    }
}