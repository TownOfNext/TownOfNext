namespace TONX.Roles.AddOns.Impostor;
public sealed class Mimic : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Mimic),
            player => new Mimic(player),
            CustomRoles.Mimic,
            82000,
            SetupCustomOption,
            "mi|寶箱怪|宝箱",
            "#ff1919"
        );
    public Mimic(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    private static void SetupCustomOption()
    {
        AddOnsAssignData.Create(RoleInfo, 10, CustomRoles.Mimic, false, true, false);
    }
}