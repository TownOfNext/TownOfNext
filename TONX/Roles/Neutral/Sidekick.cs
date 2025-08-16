using AmongUs.GameOptions;

using TONX.Roles.Core;
using TONX.Roles.Core.Interfaces;

namespace TONX.Roles.Neutral;

public sealed class Sidekick : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Sidekick),
            player => new Sidekick(player),
            CustomRoles.Sidekick,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            51800,
            null,
            "sdk|跟班",
            "#00b4eb",
            true,
            countType: CountTypes.Jackal,
            Hidden: true
        );
    public Sidekick(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        CanKill = Jackal.OptionSidekickCanKill.GetBool();
        KillCooldown = Jackal.OptionSidekickKillCooldown.GetInt();
        CanVent = Jackal.OptionSidekickCanVent.GetBool();
        CanUseSabotage = Jackal.OptionSidekickCanUseSabotage.GetBool();
        HasImpostorVision = Jackal.OptionSidekickHasImpostorVision.GetBool();
    }
    public static bool CanKill;
    private static float KillCooldown;
    public static bool CanVent;
    public static bool CanUseSabotage;
    private static bool HasImpostorVision;

    public bool CanUseKillButton() => CanKill;
    public float CalculateKillCooldown() => CanUseKillButton() ? KillCooldown : 255f;
    public bool CanUseSabotageButton() => CanUseSabotage;
    public bool CanUseImpostorVentButton() => CanVent;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision);
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!CanKill) return false;
        var target = info.AttemptTarget;
        if (target.GetCustomRole() is CustomRoles.Jackal or CustomRoles.Sidekick) return false;
        return true;
    }
}