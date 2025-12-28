using AmongUs.GameOptions;
using UnityEngine;

namespace TONX.Roles.Neutral;
public sealed class Workaholic : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Workaholic),
            player => new Workaholic(player),
            CustomRoles.Workaholic,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Neutral,
            51600,
            SetupOptionItem,
            "wk|工作狂|工作",
            "#008b8b",
            introSound: () => DestroyableSingleton<HudManager>.Instance.TaskCompleteSound
        );
    public Workaholic(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    { }

    private static OptionItem OptionVentCooldown;
    private static OptionItem OptionNoWinAtDeath;
    private static OptionItem OptionVisibleToEveryone;
    private static Options.OverrideTasksData Tasks;
    enum OptionName
    {
        WorkaholicCannotWinAtDeath,
        WorkaholicVisibleToEveryone,
    }

    private static void SetupOptionItem()
    {
        OptionVentCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.VentCooldown, new(2.5f, 180f, 2.5f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionNoWinAtDeath = BooleanOptionItem.Create(RoleInfo, 11, OptionName.WorkaholicCannotWinAtDeath, false, false);
        OptionVisibleToEveryone = BooleanOptionItem.Create(RoleInfo, 12, OptionName.WorkaholicVisibleToEveryone, true, false);
        // 20-23を使用
        Tasks = Options.OverrideTasksData.Create(RoleInfo, 20);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = OptionVentCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 0f;
    }
    public static bool KnowTargetRoleColor(PlayerControl target, bool isMeeting)
        => target.Is(CustomRoles.Workaholic) && OptionVisibleToEveryone.GetBool();
    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seer, ref bool enabled, ref Color roleColor, ref string roleText)
        => enabled |= OptionVisibleToEveryone.GetBool();
    public override bool OnCompleteTask(out bool cancel)
    {
        if (MyTaskState.IsTaskFinished && (Player.IsAlive() || !OptionNoWinAtDeath.GetBool()))
        {
            Logger.Info("Workaholic completed all tasks", "Workaholic");
            Win();
        }
        cancel = false;
        return false;
    }
    public void Win()
    {
        foreach (var otherPlayer in Main.AllAlivePlayerControls.Where(p => !Is(p)))
        {
            otherPlayer.SetRealKiller(Player);
            otherPlayer.RpcMurderPlayer(otherPlayer);
            var playerState = PlayerState.GetByPlayerId(otherPlayer.PlayerId);
            playerState.DeathReason = CustomDeathReason.Ashamed;
            playerState.SetDead();
        }
        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Workaholic);
        CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
    }
}