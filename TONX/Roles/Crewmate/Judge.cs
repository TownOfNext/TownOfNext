using AmongUs.GameOptions;
using System;
using System.Text.RegularExpressions;
using TONX.Modules;
using TONX.Roles.Core;
using TONX.Roles.Core.Interfaces;
using UnityEngine;
using static TONX.Translator;

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

            _ = new LateTask(() => { Utils.SendMessage(string.Format(GetString("TrialKill"), Name), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Judge), GetString("TrialKillTitle"))); }, 0.6f, "Guess Msg");

        }, 0.2f, "Trial Kill");

        return true;
    }
    public bool TrialMsg(PlayerControl pc, string msg, out bool spam)
    {
        spam = false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.Judge)) return false;

        int operate; // 1:ID 2:猜测
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (MatchCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id")) operate = 1;
        else if (MatchCommond(ref msg, "shoot|guess|bet|st|gs|bt|猜|赌|sp|jj|tl|trial|审判|判|审", false)) operate = 2;
        else return false;

        if (!pc.IsAlive())
        {
            Utils.SendMessage(GetString("JudgeDead"), pc.PlayerId);
            return true;
        }

        if (operate == 1)
        {
            Utils.SendMessage(GuesserHelper.GetFormatString(), pc.PlayerId);
            return true;
        }
        if (operate == 2)
        {
            spam = true;
            if (!AmongUsClient.Instance.AmHost) return true;

            if (!MsgToPlayer(msg, out byte targetId, out string error))
            {
                Utils.SendMessage(error, pc.PlayerId);
                return true;
            }

            var target = Utils.GetPlayerById(targetId);
            if (!Trial(target, out var reason))
                Utils.SendMessage(reason, pc.PlayerId);
        }
        return true;
    }
    private static bool MsgToPlayer(string msg, out byte id, out string error)
    {
        if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);

        Regex r = new("\\d+");
        MatchCollection mc = r.Matches(msg);
        string result = string.Empty;
        for (int i = 0; i < mc.Count; i++)
        {
            result += mc[i];//匹配结果是完整的数字，此处可以不做拼接的
        }

        if (int.TryParse(result, out int num))
        {
            id = Convert.ToByte(num);
        }
        else
        {
            //并不是玩家编号，判断是否颜色
            //byte color = GetColorFromMsg(msg);
            //好吧我不知道怎么取某位玩家的颜色，等会了的时候再来把这里补上
            id = byte.MaxValue;
            error = GetString("TrialHelp");
            return false;
        }

        //判断选择的玩家是否合理
        PlayerControl target = Utils.GetPlayerById(id);
        if (target == null || target.Data.IsDead)
        {
            error = GetString("TrialNull");
            return false;
        }

        error = string.Empty;
        return true;
    }
    public static bool MatchCommond(ref string msg, string command, bool exact = true)
    {
        var comList = command.Split('|');
        for (int i = 0; i < comList.Length; i++)
        {
            if (exact)
            {
                if (msg == "/" + comList[i]) return true;
            }
            else
            {
                if (msg.StartsWith("/" + comList[i]))
                {
                    msg = msg.Replace("/" + comList[i], string.Empty);
                    return true;
                }
            }
        }
        return false;
    }
}