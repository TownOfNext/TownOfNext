using AmongUs.GameOptions;
using TONX.Roles.Core.Interfaces;

namespace TONX.Roles.Vanilla;

public sealed class Viper : RoleBase, IImpostor, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(Viper),
            player => new Viper(player),
            RoleTypes.Viper
        );
    public Viper(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}