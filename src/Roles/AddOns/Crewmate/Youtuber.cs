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
}