using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using UnityEngine;

namespace TONX.Modules;

public class RoleDraftManager
{
    /// <summary>
    /// 职业 ([0]船员(启用)/[1]船员(优先)/[2]内鬼(启用)/[3]内鬼(优先)/[4]中立(启用)/[5]中立(优先))
    /// </summary>
    private List<List<CustomRoles>> RolesToAssign;
    /// <summary>
    /// 导演模式选择的职业
    /// </summary>
    private Dictionary<byte, CustomRoles> DevRoles;
    /// <summary>
    /// 职业数量 (内鬼, 中立)
    /// </summary>
    private (int, int) OptRoleNum;
    private List<CustomRoles> RandomRoles;
    private Dictionary<PlayerControl, CustomRoles> DraftRoleResult;
    private List<byte> ArrangedPlayers;
    private int CurrentAssignIndex;

    private const float DraftTimeLimit = 20f;
    private const float NoticeTime = 10f;

    public static RoleDraftManager Instance => _instance;
    private static RoleDraftManager _instance;

    private static bool IsPlayerNull(PlayerControl pc) => pc?.Data == null || pc.Data.IsDead || pc.Data.Disconnected;

    public static void Start(List<List<CustomRoles>> rolesLists, (int, int) counts) => _instance = new(rolesLists, counts);
    public void Destroy()
    {
        AmongUsClient.Instance.StopCoroutine(CoDraftRoles().WrapToIl2Cpp());
        _instance = null;
    }
    private RoleDraftManager(List<List<CustomRoles>> rolesLists, (int, int) counts)
    {
        RolesToAssign = rolesLists;
        DevRoles = new();
        foreach (var (id, dr) in Main.DevRole) foreach (var list in RolesToAssign) if (list.Remove(dr)) DevRoles.Add(id, dr);
        Main.DevRole.Clear();
        OptRoleNum = counts;
    }
    public bool OnSendMessage(MessageControl mc, out MsgRecallMode recallMode)
    {
        bool isCommand = RoleDraftMsg(mc.Player, mc.Args, out bool spam);
        recallMode = spam ? MsgRecallMode.Spam : MsgRecallMode.None;
        return isCommand;
    }
    public bool RoleDraftMsg(PlayerControl pc, string msg, out bool spam)
    {
        spam = false;
        if (!GameStates.IsMeeting || IsPlayerNull(pc)) return false;
        spam = true;
        if (pc.PlayerId != ArrangedPlayers[CurrentAssignIndex]) _ = new LateTask(() => { Utils.SendMessage(GetString("RoleDraft.DraftAssignWait"), pc.PlayerId); }, 0.2f, "RoleDraftSelectRole");
        else _ = new LateTask(() => { ChooseRole(pc.PlayerId, msg); }, 0.2f, "RoleDraftSelectRole");
        return true;
    }
    private void ChooseRole(byte playerId, string id)
    {
        int roleId = id switch
        {
            "1" when RandomRoles.Count > 0 => 0,
            "2" when RandomRoles.Count > 1 => 1,
            "3" when RandomRoles.Count > 2 => 2,
            "4" => -1, // 随机选择
            _ => -2 // 无效选择
        };

        if (roleId == -2)
        {
            Utils.SendMessage(GetString("RoleDraft.FailedChosen"), playerId);
            return;
        }

        bool isRandom = roleId == -1;
        CustomRoles role = isRandom ? TryGetDevRole(playerId, out CustomRoles dr) ? dr : GetRandomDraftRole() : RandomRoles[roleId];
        DraftRoleResult.Add(Utils.GetPlayerById(playerId), role);
        RolesToAssign.ForEach(list => list.Remove(role));
        Utils.SendMessage(string.Format(GetString("RoleDraft.SuccessfullyChosen"), Utils.GetColoredRoleName(role, true)), playerId);
        if (!Options.ShowSelectedRoles.GetBool()) return;

        Utils.SendMessage(string.Format(
            GetString("RoleDraft.OtherSuccessfullyChosen"),
            CurrentAssignIndex + 1,
            isRandom ? GetString("RoleDraft.Random") : Utils.GetColoredRoleName(role, true)
        )); // 向所有玩家发送选择消息
    }
    private bool TryGetDevRole(byte playerId, out CustomRoles devRole)
    {
        devRole = CustomRoles.Crewmate;
        if (!DevRoles.TryGetValue(playerId, out var dr)) return false;
        devRole = dr;
        return true;
    }
    private CustomRoles GetRandomDraftRole(List<CustomRoles> existedRoles = null)
    {
        int neededimps = OptRoleNum.Item1 - DraftRoleResult.Values.Where(v => v.IsImpostor()).Count();
        int neededneuts = OptRoleNum.Item2 - DraftRoleResult.Values.Where(v => v.IsNeutral()).Count();
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

        rolesToAssign = RolesToAssign[1]; // 优先
        if (neededimps > 0) rolesToAssign.AddRange(RolesToAssign[3]);
        if (neededneuts > 0) rolesToAssign.AddRange(RolesToAssign[5]);
        if (rolesToAssign.Count > 0) return rolesToAssign[IRandom.Instance.Next(0, rolesToAssign.Count)];

        rolesToAssign = RolesToAssign[0]; // 启用
        if (neededimps > 0) rolesToAssign.AddRange(RolesToAssign[2]);
        if (neededneuts > 0) rolesToAssign.AddRange(RolesToAssign[4]);
        if (rolesToAssign.Count > 0) return rolesToAssign[IRandom.Instance.Next(0, rolesToAssign.Count)];

        Logger.Info("职业分配错误：总职业数量不足以轮抽选角", "RoleDraftManager");
        return CustomRoles.Crewmate;
    }
    private void SelectRandomRoles(byte playerId)
    {
        RandomRoles = new();
        List<(int, CustomRoles)> cachedRoleData = new();
        if (TryGetDevRole(playerId, out CustomRoles dr)) RandomRoles.Add(dr);
        else
        {
            for (int i = 0; i < 3; i++)
            {
                CustomRoles chosenRole = GetRandomDraftRole(RandomRoles);
                if (chosenRole == CustomRoles.Crewmate && RandomRoles.Count > 0) break;
                RandomRoles.Add(chosenRole);
                for (int j = 0; j < RolesToAssign.Count; j++)
                {
                    if (RolesToAssign[j].Remove(chosenRole))
                    {
                        cachedRoleData.Add((j, chosenRole));
                        break;
                    }
                }
            }
            foreach (var (id, role) in cachedRoleData) RolesToAssign[id].Add(role);
        }

        string text = string.Join("\n", RandomRoles.Select((role, index) => $" {index + 1} => {Utils.GetColoredRoleName(role, true)}").ToList());
        text += $"\n 4 => {GetString("RoleDraft.Random")}";
        Utils.SendMessage(string.Format(GetString("RoleDraft.Choices"), text, DraftTimeLimit), playerId);
    }
    public void StartRoleDraft()
    {
        DraftRoleResult = new();
        AmongUsClient.Instance.StartCoroutine(CoDraftRoles().WrapToIl2Cpp());
    }
    private IEnumerator CoDraftRoles()
    {
        ArrangedPlayers = Main.AllAlivePlayerControls
            .OrderBy(_ => IRandom.Instance.Next(Main.AllAlivePlayerControls.Count()))
            .Select(p => p.PlayerId)
            .ToList();
        CurrentAssignIndex = -1;
        for (int i = 0; i < ArrangedPlayers.Count; i++) Utils.SendMessage(string.Format(GetString("RoleDraft.StartDraft"), i + 1), ArrangedPlayers[i]);

        foreach (var playerId in ArrangedPlayers)
        {
            CurrentAssignIndex++;
            SelectRandomRoles(playerId);
            Utils.KillFlash(Utils.GetPlayerById(playerId));
            yield return CoDraftPlayer(playerId);
        }

        yield return new WaitForSeconds(5.0f);
        if (GameStates.IsMeeting) MeetingHud.Instance?.RpcForceEndMeeting();
    }
    private IEnumerator CoDraftPlayer(byte playerId)
    {
        bool noticed = false;
        for (float timer = 0f; timer < DraftTimeLimit + 0.5f /* 0.5f为消息延迟 */ ; timer += Time.deltaTime)
        {
            if (IsPlayerNull(Utils.GetPlayerById(playerId)) || DraftRoleResult.ContainsKey(Utils.GetPlayerById(playerId))) yield break;
            if (timer >= NoticeTime && !noticed)
            {
                Utils.SendMessage(string.Format(GetString("RoleDraft.TimeNotice"), NoticeTime), playerId);
                noticed = true;
            }
            yield return null;
        }
        if (IsPlayerNull(Utils.GetPlayerById(playerId))) yield break;
        ChooseRole(playerId, "4" /* 随机选择 */ ); 
    }
    public void AssignDraftRoles()
    {
        if (!GameStates.IsInGame) return;

        foreach (var (player, role) in DraftRoleResult.Where(kvp => !IsPlayerNull(Utils.GetPlayerById(kvp.Key.PlayerId))).OrderByDescending(kvp => kvp.Value.GetRoleInfo()?.IsDesyncImpostor ?? false))
            player.RpcChangeRole(role, refreshSeen: false, refreshTasks: false);
        SelectRolesPatch.AssignAddons();

        Main.AllPlayerControls.Do(x => PlayerState.GetByPlayerId(x.PlayerId).InitTask(x));
        GameData.Instance.RecomputeTaskCounts();
        TaskState.InitialTotalTasks = GameData.Instance.TotalTasks;

        Main.CanRecord = true;
        foreach (var pc in Main.AllPlayerControls) Utils.RecordPlayerRoles(pc.PlayerId);
    }
}