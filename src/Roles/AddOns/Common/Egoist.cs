using TONX.Roles.Core.Interfaces;

namespace TONX.Roles.AddOns.Common;
public sealed class Egoist : AddonBase, IOverrideWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Egoist),
            player => new Egoist(player),
            CustomRoles.Egoist,
            80800,
            SetupCustomOption,
            "ego|利己主義者|利己主义|利己|野心",
            "#5600ff",
            assignTeam: (true, true, false),
            conflicts: Conflicts
        );
    public Egoist(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public static OptionItem OptionImpEgoVisibalToAllies;

    enum OptionName
    {
        ImpEgoistVisibalToAllies
    }

    private static List<CustomRoles> Conflicts = new() { CustomRoles.Madmate };
    private static void SetupCustomOption()
    {
        OptionImpEgoVisibalToAllies = BooleanOptionItem.Create(RoleInfo, 20, OptionName.ImpEgoistVisibalToAllies, true, false);
    }

    public void CheckWin(ref CustomWinner WinnerTeam, ref HashSet<byte> WinnerIds)
    {
        if ((CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate && Player.GetCustomRole().IsCrewmate())
            || (CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor && Player.GetCustomRole().IsImpostor()))
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Egoist);
            CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
        }
    }
}