namespace TONX.Roles.AddOns.Common;
public sealed class Schizophrenic : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Schizophrenic),
            player => new Schizophrenic(player),
            CustomRoles.Schizophrenic,
            81500,
            SetupCustomOption,
            "sp|雙重人格|双重|双人格|人格",
            "#3a648f"
        );
    public Schizophrenic(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    private static void SetupCustomOption()
    {
        AddOnsAssignData.Create(RoleInfo, 10, CustomRoles.Schizophrenic, true, true, true);
    }
}