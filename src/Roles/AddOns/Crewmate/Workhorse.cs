using static TONX.Options;

namespace TONX.Roles.AddOns.Crewmate;
public sealed class Workhorse : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Workhorse),
            player => new Workhorse(player),
            CustomRoles.Workhorse,
            80400,
            SetupCustomOption,
            "wh|加班",
            "#00ffff",
            assignMode: RoleAssignMode.Toggle,
            hasAssignData: false,
            assignTeam: (true, false, false)
        );
    public Workhorse(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        AssignOnlyToCrewmate = OptionAssignOnlyToCrewmate.GetBool();
        SnitchCanBeWorkhorse = OptionSnitchCanBeWorkhorse.GetBool();
        NumLongTasks = OptionNumLongTasks.GetInt();
        NumShortTasks = OptionNumShortTasks.GetInt();
    }

    private static OptionItem OptionAssignOnlyToCrewmate;
    private static OptionItem OptionSnitchCanBeWorkhorse;
    private static OptionItem OptionNumLongTasks;
    private static OptionItem OptionNumShortTasks;
    enum OptionName
    {
        AssignOnlyToCrewmate,
        SnitchCanBeWorkhorse,
        WorkhorseNumLongTasks,
        WorkhorseNumShortTasks
    }
    public static bool AssignOnlyToCrewmate;
    public static bool SnitchCanBeWorkhorse;
    public static int NumLongTasks;
    public static int NumShortTasks;

    private static void SetupCustomOption()
    {
        OptionAssignOnlyToCrewmate = BooleanOptionItem.Create(RoleInfo, 10, OptionName.AssignOnlyToCrewmate, true, false);
        OptionSnitchCanBeWorkhorse = BooleanOptionItem.Create(RoleInfo, 13, OptionName.SnitchCanBeWorkhorse, false, false);
        OptionNumLongTasks = IntegerOptionItem.Create(RoleInfo, 11, OptionName.WorkhorseNumLongTasks, new(0, 5, 1), 1, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionNumShortTasks = IntegerOptionItem.Create(RoleInfo, 12, OptionName.WorkhorseNumShortTasks, new(0, 5, 1), 1, false)
            .SetValueFormat(OptionFormat.Pieces);
    }

    public static (bool, int, int) TaskData => (false, NumLongTasks, NumShortTasks);
    private static bool IsAssignTarget(PlayerControl pc)
    {
        if (!pc.IsAlive() || pc.Is(CustomRoles.Workhorse)) return false;
        var taskState = pc.GetPlayerTaskState();
        if (taskState.CompletedTasksCount < taskState.AllTasksCount) return false;
        if (!Utils.HasTasks(pc.Data)) return false;
        if (pc.Is(CustomRoles.Snitch) && !SnitchCanBeWorkhorse) return false;
        if (AssignOnlyToCrewmate)
            return pc.Is(CustomRoleTypes.Crewmate);
        return !OverrideTasksData.AllData.ContainsKey(pc.GetCustomRole()); //タスク上書きオプションが無い
    }
    public static bool OnCompleteTask(PlayerControl pc)
    {
        if (Main.AllPlayerControls.Count(p => p.Is(CustomRoles.Workhorse)) >= CustomRoles.Workhorse.GetCount()) return true;
        if (!IsAssignTarget(pc)) return true;

        pc.RpcSetCustomRole(CustomRoles.Workhorse);
        var taskState = pc.GetPlayerTaskState();
        taskState.AllTasksCount += NumLongTasks + NumShortTasks;

        if (AmongUsClient.Instance.AmHost)
        {
            pc.Data.RpcSetTasks(Array.Empty<byte>()); //タスクを再配布
            pc.SyncSettings();
            Utils.NotifyRoles();
        }

        return false;
    }
}