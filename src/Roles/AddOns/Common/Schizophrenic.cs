namespace TONX.Roles.AddOns.Common;
public sealed class Schizophrenic : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Schizophrenic),
            player => new Schizophrenic(player),
            CustomRoles.Schizophrenic,
            81500,
            null,
            "sp|雙重人格|双重|双人格|人格",
            "#3a648f",
            assignTeam: (true, true, false),
            conflicts: Conflicts
        );
    public Schizophrenic(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    private static List<CustomRoles> Conflicts = new() { CustomRoles.Madmate };
}