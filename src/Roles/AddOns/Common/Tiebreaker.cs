using Rewired;

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
    { }

    private byte TiebreakerVote = byte.MaxValue;

    private static List<CustomRoles> Conflicts = new() { CustomRoles.Dictator };

    public override void OnVote(byte target)
    {
        if (Utils.GetPlayerById(target) != null) TiebreakerVote = target;
    }
    public static bool ChooseExileTarget(byte[] mostVotedPlayers, out byte target)
    {
        target = byte.MaxValue;
        List<byte> votes = new();
        foreach (var tieBreaker in Main.AllAlivePlayerControls.Where(p => p.Is(CustomRoles.Tiebreaker)))
        {
            var bclass = tieBreaker.GetAddonClasses().FirstOrDefault(c => c is Tiebreaker) as Tiebreaker;
            votes.Add(bclass.TiebreakerVote);
        }
        if (mostVotedPlayers.Count(votes.Contains) == 1)
        {
            target = mostVotedPlayers.FirstOrDefault(votes.Contains);
            Logger.Info($"Tiebreaker Override Tie => {Utils.GetPlayerById(target)?.GetNameWithRole()}", "Tiebreaker");
            return true;
        }
        return false;
    }
    public override void OnStartMeeting()
    {
        TiebreakerVote = byte.MaxValue;
    }
}