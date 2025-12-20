namespace TONX.Roles.AddOns.Crewmate;
public sealed class YouTuber : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(YouTuber),
            player => new YouTuber(player),
            CustomRoles.YouTuber,
            80700,
            SetupCustomOption,
            "yt|up",
            "#fb749b"
        );
    public YouTuber(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    private static void SetupCustomOption()
    {
        AddOnsAssignData.Create(RoleInfo, 10, CustomRoles.YouTuber, true, false, false);
    }
}