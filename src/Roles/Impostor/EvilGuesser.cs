using AmongUs.GameOptions;
using TONX.Modules;
using TONX.Roles.Core.Interfaces;
using TONX.Roles.Crewmate;
using static TONX.GuesserHelper;

namespace TONX.Roles.Impostor;
public sealed class EvilGuesser : RoleBase, IImpostor, IMeetingButton, IGuesser
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EvilGuesser),
            player => new EvilGuesser(player),
            CustomRoles.EvilGuesser,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            1000,
            SetupOptionItem,
            "eg|邪惡賭怪|邪恶的赌怪|坏赌|邪恶赌|恶赌|赌怪"
        );
    public EvilGuesser(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public static OptionItem OptionGuessNums;
    public static OptionItem OptionCanGuessImp;
    public static OptionItem OptionCanGuessAddons;
    public static OptionItem OptionCanGuessVanilla;
    public static OptionItem OptionCanGuessTaskDoneSnitch;
    enum OptionName
    {
        GuesserCanGuessTimes,
        EGCanGuessImp,
        EGCanGuessAdt,
        EGCanGuessVanilla,
        EGCanGuessTaskDoneSnitch,
    }

    public int GuessLimit { get; set; }
    public string GuessMaxMsg { get; set; } = "EGGuessMax";
    public bool CanGuessAddons => OptionCanGuessAddons.GetBool();
    public bool CanGuessVanilla => OptionCanGuessVanilla.GetBool();
    private static void SetupOptionItem()
    {
        OptionGuessNums = IntegerOptionItem.Create(RoleInfo, 10, OptionName.GuesserCanGuessTimes, new(1, 15, 1), 15, false)
            .SetValueFormat(OptionFormat.Times);
        OptionCanGuessImp = BooleanOptionItem.Create(RoleInfo, 11, OptionName.EGCanGuessImp, true, false);
        OptionCanGuessAddons = BooleanOptionItem.Create(RoleInfo, 12, OptionName.EGCanGuessAdt, false, false);
        OptionCanGuessVanilla = BooleanOptionItem.Create(RoleInfo, 13, OptionName.EGCanGuessVanilla, true, false);
        OptionCanGuessTaskDoneSnitch = BooleanOptionItem.Create(RoleInfo, 14, OptionName.EGCanGuessTaskDoneSnitch, true, false);
    }
    public override void Add()
    {
        GuessLimit = OptionGuessNums.GetInt();
    }
    public override void OverrideNameAsSeer(PlayerControl seen, ref string nameText, bool isForMeeting = false)
    {
        if (Player.IsAlive() && seen.IsAlive() && isForMeeting)
        {
            nameText = Utils.ColorString(Utils.GetRoleColor(CustomRoles.EvilGuesser), seen.PlayerId.ToString()) + " " + nameText;
        }
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
        if (target.Is(CustomRoles.Snitch) && target.AllTasksCompleted() && !OptionCanGuessTaskDoneSnitch.GetBool())
        {
            reason = GetString("EGGuessSnitchTaskDone");
            return false;
        }
        return true;
    }
    public bool OnCheckSuicide(PlayerControl guesser, PlayerControl target, CustomRoles role)
        => role.IsImpostor() && !OptionCanGuessImp.GetBool();
    public List<CustomRoleTypes> GetCustomRoleTypesList()
    {
        List<CustomRoleTypes> list = new() { CustomRoleTypes.Impostor, CustomRoleTypes.Crewmate, CustomRoleTypes.Neutral, CustomRoleTypes.Addon };
        if (!OptionCanGuessImp.GetBool()) list.Remove(CustomRoleTypes.Impostor);
        return list;
    }
}