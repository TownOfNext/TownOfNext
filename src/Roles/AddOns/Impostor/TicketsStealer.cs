namespace TONX.Roles.AddOns.Impostor;
public sealed class TicketsStealer : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(TicketsStealer),
            player => new TicketsStealer(player),
            CustomRoles.TicketsStealer,
            81900,
            SetupCustomOption,
            "ts|竊票者|偷票|偷票者|窃票师|窃票",
            "#ff1919",
            assignTeam: (false, true, false),
            conflicts: Conflicts
        );
    public TicketsStealer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public static OptionItem OptionTicketsPerKill;
    enum OptionName
    {
        TicketsPerKill
    }

    private static List<CustomRoles> Conflicts = new() { CustomRoles.Bomber, CustomRoles.BoobyTrap, CustomRoles.Capitalist };
    private static void SetupCustomOption()
    {
        OptionTicketsPerKill = FloatOptionItem.Create(RoleInfo, 20, OptionName.TicketsPerKill, new(0.1f, 10f, 0.1f), 0.5f, false)
            .SetValueFormat(OptionFormat.Votes);
    }

    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        if (voterId == Player.PlayerId)
        {
            numVotes += (int)((PlayerState.GetByPlayerId(voterId)?.GetKillCount(true) ?? 0) * OptionTicketsPerKill.GetFloat());
            Logger.Info($"TicketsStealer Additional Votes: {numVotes}", "TicketsStealer.OnVote");
        }
        return (votedForId, numVotes, doVote);
    }
    public static string GetProgressText(byte playerId, bool comms = false)
    {
        if (!Utils.GetPlayerById(playerId)?.Is(CustomRoles.TicketsStealer) ?? true) return "";
        var votes = (int)((PlayerState.GetByPlayerId(playerId)?.GetKillCount(true) ?? 0) * OptionTicketsPerKill.GetFloat());
        return votes > 0 ? Utils.ColorString(RoleInfo.RoleColor.ShadeColor(0.5f), $"+{votes}") : "";
    }
}