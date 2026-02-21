using UnityEngine;
using TONX.Modules;

namespace TONX.GameModes;

public sealed class RoleDraft : GameModeBase
{
    public static readonly GameModeInfo ModeInfo =
        GameModeInfo.Create(
            typeof(RoleDraft),
            () => new RoleDraft(),
            CustomGameMode.RoleDraft,
            30_000_000,
            SetupCustomOption,
            "#ffffff"
        );
    public RoleDraft() : base(ModeInfo)
    { }

    private AvailableRolesData Data;
    private Dictionary<byte, CustomRoles> DevRoles;
    private List<CustomRoles> RandomRoles;
    private Dictionary<PlayerControl, CustomRoles> DraftRoleResult;
    private List<byte> ArrangedPlayers;
    private int CurrentAssignIndex;

    private const float NoticeTime = 10f;

    private static OptionItem RD_OptionNum;
    private static OptionItem RD_DraftTimeLimit;
    private static OptionItem RD_ShowSelectedRoles;

    public static void SetupCustomOption()
    {
        TextOptionItem.Create(ModeInfo, 100_001, "MenuTitle.GameMode");
        RD_OptionNum = IntegerOptionItem.Create(ModeInfo, 1, "RD_OptionNum", new(2, 5, 1), 3, false)
            .SetValueFormat(OptionFormat.Pieces)
            .SetHeader(true);
        RD_DraftTimeLimit = FloatOptionItem.Create(ModeInfo, 2, "RD_DraftTimeLimit", new(15f, 60f, 5f), 25f, false)
            .SetValueFormat(OptionFormat.Seconds);
        RD_ShowSelectedRoles = BooleanOptionItem.Create(ModeInfo, 3, "RD_ShowSelectedRoles", true, false);
    }

    private enum DraftState
    {
        Idle,     // 空闲
        Drafting, // 正在选角
        WaitNext, // 等待切换下一位
        WaitEnd,  // 等待结束
        Ending    // 结束
    }
    private DraftState _state = DraftState.Idle;
    private long _timer;
    private bool _noticed;
    private byte _playerId => CurrentAssignIndex > -1 && CurrentAssignIndex < ArrangedPlayers.Count ? ArrangedPlayers[CurrentAssignIndex] : byte.MaxValue;

    public override void Add() => _state = DraftState.Idle;
    public override void SelectCustomRoles(ref Dictionary<PlayerControl, CustomRoles> RoleResult, ref AvailableRolesData data)
    {
        Data = data;

        if (!Options.DisableHiddenRoles.GetBool())
        {
            foreach (var role in EnumHelper.GetAllValues<CustomRoles>())
            {
                if (!role.IsHidden(out var hiddenRoleInfo) || hiddenRoleInfo.TargetRole == null) continue;
                if (IRandom.Instance.Next(0, 100) < hiddenRoleInfo.Probability)
                {
                    Data.ReplaceRole(hiddenRoleInfo.TargetRole.Value, role);
                }
            }
        }

        DevRoles = new();
        foreach (var (id, dr) in Main.DevRole) if (Data.RemoveRole(dr)) DevRoles.Add(id, dr);
        Main.DevRole.Clear();
    }
    public override bool ShouldAssignAddons() => false;
    public override void AfterAssignRoles() { }

    public override void EditIntroFormat(ref IntroCutscene intro)
    {
        intro.TeamTitle.text = GetString("RoleDraft");
        intro.TeamTitle.color = Color.gray;
        intro.ImpostorText.gameObject.SetActive(false);
        intro.BackgroundBar.material.color = Color.gray;
    }
    public override void OnGameStart() => PlayerControl.LocalPlayer.NoCheckStartMeeting(null, true);
    public override void AlterMeetingTime(ref int discussionTime, ref int votingTime)
    {
        if (CustomRoleSelector.RoleAssigned) return;
        discussionTime = Main.AllAlivePlayerControls.Count() * (int)RD_DraftTimeLimit.GetFloat() + 10;
        votingTime = 0;
    }

    public override void OnStartMeeting()
    {
        if (CustomRoleSelector.RoleAssigned) return;
        _ = new LateTask(StartDraft, 8f, "StartRoleDraft");
    }
    public override void AfterMeetingTasks()
    {
        if (CustomRoleSelector.RoleAssigned) return;
        EndDraft();
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || CustomRoleSelector.RoleAssigned) return;

        switch (_state)
        {
            case DraftState.Idle:
                break;
            case DraftState.Drafting:
                if (IsPlayerNull(Utils.GetPlayerById(_playerId)) || DraftRoleResult.ContainsKey(Utils.GetPlayerById(_playerId)))
                {
                    _state = DraftState.WaitNext;
                    break;
                }
                if (Utils.GetTimeStamp() - _timer >= (long)RD_DraftTimeLimit.GetFloat())
                {
                    ChooseRole(_playerId, (RandomRoles.Count + 1).ToString() /* 随机选择 */ );
                    _state = DraftState.WaitNext;
                }
                else if (Utils.GetTimeStamp() - _timer >= (long)(RD_DraftTimeLimit.GetFloat() - NoticeTime) && !_noticed)
                {
                    Utils.SendMessage(string.Format(GetString("RoleDraft.TimeNotice"), NoticeTime), _playerId);
                    _noticed = true;
                }
                break;
            case DraftState.WaitNext:
                CurrentAssignIndex++;
                if (CurrentAssignIndex >= ArrangedPlayers.Count)
                {
                    _state = DraftState.WaitEnd;
                    break;
                }
                SelectRandomRoles(_playerId, RD_OptionNum.GetInt());
                Utils.KillFlash(Utils.GetPlayerById(_playerId));
                _noticed = false;
                _timer = Utils.GetTimeStamp();
                _state = DraftState.Drafting;
                break;
            case DraftState.WaitEnd:
                _timer = Utils.GetTimeStamp();
                _state = DraftState.Ending;
                break;
            case DraftState.Ending:
                if (Utils.GetTimeStamp() - _timer >= 5L)
                {
                    MeetingHud.Instance?.RpcForceEndMeeting();
                    _state = DraftState.Idle;
                }
                break;
        }
    }

    public override bool OnSendMessage(PlayerControl player, string msg, out MsgRecallMode recallMode)
    {
        bool isCommand = RoleDraftMsg(player, msg, out bool spam);
        recallMode = spam ? MsgRecallMode.Spam : MsgRecallMode.None;
        return isCommand;
    }
    private bool RoleDraftMsg(PlayerControl pc, string msg, out bool spam)
    {
        spam = false;
        if (!GameStates.IsMeeting || CustomRoleSelector.RoleAssigned || IsPlayerNull(pc)) return false;

        int operate = 0;
        bool isCmd = msg.StartsWith("/cmd ");
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (ChatCommand.MatchCommand(ref msg, "ch|choose|选|选择", true, isCmd)) operate = 1;
        else if (ChatCommand.MatchCommand(ref msg, "ch|choose|选|选择", false, isCmd)) operate = 2;
        else return false;

        spam = true;
        if (!AmongUsClient.Instance.AmHost) return true;

        if (pc.PlayerId != _playerId) Utils.SendMessage(GetString("RoleDraft.DraftAssignWait"), pc.PlayerId);
        else if (operate == 1) SendRandomRoles(pc.PlayerId);
        else if (operate == 2) ChooseRole(pc.PlayerId, msg.TrimStart().TrimEnd());
        return true;
    }

    private void ChooseRole(byte playerId, string input)
    {
        if (!int.TryParse(input, out int roleId) || roleId < 1 || roleId > RandomRoles.Count + 1)
        {
            Utils.SendMessage(GetString("RoleDraft.FailedChosen"), playerId);
            return;
        }

        bool isRandom = roleId == RandomRoles.Count + 1;
        CustomRoles role = isRandom ? TryGetDevRole(playerId, out CustomRoles dr) ? dr : GetRandomDraftRole(Data) : RandomRoles[roleId - 1];
        DraftRoleResult.Add(Utils.GetPlayerById(playerId), role);
        Data.RemoveRole(role);
        Utils.SendMessage(string.Format(GetString("RoleDraft.SuccessfullyChosen"), Utils.GetColoredRoleName(role, true)), playerId);
        if (!RD_ShowSelectedRoles.GetBool()) return;

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
    private CustomRoles GetRandomDraftRole(AvailableRolesData data, List<CustomRoles> existedRoles = null)
    {
        int neededimps = data.optImpNum - DraftRoleResult.Values.Count(v => v.IsImpostor());
        int needednks = data.optNeutralKillingNum - DraftRoleResult.Values.Count(v => v.IsNeutralKiller());
        int neededneuts = data.optNeutralNum - DraftRoleResult.Values.Count(v => v.IsNeutral());
        int leftplayers = ArrangedPlayers.Count - CurrentAssignIndex - DevRoles.Values.Count(v => v.IsCrewmate()) - 1;

        if (neededimps + needednks + neededneuts >= leftplayers) // 若玩家人数即将不够分配内鬼和中立职业，优先分配内鬼或中立职业
        {
            if (neededimps > 0) // 内鬼
            {
                if (data.ImpOnList.Count > 0) return data.ImpOnList[IRandom.Instance.Next(0, data.ImpOnList.Count)];
                if (data.ImpRateList.Count > 0) return data.ImpRateList[IRandom.Instance.Next(0, data.ImpRateList.Count)];
            }
            if (needednks > 0) // 中立杀手
            {
                if (data.NeutralKillingOnList.Count > 0) return data.NeutralKillingOnList[IRandom.Instance.Next(0, data.NeutralKillingOnList.Count)];
                if (data.NeutralKillingRateList.Count > 0) return data.NeutralKillingRateList[IRandom.Instance.Next(0, data.NeutralKillingRateList.Count)];
            }
            if (neededneuts > 0) // 中立
            {
                if (data.NeutralOnList.Count > 0) return data.NeutralOnList[IRandom.Instance.Next(0, data.NeutralOnList.Count)];
                if (data.NeutralRateList.Count > 0) return data.NeutralRateList[IRandom.Instance.Next(0, data.NeutralRateList.Count)];
            }
            if (existedRoles?.Where(r => !r.IsCrewmate())?.Any() ?? false)
            {
                Logger.Info("职业分配错误：内鬼或中立职业数量不足以轮抽选角", "RoleDraft");
                return CustomRoles.Crewmate;
            }
        }

        List<CustomRoles> rolesToAssign = new();

        rolesToAssign = data.roleOnList; // 优先
        if (neededimps > 0) rolesToAssign.AddRange(data.ImpOnList);
        if (needednks > 0) rolesToAssign.AddRange(data.NeutralKillingOnList);
        if (neededneuts > 0) rolesToAssign.AddRange(data.NeutralOnList);
        if (rolesToAssign.Count > 0) return rolesToAssign[IRandom.Instance.Next(0, rolesToAssign.Count)];

        rolesToAssign = data.roleRateList; // 启用
        if (neededimps > 0) rolesToAssign.AddRange(data.ImpRateList);
        if (needednks > 0) rolesToAssign.AddRange(data.NeutralKillingRateList);
        if (neededneuts > 0) rolesToAssign.AddRange(data.NeutralRateList);
        if (rolesToAssign.Count > 0) return rolesToAssign[IRandom.Instance.Next(0, rolesToAssign.Count)];

        Logger.Info("职业分配错误：总职业数量不足以轮抽选角", "RoleDraft");
        return CustomRoles.Crewmate;
    }
    private void SelectRandomRoles(byte playerId, int count = 3)
    {
        RandomRoles = new();
        if (TryGetDevRole(playerId, out CustomRoles dr)) RandomRoles.Add(dr);
        else
        {
            AvailableRolesData cachedRoleData = new(
                Data.optImpNum, Data.optNeutralKillingNum, Data.optNeutralNum,
                [.. Data.roleOnList], [.. Data.ImpOnList], [.. Data.NeutralKillingOnList], [.. Data.NeutralOnList],
                [.. Data.roleRateList], [.. Data.ImpRateList], [.. Data.NeutralKillingRateList], [.. Data.NeutralRateList]
            );
            for (int i = 0; i < count; i++)
            {
                CustomRoles chosenRole = GetRandomDraftRole(cachedRoleData, RandomRoles);
                if (chosenRole == CustomRoles.Crewmate && RandomRoles.Count > 0) break;
                RandomRoles.Add(chosenRole);
                cachedRoleData.RemoveRole(chosenRole);
            }
        }
        SendRandomRoles(playerId);
    }
    private void SendRandomRoles(byte playerId)
    {
        string text = string.Join("\n", RandomRoles.Select((role, index) => $" {index + 1} => {Utils.GetColoredRoleName(role, true)}").ToList());
        text += $"\n {RandomRoles.Count + 1} => {GetString("RoleDraft.Random")}";
        Utils.SendMessage(string.Format(GetString("RoleDraft.Choices"), text, RD_DraftTimeLimit.GetFloat()), playerId);
    }
    private void StartDraft()
    {
        DraftRoleResult = new();
        ArrangedPlayers = Main.AllAlivePlayerControls
            .OrderBy(_ => IRandom.Instance.Next(Main.AllAlivePlayerControls.Count()))
            .Select(p => p.PlayerId)
            .ToList();
        CurrentAssignIndex = -1;
        for (int i = 0; i < ArrangedPlayers.Count; i++) Utils.SendMessage(string.Format(GetString("RoleDraft.StartDraft"), i + 1), ArrangedPlayers[i]); 
        _state = DraftState.WaitNext;  
    }
    private void EndDraft()
    {
        AssignDraftRoles();

        Main.AllPlayerControls.Do(x => PlayerState.GetByPlayerId(x.PlayerId).InitTask(x));
        GameData.Instance.RecomputeTaskCounts();
        TaskState.InitialTotalTasks = GameData.Instance.TotalTasks;

        Main.CanRecord = CustomRoleSelector.RoleAssigned = true;
        foreach (var pc in Main.AllPlayerControls) Utils.RecordPlayerRoles(pc.PlayerId);
    }
    private void AssignDraftRoles()
    {
        foreach (var (player, role) in DraftRoleResult.Where(kvp => !IsPlayerNull(Utils.GetPlayerById(kvp.Key.PlayerId))).OrderByDescending(kvp => kvp.Value.GetRoleInfo()?.IsDesyncImpostor ?? false))
            player.RpcChangeRole(role, refreshSeen: false, refreshTasks: false);
        SelectRolesPatch.AssignAddons();
    }
    private static bool IsPlayerNull(PlayerControl pc) => pc?.Data == null || pc.Data.IsDead || pc.Data.Disconnected;
}