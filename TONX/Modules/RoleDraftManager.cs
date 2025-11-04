using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using UnityEngine;

namespace TONX.Modules;

public class RoleDraftManager
{
    /// <summary>
    /// 职业 ([0]船员(启用)/[1]船员(优先)/[2]内鬼(启用)/[3]内鬼(优先)/[4]中立(启用)/[5]中立(优先))
    /// </summary>
    private static List<List<CustomRoles>> RolesToAssign;
    /// <summary>
    /// 导演模式选择的职业
    /// </summary>
    private static Dictionary<byte, CustomRoles> DevRoles;
    /// <summary>
    /// 职业数量 ([0]内鬼/[1]中立)
    /// </summary>
    private static List<int> OptRoleNum;
    private static List<CustomRoles> RandomRoles;
    public static Dictionary<PlayerControl, CustomRoles> DraftRoleResult;
    private static List<byte> ArrangedPlayers;
    private static int CurrentAssignIndex;
    private const float DraftTimeLimit = 20f;
    private const float NoticeTime = 10f;
    public static RoleDraftState RoleDraftState;
    public static bool IsRoleDrafting() => Options.EnableRoleDraftMode.GetBool() && RoleDraftState == RoleDraftState.Drafting && GameStates.IsMeeting;
    private static string GetColoredRoleName(CustomRoles role) => Utils.ColorString(Utils.GetRoleColor(role).ToReadableColor(), Utils.GetRoleName(role));
    private static bool IsInvalidPlayer(byte playerId)
    {
        var data = Utils.GetPlayerById(playerId)?.Data ?? null;
        return data == null || data.IsDead || data.Disconnected;
    }
    public static void Init(List<List<CustomRoles>> rolesLists, int ic, int nc)
    {
        RolesToAssign = rolesLists;
        DevRoles = new();
        foreach (var (id, dr) in Main.DevRole) foreach (var list in RolesToAssign) if (list.Remove(dr)) DevRoles.Add(id, dr);
        Main.DevRole.Clear();
        OptRoleNum = new List<int> { ic, nc };
    }
    public static void RoleDraftMsg(MessageControl mc, out bool spam)
    {
        spam = IsRoleDrafting();
        if (!spam) return;
        _ = new LateTask(() => { OnPlayerChooseRole(mc.Player.PlayerId, mc.Args); }, 0.2f, "RoleDraftSelectRole");
    }
    public static void OnPlayerChooseRole(byte playerId, string id)
    {
        if (!IsRoleDrafting()) return;
        if (playerId != ArrangedPlayers[CurrentAssignIndex])
        {
            Utils.SendMessage(GetString("RoleDraft.DraftAssignWait"), playerId);
            return;
        }
        int roleId = id switch
        {
            "1" when RandomRoles.Count > 0 => 0,
            "2" when RandomRoles.Count > 1 => 1,
            "3" when RandomRoles.Count > 2 => 2,
            "4" => -1, // 随机选择
            _ => -2 // 无效选择
        };
        AfterChosenRole(playerId, roleId);
    }
    public static void RandomlyChooseRole(byte playerId)
    {
        if (!IsRoleDrafting()) return;
        AfterChosenRole(playerId, -1);
    }
    private static void AfterChosenRole(byte playerId, int roleId)
    {
        if (!IsRoleDrafting()) return;
        if (roleId == -2)
        {
            Utils.SendMessage(GetString("RoleDraft.FailedChosen"), playerId);
            return;
        }
        bool isRandom = roleId == -1;
        CustomRoles role = isRandom ? TryGetDraftDevRole(playerId, out CustomRoles dr) ? dr : GetRandomDraftRole() : RandomRoles[roleId];
        DraftRoleResult.Add(Utils.GetPlayerById(playerId), role);
        RolesToAssign.ForEach(list => list.Remove(role));
        Utils.SendMessage(string.Format(GetString("RoleDraft.SuccessfullyChosen"), GetColoredRoleName(role)), playerId);
        if (!Options.ShowSelectedRoles.GetBool()) return;
        Utils.SendMessage(string.Format(
            GetString("RoleDraft.OtherSuccessfullyChosen"),
            CurrentAssignIndex + 1,
            isRandom ? GetString("RoleDraft.Random") : GetColoredRoleName(role)
        )); // 向所有玩家发送选择消息
    }
    private static int TryGetListIdOfRole(CustomRoles role)
    {
        int TwoTimesRoleType = (int)role.GetCustomRoleTypes() * 2;
        if (TwoTimesRoleType + 1 >= RolesToAssign.Count) return -1;
        if (RolesToAssign[TwoTimesRoleType].Contains(role)) return TwoTimesRoleType;
        if (RolesToAssign[TwoTimesRoleType + 1].Contains(role)) return TwoTimesRoleType + 1;
        return -1;
    }
    private static bool TryGetDraftDevRole(byte playerId, out CustomRoles devRole)
    {
        devRole = CustomRoles.Crewmate;
        if (!DevRoles.TryGetValue(playerId, out var dr)) return false;
        devRole = dr;
        return true;
    }
    private static CustomRoles GetRandomDraftRole(List<CustomRoles> existedRoles = null)
    {
        int neededimps = OptRoleNum[0] - DraftRoleResult.Values.Where(v => v.IsImpostor()).Count();
        int neededneuts = OptRoleNum[1] - DraftRoleResult.Values.Where(v => v.IsNeutral()).Count();
        int leftplayers = ArrangedPlayers.Count - CurrentAssignIndex - DevRoles.Values.Where(v => v.IsCrewmate()).Count() - 1;

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
            if (existedRoles?.Where(r => !r.IsCrewmate())?.Any() ?? false)
            {
                Logger.Info("职业分配错误：内鬼或中立职业数量不足以轮抽选角", "RoleDraftManager");
                return CustomRoles.Crewmate;
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

        Logger.Info("职业分配错误：总职业数量不足以轮抽选角", "RoleDraftManager");
        return CustomRoles.Crewmate;
    }
    private static void SelectRandomRoles(byte playerId)
    {
        if (!IsRoleDrafting()) return;

        RandomRoles = new();
        List<(int, CustomRoles)> CachedRoleData = new();
        if (TryGetDraftDevRole(playerId, out CustomRoles dr))
        {
            RandomRoles.Add(dr);
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                CustomRoles chosenRole = GetRandomDraftRole(RandomRoles);
                if (chosenRole == CustomRoles.Crewmate && RandomRoles.Count > 0) break;
                RandomRoles.Add(chosenRole);
                if (chosenRole == CustomRoles.Crewmate) break;
                int listId = TryGetListIdOfRole(chosenRole);
                if (listId != -1) CachedRoleData.Add((listId, chosenRole));
                RolesToAssign.ForEach(list => list.Remove(chosenRole));
            }
            foreach (var (id, role) in CachedRoleData) RolesToAssign[id].Add(role);
        }

        string text = string.Join("\n", RandomRoles.Select((role, index) => $" {index + 1} => {GetColoredRoleName(role)}").ToList());
        text += $"\n 4 => {GetString("RoleDraft.Random")}";
        Utils.SendMessage(string.Format(GetString("RoleDraft.Choices"), text, DraftTimeLimit), playerId);
    }
    public static void StartRoleDraft()
    {
        DraftRoleResult = new();
        RoleDraftState = RoleDraftState.Drafting;
        AmongUsClient.Instance.StartCoroutine(CoDraftRoles().WrapToIl2Cpp());
    }
    private static IEnumerator CoDraftRoles()
    {
        ArrangedPlayers = Main.AllAlivePlayerControls
            .OrderBy(_ => IRandom.Instance.Next(Main.AllAlivePlayerControls.Count()))
            .Select(p => p.PlayerId)
            .ToList();
        CurrentAssignIndex = -1;
        for (int i = 0; i < ArrangedPlayers.Count; i++)
            Utils.SendMessage(string.Format(GetString("RoleDraft.StartDraft"), i + 1), ArrangedPlayers[i]);

        foreach (var playerId in ArrangedPlayers)
        {
            if (!IsRoleDrafting()) yield break;
            CurrentAssignIndex++;
            SelectRandomRoles(playerId);
            Utils.KillFlash(Utils.GetPlayerById(playerId));
            yield return CoDraftPlayer(playerId);
        }

        yield return new WaitForSeconds(5.0f);
        if (GameStates.IsMeeting) MeetingHud.Instance?.RpcForceEndMeeting();
    }
    private static IEnumerator CoDraftPlayer(byte playerId)
    {
        float timer = 0f;
        bool noticed = false;
        while (timer < DraftTimeLimit + 0.5f) // 0.5f为消息延迟
        {
            if (!IsRoleDrafting() || IsInvalidPlayer(playerId) || DraftRoleResult.ContainsKey(Utils.GetPlayerById(playerId))) yield break;
            timer += Time.deltaTime;
            if (timer >= NoticeTime && !noticed)
            {
                Utils.SendMessage(string.Format(GetString("RoleDraft.TimeNotice"), NoticeTime), playerId);
                noticed = true;
            }
            yield return null;
        }
        RandomlyChooseRole(playerId);
    }
    public static void AssignDraftRoles()
    {
        foreach (var (player, role) in DraftRoleResult.Where(kvp => !IsInvalidPlayer(kvp.Key.PlayerId) && (kvp.Value.GetRoleInfo()?.IsDesyncImpostor ?? false)))
            player.RpcChangeRole(role, refreshSeen: false, refreshTasks: false);
        foreach (var (player, role) in DraftRoleResult.Where(kvp => !IsInvalidPlayer(kvp.Key.PlayerId) && !(kvp.Value.GetRoleInfo()?.IsDesyncImpostor ?? false)))
            player.RpcChangeRole(role, refreshSeen: false, refreshTasks: false);
        SelectRolesPatch.AssignAddons();
        Main.AllPlayerControls.Do(x => PlayerState.GetByPlayerId(x.PlayerId).InitTask(x));
        GameData.Instance.RecomputeTaskCounts();
        TaskState.InitialTotalTasks = GameData.Instance.TotalTasks;
        Utils.CanRecord = true;
        foreach (var pc in Main.AllPlayerControls) Utils.RecordPlayerRoles(pc.PlayerId);
        RolesToAssign.Clear();
        OptRoleNum.Clear();
        RandomRoles.Clear();
        DraftRoleResult.Clear();
        ArrangedPlayers.Clear();
    }
}
public enum RoleDraftState
{
    None,
    ReadyToDraft,
    Drafting
}