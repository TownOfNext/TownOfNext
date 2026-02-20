using AmongUs.GameOptions;
using TONX.Modules;
using TONX.Roles.Core.Interfaces;

namespace TONX.Roles.Impostor;
public sealed class Mafia : RoleBase, IImpostor, IMeetingButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Mafia),
            player => new Mafia(player),
            CustomRoles.Mafia,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            2200,
            SetupOptionItem,
            "mf|黑手黨|黑手"
        );
    public Mafia(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        RevengeLimit = OptionRevengeNum.GetInt();
    }

    private static OptionItem OptionRevengeNum;
    enum OptionName
    {
        MafiaCanKillNum,
    }
    public int RevengeLimit = 0;
    private static void SetupOptionItem()
    {
        OptionRevengeNum = IntegerOptionItem.Create(RoleInfo, 10, OptionName.MafiaCanKillNum, new(0, 15, 1), 1, false)
            .SetValueFormat(OptionFormat.Players);
    }
    public override void Add()
    {
        RevengeLimit = OptionRevengeNum.GetInt();
    }
    public bool CanUseKillButton()
    {
        if (PlayerState.AllPlayerStates == null) return false;
        //マフィアを除いた生きているインポスターの人数  Number of Living Impostors excluding mafia
        int livingImpostorsNum = 0;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            var role = pc.GetCustomRole();
            if (role != CustomRoles.Mafia && role.IsImpostor()) livingImpostorsNum++;
        }

        return livingImpostorsNum <= 0;
    }
    public string ButtonName { get; private set; } = "Target";
    public bool ShouldShowButton() => !Player.IsAlive();
    public bool ShouldShowButtonFor(PlayerControl target) => target.IsAlive();
    public override bool OnSendMessage(string msg, out MsgRecallMode recallMode)
    {
        bool isCommand = RevengeMsg(Player, msg);
        recallMode = MsgRecallMode.None;
        return isCommand;
    }
    public void OnClickButton(PlayerControl target)
    {
        Logger.Msg($"Click: ID {target.GetNameWithRole()}", "Mafia UI");
        if (!Revenge(target, out var reason, true))
            Player.ShowPopUp(reason);
    }
    private bool Revenge(PlayerControl target, out string reason, bool isUi = false)
    {
        reason = string.Empty;

        if (OptionRevengeNum.GetInt() < 1)
        {
            reason = GetString("MafiaKillDisable");
            return false;
        }
        if (Player.IsAlive())
        {
            reason = GetString("MafiaAliveKill");
            return false;
        }
        if (RevengeLimit < 1)
        {
            reason = GetString("MafiaKillMax");
            return false;
        }

        Logger.Info($"{Player.GetNameWithRole()} 复仇了 {target.GetNameWithRole()}", "Mafia");
        

        string Name = target.GetRealName();

        RevengeLimit--;
        CustomSoundsManager.RPCPlayCustomSoundAll("AWP");

        _ = new LateTask(() =>
        {
            var state = PlayerState.GetByPlayerId(target.PlayerId);
            state.DeathReason = CustomDeathReason.Revenge;
            target.SetRealKiller(Player);
            if (GameStates.IsMeeting)
            {
                target.RpcSuicideWithAnime();
                //死者检查
                Utils.NotifyRoles(isForMeeting: true, NoCache: true);
            }
            else
            {
                target.RpcMurderPlayer(target);
                state.SetDead();
            }
            _ = new LateTask(() => { Utils.SendMessage(string.Format(GetString("MafiaKillSucceed"), Name), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mafia), GetString("MafiaRevengeTitle")), false, true, Name); }, 0.6f, "Mafia Kill");
        }, 0.2f, "Mafia Kill");

        return true;
    }
    public bool RevengeMsg(PlayerControl pc, string msg)
    {
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.Mafia)) return false;

        if (!ChatCommand.OperateRoleCommand(ref msg, "rv|revenge|复仇", out int operate)) return false;

        if (operate == 1)
        {
            Utils.SendMessage(ChatCommand.GetFormatString(true), pc.PlayerId);
            return true;
        }
        if (operate == 2)
        {
            if (!AmongUsClient.Instance.AmHost) return true;

            if (!MsgToPlayer(msg, out PlayerControl target, out string error))
            {
                Utils.SendMessage(error, pc.PlayerId);
                return true;
            }

            if (!Revenge(target, out var reason))
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
            error = multiplePlayers ? GetString("RevengeMultipleColor") : GetString("MafiaHelp");
            return false;
        }
        if (target.Data.IsDead)
        {
            error = GetString("MafiaKillDead");
            return false;
        }
        return true;
    }
}