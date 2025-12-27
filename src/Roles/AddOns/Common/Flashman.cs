using AmongUs.GameOptions;

namespace TONX.Roles.AddOns.Common;
public sealed class Flashman : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Flashman),
            player => new Flashman(player),
            CustomRoles.Flashman,
            80500,
            SetupCustomOption,
            "fl|閃電俠|闪电",
            "#ff8400",
            experimental: true,
            conflicts: Conflicts
        );
    public Flashman(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public static OptionItem OptionSpeed;

    enum OptionName
    {
        FlashmanSpeed
    }

    private static List<CustomRoles> Conflicts = new() { CustomRoles.Swooper };
    private static void SetupCustomOption()
    {
        OptionSpeed = FloatOptionItem.Create(RoleInfo, 20, OptionName.FlashmanSpeed, new(0.25f, 5f, 0.25f), 2.5f, false)
            .SetValueFormat(OptionFormat.Multiplier);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        Main.AllPlayerSpeed[Player.PlayerId] = OptionSpeed.GetFloat();
    }
}