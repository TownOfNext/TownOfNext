using AmongUs.GameOptions;
using TONX.Modules;

namespace TONX.Roles.Crewmate;
public sealed class Transporter : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Transporter),
            player => new Transporter(player),
            CustomRoles.Transporter,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            21500,
            SetupOptionItem,
            "tr|傳送師|传送",
            "#42D1FF"
        );
    public Transporter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionTeleportNums;
    enum OptionName
    {
        TransporterTeleportMax
    }

    private static void SetupOptionItem()
    {
        OptionTeleportNums = IntegerOptionItem.Create(RoleInfo, 10, OptionName.TransporterTeleportMax, new(1, 99, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
        Options.OverrideTasksData.Create(RoleInfo, 20);
    }
    public override bool OnCompleteTask(out bool cancel)
    {
        cancel = false;
        if (!Player.IsAlive() || MyTaskState.CompletedTasksCount + 1 > OptionTeleportNums.GetInt()) return false;

        Logger.Info("传送师触发传送:" + Player.GetNameWithRole(), "Transporter");

        var rd = IRandom.Instance;
        List<PlayerControl> AllAlivePlayer = new();
        foreach (var pc in Main.AllAlivePlayerControls.Where(x => !x.IsEaten() && !x.inVent)) AllAlivePlayer.Add(pc);
        if (AllAlivePlayer.Count >= 2)
        {
            var tar1 = AllAlivePlayer[rd.Next(0, AllAlivePlayer.Count)];
            AllAlivePlayer.Remove(tar1);
            var tar2 = AllAlivePlayer[rd.Next(0, AllAlivePlayer.Count)];
            var pos = tar1.GetTruePosition();
            Utils.TP(tar1.NetTransform, tar2.GetTruePosition());
            Utils.TP(tar2.NetTransform, pos);
            tar1.RPCPlayCustomSound("Teleport");
            tar2.RPCPlayCustomSound("Teleport");
            tar1.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Transporter), string.Format(GetString("TeleportedByTransporter"), tar2.GetRealName())));
            tar2.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Transporter), string.Format(GetString("TeleportedByTransporter"), tar1.GetRealName())));
        }

        return false;
    }
}