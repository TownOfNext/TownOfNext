using System.Text;

namespace TONX.Roles.AddOns.Impostor;
public sealed class Mimic : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Mimic),
            player => new Mimic(player),
            CustomRoles.Mimic,
            82000,
            null,
            "mi|寶箱怪|宝箱",
            "#ff1919",
            assignTeam: (false, true, false),
            conflicts: Conflicts
        );
    public Mimic(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    private List<(string, byte, string)> MsgToSend;
    private static List<CustomRoles> Conflicts = new() { CustomRoles.Mafia };

    public override void OnPlayerDeath(PlayerControl player, CustomDeathReason deathReason, bool isOnMeeting = false)
    {
        //Mimic Msg Combine
        var mimicSb = new StringBuilder();
        foreach (var vic in Main.AllPlayerControls.Where(p => !p.IsAlive()))
        {
            if ((vic.GetRealKiller()?.Is(CustomRoles.Mimic) ?? false) && (!vic.GetRealKiller()?.IsAlive() ?? false))
                mimicSb.Append($"\n{vic.GetNameWithRole(true)}");
        }
        if (mimicSb.Length > 1)
        {
            string mimicMsg = GetString("MimicDeadMsg") + "\n" + mimicSb.ToString();
            foreach (var ipc in Main.AllPlayerControls.Where(x => x.Is(CustomRoleTypes.Impostor)))
                MsgToSend.Add((mimicMsg, ipc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mimic), GetString("MimicMsgTitle"))));
        }
    }
    public override void NotifyOnMeetingStart(ref List<(string, byte, string)> msgToSend)
    {
        if (MsgToSend?.Any() ?? false)
            msgToSend.AddRange(MsgToSend.ToArray());
        MsgToSend = new();
    }

}