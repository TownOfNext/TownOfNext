using TONX.Modules;
using TONX.Roles.Core.Interfaces;

namespace TONX.Roles.AddOns.Common;

public sealed class Guesser : AddonBase, IGuesser, IMeetingButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Guesser),
            player => new Guesser(player),
            CustomRoles.Guesser,
            82000,
            SetupCustomOption,
            "gs|guesser|赌怪|猜测",
            "#eede26",
            assignTeam: (true, true, true)
        );

    public Guesser(PlayerControl player) : base(RoleInfo, player) { }

    public static OptionItem OptionGuessLimit;
    public static OptionItem OptionCanGuessAddons;
    public static OptionItem OptionCanGuessVanilla;

    private static void SetupCustomOption()
    {
        OptionGuessLimit = IntegerOptionItem.Create(RoleInfo, 20, OptionName.GuesserCanGuessTimes, new(1, 15, 1), 3, false);
        OptionCanGuessAddons = BooleanOptionItem.Create(RoleInfo, 21, OptionName.GGCanGuessAdt, true, false);
        OptionCanGuessVanilla = BooleanOptionItem.Create(RoleInfo, 22, OptionName.GGCanGuessVanilla, false, false);
    }

    enum OptionName
    {
        GuesserCanGuessTimes,
        GGCanGuessAdt,
        GGCanGuessVanilla,
    }
    
    public int GuessLimit { get; set; } = 3;
    public string GuessMaxMsg => "GGGuessMax";
    public bool CanGuessAddons => OptionCanGuessAddons.GetBool();
    public bool CanGuessVanilla => OptionCanGuessVanilla.GetBool();

    public override void Add()
    {
        GuessLimit = OptionGuessLimit.GetInt();
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
    public bool OnClickButtonLocal(PlayerControl target)
    {
        GuesserHelper.ShowGuessPanel(target.PlayerId, MeetingHud.Instance);
        return false;
    }

    public override string GetProgressText(bool comms = false)
        => Utils.ColorString(Utils.GetRoleColor(CustomRoles.Guesser), $"({GuessLimit})");
}
