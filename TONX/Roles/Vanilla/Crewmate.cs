using AmongUs.GameOptions;

namespace TONX.Roles.Vanilla;

public sealed class Crewmate : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(Crewmate),
            player => new Crewmate(player),
            RoleTypes.Crewmate,
            "#8cffff"
        );
    public Crewmate(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}