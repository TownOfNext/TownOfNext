namespace TONX.Roles.AddOns.Common;
public sealed class Oblivious : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Oblivious),
            player => new Oblivious(player),
            CustomRoles.Oblivious,
            81100,
            null,
            "pb|膽小鬼|胆小",
            "#424242",
            conflicts: Conflicts
        );
    public Oblivious(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    private static List<CustomRoles> Conflicts = new() { CustomRoles.DetectiveTONX, CustomRoles.Cleaner, CustomRoles.Mortician, CustomRoles.Medium };
}