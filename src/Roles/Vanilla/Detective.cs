using AmongUs.GameOptions;

namespace TONX.Roles.Vanilla;

public sealed class Detective : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(Detective),
            player => new Detective(player),
            RoleTypes.Detective
        );
    public Detective(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}