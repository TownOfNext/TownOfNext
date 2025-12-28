using AmongUs.GameOptions;

namespace TONX.Roles.AddOns.Common;
public sealed class Bewilder : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Bewilder),
            player => new Bewilder(player),
            CustomRoles.Bewilder,
            81200,
            SetupCustomOption,
            "bwd|迷幻|迷惑者",
            "#c894f5",
            assignTeam: (true, false, true),
            conflicts: Conflicts
        );
    public Bewilder(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CustomRoleManager.ApplyGameOptionsOthers.Add(ApplyGameOptionsOthers);
    }

    public static OptionItem OptionVision;

    enum OptionName
    {
        BewilderVision
    }

    private static List<CustomRoles> Conflicts = new() { CustomRoles.Lighter };
    private static void SetupCustomOption()
    {
        OptionVision = FloatOptionItem.Create(RoleInfo, 20, OptionName.BewilderVision, new(0f, 5f, 0.05f), 0.6f, false)
            .SetValueFormat(OptionFormat.Multiplier);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
        opt.SetFloat(FloatOptionNames.CrewLightMod, OptionVision.GetFloat());
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, OptionVision.GetFloat());
    }
    private static void ApplyGameOptionsOthers(PlayerControl player, IGameOptions opt)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        // 为迷惑者的凶手
        if (Main.AllPlayerControls.Any(x => x.Is(CustomRoles.Bewilder) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == player.PlayerId && !x.Is(CustomRoles.Hangman)))
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, OptionVision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, OptionVision.GetFloat());
        }
    }
}