namespace TONX.Roles.AddOns.Common;
public sealed class Lovers : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Lovers),
            player => new Lovers(player),
            CustomRoles.Lovers,
            80100,
            SetupCustomOption,
            "lo|情人|愛人|链子",
            "#ff9ace",
            assignCountRule: new(2, 2, 2),
            hasAssignData: false
        );
    public Lovers(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public static OptionItem LoverKnowRoles;
    public static OptionItem LoverSuicide;
    enum OptionName
    {
        LoverKnowRoles, 
        LoverSuicide
    }
    private static void SetupCustomOption()
    {
        LoverKnowRoles = BooleanOptionItem.Create(RoleInfo, 11, OptionName.LoverKnowRoles, true, false)
            .SetGameMode(CustomGameMode.Standard);
        LoverSuicide = BooleanOptionItem.Create(RoleInfo, 10, OptionName.LoverSuicide, true, false)
            .SetGameMode(CustomGameMode.Standard);
    }
}