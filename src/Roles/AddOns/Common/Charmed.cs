namespace TONX.Roles.AddOns.Common;
public sealed class Charmed : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Charmed),
            player => new Charmed(player),
            CustomRoles.Charmed,
            82200,
            null,
            "cha|魅惑",
            "#ff00ff",
            Hidden: new HiddenRoleInfo(0, null),
            hasAssignData: false,
            assignTeam: (true, true, false)
        );
    public Charmed(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}