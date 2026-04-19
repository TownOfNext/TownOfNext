using AmongUs.GameOptions;
using UnityEngine;

namespace TONX.Roles.Crewmate;
public sealed class Drafter : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Drafter),
            player => new Drafter(player),
            CustomRoles.Drafter,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.GameMode,
            100001,
            null,
            "dra|起草者",
            "#ffffff",
            Hidden: new HiddenRoleInfo(0, null)
        );
    public Drafter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}