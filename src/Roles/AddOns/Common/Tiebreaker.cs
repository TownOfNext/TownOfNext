namespace TONX.Roles.AddOns.Common;
public sealed class Tiebreaker : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Tiebreaker),
            player => new Tiebreaker(player),
            CustomRoles.Tiebreaker,
            81000,
            null,
            "br|破平",
            "#1447af",
            conflicts: Conflicts
        );
    public Tiebreaker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        TiebreakerVotes = new();
    }

    private static Dictionary<byte, byte> TiebreakerVotes = new();

    private static List<CustomRoles> Conflicts = new() { CustomRoles.Dictator };

    public static void OnVote(byte voter, byte target)
    {
        if (Utils.GetPlayerById(voter).Is(CustomRoles.Tiebreaker))
        {
            TiebreakerVotes.TryAdd(voter, target);
            TiebreakerVotes[voter] = target;
        }
    }
    public static bool ChooseExileTarget(byte[] mostVotedPlayers, out byte target)
    {
        target = byte.MaxValue;
        if (mostVotedPlayers.Count(TiebreakerVotes.ContainsValue) == 1)
        {
            target = mostVotedPlayers.FirstOrDefault(TiebreakerVotes.ContainsValue);
            Logger.Info($"Tiebreaker Override Tie => {Utils.GetPlayerById(target)?.GetNameWithRole()}", "Tiebreaker");
            return true;
        }
        return false;
    }
    public static void OnMeetingStart()
    {
        TiebreakerVotes = new();
    }
}