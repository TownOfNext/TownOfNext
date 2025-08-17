using AmongUs.GameOptions;

namespace TONX.Roles.Crewmate;
public sealed class SpeedBooster : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SpeedBooster),
            player => new SpeedBooster(player),
            CustomRoles.SpeedBooster,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            22900,
            SetupOptionItem,
            "sb|增速",
            "#00ffff",
            experimental: true
        );
    public SpeedBooster(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        UpSpeed = OptionUpSpeed.GetFloat();
        BoostTimes = OptionSpeedBoosterTimes.GetInt();
    }

    private static OptionItem OptionUpSpeed; //加速値
    private static OptionItem OptionSpeedBoosterTimes; //効果を発動するタスク完了数
    enum OptionName
    {
        SpeedBoosterUpSpeed,
        SpeedBoosterTimes
    }
    private static float UpSpeed;
    private static int BoostTimes;

    private static void SetupOptionItem()
    {
        OptionUpSpeed = FloatOptionItem.Create(RoleInfo, 10, OptionName.SpeedBoosterUpSpeed, new(0.1f, 1.0f, 0.1f), 0.2f, false)
                .SetValueFormat(OptionFormat.Multiplier);
        OptionSpeedBoosterTimes = IntegerOptionItem.Create(RoleInfo, 11, OptionName.SpeedBoosterTimes, new(1, 99, 1), 5, false)
            .SetValueFormat(OptionFormat.Times);
    }
    public override bool OnCompleteTask(out bool cancel)
    {
        var playerId = Player.PlayerId;
        if (!MyTaskState.HasCompletedEnoughCountOfTasks(BoostTimes))
        {
            Main.AllPlayerSpeed[playerId] += UpSpeed;
            if (Main.AllPlayerSpeed[playerId] > 3) Player.Notify(GetString("SpeedBoosterSpeedLimit"));
            else Player.Notify(string.Format(GetString("SpeedBoosterTaskDone"), Main.AllPlayerSpeed[playerId].ToString("0.0#####")));
            Logger.Info("增速者触发加速:" + Player.GetNameWithRole(), "SpeedBooster");
        }
        cancel = false;
        return false;
    }
}