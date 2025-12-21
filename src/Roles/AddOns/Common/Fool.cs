namespace TONX.Roles.AddOns.Common;
public sealed class Fool : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Fool),
            player => new Fool(player),
            CustomRoles.Fool,
            81300,
            SetupCustomOption,
            "fo|蠢蛋|笨蛋|蠢狗|傻逼",
            "#e6e7ff",
            conflicts: Conflicts
        );
    public Fool(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public static OptionItem OptionImpFoolCanNotSabotage;
    public static OptionItem OptionImpFoolCanNotOpenDoor;

    enum OptionName
    {
        ImpFoolCanNotSabotage,
        FoolCanNotOpenDoor
    }

    private static List<CustomRoles> Conflicts = new() { CustomRoles.Repairman };
    private static void SetupCustomOption()
    {
        OptionImpFoolCanNotSabotage = BooleanOptionItem.Create(RoleInfo, 20, OptionName.ImpFoolCanNotSabotage, true, false);
        OptionImpFoolCanNotOpenDoor = BooleanOptionItem.Create(RoleInfo, 21, OptionName.FoolCanNotOpenDoor, false, false);
    }
}