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
    public override bool OnSendMessage(string msg, out MsgRecallMode recallMode)
    {
        recallMode = MsgRecallMode.None;

        if (!GameStates.IsInGame) return false;

        msg = msg.Trim().ToLower();
        if (msg.Length < 3 || msg[..3] != "/rv") return false;

        if (!AmongUsClient.Instance.AmHost) return true;

        if (msg == "/rv")
        {
            string text = GetString("PlayerIdList");
            foreach (var npc in Main.AllAlivePlayerControls)
                text += "\n" + npc.PlayerId + " → (" + Utils.GetTrueRoleName(npc.PlayerId, false) + ") " + npc.GetRealName();
            Utils.SendMessage(text, Player.PlayerId);
            return true;
        }

        PlayerControl target;
        try
        {
            var targetId = byte.Parse(msg.Replace("/rv", string.Empty));
            target = Utils.GetPlayerById(targetId);
        }
        catch
        {
            Utils.SendMessage(GetString("MafiaKillDead"), Player.PlayerId);
            return true;
        }

        if (!CanRevenge(target, out var reason))
        {
            Utils.SendMessage(reason, Player.PlayerId);
            return true;
        }

        RevengeLimit--;
        RevengeKill(Player, target);

        return true;
    }
    private static void RevengeKill(PlayerControl killer, PlayerControl target)
    {
        Logger.Info($"{killer.GetNameWithRole()} 复仇了 {target.GetNameWithRole()}", "Mafia");
        CustomSoundsManager.RPCPlayCustomSoundAll("AWP");
        string Name = target.GetRealName();
        _ = new LateTask(() =>
        {
            var state = PlayerState.GetByPlayerId(target.PlayerId);
            state.DeathReason = CustomDeathReason.Revenge;
            target.SetRealKiller(killer);
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
            _ = new LateTask(() => { Utils.SendMessage(string.Format(GetString("MafiaKillSucceed"), Name), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mafia), GetString("MafiaRevengeTitle"))); }, 0.6f, "Mafia Kill");
        }, 0.2f, "Mafia Kill");
    }
    private bool CanRevenge(PlayerControl target, out string reason)
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
        if (RevengeLimit <= 0)
        {
            reason = GetString("MafiaKillMax");
            return false;
        }
        if (target == null || target.Data.IsDead)
        {
            reason = GetString("MafiaKillDead");
            return false;
        }
        return true;
    }

    private void MafiaOnClick(PlayerControl target)
    {
        Logger.Msg($"Click: ID {target.GetNameWithRole()}", "Mafia UI");
        if (target == null || !target.IsAlive() || !GameStates.IsVoting) return;

        if (!CanRevenge(target, out var reason))
        {
            PlayerControl.LocalPlayer.ShowPopUp(reason);
            return;
        }

        RevengeLimit--;
        RevengeKill(PlayerControl.LocalPlayer, target);
    }

    public string ButtonName { get; private set; } = "Target";
    public bool ShouldShowButton() => !Player.IsAlive();
    public bool ShouldShowButtonFor(PlayerControl target) => target.IsAlive();
    public void OnClickButton(PlayerControl target) => MafiaOnClick(target);
}