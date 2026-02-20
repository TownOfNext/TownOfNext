using AmongUs.GameOptions;
using TONX.Roles.Core.Interfaces;

namespace TONX.Roles.Neutral;

public sealed class Sunnyboy : RoleBase, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
       SimpleRoleInfo.Create(
            typeof(Sunnyboy),
            player => new Sunnyboy(player),
            CustomRoles.Sunnyboy,
            () => RoleTypes.Scientist,
            CustomRoleTypes.Neutral,
            52700,
            null,
            "sb|阳光开朗大男孩|大男孩",
            "#ff9902"
#if RELEASE
            ,
            Hidden: new HiddenRoleInfo(3, CustomRoles.Jester) // For Debug
#endif
        );
    public Sunnyboy(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        
    }

    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);

    public bool CheckWin(ref CustomRoles winnerRole)
    {
        return !Player.IsAlive();
    }
}
