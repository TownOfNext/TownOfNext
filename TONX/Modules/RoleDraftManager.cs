namespace TONX.Modules;

public class RoleDraftManager
{
    public static int Timer;
    private static long lastFixedUpdate;
    public static RoleDraftState RoleDraftState;
    public static List<CustomRoles> RolesToAssign;
    public static List<CustomRoles> ThreeRoles;
    public static Dictionary<byte, CustomRoles> DraftRoleResult;
    public static List<byte> ArrangedPlayers;
    public static int CurrentAssignIndex;
    public static bool IsValidRoleDraftState() => RoleDraftState == RoleDraftState.Drafting && GameStates.IsMeeting;
    private static string GetColoredRoleName(CustomRoles role) => Utils.ColorString(Utils.GetRoleColor(role).ToReadableColor(), Utils.GetRoleName(role));
    private static bool IsInvalidPlayer(byte playerId)
    {
        var data = Utils.GetPlayerById(playerId)?.Data ?? null;
        return data == null || data.IsDead || data.Disconnected;
    }
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
        bool isRandom = roleId == -1;
        CustomRoles role = isRandom ? RolesToAssign.Count > 0 ? RolesToAssign[IRandom.Instance.Next(0, RolesToAssign.Count)] : CustomRoles.Crewmate : ThreeRoles[roleId];
        DraftRoleResult.Add(playerId, role);
        RolesToAssign.Remove(role);
        Utils.SendMessage(string.Format(GetString("RoleDraft.SuccessfullyChosen"), GetColoredRoleName(role)), playerId);
        Utils.SendMessage(string.Format(
            GetString("RoleDraft.OtherSuccessfullyChosen"),
            CurrentAssignIndex + 1,
            isRandom ? GetString("RoleDraft.Random") : GetColoredRoleName(role)
        )); // 向所有玩家发送选择消息
        MoveToNextPlayer();
    }
    private static void ArrangePlayers()
    {
        ArrangedPlayers = Main.AllAlivePlayerControls
            .OrderBy(_ => IRandom.Instance.Next(Main.AllAlivePlayerControls.Count()))
            .Select(p => p.PlayerId)
            .ToList();
    }
    private static void SelectThreeRoles()
    {
        if (!IsValidRoleDraftState()) return;
        ThreeRoles = Enumerable.Repeat(CustomRoles.Crewmate, 3).ToList();
        if (RolesToAssign.Count > 0)
            for (var i = 0; i < 3; i++) ThreeRoles[i] = RolesToAssign[IRandom.Instance.Next(0, RolesToAssign.Count)];
        Utils.SendMessage(string.Format(
            GetString("RoleDraft.FourChoices"),
            GetColoredRoleName(ThreeRoles[0]),
            GetColoredRoleName(ThreeRoles[1]),
            GetColoredRoleName(ThreeRoles[2]),
            GetString("RoleDraft.Random")
        ), ArrangedPlayers[CurrentAssignIndex]);
    }
    public static void StartRoleDraft()
    {
        ArrangePlayers();
        Timer = 0;
        CurrentAssignIndex = -1;
        DraftRoleResult = new();
        RoleDraftState = RoleDraftState.Drafting;
        MoveToNextPlayer();
    }
    private static void MoveToNextPlayer()
    {
        if (!IsValidRoleDraftState()) return;
        if (CurrentAssignIndex < 0) CurrentAssignIndex = 0;
        else CurrentAssignIndex++;
        if (CurrentAssignIndex >= ArrangedPlayers.Count)
        {
            Timer = 0;
            new LateTask(() => { if (GameStates.IsMeeting) MeetingHud.Instance.RpcClose(); }, 5f, "FinishRoleDraft");
            return;
        }
        SelectThreeRoles();
        Timer = 15;
    }
    public static void AssignDraftRoles()
    {
        foreach (var (id, role) in DraftRoleResult.Where(kvp => !IsInvalidPlayer(kvp.Key) && kvp.Value != CustomRoles.Crewmate))
        {
            var pc = Utils.GetPlayerById(id);
            Logger.Info($"{pc.Data.IsDead}", "Test");
            Utils.GetPlayerById(id).RpcChangeRole(role);
        }
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
    public static void OnFixedUpdate()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!IsValidRoleDraftState() || Timer <= 0 || CurrentAssignIndex < 0 || CurrentAssignIndex >= ArrangedPlayers.Count) return;
        if (IsInvalidPlayer(ArrangedPlayers[CurrentAssignIndex]))
        {
            MoveToNextPlayer();
            return;
        }
        if (Utils.GetTimeStamp() == lastFixedUpdate) return;
        lastFixedUpdate = Utils.GetTimeStamp();
        Timer--;
        if (Timer <= 0) RandomlyChooseRole(ArrangedPlayers[CurrentAssignIndex]);
        else if (Timer == 7) Utils.SendMessage(string.Format(GetString("RoleDraft.TimeNotice"), 5f), ArrangedPlayers[CurrentAssignIndex]);
    }
}
public enum RoleDraftState
{
    None,
    ReadyToDraft,
    Drafting
}