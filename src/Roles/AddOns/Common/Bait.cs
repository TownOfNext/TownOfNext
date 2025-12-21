using TONX.Modules;

namespace TONX.Roles.AddOns.Common;
public sealed class Bait : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Bait),
            player => new Bait(player),
            CustomRoles.Bait,
            81700,
            SetupCustomOption,
            "ba|誘餌|大奖|头奖",
            "#00f7ff"
        );
    public Bait(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CustomRoleManager.OnMurderPlayerOthers.Add(OnMurderPlayerOthers);
    }

    public static OptionItem OptionReportDelayMin;
    public static OptionItem OptionReportDelayMax;
    public static OptionItem OptionDelayNotifyForKiller;

    enum OptionName
    {
        BaitDelayMin,
        BaitDelayMax,
        BaitDelayNotify
    }

    private static void SetupCustomOption()
    {
        OptionReportDelayMin = FloatOptionItem.Create(RoleInfo, 20, OptionName.BaitDelayMin, new(0f, 5f, 1f), 0f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionReportDelayMax = FloatOptionItem.Create(RoleInfo, 21, OptionName.BaitDelayMax, new(0f, 10f, 1f), 0f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionDelayNotifyForKiller = BooleanOptionItem.Create(RoleInfo, 22, OptionName.BaitDelayNotify, true, false);
    }
    private static void OnMurderPlayerOthers(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (!target.Is(CustomRoles.Bait) || info.IsSuicide) return;
        if (!info.IsSuicide)
        {
            killer.RPCPlayCustomSound("Congrats");
            target.RPCPlayCustomSound("Congrats");
            float delay;
            if (OptionReportDelayMax.GetFloat() < OptionReportDelayMin.GetFloat()) delay = 0f;
            else delay = IRandom.Instance.Next((int)OptionReportDelayMin.GetFloat(), (int)OptionReportDelayMax.GetFloat() + 1);
            delay = Math.Max(delay, 0.15f);
            if (delay > 0.15f && OptionDelayNotifyForKiller.GetBool()) killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Bait), string.Format(GetString("KillBaitNotify"), (int)delay)), delay);
            Logger.Info($"{killer.GetNameWithRole()} Killed Bait => {target.GetNameWithRole()}", "Bait.OnMurderPlayerAsTarget");
            _ = new LateTask(() => { if (GameStates.IsInTask) killer.CmdReportDeadBody(target.Data); }, delay, "Bait Self Report");
        }
    }
}