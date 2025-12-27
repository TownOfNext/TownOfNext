namespace TONX.Roles.AddOns.Common;
public sealed class Beartrap : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Beartrap),
            player => new Beartrap(player),
            CustomRoles.Beartrap,
            81800,
            SetupCustomOption,
            "tra|陷阱師|陷阱|小奖",
            "#5a8fd0"
        );
    public Beartrap(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public static OptionItem OptionBlockMoveTime;

    enum OptionName
    {
        BeartrapBlockMoveTime
    }

    private static void SetupCustomOption()
    {
        OptionBlockMoveTime = FloatOptionItem.Create(RoleInfo, 20, OptionName.BeartrapBlockMoveTime, new(1f, 180f, 1f), 5f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (!target.Is(CustomRoles.Beartrap) || info.IsSuicide) return;

        var tmpSpeed = Main.AllPlayerSpeed[killer.PlayerId];
        Main.AllPlayerSpeed[killer.PlayerId] = Main.MinSpeed;    //tmpSpeed����ۤɂ�������ΤǴ��뤷�Ƥ��ޤ���
        ReportDeadBodyPatch.CanReport[killer.PlayerId] = false;
        killer.MarkDirtySettings();
        _ = new LateTask(() =>
        {
            Main.AllPlayerSpeed[killer.PlayerId] = tmpSpeed;
            ReportDeadBodyPatch.CanReport[killer.PlayerId] = true;
            killer.MarkDirtySettings();
            RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
        }, OptionBlockMoveTime.GetFloat(), "Beartrap BlockMove");
    }
}