namespace TONX.Roles.AddOns.Common;
public sealed class Neptune : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Neptune),
            player => new Neptune(player),
            CustomRoles.Neptune,
            80600,
            null,
            "np|ntr|渣男",
            "#00a4ff",
            experimental: true,
            conflicts: Conflicts
        );
    public Neptune(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    private static List<CustomRoles> Conflicts = new() { CustomRoles.Lovers, CustomRoles.Hater };
}