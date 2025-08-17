using AmongUs.GameOptions;

namespace TONX.Roles.Vanilla;

public sealed class Scientist : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(Scientist),
            player => new Scientist(player),
            RoleTypes.Scientist,
            "#8cffff"
        );
    public Scientist(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}