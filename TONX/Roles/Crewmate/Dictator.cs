using AmongUs.GameOptions;
using TONX.Modules;

namespace TONX.Roles.Crewmate;
public sealed class Dictator : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Dictator),
            player => new Dictator(player),
            CustomRoles.Dictator,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            21200,
            null,
            "dic|独裁者|独裁",
            "#df9b00"
        );
    public Dictator(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public override void OnStartMeeting() => lastVoted = byte.MaxValue;
    private byte lastVoted;
    public override bool CheckVoteAsVoter(PlayerControl votedFor)
    {
        if (votedFor != null && lastVoted == votedFor.PlayerId) return true;
        lastVoted = votedFor.PlayerId;
        ModifyVote(Player.PlayerId, votedFor.PlayerId, true);
        Utils.SendMessage(GetString("DictatorOnVote"), Player.PlayerId);
        return false;
    }
    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        var baseVote = (votedForId, numVotes, doVote);
        if (!isIntentional || voterId != Player.PlayerId || sourceVotedForId == Player.PlayerId || sourceVotedForId >= 253 || !Player.IsAlive())
        {
            return baseVote;
        }
        MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Suicide, Player.PlayerId);
        Utils.GetPlayerById(sourceVotedForId).SetRealKiller(Player);
        MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, sourceVotedForId);
        return (votedForId, numVotes, false);
    }
}