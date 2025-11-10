using AmongUs.GameOptions;
using Hazel;
using TONX.Roles.Core.Interfaces;
using UnityEngine;

namespace TONX.Roles.Crewmate;
public sealed class Collator : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Collator),
            player => new Collator(player),
            CustomRoles.Collator,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Crewmate,
            23100,
            SetupOptionItem,
            "cl|校对|校对员",
            "#259F94",
            true,
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Collator(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        CurrentKillCooldown = OptionSampleCooldown.GetFloat();
        CollateLimit = OptionCollateLimit.GetInt();
    }

    private static OptionItem OptionSampleCooldown;
    private static OptionItem OptionCollateLimit;
    private static OptionItem OptionMadTeamType;
    enum OptionName
    {
        CollatorSampleCooldown,
        CollatorCollateLimit,
        MadTeamType
    }
    public int CollateLimit = 0;
    public float CurrentKillCooldown = 30;
    public List<(byte, CustomRoleTypes)> Samples = new(); 
    public static readonly string[] madTeamType =
    {
        "TeamImpostor",
        "TeamCrewmate"
    };

    private static void SetupOptionItem()
    {
        OptionSampleCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.CollatorSampleCooldown, new(2.5f, 180f, 2.5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCollateLimit = IntegerOptionItem.Create(RoleInfo, 11, OptionName.CollatorCollateLimit, new(1, 15, 1), 15, false)
            .SetValueFormat(OptionFormat.Times);
        OptionMadTeamType = StringOptionItem.Create(RoleInfo, 12, OptionName.MadTeamType, madTeamType, 0, false);
    }
    public override void Add()
    {
        CurrentKillCooldown = OptionSampleCooldown.GetFloat();
        CollateLimit = OptionCollateLimit.GetInt();
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(CollateLimit);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        CollateLimit = reader.ReadInt32();
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? CurrentKillCooldown : 255f;
    public bool CanUseKillButton() => Player.IsAlive() && CollateLimit > 0 && Samples.Count < 2;
    public bool CanUseImpostorVentButton() => false;
    public bool CanUseSabotageButton() => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        if (CollateLimit < 1 || Samples.Count == 2) return false;
        var (killer, target) = info.AttemptTuple;

        var secondCollate = Samples.Count == 1;
        if (secondCollate)
        {
            CollateLimit--;
            SendRPC();
        }

        var team = target.GetCustomRole().GetCustomRoleTypes();
        if (target.GetCustomSubRoles().Contains(CustomRoles.Madmate) && OptionMadTeamType.GetValue() == 0)
            team = CustomRoleTypes.Impostor;

        killer.ResetKillCooldown();
        killer.SetKillCooldownV2();

        Samples.Add((target.PlayerId, team));

        Logger.Info($"{killer.GetNameWithRole()}: 提取样本 => {target.GetNameWithRole()}", "Collator.OnCheckMurderAsKiller");
        if (secondCollate) Logger.Info($"{killer.GetNameWithRole()}: 剩余{CollateLimit}次提取机会", "Collator.OnCheckMurderAsKiller");
        return false;
    }
    public override void NotifyOnMeetingStart(ref List<(string, byte, string)> msgToSend)
    {
        if (Samples.Count < 2) return;
        msgToSend.Add((
            GetString("CollatorCheckMatch") + GetString(Samples[0].Item2 == Samples[1].Item2 ? "CollatorMatched" : "CollatorUnmatched"),
            Player.PlayerId,
            "<color=#aaaaff>" + GetString("DefaultSystemMessageTitle") + "</color>"
        ));
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(CanUseKillButton() ? RoleInfo.RoleColor : Color.gray, $"({CollateLimit})");
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        seen ??= seer;

        if (Samples.Select(p => p.Item1).ToList().Contains(seen.PlayerId))
            return Utils.ColorString(RoleInfo.RoleColor, "●");

        return "";
    }
    public override void AfterMeetingTasks()
    {
        Samples = new();
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("CollatorButtonText");
        return true;
    }
}