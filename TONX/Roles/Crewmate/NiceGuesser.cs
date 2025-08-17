using AmongUs.GameOptions;
using TONX.Modules;
using TONX.Roles.Core.Interfaces;
using static TONX.GuesserHelper;

namespace TONX.Roles.Crewmate;
public sealed class NiceGuesser : RoleBase, IMeetingButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(NiceGuesser),
            player => new NiceGuesser(player),
            CustomRoles.NiceGuesser,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20000,
            SetupOptionItem,
            "ng|正義賭怪|正义的赌怪|好赌|正义赌|正赌|挣亿的赌怪|挣亿赌怪",
            "#eede26"
        );
    public NiceGuesser(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public static OptionItem OptionGuessNums;
    public static OptionItem OptionCanGuessCrew;
    public static OptionItem OptionCanGuessAddons;
    public static OptionItem OptionCanGuessVanilla;
    enum OptionName
    {
        GuesserCanGuessTimes,
        GGCanGuessCrew,
        GGCanGuessAdt,
        GGCanGuessVanilla,
    }

    public int GuessLimit;
    private static void SetupOptionItem()
    {
        OptionGuessNums = IntegerOptionItem.Create(RoleInfo, 10, OptionName.GuesserCanGuessTimes, new(1, 15, 1), 15, false)
            .SetValueFormat(OptionFormat.Times);
        OptionCanGuessCrew = BooleanOptionItem.Create(RoleInfo, 11, OptionName.GGCanGuessCrew, true, false);
        OptionCanGuessAddons = BooleanOptionItem.Create(RoleInfo, 12, OptionName.GGCanGuessAdt, false, false);
        OptionCanGuessVanilla = BooleanOptionItem.Create(RoleInfo, 13, OptionName.GGCanGuessVanilla, true, false);
    }
    public override void Add()
    {
        GuessLimit = OptionGuessNums.GetInt();
    }
    public override void OverrideNameAsSeer(PlayerControl seen, ref string nameText, bool isForMeeting = false)
    {
        if (Player.IsAlive() && seen.IsAlive() && isForMeeting)
        {
            nameText = Utils.ColorString(RoleInfo.RoleColor, seen.PlayerId.ToString()) + " " + nameText;
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
}