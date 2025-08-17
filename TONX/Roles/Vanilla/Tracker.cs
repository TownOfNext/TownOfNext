using AmongUs.GameOptions;

namespace TONX.Roles.Vanilla;

public sealed class Tracker : RoleBase
{
    public readonly static SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(Tracker),
            player => new Tracker(player),
            RoleTypes.Tracker,
            "#8cffff"
        );
    public Tracker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}
