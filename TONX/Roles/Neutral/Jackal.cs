using AmongUs.GameOptions;
using System.Linq;

using TONX.Roles.Core;
using TONX.Roles.Core.Interfaces;

namespace TONX.Roles.Neutral;
public sealed class Jackal : RoleBase, IKiller, ISchrodingerCatOwner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Jackal),
            player => new Jackal(player),
            CustomRoles.Jackal,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            50900,
            SetupOptionItem,
            "jac|豺狼",
            "#00b4eb",
            true,
            countType: CountTypes.Jackal,
            assignCountRule: new(1, 1, 1)
        );
    public Jackal(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        CanVent = OptionCanVent.GetBool();
        CanUseSabotage = OptionCanUseSabotage.GetBool();
        WinBySabotage = OptionCanWinBySabotageWhenNoImpAlive.GetBool();
        HasImpostorVision = OptionHasImpostorVision.GetBool();
        ResetKillCooldown = OptionResetKillCooldownWhenSbGetKilled.GetBool();

        CustomRoleManager.OnMurderPlayerOthers.Add(OnMurderPlayerOthers);
    }

    private static OptionItem OptionKillCooldown;
    public static OptionItem OptionCanVent;
    public static OptionItem OptionCanUseSabotage;
    public static OptionItem OptionCanWinBySabotageWhenNoImpAlive;
    private static OptionItem OptionHasImpostorVision;
    private static OptionItem OptionResetKillCooldownWhenSbGetKilled;
    enum OptionName
    {
        JackalCanWinBySabotageWhenNoImpAlive,
        ResetKillCooldownWhenPlayerGetKilled,
    }
    private static float KillCooldown;
    public static bool CanVent;
    public static bool CanUseSabotage;
    public static bool WinBySabotage;
    private static bool HasImpostorVision;
    private static bool ResetKillCooldown;

    public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Jackal;

    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, true, false);
        OptionCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanUseSabotage, false, false);
        OptionCanWinBySabotageWhenNoImpAlive = BooleanOptionItem.Create(RoleInfo, 14, OptionName.JackalCanWinBySabotageWhenNoImpAlive, true, false, OptionCanUseSabotage);
        OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 13, GeneralOption.ImpostorVision, true, false);
        OptionResetKillCooldownWhenSbGetKilled = BooleanOptionItem.Create(RoleInfo, 15, OptionName.ResetKillCooldownWhenPlayerGetKilled, true, false);
    }
    public float CalculateKillCooldown() => KillCooldown;
    public bool CanUseSabotageButton() => CanUseSabotage;
    public bool CanUseImpostorVentButton() => CanVent;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision);
    public void ApplySchrodingerCatOptions(IGameOptions option) => ApplyGameOptions(option);
    public static void OnMurderPlayerOthers(MurderInfo info)
    {
        if (!ResetKillCooldown || info.IsSuicide || info.IsAccident) return;
        foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Jackal) && x.PlayerId != info.AttemptKiller.PlayerId))
        {
            pc.SetKillCooldownV2(0);
            RPC.PlaySoundRPC(pc.PlayerId, Sounds.ImpTransform);
            pc.Notify(Translator.GetString("JackalResetKillCooldown"));
        }
    }
}