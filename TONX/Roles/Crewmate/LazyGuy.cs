using AmongUs.GameOptions;

namespace TONX.Roles.Crewmate;
public sealed class LazyGuy : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(LazyGuy),
            player => new LazyGuy(player),
            CustomRoles.LazyGuy,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20200,
            null,
            "lg|擺爛人|摆烂",
            "#a4dffe"
        );
    public LazyGuy(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}