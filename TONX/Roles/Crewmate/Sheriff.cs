using AmongUs.GameOptions;
using Hazel;
using TONX.Roles.Core.Interfaces;
using UnityEngine;

namespace TONX.Roles.Crewmate;
public sealed class Sheriff : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Sheriff),
            player => new Sheriff(player),
            CustomRoles.Sheriff,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Crewmate,
            20700,
            SetupOptionItem,
            "sh|警長",
            "#f8cd46",
            true,
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Sheriff(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        ShotLimit = ShotLimitOpt.GetInt();
        CurrentKillCooldown = KillCooldown.GetFloat();
    }

    private static OptionItem KillCooldown;
    private static OptionItem MisfireKillsTarget;
    private static OptionItem ShotLimitOpt;
    private static OptionItem CanKillAllAlive;
    private static OptionItem CanKillNeutrals;
    public static OptionItem CanKillNeutralsMode;
    private static OptionItem CanKillMadmate;
    private static OptionItem CanKillCharmed;
    private static OptionItem SetMadCanKill;
    private static OptionItem MadCanKillCrew;
    private static OptionItem MadCanKillImp;
    private static OptionItem MadCanKillNeutral;
    enum OptionName
    {
        SheriffMisfireKillsTarget,
        SheriffShotLimit,
        SheriffCanKillAllAlive,
        SheriffCanKillNeutrals,
        SheriffCanKillNeutralsMode,
        SheriffCanKill,
        SheriffCanKillMadmate,
        SheriffCanKillCharmed,
        SheriffSetMadCanKill,
        SheriffMadCanKillImp,
        SheriffMadCanKillNeutral,
        SheriffMadCanKillCrew
    }
    public static Dictionary<CustomRoles, OptionItem> KillTargetOptions = new();
    public int ShotLimit = 0;
    public float CurrentKillCooldown = 30;
    public static readonly string[] KillOption =
    {
        "SheriffCanKillAll", "SheriffCanKillSeparately"
    };

    private static void SetupOptionItem()
    {
        KillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
        MisfireKillsTarget = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SheriffMisfireKillsTarget, false, false);
        ShotLimitOpt = IntegerOptionItem.Create(RoleInfo, 12, OptionName.SheriffShotLimit, new(1, 15, 1), 15, false)
            .SetValueFormat(OptionFormat.Times);
        CanKillAllAlive = BooleanOptionItem.Create(RoleInfo, 15, OptionName.SheriffCanKillAllAlive, true, false);
        CanKillMadmate = BooleanOptionItem.Create(RoleInfo, 17, OptionName.SheriffCanKillMadmate, true, false);
        CanKillCharmed = BooleanOptionItem.Create(RoleInfo, 22, OptionName.SheriffCanKillCharmed, true, false);
        CanKillNeutrals = BooleanOptionItem.Create(RoleInfo, 16, OptionName.SheriffCanKillNeutrals, true, false);
        CanKillNeutralsMode = StringOptionItem.Create(RoleInfo, 14, OptionName.SheriffCanKillNeutralsMode, KillOption, 0, false, CanKillNeutrals);
        SetUpNeutralOptions(30);
        SetMadCanKill = BooleanOptionItem.Create(RoleInfo, 18, OptionName.SheriffSetMadCanKill, false, false);
        MadCanKillImp = BooleanOptionItem.Create(RoleInfo, 19, OptionName.SheriffMadCanKillImp, true, false, SetMadCanKill);
        MadCanKillNeutral = BooleanOptionItem.Create(RoleInfo, 20, OptionName.SheriffMadCanKillNeutral, true, false, SetMadCanKill);
        MadCanKillCrew = BooleanOptionItem.Create(RoleInfo, 21, OptionName.SheriffMadCanKillCrew, true, false, SetMadCanKill);
    }
    public static void SetUpNeutralOptions(int idOffset)
    {
        foreach (var neutral in CustomRolesHelper.AllStandardRoles.Where(x => x.IsNeutral()).ToArray())
        {
            if (neutral is CustomRoles.SchrodingerCat) continue;
            SetUpKillTargetOption(neutral, idOffset, true, CanKillNeutralsMode);
            idOffset++;
        }
    }
    public static void SetUpKillTargetOption(CustomRoles role, int idOffset, bool defaultValue = true, OptionItem parent = null)
    {
        var id = RoleInfo.ConfigId + idOffset;
        parent ??= RoleInfo.RoleOption;
        var roleName = Utils.GetRoleName(role);
        Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(role), roleName) } };
        KillTargetOptions[role] = BooleanOptionItem.Create(id, OptionName.SheriffCanKill + "%role%", defaultValue, RoleInfo.Tab, false).SetParent(parent);
        KillTargetOptions[role].ReplacementDictionary = replacementDic;
    }
    public override void Add()
    {
        var playerId = Player.PlayerId;
        CurrentKillCooldown = KillCooldown.GetFloat();

        ShotLimit = ShotLimitOpt.GetInt();
        Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 残り{ShotLimit}発", "Sheriff");
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(ShotLimit);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        ShotLimit = reader.ReadInt32();
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? CurrentKillCooldown : 255f;
    public bool CanUseKillButton()
        => Player.IsAlive()
        && (CanKillAllAlive.GetBool() || GameStates.AlreadyDied)
        && ShotLimit > 0;
    public bool CanUseImpostorVentButton() => false;
    public bool CanUseSabotageButton() => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        if (Is(info.AttemptKiller) && !info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;

            Logger.Info($"{killer.GetNameWithRole()} : 残り{ShotLimit}発", "Sheriff");
            if (ShotLimit <= 0) return false;
            ShotLimit--;
            SendRPC();
            if (!killer.Is(CustomRoles.Madmate) ?
                CanBeKilledBy(target) :
                (!SetMadCanKill.GetBool() ||
                (target.GetCustomRole().IsCrewmate() && MadCanKillCrew.GetBool()) ||
                (target.GetCustomRole().IsNeutral() && MadCanKillNeutral.GetBool()) ||
                (target.GetCustomRole().IsImpostor() && MadCanKillImp.GetBool())
                ))
            {
                killer.ResetKillCooldown();
                return true;
            }
            PlayerState.GetByPlayerId(killer.PlayerId).DeathReason = CustomDeathReason.Misfire;
            killer.RpcMurderPlayer(killer);
            if (!MisfireKillsTarget.GetBool()) return false;
        }
        return true;
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(CanUseKillButton() ? Color.yellow : Color.gray, $"({ShotLimit})");
    public static bool CanBeKilledBy(PlayerControl player)
    {
        var cRole = player.GetCustomRole();
        var subRole = player.GetCustomSubRoles();
        bool CanKill = false;
        foreach (var role in subRole)
        {
            if (role == CustomRoles.Madmate)
                CanKill = CanKillMadmate.GetBool();
            if (role == CustomRoles.Charmed)
                CanKill = CanKillCharmed.GetBool();
        }

        return cRole.GetCustomRoleTypes() switch
        {
            CustomRoleTypes.Impostor => true,
            CustomRoleTypes.Neutral => CanKillNeutrals.GetBool() && (CanKillNeutralsMode.GetValue() == 0 || (!KillTargetOptions.TryGetValue(cRole, out var option) || option.GetBool())),
            _ => CanKill,
        };
    }
}