namespace TONX.Roles.AddOns.Common;
public sealed class Seer : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Seer),
            player => new Seer(player),
            CustomRoles.Seer,
            80900,
            SetupCustomOption,
            "se|靈媒",
            "#61b26c"
        );
    public Seer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    private static void SetupCustomOption()
    {
        AddOnsAssignData.Create(RoleInfo, 10, CustomRoles.Seer, true, true, true);
    }
}