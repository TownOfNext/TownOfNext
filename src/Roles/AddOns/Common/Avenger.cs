namespace TONX.Roles.AddOns.Common;
public sealed class Avenger : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Avenger),
            player => new Avenger(player),
            CustomRoles.Avenger,
            81400,
            SetupCustomOption,
            "av|復仇者|复仇",
            "#ffab1b"
        );
    public Avenger(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public static OptionItem OptionRevengeMode;
    public static OptionItem OptionRevengeNums;
    public static OptionItem OptionRevengeOnKilled;
    public static OptionItem OptionRevengeOnSuicide;
    public static readonly string[] revengeModes =
    {
        "AvengerMode.Killer",
        "AvengerMode.Random",
        "AvengerMode.Enimies",
        "AvengerMode.Teammates",
    };
    enum OptionName
    {
        AvengerRevengeMode,
        AvengerRevengeNums,
        AvengerRevengeOnKilled,
        AvengerRevengeOnSuicide
    }

    private static void SetupCustomOption()
    {
        OptionRevengeMode = StringOptionItem.Create(RoleInfo, 20, OptionName.AvengerRevengeMode, revengeModes, 1, false);
        OptionRevengeNums = IntegerOptionItem.Create(RoleInfo, 21, OptionName.AvengerRevengeNums, new(1, 3, 1), 1, false)
            .SetValueFormat(OptionFormat.Players);
        OptionRevengeOnKilled = BooleanOptionItem.Create(RoleInfo, 22, OptionName.AvengerRevengeOnKilled, true, false);
        OptionRevengeOnSuicide = BooleanOptionItem.Create(RoleInfo, 23, OptionName.AvengerRevengeOnSuicide, true, false);
    }

    // FIX ME : Recursive Murder
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (info.IsSuicide
            ? !OptionRevengeOnSuicide.GetBool()
            : !OptionRevengeOnKilled.GetBool()
            ) return;

        List<PlayerControl> targets = new();
        if (OptionRevengeMode.GetInt() == 0)
        {
            targets.Add(killer);
        }
        else
        {
            List<PlayerControl> list = new();
            switch (OptionRevengeMode.GetInt())
            {
                case 1:
                    list = Main.AllAlivePlayerControls.ToList();
                    break;
                case 2:
                    list = Main.AllAlivePlayerControls.Where(p => p.GetCustomRole().GetCustomRoleTypes() != target.GetCustomRole().GetCustomRoleTypes()).ToList();
                    break;
                case 3:
                    list = Main.AllAlivePlayerControls.Where(p => p.GetCustomRole().GetCustomRoleTypes() == target.GetCustomRole().GetCustomRoleTypes()).ToList();
                    break;
            }
            list = list.Where(p => p != target).ToList();
            for (int i = 0; i < OptionRevengeNums.GetInt(); i++)
            {
                if (list.Count < 1) break;
                int index = IRandom.Instance.Next(0, list.Count);
                targets.Add(list[index]);
                list.RemoveAt(index);
            }
        }

        _ = new LateTask(() =>
        {
            foreach (var pc in targets)
            {
                pc.SetRealKiller(target);
                pc.SetDeathReason(CustomDeathReason.Revenge);
                target.RpcMurderPlayer(pc);
                Logger.Info($"Avenger {target.GetNameWithRole()} revenged => {pc.GetNameWithRole()}", "Avenger.OnMurderPlayerAsTarget");
            }
        }, 0.2f, "AvengerRevenge");
    }
}