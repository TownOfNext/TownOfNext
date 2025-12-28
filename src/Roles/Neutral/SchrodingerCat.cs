using AmongUs.GameOptions;
using TONX.Roles.Core.Interfaces;

namespace TONX.Roles.Neutral;

// マッドが属性化したらマッド状態時の特別扱いを削除する
public sealed class SchrodingerCat : RoleBase, IAdditionalWinner, IDeathReasonSeeable, IKillFlashSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SchrodingerCat),
            player => new SchrodingerCat(player),
            CustomRoles.SchrodingerCat,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            50400,
            SetupOptionItem,
            "sc|cat|猫|猫猫|薛丁格的貓|貓",
            "#696969",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public SchrodingerCat(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CanWinTheCrewmateBeforeChange = OptionCanWinTheCrewmateBeforeChange.GetBool();
        ChangeTeamWhenExile = OptionChangeTeamWhenExile.GetBool();
        // CanSeeKillableTeammate = OptionCanSeeKillableTeammate.GetBool();
        ChangeToSpecificImpostorRole = OptionChangeToSpecficImpostorRole.GetBool();
        
    }
    static OptionItem OptionCanWinTheCrewmateBeforeChange;
    static OptionItem OptionChangeTeamWhenExile;
    // static OptionItem OptionCanSeeKillableTeammate;
    static OptionItem OptionChangeToSpecficImpostorRole;

    enum OptionName
    {
        CanBeforeSchrodingerCatWinTheCrewmate,
        SchrodingerCatExiledTeamChanges,
        // SchrodingerCatCanSeeKillableTeammate,
        SchrodingerCatChangeToSpecficImpostorRole,
    }
    static bool CanWinTheCrewmateBeforeChange;
    static bool ChangeTeamWhenExile;
    // static bool CanSeeKillableTeammate;
    static bool ChangeToSpecificImpostorRole;

    public static void SetupOptionItem()
    {
        OptionCanWinTheCrewmateBeforeChange = BooleanOptionItem.Create(RoleInfo, 10, OptionName.CanBeforeSchrodingerCatWinTheCrewmate, true, false);
        OptionChangeTeamWhenExile = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SchrodingerCatExiledTeamChanges, false, false);
        // OptionCanSeeKillableTeammate = BooleanOptionItem.Create(RoleInfo, 12, OptionName.SchrodingerCatCanSeeKillableTeammate, false, false);
        OptionChangeToSpecficImpostorRole = BooleanOptionItem.Create(RoleInfo, 13, OptionName.SchrodingerCatChangeToSpecficImpostorRole, true, false);
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        var killer = info.AttemptKiller;
        var target = info.AttemptTarget;

        //自殺ならスルー
        if (info.IsSuicide) return true;

        var role = killer.GetCustomRole();
        if (!ChangeToSpecificImpostorRole && role.IsImpostor()) role = CustomRoles.Impostor;
        target.RpcChangeRole(role);
        Utils.NotifyRoles();
        Logger.Info($"薛定谔的猫{target?.Data?.PlayerName}被{killer.GetNameWithRole()}击杀了", "SchrodingerCat");
        return false;
    }
    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner)
    {
        if (exiled.PlayerId != Player.PlayerId || !ChangeTeamWhenExile) return;
        ChangeTeamRandomly();
    }
    /// <summary>
    /// ゲームに存在している陣営の中からランダムに自分の陣営を変更する
    /// </summary>
    private void ChangeTeamRandomly()
    {
        var rand = IRandom.Instance;
        List<CustomRoles> allCandidates = new List<CustomRoles>
        {
            CustomRoles.Crewmate,
            CustomRoles.Impostor,
            CustomRoles.Sidekick,
            CustomRoles.Pelican,
            CustomRoles.BloodKnight,
            CustomRoles.Demon,
            CustomRoles.Hater,
            CustomRoles.Stalker
        };
        List<CustomRoles> validCandidates = new List<CustomRoles>();
        allCandidates.Where(r => r.IsExist()).Do(validCandidates.Add);
        var newRole = validCandidates[IRandom.Instance.Next(validCandidates.Count)];
        Player.RpcChangeRole(newRole);
        Utils.NotifyRoles();
        Logger.Info($"薛定谔的猫{Player?.Data?.PlayerName}被票出了", "SchrodingerCat");
    }
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        return CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate && CanWinTheCrewmateBeforeChange;
    }
}
