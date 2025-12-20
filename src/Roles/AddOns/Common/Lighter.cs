namespace TONX.Roles.AddOns.Common;
public sealed class Lighter : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Lighter),
            player => new Lighter(player),
            CustomRoles.Lighter,
            82100,
            SetupCustomOption,
            "li|執燈人|执灯|灯人|小灯人",
            "#eee5be"
        );
    public Lighter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public static OptionItem OptionVistion;

    enum OptionName
    {
        LighterVision
    }

    private static void SetupCustomOption()
    {
        AddOnsAssignData.Create(RoleInfo, 10, CustomRoles.Lighter, true, true, true);
        OptionVistion = FloatOptionItem.Create(RoleInfo, 20, OptionName.LighterVision, new(0.5f, 5f, 0.25f), 1.5f, false)
            .SetValueFormat(OptionFormat.Multiplier);
    }
}