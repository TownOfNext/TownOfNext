using AmongUs.GameOptions;
using TONX.Modules;
using TONX.Roles.Core.Interfaces;
using UnityEngine;

namespace TONX.Roles.Crewmate;
public sealed class Judge : RoleBase, IMeetingButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Judge),
            player => new Judge(player),
            CustomRoles.Judge,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            22300,
            SetupOptionItem,
            "ju|法官|审判",
            "#f8d85a"
        );
    public Judge(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionTrialLimitPerMeeting;
    static OptionItem OptionCanTrialMadmate;
    static OptionItem OptionCanTrialCharmed;
    static OptionItem OptionCanTrialCrewKilling;
    static OptionItem OptionCanTrialNeutralB;
    static OptionItem OptionCanTrialNeutralK;
    enum OptionName
    {
        TrialLimitPerMeeting,
        JudgeCanTrialMadmate,
        JudgeCanTrialCharmed,
        JudgeCanTrialnCrewKilling,
        JudgeCanTrialNeutralB,
        JudgeCanTrialNeutralK,
    }

    private int TrialLimit;
    private static void SetupOptionItem()
    {
        OptionTrialLimitPerMeeting = IntegerOptionItem.Create(RoleInfo, 10, OptionName.TrialLimitPerMeeting, new(1, 99, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
        OptionCanTrialMadmate = BooleanOptionItem.Create(RoleInfo, 12, OptionName.JudgeCanTrialMadmate, true, false);
        OptionCanTrialCharmed = BooleanOptionItem.Create(RoleInfo, 13, OptionName.JudgeCanTrialCharmed, true, false);
        OptionCanTrialCrewKilling = BooleanOptionItem.Create(RoleInfo, 14, OptionName.JudgeCanTrialnCrewKilling, true, false);
        OptionCanTrialNeutralB = BooleanOptionItem.Create(RoleInfo, 15, OptionName.JudgeCanTrialNeutralB, false, false);
        OptionCanTrialNeutralK = BooleanOptionItem.Create(RoleInfo, 16, OptionName.JudgeCanTrialNeutralK, true, false);
    }
    public override void Add() => TrialLimit = OptionTrialLimitPerMeeting.GetInt();
    public override void OnStartMeeting() => TrialLimit = OptionTrialLimitPerMeeting.GetInt();
    public override void OverrideNameAsSeer(PlayerControl seen, ref string nameText, bool isForMeeting = false)
    {
        if (Player.IsAlive() && seen.IsAlive() && isForMeeting)
        {
            nameText = Utils.ColorString(RoleInfo.RoleColor, seen.PlayerId.ToString()) + " " + nameText;
        }
    }
    public string ButtonName { get; private set; } = "Judge";
    public bool ShouldShowButton() => Player.IsAlive();
    public bool ShouldShowButtonFor(PlayerControl target) => target.IsAlive();
    public override bool OnSendMessage(string msg, out MsgRecallMode recallMode)
    {
        bool isCommand = TrialMsg(Player, msg, out bool spam);
        recallMode = spam ? MsgRecallMode.Spam : MsgRecallMode.None;
        return isCommand;
    }
    public void OnClickButton(PlayerControl target)
    {
        if (!Trial(target, out var reason, true))
            Player.ShowPopUp(reason);
    }
    private bool Trial(PlayerControl target, out string reason, bool isUi = false)
    {
        reason = string.Empty;

        bool judgeSuicide = true;
        if (TrialLimit < 1)
        {
            reason = GetString("JudgeTrialMax");
            return false;
        }
        if (Is(target))
        {
            if (!isUi) Utils.SendMessage(GetString("LaughToWhoTrialSelf"), Player.PlayerId, Utils.ColorString(Color.cyan, GetString("MessageFromKPD")));
            else Player.ShowPopUp(Utils.ColorString(Color.cyan, GetString("MessageFromKPD")) + "\n" + GetString("LaughToWhoTrialSelf"));
            judgeSuicide = true;
        }
        else if (Player.Is(CustomRoles.Madmate)) judgeSuicide = false;
        else if (target.Is(CustomRoles.Madmate) && OptionCanTrialMadmate.GetBool()) judgeSuicide = false;
        else if (target.Is(CustomRoles.Charmed) && OptionCanTrialCharmed.GetBool()) judgeSuicide = false;
        else if (target.IsCrewKiller() && OptionCanTrialCrewKilling.GetBool()) judgeSuicide = false;
        else if (target.IsNeutralKiller() && OptionCanTrialNeutralK.GetBool()) judgeSuicide = false;
        else if (target.IsNeutralNonKiller() && OptionCanTrialNeutralB.GetBool()) judgeSuicide = false;
        else if (target.GetCustomRole().IsImpostor()) judgeSuicide = false;
        else judgeSuicide = true;

        var dp = judgeSuicide ? Player : target;
        target = dp;

        string Name = dp.GetRealName();

        TrialLimit--;

        _ = new LateTask(() =>
        {
            var state = PlayerState.GetByPlayerId(dp.PlayerId);
            state.DeathReason = CustomDeathReason.Trialed;
            dp.SetRealKiller(Player);
            dp.RpcSuicideWithAnime();

            //死者检查
            Utils.NotifyRoles(isForMeeting: true, NoCache: true);

            _ = new LateTask(() => { Utils.SendMessage(string.Format(GetString("TrialKill"), Name), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Judge), GetString("TrialKillTitle")), false, true, Name); }, 0.6f, "Guess Msg");

        }, 0.2f, "Trial Kill");

        return true;
    }
    public bool TrialMsg(PlayerControl pc, string msg, out bool spam)
    {
        spam = false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.Judge)) return false;

        if (!ChatCommand.OperateRoleCommand(ref msg, "sp|jj|tl|trial|审判|判|审", out int operate)) return false;

        if (!pc.IsAlive())
        {
            Utils.SendMessage(GetString("JudgeDead"), pc.PlayerId);
            return true;
        }

        if (operate == 1)
        {
            Utils.SendMessage(ChatCommand.GetFormatString(), pc.PlayerId);
            return true;
        }
        if (operate == 2)
        {
            spam = true;
            if (!AmongUsClient.Instance.AmHost) return true;

            if (!MsgToPlayer(msg, out PlayerControl target, out string error))
            {
                Utils.SendMessage(error, pc.PlayerId);
                return true;
            }

            if (!Trial(target, out var reason))
                Utils.SendMessage(reason, pc.PlayerId);
        }
        return true;
    }
    private static bool MsgToPlayer(string msg, out PlayerControl target, out string error)
    {
        error = string.Empty;

        //判断选择的玩家是否合理
        target = Utils.MsgToPlayer(ref msg, out bool multiplePlayers);
        if (target == null)
        {
            error = multiplePlayers ? GetString("TrialMultipleColor") : GetString("TrialHelp");
            return false;
        }
        if (target.Data.IsDead)
        {
            error = GetString("TrialNull");
            return false;
        }
        if (Justice.UnableToBeTargetedInJusticeMeeting(target))
        {
            error = GetString("JusticeMeetingBanAbility");
            return false;
        }
        return true;
    }
}