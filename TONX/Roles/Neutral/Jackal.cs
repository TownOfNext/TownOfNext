using AmongUs.GameOptions;
using System.Linq;
using UnityEngine;

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

        LeftRecruitCount = OptionJackalRecruitLimit.GetInt();
        KillCount = 0;

        CustomRoleManager.OnMurderPlayerOthers.Add(OnMurderPlayerOthers);
    }

    private static OptionItem OptionKillCooldown;
    public static OptionItem OptionCanVent;
    public static OptionItem OptionCanUseSabotage;
    public static OptionItem OptionCanWinBySabotageWhenNoImpAlive;
    private static OptionItem OptionHasImpostorVision;
    private static OptionItem OptionResetKillCooldownWhenSbGetKilled;
    public static OptionItem OptionCanRecruitSidekick;
    public static OptionItem OptionJackalRecruitLimit;
    public static OptionItem OptionNeededKillCountToRecruit;
    public static OptionItem OptionSidekickCanKill;
    public static OptionItem OptionSidekickKillCooldown;
    public static OptionItem OptionSidekickCanVent;
    public static OptionItem OptionSidekickCanUseSabotage;
    public static OptionItem OptionSidekickHasImpostorVision;
    public static OptionItem OptionSidekickCanBecomeJackal;
    enum OptionName
    {
        JackalCanWinBySabotageWhenNoImpAlive,
        ResetKillCooldownWhenPlayerGetKilled,
        JackalCanRecruitSidekick,
        JackalRecruitLimit,
        NeededKillsToRecruit,
        SidekickCanKill,
        SidekickCanBecomeJackal,
    }
    private static float KillCooldown;
    public static bool CanVent;
    public static bool CanUseSabotage;
    public static bool WinBySabotage;
    private static bool HasImpostorVision;
    private static bool ResetKillCooldown;
    public int LeftRecruitCount;
    public int KillCount;

    public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Jackal;

    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, true, false);
        OptionCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanUseSabotage, false, false);
        OptionCanWinBySabotageWhenNoImpAlive = BooleanOptionItem.Create(RoleInfo, 13, OptionName.JackalCanWinBySabotageWhenNoImpAlive, true, false, OptionCanUseSabotage);
        OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 14, GeneralOption.ImpostorVision, true, false);
        OptionResetKillCooldownWhenSbGetKilled = BooleanOptionItem.Create(RoleInfo, 15, OptionName.ResetKillCooldownWhenPlayerGetKilled, false, false);
        OptionCanRecruitSidekick = BooleanOptionItem.Create(RoleInfo, 16, OptionName.JackalCanRecruitSidekick, true, false);
        OptionJackalRecruitLimit = IntegerOptionItem.Create(RoleInfo, 17, OptionName.JackalRecruitLimit, new(1, 15, 1), 1, false, OptionCanRecruitSidekick)
            .SetValueFormat(OptionFormat.Players);
        OptionNeededKillCountToRecruit = IntegerOptionItem.Create(RoleInfo, 18, OptionName.NeededKillsToRecruit, new(0, 14, 1), 0, false, OptionCanRecruitSidekick)
            .SetValueFormat(OptionFormat.Times);
        OptionSidekickCanKill = BooleanOptionItem.Create(RoleInfo, 19, OptionName.SidekickCanKill, false, false, OptionCanRecruitSidekick);
        OptionSidekickKillCooldown = FloatOptionItem.Create(RoleInfo, 20, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 20f, false, OptionSidekickCanKill)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSidekickCanVent = BooleanOptionItem.Create(RoleInfo, 21, GeneralOption.CanVent, true, false, OptionCanRecruitSidekick);
        OptionSidekickCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 22, GeneralOption.CanUseSabotage, false, false, OptionCanRecruitSidekick);
        OptionSidekickHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 23, GeneralOption.ImpostorVision, true, false, OptionCanRecruitSidekick);
        OptionSidekickCanBecomeJackal = BooleanOptionItem.Create(RoleInfo, 24, OptionName.SidekickCanBecomeJackal, true, false, OptionCanRecruitSidekick);
    }
    public float CalculateKillCooldown() => KillCooldown;
    public bool CanUseSabotageButton() => CanUseSabotage;
    public bool CanUseImpostorVentButton() => CanVent;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision);
    public void ApplySchrodingerCatOptions(IGameOptions option) => ApplyGameOptions(option);
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var killer = info.AttemptKiller;
        var target = info.AttemptTarget;
        if (target.GetCustomRole() is CustomRoles.Jackal or CustomRoles.Sidekick) return false;
        if (!OptionCanRecruitSidekick.GetBool() || LeftRecruitCount <= 0) return true;
        if (KillCount < OptionNeededKillCountToRecruit.GetInt())
        {
            KillCount++;
            return true;
        }
        target.ChangeRole(CustomRoles.Sidekick);
        PlayerState.GetByPlayerId(target.PlayerId).RemoveAllSubRoles();
        Logger.Info($"豺狼{killer?.Data?.PlayerName}招募了{target?.Data?.PlayerName}", "Jackal");
        LeftRecruitCount--;
        Utils.NotifyRoles();
        return false;
    }
    public override void OnPlayerDeath(PlayerControl player, CustomDeathReason deathReason, bool isOnMeeting = false)
    {
        if (!OptionSidekickCanBecomeJackal.GetBool()) return;
        new LateTask(() =>
        {
            foreach (var sidekick in Main.AllPlayerControls.Where(p => p.IsAlive() && p.Is(CustomRoles.Sidekick)).ToList())
            {
                sidekick.ChangeRole(CustomRoles.Jackal);
                Logger.Info($"跟班{sidekick?.Data?.PlayerName}上位", "Jackal");
            }
            Utils.NotifyRoles();
        }, 0.1f, "Sidekick become Jackal");
    }
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
    public bool OverrideKillButtonText(out string text)
    {
        text = Translator.GetString("JackalButtonText");
        return LeftRecruitCount > 0 && KillCount >= OptionNeededKillCountToRecruit.GetInt();
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(LeftRecruitCount > 0 && KillCount >= OptionNeededKillCountToRecruit.GetInt() ? Color.yellow : Color.gray, $"({LeftRecruitCount})");
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (!Is(seen) || isForMeeting) return "";
        var LeftKills = OptionNeededKillCountToRecruit.GetInt() - KillCount;
        if (LeftKills == 0) return "";
        return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), string.Format(Translator.GetString("JackalNeededKillsToRecruit"), LeftKills));
        
    }
}