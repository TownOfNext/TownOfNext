using TONX.Modules;

namespace TONX.Roles.AddOns.Crewmate;
public sealed class YouTuber : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(YouTuber),
            player => new YouTuber(player),
            CustomRoles.YouTuber,
            80700,
            null,
            "yt|up",
            "#fb749b",
            assignTeam: (true, false, false),
            conflicts: Conflicts
        );
    public YouTuber(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    private static List<CustomRoles> Conflicts = new() { CustomRoles.Madmate, CustomRoles.Sheriff };

    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        //看看UP是不是被首刀了
        if (Main.FirstDied == byte.MaxValue)
        {
            CustomSoundsManager.RPCPlayCustomSoundAll("Congrats");
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.YouTuber); //UP主被首刀了，哈哈哈哈哈
            CustomWinnerHolder.WinnerIds.Add(info.AttemptTuple.target.PlayerId);
        }
    }
}