using AmongUs.GameOptions;

namespace TONX.Roles.AddOns.Common;
public sealed class Reach : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Reach),
            player => new Reach(player),
            CustomRoles.Reach,
            81600,
            null,
            "re|持槍|手长",
            "#74ba43"
        );
    public Reach(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetInt(Int32OptionNames.KillDistance, 2);
    }
}