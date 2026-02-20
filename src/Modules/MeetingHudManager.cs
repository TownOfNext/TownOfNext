namespace TONX.Modules;

static class MeetingHudManager
{
    /// <summary>
    /// 用于强制结束会议<br/>
    /// 所有投票都将被清空<br/>
    /// </summary>s
    public static void RpcForceEndMeeting(this MeetingHud meetingHud)
    {
        if (meetingHud == null) return;
        foreach (var pva in meetingHud.playerStates)
        {
            if (pva == null) continue;
            if (pva.VotedFor < 253) meetingHud.RpcClearVote(pva.TargetPlayerId);
        }
        List<MeetingHud.VoterState> voterStates = [];
        meetingHud.RpcVotingComplete(voterStates.ToArray(), null, true);
        meetingHud.RpcClose();
    }
}