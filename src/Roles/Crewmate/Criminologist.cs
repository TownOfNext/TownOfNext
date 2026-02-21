using AmongUs.GameOptions;
using Hazel;
using TONX.Modules;
using UnityEngine;
using TONX.Roles.Core.Interfaces;

namespace TONX.Roles.Crewmate;

public class Criminologist : RoleBase, IMeetingButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Criminologist),
            player => new Criminologist(player),
            CustomRoles.Criminologist,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            23300,
            SetupOptionItem,
            "crm|犯罪学|犯罪学家",
            "#3C5BA3"
        );
    public Criminologist(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    private static OptionItem OptionVerifyLimitPerMeeting;
    private static OptionItem OptionDeductOnFailed;
    enum OptionName
    {
        CriminologistVerifyLimitPerMeeting,
        CriminologistDeductOnFailed,
    }

    public int VerifyLimitPerMeeting;
    public byte DeadPlayerChosen = byte.MaxValue;
    private bool HasExecutedThisMeeting = false;
    private int CurrentUsesThisMeeting = 0;

    private static void SetupOptionItem()
    {
        OptionVerifyLimitPerMeeting = IntegerOptionItem.Create(RoleInfo, 10, OptionName.CriminologistVerifyLimitPerMeeting, new(1, 15, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
        OptionDeductOnFailed = BooleanOptionItem.Create(RoleInfo, 11, OptionName.CriminologistDeductOnFailed, false, false);
    }

    public override void Add() => VerifyLimitPerMeeting = OptionVerifyLimitPerMeeting.GetInt();
    public override void OverrideNameAsSeer(PlayerControl seen, ref string nameText, bool isForMeeting = false)
    {
        if (Player.IsAlive() && isForMeeting)
        {
            nameText = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Criminologist), seen.PlayerId.ToString()) + " " + nameText;
        }
    }
    public override void OnStartMeeting()
    {
        DeadPlayerChosen = byte.MaxValue;
        HasExecutedThisMeeting = false;
        CurrentUsesThisMeeting = VerifyLimitPerMeeting;
        SendRPC();
    }
    public override void NotifyOnMeetingStart(ref List<(string, byte, string)> msgToSend)
    {
        if (Player.IsAlive())
        {
            msgToSend.Add((string.Format(GetString("CriminologistUsesRemaining"), CurrentUsesThisMeeting),
            Player.PlayerId,
            Utils.ColorString(Utils.GetRoleColor(CustomRoles.Criminologist), GetString("CriminologistVerifyTitle"))));
        }
    }

    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(DeadPlayerChosen);
        sender.Writer.Write(CurrentUsesThisMeeting);
        sender.Writer.Write(HasExecutedThisMeeting);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        DeadPlayerChosen = reader.ReadByte();
        CurrentUsesThisMeeting = reader.ReadInt32();
        HasExecutedThisMeeting = reader.ReadBoolean();
    }

    public string ButtonName { get; private set; } = "Verify";
    public bool ShouldShowButton() => Player.IsAlive() && !HasExecutedThisMeeting && CurrentUsesThisMeeting > 0;
    public bool ShouldShowButtonFor(PlayerControl target) => target.IsAlive() == (DeadPlayerChosen != byte.MaxValue) || target.PlayerId == DeadPlayerChosen;
    public override bool OnSendMessage(string msg, out MsgRecallMode recallMode)
    {
        bool isCommand = VerifyMsg(Player, msg, out bool spam);
        recallMode = spam ? MsgRecallMode.Spam : MsgRecallMode.None;
        return isCommand;
    }
    public void OnClickButton(PlayerControl target)
    {
        if (target.PlayerId == DeadPlayerChosen)
        {
            DeadPlayerChosen = byte.MaxValue; // 取消选择
            SendRPC();
            return;
        }
        var dead = Utils.GetPlayerById(DeadPlayerChosen);
        if (dead == null)
        {
            DeadPlayerChosen = target.IsAlive() ? byte.MaxValue : target.PlayerId;
            SendRPC();
            return;
        }
        if (!Verify(dead, target, out string reason, true))
        {
            Player.ShowPopUp(reason);
            return;
        }
    }
    public void OnUpdateButton(MeetingHud meetingHud)
    {
        foreach (var pva in meetingHud.playerStates)
        {
            var btn = pva?.transform?.FindChild("Custom Meeting Button")?.gameObject;
            if (!btn) continue;

            if (HasExecutedThisMeeting || CurrentUsesThisMeeting <= 0)
                btn.GetComponent<SpriteRenderer>().color = Color.gray;
            else if (DeadPlayerChosen == pva.TargetPlayerId)
                btn.GetComponent<SpriteRenderer>().color = Color.green;
            else
                btn.GetComponent<SpriteRenderer>().color = Color.red;
        }
    }

    private bool Verify(PlayerControl target /* 受害人 */ , PlayerControl killer /* 嫌疑人 */ , out string reason, bool isUi = false)
    {
        reason = string.Empty;

        if (CurrentUsesThisMeeting < 1)
        {
            reason = GetString("CriminologistVerifyMax");
            return false;
        }
        if (HasExecutedThisMeeting)
        {
            reason = GetString("VerifyUsed");
            return false;
        }

        if (Is(killer))
        {
            if (!isUi) Utils.SendMessage(GetString("VerifySuicideMessage"), Player.PlayerId, Utils.ColorString(Color.cyan, GetString("MessageFromKPD")));
            else Player.ShowPopUp(Utils.ColorString(Color.cyan, GetString("MessageFromKPD")) + "\n" + GetString("VerifySuicideMessage"));
        }

        DeadPlayerChosen = byte.MaxValue;
        var succeed = target.GetRealKiller()?.PlayerId == killer.PlayerId;
        if (!succeed && !OptionDeductOnFailed.GetBool()) HasExecutedThisMeeting = true;
        CurrentUsesThisMeeting--;

        Logger.Info($"{Player.GetNameWithRole()} => Verify {target.GetNameWithRole()}(Victim) with {killer.GetNameWithRole()}(Suspect) (Succeed: {succeed})", "Criminologist");

        SendRPC();

        string killerName = killer.GetRealName();
        string targetName = target.GetRealName();

        if (!succeed)
        {
            _ = new LateTask(() =>
            {
                Utils.SendMessage(
                        string.Format(GetString("VerifyFailed"), killerName, targetName), 
                        255, 
                        Utils.ColorString(Utils.GetRoleColor(CustomRoles.Criminologist), GetString("CriminologistVerifyTitle"))
                    );
            }, 0.8f, "Criminologist Execute Failed");

            return true;
        }

        _ = new LateTask(() =>
        {
            var state = PlayerState.GetByPlayerId(killer.PlayerId);
            state.DeathReason = CustomDeathReason.Verifyed;
            killer.SetRealKiller(Player);
            killer.RpcSuicideWithAnime();

            //死者检查
            Utils.NotifyRoles(isForMeeting: true, NoCache: true);

            _ = new LateTask(() =>
            {
                Utils.SendMessage(
                    string.Format(GetString("VerifySucceed"), killerName, targetName),
                    255,
                    Utils.ColorString(Utils.GetRoleColor(CustomRoles.Criminologist), GetString("CriminologistVerifyTitle")),
                    false, true, killerName
                );
            }, 0.6f, "Criminologist Execute Msg");
        }, 0.2f, "Criminologist Execute");

        return true;
    }

    public bool VerifyMsg(PlayerControl pc, string msg, out bool spam)
    {
        spam = false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.Criminologist)) return false;

        if (!ChatCommand.OperateRoleCommand(ref msg, "vf|vrf|verify|推理", out int operate)) return false;

        if (!pc.IsAlive())
        {
            Utils.SendMessage(GetString("CriminologistDead"), pc.PlayerId);
            return true;
        }

        if (operate == 1)
        {
            Utils.SendMessage(ChatCommand.GetFormatString(containDeadPlayers: true), pc.PlayerId);
            return true;
        }
        if (operate == 2)
        {
            spam = true;
            if (!AmongUsClient.Instance.AmHost) return true;

            if (!MsgToPlayersByID(msg, out PlayerControl target, out PlayerControl killer, out string error))
            {
                Utils.SendMessage(error, pc.PlayerId);
                return true;
            }

            if (!Verify(target, killer, out var reason))
                Utils.SendMessage(reason, pc.PlayerId);
        }
        return true;
    }
    private static bool MsgToPlayersByID(string msg, out PlayerControl target, out PlayerControl killer, out string error)
    {
        target = null;
        killer = null;
        error = string.Empty;

        string[] parts = msg.Split(' ');
        if (parts.Length < 3)
        {
            error = GetString("VerifyHelp");
            return false;
        }

        //判断选择的玩家是否合理
        target = Utils.MsgToPlayer(ref parts[1], out _);
        killer = Utils.MsgToPlayer(ref parts[2], out _);

        if (target == null || killer == null)
        {
            error = GetString("VerifyHelp");
            return false;
        }
        if (Justice.UnableToBeTargetedInJusticeMeeting(target) || Justice.UnableToBeTargetedInJusticeMeeting(killer))
        {
            error = GetString("JusticeMeetingBanAbility");
            return false;
        }
        if (target.IsAlive() || !killer.IsAlive())
        {
            error = GetString("VerifyNull");
            return false;
        }
        if (target.PlayerId == killer.PlayerId) // 理论来讲这是不可能的hh
        {
            error = GetString("VerifySame");
            return false;
        }
        return true;
    }
}