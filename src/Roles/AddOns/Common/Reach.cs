namespace TONX.Roles.AddOns.Common;
public sealed class Reach : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Reach),
            player => new Reach(player),
            CustomRoles.Reach,
            81600,
            SetupCustomOption,
            "re|持槍|手长",
            "#74ba43"
        );
    public Reach(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    private static void SetupCustomOption()
    {
        AddOnsAssignData.Create(RoleInfo, 10, CustomRoles.Reach, true, true, true);
    }
}