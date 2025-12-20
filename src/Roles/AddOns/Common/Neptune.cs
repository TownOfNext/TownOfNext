namespace TONX.Roles.AddOns.Common;
public sealed class Neptune : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Neptune),
            player => new Neptune(player),
            CustomRoles.Neptune,
            80600,
            SetupCustomOption,
            "np|ntr|渣男",
            "#00a4ff",
            experimental: true
        );
    public Neptune(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    private static void SetupCustomOption()
    {
        AddOnsAssignData.Create(RoleInfo, 10, CustomRoles.Neptune, true, true, true);
    }
}