using UnityEngine;

namespace TONX.Modules;

public class RoleDraftManager
{
    public static int Timer;
    public static long lastFixedUpdate;
    public static bool IsRoleDraftMeeting;
    public static List<CustomRoles> RolesToAssign;
    public static List<CustomRoles> FourRoles;
    public static Dictionary<byte, CustomRoles> DraftRoleResult;
    public static List<byte> ArrangedPlayers;
    public static int CurrentAssignIndex;
    public static void OnPlayerChooseRole(byte playerId, string id)
    {
        if (!GameStates.InGame) return;
        if (playerId != ArrangedPlayers[CurrentAssignIndex])
        {
            Utils.SendMessage(GetString("DraftAssignWait"), playerId);
            return;
        }
        int roleId = id switch
        {
            "1" => 0,
            "2" => 1,
            "3" => 2,
            "4" => 3,
            _ => -1
        };
        AfterChooseRole(playerId, roleId);
    }
    public static void RandomlyChooseRole(byte playerId)
    {
        if (!GameStates.InGame) return;
        AfterChooseRole(playerId, IRandom.Instance.Next(0, 4));
    }
    public static void AfterChooseRole(byte playerId, int roleId)
    {
        if (!GameStates.InGame) return;
        if (roleId < 0)
        {
            Utils.SendMessage(GetString("FailedChosen"), playerId);
            return;
        }
        DraftRoleResult.Add(playerId, FourRoles[roleId]);
        RolesToAssign.Remove(FourRoles[roleId]);
        Utils.SendMessage(string.Format(GetString("SuccessfullyChosen"), Utils.GetRoleName(FourRoles[roleId])), playerId);
        NextPlayer();
    }
    public static void ArrangePlayers()
    {
        ArrangedPlayers = new();
        var players = Main.AllAlivePlayerControls.ToList();
        while (players.Count > 0)
        {
            var rd = IRandom.Instance.Next(0, players.Count);
            ArrangedPlayers.Add(players[rd].PlayerId);
            players.Remove(players[rd]);
        }
    }
    public static void SelectFourRoles()
    {
        FourRoles = Enumerable.Repeat(CustomRoles.Crewmate, 4).ToList();
        if (RolesToAssign.Count <= 0) return;
        for (var i = 0; i < 4; i++) FourRoles[i] = RolesToAssign[IRandom.Instance.Next(0, RolesToAssign.Count)];
    }
    public static void AssignDraftRoles()
    {
        Timer = 0;
        foreach (var (id, role) in DraftRoleResult) Utils.GetPlayerById(id).RpcChangeRole(role);
        RolesToAssign.Clear();
        FourRoles.Clear();
        DraftRoleResult.Clear();
        ArrangedPlayers.Clear();
    }
    public static void StartRoleDraft()
    {
        ArrangePlayers();
        CurrentAssignIndex = -1;
        DraftRoleResult = new();
        NextPlayer();
    }
    public static void NextPlayer()
    {
        if (!GameStates.InGame) return;
        if (CurrentAssignIndex < 0) CurrentAssignIndex = 0;
        else CurrentAssignIndex++;
        if (CurrentAssignIndex >= ArrangedPlayers.Count)
        {
            AssignDraftRoles();
            new LateTask(() => { if (GameStates.IsMeeting) MeetingHud.Instance.RpcClose(); }, 7f, "FinishRoleDraft");
            return;
        }
        SelectFourRoles();
        Utils.SendMessage(string.Format(GetString("FourChoices"), Utils.GetRoleName(FourRoles[0]), Utils.GetRoleName(FourRoles[1]), Utils.GetRoleName(FourRoles[2]), Utils.GetRoleName(FourRoles[3])), ArrangedPlayers[CurrentAssignIndex]);
        Timer = 15;
    }
    public static void OnFixedUpdate()
    {
        if (!AmongUsClient.Instance.AmHost || !IsRoleDraftMeeting || Timer <= 0 || CurrentAssignIndex >= ArrangedPlayers.Count || Utils.GetTimeStamp() == lastFixedUpdate) return;
        lastFixedUpdate = Utils.GetTimeStamp();
        Timer--;
        if (Timer <= 0) RandomlyChooseRole(ArrangedPlayers[CurrentAssignIndex]);
        else Utils.SendMessage(string.Format(GetString("TimeNotice"), Timer), ArrangedPlayers[CurrentAssignIndex]);
    }
}