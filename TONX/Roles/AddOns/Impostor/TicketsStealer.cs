using TONX.Attributes;
using UnityEngine;
using static TONX.Options;

namespace TONX.Roles.AddOns.Impostor;
public static class TicketsStealer
{
    private static readonly int Id = 81900;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.TicketsStealer);
    private static List<byte> playerIdList = new();

    public static OptionItem OptionTicketsPerKill;

    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.TicketsStealer);
        AddOnsAssignData.Create(Id + 10, CustomRoles.TicketsStealer, false, true, false);
        OptionTicketsPerKill = FloatOptionItem.Create(Id + 20, "TicketsPerKill", new(0.1f, 10f, 0.1f), 0.5f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.TicketsStealer])
            .SetValueFormat(OptionFormat.Votes);
    }
    [GameModuleInitializer]
    public static void Init()
    {
        playerIdList = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
    public static void ModifyVote(ref byte voterId, ref byte voteFor, ref bool isIntentional, ref int numVotes, ref bool doVote)
    {
        if (playerIdList.Contains(voterId))
        {
            numVotes += (int)((PlayerState.GetByPlayerId(voterId)?.GetKillCount(true) ?? 0) * OptionTicketsPerKill.GetFloat());
            Logger.Info($"TicketsStealer Additional Votes: {numVotes}", "TicketsStealer.OnVote");
        }
    }
    public static string GetProgressText(byte playerId, bool comms = false)
    {
        if (!playerIdList.Contains(playerId)) return "";
        var votes = (int)((PlayerState.GetByPlayerId(playerId)?.GetKillCount(true) ?? 0) * OptionTicketsPerKill.GetFloat());
        return votes > 0 ? Utils.ColorString(RoleColor.ShadeColor(0.5f), $"+{votes}") : "";
    }
}