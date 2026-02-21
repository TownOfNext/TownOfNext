using AmongUs.GameOptions;
using Hazel;
using TONX.Modules;
using TONX.Roles.Core.Interfaces;
using UnityEngine;

namespace TONX.Roles.Crewmate;
public sealed class Swapper : RoleBase, IMeetingButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Swapper),
            player => new Swapper(player),
            CustomRoles.Swapper,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            23200,
            SetupOptionItem,
            "swa|换票师|换票",
            "#863756"
        );
    public Swapper(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        SwapLimit = OptionSwapLimit.GetInt();
    }

    private static OptionItem OptionSwapLimit;
    private static OptionItem OptionCanUseButton;
    enum OptionName
    {
        SwapperSwapLimit,
        SwapperCanUseButton
    }

    private int SwapLimit;
    private List<byte> Targets = new();

    private static void SetupOptionItem()
    {
        OptionSwapLimit = IntegerOptionItem.Create(RoleInfo, 10, OptionName.SwapperSwapLimit, new(1, 99, 1), 15, false)
            .SetValueFormat(OptionFormat.Times);
        OptionCanUseButton = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SwapperCanUseButton, true, false);
    }
    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (Is(reporter) && target == null && !OptionCanUseButton.GetBool())
        {
            Logger.Info("因禁止换票师拍灯取消会议", "Swapper.OnCheckReportDeadBody");
            return false;
        }
        return true;
    }
    public override void OnStartMeeting()
    {
        Targets.Clear();
    }
    public override void OverrideNameAsSeer(PlayerControl seen, ref string nameText, bool isForMeeting = false)
    {
        if (Player.IsAlive() && seen.IsAlive() && isForMeeting)
        {
            nameText = Utils.ColorString(RoleInfo.RoleColor, seen.PlayerId.ToString()) + " " + nameText;
        }
    }
    public override void AfterMeetingTasks()
    {
        if (Targets.Count == 2) SwapLimit--;
    }

    public string ButtonName { get; private set; } = "Swapper";
    public bool ShouldShowButton() => Player.IsAlive();
    public bool ShouldShowButtonFor(PlayerControl target) => target.IsAlive();
    public override bool OnSendMessage(string msg, out MsgRecallMode recallMode)
    {
        bool isCommand = SwapMsg(Player, msg, out bool spam);
        recallMode = spam ? MsgRecallMode.Spam : MsgRecallMode.None;
        return isCommand;
    }
    public void OnClickButton(PlayerControl target)
    {
        if (!Swap(target, out var reason))
            Player.ShowPopUp(reason);
    }
    public void OnUpdateButton(MeetingHud meetingHud)
    {
        foreach (var pva in meetingHud.playerStates)
        {
            var btn = pva?.transform?.FindChild("Custom Meeting Button")?.gameObject;
            if (!btn) continue;
            if (Targets.Contains(pva.TargetPlayerId)) btn.GetComponent<SpriteRenderer>().color = Color.green;
            else btn.GetComponent<SpriteRenderer>().color = Targets.Count == 2 ? Color.gray : Color.red;
        }
    }
    private bool Swap(PlayerControl target, out string reason)
    {
        reason = string.Empty;

        if (Targets.Remove(target.PlayerId))
        {
            string Name = target.GetRealName();

            Logger.Info($"{Player.GetNameWithRole()} => Cancel Swap {target.GetNameWithRole()}({Targets.Count})", "Swapper");

            SendRpc();

            _ = new LateTask (() =>
            {
                Utils.SendMessage(
                    string.Format(GetString("SwapSkillCancelled"), Name),
                    Player.PlayerId,
                    Utils.ColorString(Utils.GetRoleColor(CustomRoles.Swapper), GetString("SwapVoteTitle")));
            }, 0.8f, "Swap Skill Cancelled");
        }
        else
        {
            if (Targets.Count == 2)
            {
                reason = GetString("SwapUsed");
                return false;
            }
            if (SwapLimit < 1)
            {
                reason = GetString("SwapperSwapMax");
                return false;
            }

            string Name2 = target.GetRealName();

            Targets.Add(target.PlayerId);
            Logger.Info($"{Player.GetNameWithRole()} => Swap {target.GetNameWithRole()}({Targets.Count})", "Swapper");
            
            SendRpc();

            _ = new LateTask (() =>
            {
                Utils.SendMessage(
                    string.Format(GetString("SwapSkill"), Name2),
                    Player.PlayerId,
                    Utils.ColorString(Utils.GetRoleColor(CustomRoles.Swapper), GetString("SwapVoteTitle")));
            }, 0.8f, "Swap Skill");
        }

        return true;
    }
    public bool SwapMsg(PlayerControl pc, string msg, out bool spam)
    {
        spam = false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.Swapper)) return false;

        if (!ChatCommand.OperateRoleCommand(ref msg, "sw|swa|swap|换票|换", out int operate)) return false;

        if (!pc.IsAlive())
        {
            Utils.SendMessage(GetString("SwapperDead"), pc.PlayerId);
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

            if (!Swap(target, out var reason))
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
            error = multiplePlayers ? GetString("SwapMultipleColor") : GetString("SwapHelp");
            return false;
        }
        if (target.Data.IsDead)
        {
            error = GetString("SwapNull");
            return false;
        }
        if (Justice.UnableToBeTargetedInJusticeMeeting(target))
        {
            error = GetString("JusticeMeetingBanAbility");
            return false;
        }
        return true;
    }
    private void SendRpc()
    {
        using var sender = CreateSender();
        sender.Writer.Write(Targets.Count);
        foreach (var target in Targets) sender.Writer.Write(target);
        RefreshTargets();
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        Targets = new();
        var num = reader.ReadInt32();
        for (var i = 0; i < num; i++) Targets.Add(reader.ReadByte());
        RefreshTargets();
    }
    private void RefreshTargets()
    {
        if (Targets.Count == 2) MeetingVoteManager.Instance?.AddSwappedPlayers(Player.PlayerId, Targets[0], Targets[1], true);
        else MeetingVoteManager.Instance?.RemoveSwappedPlayers(Player.PlayerId);
    }
}