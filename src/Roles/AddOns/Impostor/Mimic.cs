namespace TONX.Roles.AddOns.Impostor;
public sealed class Mimic : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Mimic),
            player => new Mimic(player),
            CustomRoles.Mimic,
            82000,
            null,
            "mi|寶箱怪|宝箱",
            "#ff1919",
            assignTeam: (false, true, false),
            conflicts: Conflicts
        );
    public Mimic(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    private static List<CustomRoles> Conflicts = new() { CustomRoles.Mafia };
}