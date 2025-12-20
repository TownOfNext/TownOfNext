namespace TONX.Roles.AddOns.Common;
public sealed class Egoist : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Egoist),
            player => new Egoist(player),
            CustomRoles.Egoist,
            80800,
            SetupCustomOption,
            "ego|利己主義者|利己主义|利己|野心",
            "#5600ff"
        );
    public Egoist(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public static OptionItem OptionImpEgoVisibalToAllies;

    enum OptionName
    {
        ImpEgoistVisibalToAllies
    }

    private static void SetupCustomOption()
    {
        AddOnsAssignData.Create(RoleInfo, 10, CustomRoles.Egoist, true, true, false);
        OptionImpEgoVisibalToAllies = BooleanOptionItem.Create(RoleInfo, 20, OptionName.ImpEgoistVisibalToAllies, true, false);
    }
}