namespace TONX.Modules;

public class RoleDraftManager
{
    // 职业
    // [0]船员(启用)/[1]船员(优先)
    // [2]内鬼(启用)/[3]内鬼(优先)
    // [4]中立(启用)/[5]中立(优先)
    private static List<List<CustomRoles>> RolesToAssign; 
    // 职业数量 ([0]内鬼/[1]中立)
    private static List<int> OptRoleNum;
    private static List<CustomRoles> ThreeRoles;
    public static Dictionary<byte, CustomRoles> DraftRoleResult;
    private static List<byte> ArrangedPlayers;
    private static int CurrentAssignIndex;
    private static int Timer;
    private static long lastFixedUpdate;
    public static RoleDraftState RoleDraftState;
    public static bool IsValidRoleDraftState() => RoleDraftState == RoleDraftState.Drafting && GameStates.IsMeeting;
    private static string GetColoredRoleName(CustomRoles role) => Utils.ColorString(Utils.GetRoleColor(role).ToReadableColor(), Utils.GetRoleName(role));
    private static bool IsInvalidPlayer(byte playerId)
    {
        var data = Utils.GetPlayerById(playerId)?.Data ?? null;
        return data == null || data.IsDead || data.Disconnected;
    }
    public static void Init(List<List<CustomRoles>> rolesLists, int ic, int nc)
    {
        RolesToAssign = rolesLists;
        OptRoleNum = new List<int> { ic, nc };
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
        bool isRandom = roleId == -1;
        CustomRoles role = isRandom ? RandomDraftRole() : ThreeRoles[roleId];
            DraftRoleResult.Add(playerId, role);
            RolesToAssign.ForEach(list => list.Remove(role));
            Utils.SendMessage(string.Format(GetString("RoleDraft.SuccessfullyChosen"), GetColoredRoleName(role)), playerId);
        Utils.SendMessage(string.Format(
            GetString("RoleDraft.OtherSuccessfullyChosen"),
            CurrentAssignIndex + 1,
            isRandom ? GetString("RoleDraft.Random") : GetColoredRoleName(role)
        )); // 向所有玩家发送选择消息
        MoveToNextPlayer();
    }
    private static int TryGetListIdOfRole(CustomRoles role)
    {
        int TwoTimesRoleType = (int)role.GetCustomRoleTypes() * 2;
        if (TwoTimesRoleType > RolesToAssign.Count) return -1;
        if (RolesToAssign[TwoTimesRoleType].Contains(role)) return TwoTimesRoleType;
        if (RolesToAssign[TwoTimesRoleType + 1].Contains(role)) return TwoTimesRoleType + 1;
        return -1;
    }
    private static CustomRoles RandomDraftRole()
    {
        int neededimps = OptRoleNum[0] - DraftRoleResult.Values.Where(v => v.IsImpostor()).Count();
        int neededneuts = OptRoleNum[1] - DraftRoleResult.Values.Where(v => v.IsNeutral()).Count();
        int leftplayers = ArrangedPlayers.Count - CurrentAssignIndex - 1;

        if (neededimps + neededneuts >= leftplayers) // 若玩家人数即将不够分配内鬼和中立职业，优先分配内鬼或中立职业
        {
            if (neededimps > 0)
            {
                if (RolesToAssign[3].Count > 0) return RolesToAssign[3][IRandom.Instance.Next(0, RolesToAssign[3].Count)];
                if (RolesToAssign[2].Count > 0) return RolesToAssign[2][IRandom.Instance.Next(0, RolesToAssign[2].Count)];
            }
            if (neededneuts > 0)
            {
                if (RolesToAssign[5].Count > 0) return RolesToAssign[5][IRandom.Instance.Next(0, RolesToAssign[5].Count)];
                if (RolesToAssign[4].Count > 0) return RolesToAssign[4][IRandom.Instance.Next(0, RolesToAssign[4].Count)];
            }
        }

        List<CustomRoles> rolesToAssign = new();

        rolesToAssign = RolesToAssign[1];
        if (neededimps > 0) rolesToAssign.AddRange(RolesToAssign[3]);
        if (neededneuts > 0) rolesToAssign.AddRange(RolesToAssign[5]);
        if (rolesToAssign.Count > 0) return rolesToAssign[IRandom.Instance.Next(0, rolesToAssign.Count)];

        rolesToAssign = RolesToAssign[0];
        if (neededimps > 0) rolesToAssign.AddRange(RolesToAssign[2]);
        if (neededneuts > 0) rolesToAssign.AddRange(RolesToAssign[4]);
        if (rolesToAssign.Count > 0) return rolesToAssign[IRandom.Instance.Next(0, rolesToAssign.Count)];

        Logger.Info("职业分配错误：职业数量不足以轮抽选角", "RoleDraftManager");
        return CustomRoles.Crewmate;
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
        Dictionary<CustomRoles, (int, int)> CachedRoleData = new();
        for (int i = 0; i < 3; i++)
        {
            CustomRoles chosenRole = RandomDraftRole();
            ThreeRoles[i] = chosenRole;
            int listId = TryGetListIdOfRole(chosenRole);
            if (listId != -1) CachedRoleData.TryAdd(chosenRole, (listId, RolesToAssign[listId].IndexOf(chosenRole)));
            RolesToAssign.ForEach(list => list.Remove(chosenRole));
        }
        foreach (var (role, (list, index)) in CachedRoleData) if (index != -1) RolesToAssign[list].Insert(index, role);

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
        for (int i = 0; i < ArrangedPlayers.Count; i++)
            Utils.SendMessage(string.Format(GetString("RoleDraft.StartDraft"), i + 1), ArrangedPlayers[i]);
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
        Timer = 20;
        var id = ArrangedPlayers[CurrentAssignIndex];
        if (!IsInvalidPlayer(id)) Utils.KillFlash(Utils.GetPlayerById(id));
    }
    public static void AssignDraftRoles()
    {
        foreach (var (id, role) in DraftRoleResult.Where(kvp => !IsInvalidPlayer(kvp.Key) && kvp.Value != CustomRoles.Crewmate))
            Utils.GetPlayerById(id).RpcChangeRole(role);
        SelectRolesPatch.AssignAddons();
        Main.AllPlayerControls.Do(x => PlayerState.GetByPlayerId(x.PlayerId).InitTask(x));
        GameData.Instance.RecomputeTaskCounts();
        TaskState.InitialTotalTasks = GameData.Instance.TotalTasks;
        Utils.CanRecord = true;
        foreach (var pc in Main.AllPlayerControls) Utils.RecordPlayerRoles(pc.PlayerId);

        RolesToAssign.Clear();
        OptRoleNum.Clear();
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
        else if (Timer == 10) Utils.SendMessage(string.Format(GetString("RoleDraft.TimeNotice"), Timer), ArrangedPlayers[CurrentAssignIndex]);
    }
}
public enum RoleDraftState
{
    None,
    ReadyToDraft,
    Drafting
}