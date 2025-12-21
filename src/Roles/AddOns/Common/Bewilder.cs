namespace TONX.Roles.AddOns.Common;
public sealed class Bewilder : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Bewilder),
            player => new Bewilder(player),
            CustomRoles.Bewilder,
            81200,
            SetupCustomOption,
            "bwd|迷幻|迷惑者",
            "#c894f5",
            assignTeam: (true, false, true),
            conflicts: Conflicts
        );
    public Bewilder(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public static OptionItem OptionVision;

    enum OptionName
    {
        BewilderVision
    }

    private static List<CustomRoles> Conflicts = new() { CustomRoles.Lighter };
    private static void SetupCustomOption()
    {
        OptionVision = FloatOptionItem.Create(RoleInfo, 20, OptionName.BewilderVision, new(0f, 5f, 0.05f), 0.6f, false)
            .SetValueFormat(OptionFormat.Multiplier);
    }
}