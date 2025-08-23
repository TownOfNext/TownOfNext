namespace TONX.Modules;

public class RoleDraftManager
{
    public static int Timer;
    private static long lastFixedUpdate;
    public static bool IsRoleDraftMeeting;
    public static List<CustomRoles> RolesToAssign;
    public static List<CustomRoles> ThreeRoles;
    public static Dictionary<byte, CustomRoles> DraftRoleResult;
    public static List<byte> ArrangedPlayers;
    public static int CurrentAssignIndex;
    public static void OnPlayerChooseRole(byte playerId, string id)
    {
        if (!IsValidRoleDraftState()) return;
        if (playerId != ArrangedPlayers[CurrentAssignIndex])
        {
            Utils.SendMessage(GetString("RoleDraft.DraftAssignWait"), playerId);
            return;
        }
        int roleId = id switch
        {
            "1" => 0,
            "2" => 1,
            "3" => 2,
            "4" => -1, // 随机选择
            _ => -2 // 无效选择
        };
        AfterChooseRole(playerId, roleId);
    }
    public static void RandomlyChooseRole(byte playerId)
    {
        if (!IsValidRoleDraftState()) return;
        AfterChooseRole(playerId, -1);
    }
    private static void AfterChooseRole(byte playerId, int roleId)
    {
        if (!IsValidRoleDraftState()) return;
        if (roleId == -2)
        {
            Utils.SendMessage(GetString("RoleDraft.FailedChosen"), playerId);
            return;
        }
        if (RolesToAssign.Count <= 0) Logger.Info("职业分配错误：存在未被分配职业的玩家", "RoleDraftManager");
        var role = roleId == -1 ? RolesToAssign.Count > 0 ? RolesToAssign[IRandom.Instance.Next(0, RolesToAssign.Count)] : CustomRoles.Crewmate : ThreeRoles[roleId];
        DraftRoleResult.Add(playerId, role);
        RolesToAssign.Remove(role);
        Utils.SendMessage(string.Format(GetString("RoleDraft.SuccessfullyChosen"), Utils.GetRoleName(role)), playerId);
        Utils.SendMessage(string.Format(
            GetString("RoleDraft.OtherSuccessfullyChosen"),
            CurrentAssignIndex + 1,
            roleId == -1 ? GetString("RoleDraft.Random") : Utils.GetRoleName(role)
        ));
        MoveToNextPlayer();
    }
    private static void ArrangePlayers()
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
    private static void SelectThreeRoles()
    {
        if (!IsValidRoleDraftState()) return;
        ThreeRoles = Enumerable.Repeat(CustomRoles.Crewmate, 3).ToList();
        if (RolesToAssign.Count > 0)
            for (var i = 0; i < 3; i++) ThreeRoles[i] = RolesToAssign[IRandom.Instance.Next(0, RolesToAssign.Count)];
        Utils.SendMessage(string.Format(
            GetString("RoleDraft.FourChoices"),
                Utils.GetRoleName(ThreeRoles[0]),
                Utils.GetRoleName(ThreeRoles[1]),
                Utils.GetRoleName(ThreeRoles[2]),
                GetString("RoleDraft.Random")
            ), ArrangedPlayers[CurrentAssignIndex]);
    }
    public static void AssignDraftRoles()
    {
        Timer = 0;
        foreach (var (id, role) in DraftRoleResult.Where(kvp => kvp.Value != CustomRoles.Crewmate)) Utils.GetPlayerById(id).RpcChangeRole(role);
        SelectRolesPatch.AssignAddons();
        Main.AllPlayerControls.Do(x => PlayerState.GetByPlayerId(x.PlayerId).InitTask(x));
        GameData.Instance.RecomputeTaskCounts();
        TaskState.InitialTotalTasks = GameData.Instance.TotalTasks;
        Utils.CanRecord = true;
        foreach (var pc in Main.AllPlayerControls) Utils.RecordPlayerRoles(pc.PlayerId);
        RolesToAssign.Clear();
        ThreeRoles.Clear();
        DraftRoleResult.Clear();
        ArrangedPlayers.Clear();
    }
    public static void StartRoleDraft()
    {
        ArrangePlayers();
        CurrentAssignIndex = -1;
        DraftRoleResult = new();
        MoveToNextPlayer();
    }
    private static void MoveToNextPlayer()
    {
        if (!IsValidRoleDraftState()) return;
        if (CurrentAssignIndex < 0) CurrentAssignIndex = 0;
        else CurrentAssignIndex++;
        if (CurrentAssignIndex >= ArrangedPlayers.Count)
        {
            new LateTask(() => { if (GameStates.IsMeeting) MeetingHud.Instance.RpcClose(); }, 5f, "FinishRoleDraft");
            return;
        }
        SelectThreeRoles();
        Timer = 15;
    }
    public static void OnFixedUpdate()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!IsValidRoleDraftState() || Timer <= 0 || CurrentAssignIndex >= ArrangedPlayers.Count || Utils.GetTimeStamp() == lastFixedUpdate) return;
        lastFixedUpdate = Utils.GetTimeStamp();
        Timer--;
        if (Timer <= 0) RandomlyChooseRole(ArrangedPlayers[CurrentAssignIndex]);
        else if (Timer == 7) Utils.SendMessage(string.Format(GetString("RoleDraft.TimeNotice"), 5f), ArrangedPlayers[CurrentAssignIndex]);
    }
    public static bool IsValidRoleDraftState() => GameStates.InGame && IsRoleDraftMeeting;
}