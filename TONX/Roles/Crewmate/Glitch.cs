using AmongUs.GameOptions;

namespace TONX.Roles.Crewmate;
public sealed class Glitch : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Glitch),
            player => new Glitch(player),
            CustomRoles.Glitch,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            23000,
            SetupOptionItem,
            "gl|活死",
            "#dcdcdc"
        );
    public Glitch(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionCanVote;
    enum OptionName
    {
        GlitchCanVote,
    }

    private static void SetupOptionItem()
    {
        OptionCanVote = BooleanOptionItem.Create(RoleInfo, 10, OptionName.GlitchCanVote, true, false);
    }
    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        var baseVote = (votedForId, numVotes, doVote);
        if (!isIntentional || voterId != Player.PlayerId || OptionCanVote.GetBool() || sourceVotedForId >= 253 || !Player.IsAlive())
        {
            return baseVote;
        }
        return (votedForId, numVotes, false);
    }
}