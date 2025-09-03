using System.Text;
using TONX.Modules;

namespace TONX;

public static class MeetingStartNotify
{
    public static void OnMeetingStart()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (RoleDraftManager.RoleDraftState == RoleDraftState.ReadyToDraft)
        {
            new LateTask(RoleDraftManager.StartRoleDraft, 8f, "RoleDraftNotify");
            return;
        }

        List<(string, byte, string)> msgToSend = new();

        void AddMsg(string text, byte sendTo = 255, string title = "<Default>")
            => msgToSend.Add((text, sendTo, title));

        //首次会议技能提示
        if (Options.SendRoleDescriptionFirstMeeting.GetBool() && MeetingStates.FirstMeeting)
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => !x.IsModClient()))
            {
                var role = pc.GetCustomRole();
                var text = role.GetRoleInfo()?.Description?.GetFullFormatHelpWithAddonsByPlayer(pc) ??
                    GetString(role.ToString()) + pc.GetRoleInfo(true);
                AddMsg(text, pc.PlayerId);
            }
        if (msgToSend.Count >= 1)
        {
            var msgTemp = msgToSend.ToList();
            new LateTask(() => { msgTemp.Do(x => Utils.SendMessage(x.Item1, x.Item2, x.Item3 ?? "<Default>")); }, 3f, "NotifyOnMeetingStart");
        }

        msgToSend = new();

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
                AddMsg(mimicMsg, ipc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mimic), GetString("MimicMsgTitle")));
        }

        CustomRoleManager.AllActiveRoles.Values.ToList().Do(x => x.NotifyOnMeetingStart(ref msgToSend));
        msgToSend.Do(x => Logger.Info($"To:{x.Item2} {x.Item3 ?? ""} => {x.Item1}", "NotifyOnMeetingStart"));
        new LateTask(() => { msgToSend.DoIf(x => x.Item1 != null, x => Utils.SendMessage(x.Item1, x.Item2, x.Item3 ?? "")); }, 3f, "NotifyOnMeetingStart");
    }
}
