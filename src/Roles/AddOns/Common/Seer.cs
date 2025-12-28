using TONX.Roles.Core.Interfaces;

namespace TONX.Roles.AddOns.Common;
public sealed class Seer : AddonBase, IKillFlashSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Seer),
            player => new Seer(player),
            CustomRoles.Seer,
            80900,
            null,
            "se|靈媒",
            "#61b26c",
            conflicts: Conflicts
        );
    public Seer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    private static List<CustomRoles> Conflicts = new() { CustomRoles.Mortician };

    public bool CheckKillFlash(MurderInfo info) => true;
}