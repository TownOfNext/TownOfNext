using AmongUs.GameOptions;

namespace TONX.Roles.Crewmate;
public sealed class Observer : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Observer),
            player => new Observer(player),
            CustomRoles.Observer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            22600,
            null,
            "ob|觀察者|观察",
            "#a8e0fa"
        );
    public Observer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}