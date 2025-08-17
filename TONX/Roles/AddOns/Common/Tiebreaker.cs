using TONX.Attributes;
using UnityEngine;
using static TONX.Options;

namespace TONX.Roles.AddOns.Common;
public static class Tiebreaker
{
    private static readonly int Id = 81000;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Tiebreaker);
    private static List<byte> playerIdList = new();

    private static Dictionary<byte, byte> TiebreakerVotes = new();

    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.Tiebreaker);
        AddOnsAssignData.Create(Id + 10, CustomRoles.Tiebreaker, true, true, true);
    }
    [GameModuleInitializer]
    public static void Init()
    {
        playerIdList = new();
        TiebreakerVotes = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
    public static void OnVote(byte voter, byte target)
    {
        if (playerIdList.Contains(voter))
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
            target = mostVotedPlayers.Where(TiebreakerVotes.ContainsValue).FirstOrDefault();
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