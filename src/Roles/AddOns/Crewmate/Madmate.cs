namespace TONX.Roles.AddOns.Common;
public sealed class Madmate : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Madmate),
            player => new Madmate(player),
            CustomRoles.Madmate,
            80200,
            SetupCustomOption,
            "mm|叛徒",
            "#ff1919",
            assignMode: RoleAssignMode.Toggle,
            hasAssignData: false,
            assignTeam: (true, false, false)
        );
    public Madmate(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public static OptionItem MadmateSpawnMode;
    public static OptionItem MadmateCountMode;
    public static OptionItem SheriffCanBeMadmate;
    public static OptionItem MayorCanBeMadmate;
    public static OptionItem NGuesserCanBeMadmate;
    public static OptionItem SnitchCanBeMadmate;
    public static OptionItem JudgeCanBeMadmate;
    public static OptionItem SwapperCanBeMadmate;
    public static OptionItem MadSnitchTasks;
    enum OptionName
    {
        MadmateSpawnMode,
        MadmateCountMode,
        SheriffCanBeMadmate,
        MayorCanBeMadmate,
        NGuesserCanBeMadmate,
        SnitchCanBeMadmate,
        MadSnitchTasks,
        JudgeCanBeMadmate,
        SwapperCanBeMadmate
    }
    public static readonly string[] madmateSpawnMode =
    {
        "MadmateSpawnMode.Assign",
        "MadmateSpawnMode.FirstKill",
        "MadmateSpawnMode.SelfVote",
    };
    public static readonly string[] madmateCountMode =
    {
        "MadmateCountMode.None",
        "MadmateCountMode.Imp",
        "MadmateCountMode.Crew",
    };
    private static void SetupCustomOption()
    {
        MadmateSpawnMode = StringOptionItem.Create(RoleInfo, 10, OptionName.MadmateSpawnMode, madmateSpawnMode, 0, false);
        MadmateCountMode = StringOptionItem.Create(RoleInfo, 11, OptionName.MadmateCountMode, madmateCountMode, 0, false);
        SheriffCanBeMadmate = BooleanOptionItem.Create(RoleInfo, 12, OptionName.SheriffCanBeMadmate, false, false);
        MayorCanBeMadmate = BooleanOptionItem.Create(RoleInfo, 13, OptionName.MayorCanBeMadmate, false, false);
        NGuesserCanBeMadmate = BooleanOptionItem.Create(RoleInfo, 14, OptionName.NGuesserCanBeMadmate, false, false);
        SnitchCanBeMadmate = BooleanOptionItem.Create(RoleInfo, 15, OptionName.SnitchCanBeMadmate, false, false);
        MadSnitchTasks = IntegerOptionItem.Create(RoleInfo, 16, OptionName.MadSnitchTasks, new(1, 99, 1), 3, false)
            .SetValueFormat(OptionFormat.Pieces);
        JudgeCanBeMadmate = BooleanOptionItem.Create(RoleInfo, 17, OptionName.JudgeCanBeMadmate, false, false);
        SwapperCanBeMadmate = BooleanOptionItem.Create(RoleInfo, 18, OptionName.SwapperCanBeMadmate, false, false);
    }
}