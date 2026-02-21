using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using TONX.Modules;
using TONX.Roles.Core.Interfaces;
using static TONX.GuesserHelper;

namespace TONX.Roles.Neutral;
public sealed class Doomsayer : RoleBase, IKiller, IMeetingButton, IGuesser
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Doomsayer),
            player => new Doomsayer(player),
            CustomRoles.Doomsayer,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            52800,
            SetupOptionItem,
            "ds|末日预言家|末日赌怪|末日",
            "#14f786",
            true
        );
    public Doomsayer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    private static OptionItem OptionGuessNums;
    private static OptionItem OptionCanGuessAddons;
    private static OptionItem OptionCanGuessVanilla;
    private static OptionItem OptionGuessNumsToWin;
    private static OptionItem OptionSuicideIfGuessWrong;
    private static OptionItem OptionForbidGuessIfWrongThisMeeting;
    private static OptionItem OptionHintCooldown;
    private static OptionItem OptionHintNums;
    enum OptionName
    {
        GuesserCanGuessTimes,
        DSCanGuessAdt,
        DSCanGuessVanilla,
        DSGuessNumsToWin,
        DSSuicideIfGuessWrong,
        DSForbidGuessIfWrongThisMeeting,
        DSHintCooldown,
        DSHintNums
    }

    public int GuessLimit { get; set; }
    public string GuessMaxMsg { get; set; } = "DSGuessMax";
    public bool CanGuessAddons => OptionCanGuessAddons.GetBool();
    public bool CanGuessVanilla => OptionCanGuessVanilla.GetBool();
    private byte Target;
    private bool HasWrongGuess;
    private int CorrectGuesses;
    private int HintLimit;
    private static void SetupOptionItem()
    {
        OptionGuessNums = IntegerOptionItem.Create(RoleInfo, 10, OptionName.GuesserCanGuessTimes, new(1, 15, 1), 15, false)
            .SetValueFormat(OptionFormat.Times);
        OptionCanGuessAddons = BooleanOptionItem.Create(RoleInfo, 11, OptionName.DSCanGuessAdt, false, false);
        OptionCanGuessVanilla = BooleanOptionItem.Create(RoleInfo, 12, OptionName.DSCanGuessVanilla, true, false);
        OptionGuessNumsToWin = IntegerOptionItem.Create(RoleInfo, 13, OptionName.DSGuessNumsToWin, new(1, 15, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
        OptionSuicideIfGuessWrong = BooleanOptionItem.Create(RoleInfo, 14, OptionName.DSSuicideIfGuessWrong, false, false);
        OptionForbidGuessIfWrongThisMeeting = BooleanOptionItem.Create(RoleInfo, 15, OptionName.DSForbidGuessIfWrongThisMeeting, true, false);
        OptionHintCooldown = FloatOptionItem.Create(RoleInfo, 16, OptionName.DSHintCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionHintNums = IntegerOptionItem.Create(RoleInfo, 17, OptionName.DSHintNums, new(0, 14, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Add()
    {
        GuessLimit = OptionGuessNums.GetInt();
        Target = byte.MaxValue;
        HasWrongGuess = false;
        CorrectGuesses = 0;
        HintLimit = OptionHintNums.GetInt();
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(HintLimit);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        HintLimit = reader.ReadInt32();
    }

    public float CalculateKillCooldown() => CanUseKillButton() ? OptionHintCooldown.GetFloat() : 255f;
    public bool CanUseKillButton() => Target == byte.MaxValue && HintLimit > 0;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);

    public override string GetProgressText(bool comms = false) => Utils.ColorString(CanUseKillButton() ? RoleInfo.RoleColor : Color.gray, $"({HintLimit})");
    public override void OverrideNameAsSeer(PlayerControl seen, ref string nameText, bool isForMeeting = false)
    {
        if (Player.IsAlive() && seen.IsAlive() && isForMeeting)
        {
            nameText = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doomsayer), seen.PlayerId.ToString()) + " " + nameText;
        }
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("DoomsayerKillButtonText");
        return true;
    }

    public override void OnStartMeeting()
    {
        HasWrongGuess = false;
    }
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = (info.AttemptKiller, info.AttemptTarget);
        if (killer == null || target == null || Target != byte.MaxValue || HintLimit < 1) return false;

        Target = target.PlayerId;
        HintLimit--;
        SendRPC();

        killer.ResetKillCooldown();
        killer.SetKillCooldownV2();

        return false;
    }
    public override void NotifyOnMeetingStart(ref List<(string, byte, string)> msgToSend)
    {
        if (Target == byte.MaxValue || (Utils.GetPlayerById(Target)?.Data.IsDead ?? true)) return;
        List<CustomRoles> Suspects = new() { Utils.GetPlayerById(Target).GetCustomRole() };

        void AddSuspectedRoles(CustomRoleTypes customRoleTypes)
        {
            var roles = CustomRolesHelper.AllRoles.Where(r => r.GetCustomRoleTypes() == customRoleTypes && r.IsEnable()).ToList();
            for (int i = 0; i < 3 - Suspects.Count(r => r.GetCustomRoleTypes() == customRoleTypes); i++)
            {
                if (roles.Count == 0) break;
                var role = roles[IRandom.Instance.Next(roles.Count)];
                Suspects.Add(role);
                roles.Remove(role);
            }
        }
        AddSuspectedRoles(CustomRoleTypes.Impostor);
        AddSuspectedRoles(CustomRoleTypes.Crewmate);
        AddSuspectedRoles(CustomRoleTypes.Neutral);

        Suspects.OrderBy(_ => IRandom.Instance.Next(Suspects.Count));

        string SuspectedRoles = string.Empty;
        if (Suspects.Count > 0)
        {
            SuspectedRoles = Utils.ColorString(Utils.GetRoleColor(Suspects[0]), Utils.GetRoleName(Suspects[0]));
            for (int j = 1; j < Suspects.Count; j++)
            {
                SuspectedRoles += ", " + Utils.ColorString(Utils.GetRoleColor(Suspects[j]), Utils.GetRoleName(Suspects[j]));
            }
        }
        else SuspectedRoles = GetString("DoomsayerHintBlank");

        msgToSend.Add((string.Format(GetString("DoomsayerSuspectRoles"), SuspectedRoles), Player.PlayerId, "<color=#aaaaff>" + GetString("DefaultSystemMessageTitle") + "</color>"));
    }
    public override void AfterMeetingTasks()
    {
        Target = byte.MaxValue;
    }

    public string ButtonName { get; private set; } = "Target";
    public bool ShouldShowButton() => Player.IsAlive();
    public bool ShouldShowButtonFor(PlayerControl target) => target.IsAlive();
    public override bool OnSendMessage(string msg, out MsgRecallMode recallMode)
    {
        bool isCommand = GuesserMsg(Player, msg, out bool spam);
        recallMode = spam ? MsgRecallMode.Spam : MsgRecallMode.None;
        return isCommand;
    }
    public bool OnClickButtonLocal(PlayerControl target)
    {
        ShowGuessPanel(target.PlayerId, MeetingHud.Instance);
        return false;
    }
    public bool OnCheckGuessing(PlayerControl guesser, PlayerControl target, CustomRoles role, ref string reason)
    {
        if (HasWrongGuess && OptionForbidGuessIfWrongThisMeeting.GetBool())
        {
            reason = GetString("DoomsayerGuessForbidden");
            return false;
        }
        return true;
    }
    public bool OnGuessing(PlayerControl guesser, PlayerControl target, CustomRoles role, bool guesserSuicide, ref string reason)
    {
        if (guesserSuicide) HasWrongGuess = true;
        else CorrectGuesses++;
        if (!OptionSuicideIfGuessWrong.GetBool() && guesser == target)
        {
            reason = GetString("DoomsayerGuessSelf");
            return false;
        }
        if (!OptionSuicideIfGuessWrong.GetBool() && HasWrongGuess)
        {
            reason = GetString("DoomsayerWrongGuess");
            return false;
        }
        return true;
    }
    public void AfterGuessing(PlayerControl guesser)
    {
        if (CorrectGuesses >= OptionGuessNumsToWin.GetInt())
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Doomsayer);
            CustomWinnerHolder.WinnerIds.Add(guesser.PlayerId);
        }
    }
}