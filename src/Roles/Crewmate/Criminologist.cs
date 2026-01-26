using AmongUs.GameOptions;
using Hazel;
using TONX.Modules;
using UnityEngine;
using TONX.Roles.Core.Interfaces;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TONX.Roles.Crewmate;

public class Criminologist : RoleBase, IMeetingButton
{
    public static readonly SimpleRoleInfo RoleInfo = SimpleRoleInfo.Create(
        typeof(Criminologist),
        player => new Criminologist(player),
        CustomRoles.Criminologist,
        () => RoleTypes.Crewmate,
        CustomRoleTypes.Crewmate,
        23300,
        SetupOptionItem,
        "crm|犯罪学|犯罪学作家",
        "#3C5BA3",
        introSound: () => GetIntroSound(RoleTypes.Crewmate)
    );
    
    public Criminologist(PlayerControl player) : base(RoleInfo, player)
    {
        VerifyLimitPerMeeting = OptionVerifyLimitPerMeeting.GetInt();
        DeductOnFailed = OptionDeductOnFailed.GetBool();
    }

    private static OptionItem OptionVerifyLimitPerMeeting;
    private static OptionItem OptionDeductOnFailed;
    enum OptionName
    {
        CriminologistVerifyLimitPerMeeting,
        CriminologistDeductOnFailed,
    }

    public int VerifyLimitPerMeeting = 0;
    public bool DeductOnFailed = false;
    public List<byte> SelectedPlayers = new();
    private bool HasExecutedThisMeeting = false;
    private int CurrentUsesThisMeeting = 0;

    private enum RoleRpcType
    {
        SetSelectedPlayers
    }

    private static void SetupOptionItem()
    {
        OptionVerifyLimitPerMeeting = IntegerOptionItem.Create(RoleInfo, 11, OptionName.CriminologistVerifyLimitPerMeeting, new(1, 15, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
        OptionDeductOnFailed = BooleanOptionItem.Create(RoleInfo, 12, OptionName.CriminologistDeductOnFailed, false, false);
    }

    public override void Add()
    {
        VerifyLimitPerMeeting = OptionVerifyLimitPerMeeting.GetInt();
        DeductOnFailed = OptionDeductOnFailed.GetBool();
        CurrentUsesThisMeeting = VerifyLimitPerMeeting;
    }

    public override void OnStartMeeting()
    {
        SelectedPlayers.Clear();
        HasExecutedThisMeeting = false;
        CurrentUsesThisMeeting = VerifyLimitPerMeeting;
        SendRPC();
        
        if (Player.IsAlive())
        {
            Utils.SendMessage(
                string.Format(GetString("CriminologistUsesRemaining"), CurrentUsesThisMeeting), 
                Player.PlayerId,
                Utils.ColorString(Utils.GetRoleColor(CustomRoles.Criminologist), GetString("CriminologistVerifyTitle"))
            );
        }
    }

    public override void AfterMeetingTasks()
    {
        SelectedPlayers.Clear();
        HasExecutedThisMeeting = false;
    }
    
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write((byte)RoleRpcType.SetSelectedPlayers);
        sender.Writer.Write(SelectedPlayers.Count);
        foreach (var playerId in SelectedPlayers)
            sender.Writer.Write(playerId);
        sender.Writer.Write(CurrentUsesThisMeeting);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        var rpcType = (RoleRpcType)reader.ReadByte();
        switch (rpcType)
        {
            case RoleRpcType.SetSelectedPlayers:
                SelectedPlayers.Clear();
                var count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                    SelectedPlayers.Add(reader.ReadByte());
                CurrentUsesThisMeeting = reader.ReadInt32();
                break;
        }
    }
    
    public string ButtonName { get; private set; } = "Verify";
    public bool ShouldShowButton() => Player.IsAlive() && !HasExecutedThisMeeting && CurrentUsesThisMeeting > 0;
    public bool ShouldShowButtonFor(PlayerControl target) => true;

    public void OnClickButton(PlayerControl target)
    {
        if (!TrySelectPlayer(target, out string reason))
        {
            Player.ShowPopUp(reason);
            return;
        }
        
        if (SelectedPlayers.Count == 2)
        {
            var deader = Utils.GetPlayerById(SelectedPlayers[0]);
            var killer = Utils.GetPlayerById(SelectedPlayers[1]);
            
            if (Verify(deader, killer))
            {
                Execute(killer);
                HasExecutedThisMeeting = true;
                CurrentUsesThisMeeting--;
                SendRPC();
            }
            else
            {
                Player.ShowPopUp(GetString("VerifyFailed"));
                if (DeductOnFailed)
                {
                    CurrentUsesThisMeeting--;
                    SendRPC();
                }
            }
            
            SelectedPlayers.Clear();
            SendRPC();
        }
    }

    public void OnUpdateButton(MeetingHud meetingHud)
    {
        foreach (var pva in meetingHud.playerStates)
        {
            var btn = pva?.transform?.FindChild("Custom Meeting Button")?.gameObject;
            if (!btn) continue;
            
            if (SelectedPlayers.Contains(pva.TargetPlayerId))
                btn.GetComponent<SpriteRenderer>().color = Color.green;
            else if (SelectedPlayers.Count == 2 || HasExecutedThisMeeting || CurrentUsesThisMeeting <= 0)
                btn.GetComponent<SpriteRenderer>().color = Color.gray;
            else
                btn.GetComponent<SpriteRenderer>().color = Color.red;
        }
    }
    
    private bool TrySelectPlayer(PlayerControl target, out string reason)
    {
        reason = string.Empty;

        if (HasExecutedThisMeeting)
        {
            reason = GetString("VerifyAlreadyExecuted");
            return false;
        }

        if (CurrentUsesThisMeeting <= 0)
        {
            reason = GetString("VerifyLimitMax");
            return false;
        }

        if (SelectedPlayers.Count >= 2)
        {
            reason = GetString("VerifyAlreadySelectedTwo");
            return false;
        }
        
        if (SelectedPlayers.Count == 0 && target.IsAlive())
        {
            reason = GetString("VerifyDeaderNoDeath");
            return false;
        }

        if (SelectedPlayers.Count == 1)
        {
            var deader = Utils.GetPlayerById(SelectedPlayers[0]);
            if (target.PlayerId == deader.PlayerId)
            {
                reason = GetString("VerifyCoupleSame");
                return false;
            }
        }

        SelectedPlayers.Add(target.PlayerId);
        SendRPC();
        return true;
    }

    /// <summary>
    /// 验证传入的玩家组是否为凶杀组
    /// </summary>
    private bool Verify(PlayerControl deader, PlayerControl killer)
    {
        return deader.GetRealKiller()?.PlayerId == killer.PlayerId;
    }

    /// <summary>
    /// 执行击杀
    /// </summary>
    private bool Execute(PlayerControl target)
    {
        if (Is(target))
        {
            Utils.SendMessage(GetString("VerifySuicideMessage"), Player.PlayerId, Utils.ColorString(Color.cyan, GetString("MessageFromKPD")));
        }

        string targetName = target.GetRealName();

        _ = new LateTask(() =>
        {
            var state = PlayerState.GetByPlayerId(target.PlayerId);
            state.DeathReason = CustomDeathReason.Verifyed;
            target.SetRealKiller(Player);
            target.RpcSuicideWithAnime();
            
            _ = new LateTask(() => 
            { 
                Utils.SendMessage(
                    string.Format(GetString("VerifyExecuteKiller"), targetName), 
                    255, 
                    Utils.ColorString(Utils.GetRoleColor(CustomRoles.Criminologist), GetString("CriminologistVerifyTitle"))
                ); 
            }, 0.6f, "Criminologist Execute Msg");

        }, 0.2f, "Criminologist Execute");

        return true;
    }
    
    public override bool OnSendMessage(string msg, out MsgRecallMode recallMode)
    {
        bool isCommand = VerifyMsg(Player, msg, out bool spam);
        recallMode = spam ? MsgRecallMode.Spam : MsgRecallMode.None;
        return isCommand;
    }

    private bool VerifyMsg(PlayerControl pc, string msg, out bool spam)
    {
        spam = false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.Criminologist)) return false;

        int operate;
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (ChatCommand.MatchCommand(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id")) 
            operate = 1;
        else if (ChatCommand.MatchCommand(ref msg, "vrf|verify|推理", false))
            operate = 2;
        else 
            return false;

        if (!pc.IsAlive())
        {
            Utils.SendMessage(GetString("CriminologistDead"), pc.PlayerId);
            return true;
        }

        if (operate == 1)
        {
            Utils.SendMessage(ChatCommand.GetFormatString(false,true), pc.PlayerId);
            return true;
        }

        if (operate == 2)
        {
            spam = true;
            if (!AmongUsClient.Instance.AmHost) return true;

            if (CurrentUsesThisMeeting <= 0)
            {
                Utils.SendMessage(GetString("VerifyLimitMax"), pc.PlayerId);
                return true;
            }

            if (!MsgToPlayersByID(msg, out PlayerControl deader, out PlayerControl killer, out string error))
            {
                Utils.SendMessage(error, pc.PlayerId);
                return true;
            }

            if (!CheckAble(deader, killer))
                return true;

            if (Verify(deader, killer))
            {
                Execute(killer);
                CurrentUsesThisMeeting--;
                SendRPC();
            }
            else
            {
                Utils.SendMessage(GetString("VerifyFailed"), pc.PlayerId);
                if (DeductOnFailed)
                {
                    CurrentUsesThisMeeting--;
                    SendRPC();
                }
            }
        }
        return true;
    }

    /// <summary>
    /// 通过玩家ID解析消息
    /// </summary>
    private static bool MsgToPlayersByID(string msg, out PlayerControl deader, out PlayerControl killer, out string error)
    {
        deader = null;
        killer = null;
        error = string.Empty;
        
        string[] parts = msg.Split(' ');
        if (parts.Length < 3)
        {
            error = GetString("VerifyCommandFormatError");
            return false;
        }
        
        if (!byte.TryParse(parts[1], out byte deaderId))
        {
            error = GetString("VerifyInvalidPlayerId");
            return false;
        }
        
        if (!byte.TryParse(parts[2], out byte killerId))
        {
            error = GetString("VerifyInvalidPlayerId");
            return false;
        }
        
        deader = Utils.GetPlayerById(deaderId);
        killer = Utils.GetPlayerById(killerId);

        if (deader == null || killer == null)
        {
            error = GetString("VerifyPlayerNotFound");
            return false;
        }

        return true;
    }

    private bool CheckAble(PlayerControl deader, PlayerControl killer)
    {
        if (deader.IsAlive())
        {
            Utils.SendMessage(GetString("VerifyDeaderNoDeath"), Player.PlayerId);
            return false;
        }

        if (deader.PlayerId == killer.PlayerId)
        {
            Utils.SendMessage(GetString("VerifyCoupleSame"), Player.PlayerId);
            return false;
        }

        return true;
    }
    
}