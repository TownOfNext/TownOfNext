namespace TONX.Roles.AddOns.Common;
public sealed class Oblivious : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Oblivious),
            player => new Oblivious(player),
            CustomRoles.Oblivious,
            81100,
            SetupCustomOption,
            "pb|膽小鬼|胆小",
            "#424242"
        );
    public Oblivious(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    private static void SetupCustomOption()
    {
        AddOnsAssignData.Create(RoleInfo, 10, CustomRoles.Oblivious, true, true, true);
    }
}