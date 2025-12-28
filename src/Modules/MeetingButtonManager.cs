using Hazel;
using TONX.Roles.Core.Interfaces;
using UnityEngine;

namespace TONX;

[HarmonyPatch(typeof(MeetingHud))]
public class MeetingButtonManager
{
    private static int Count = 0;
    public static bool ButtonCreated = false;
    private static void ClearMeetingButton(MeetingHud __instance, bool forceAll = false)
     => __instance.playerStates.ToList().ForEach(x => { if ((forceAll || (!PlayerState.AllPlayerStates.TryGetValue(x.TargetPlayerId, out var ps) || ps.IsDead)) && x.transform.FindChild("Custom Meeting Button") != null) UnityEngine.Object.Destroy(x.transform.FindChild("Custom Meeting Button").gameObject); });

    [HarmonyPatch(nameof(MeetingHud.Start)), HarmonyPrefix]
    public static void Start(MeetingHud __instance)
    {
        //提前储存赌怪游戏组件的模板
        GuesserHelper.textTemplate = UnityEngine.Object.Instantiate(__instance.playerStates[0].NameText);
        GuesserHelper.textTemplate.enabled = false;

        // CreateMeetingButton
        ButtonCreated = false;
        if (PlayerControl.LocalPlayer.GetRoleClass() is IMeetingButton meetingButton && meetingButton.ShouldShowButton())
        {
            CreateMeetingButton(__instance, meetingButton);
        }
    }

    [HarmonyPatch(nameof(MeetingHud.Update)), HarmonyPostfix, HarmonyPriority(Priority.LowerThanNormal)]
    public static void Update(MeetingHud __instance)
    {
        if (__instance == null || !GameStates.IsInGame || __instance.IsDestroyedOrNull()) return;

        Count = Count > 20 ? 0 : ++Count;
        if (Count != 0) return;

        //若某玩家死亡则修复会议该玩家状态
        __instance.playerStates.Where(x => (!PlayerState.AllPlayerStates.TryGetValue(x.TargetPlayerId, out var ps) || ps.IsDead) && !x.AmDead).Do(x => x.SetDead(x.DidReport, true));

        //本地玩家并没有会议技能按钮
        if (PlayerControl.LocalPlayer.GetRoleClass() is not IMeetingButton meetingButton) return;

        //投票结束时销毁全部技能按钮
        if (GameStates.IsVotingComplete)
        {
            if (GameObject.Find("Custom Meeting Button") != null) ClearMeetingButton(__instance, true);
            return;
        }

        //检查是否应该清除全部按钮
        if (ButtonCreated && !meetingButton.ShouldShowButton())
        {
            ClearMeetingButton(__instance, true);
            ButtonCreated = false;
        }

        //检查是否应该创建按钮
        if (!ButtonCreated && meetingButton.ShouldShowButton())
        {
            CreateMeetingButton(__instance, meetingButton);
        }

        //销毁死亡玩家身上的技能按钮
        ClearMeetingButton(__instance);

        meetingButton.OnUpdateButton(__instance);
    }
    public static void CreateMeetingButton(MeetingHud __instance, IMeetingButton meetingButton)
    {
        foreach (var pva in __instance.playerStates)
        {
            var pc = Utils.GetPlayerById(pva.TargetPlayerId);
            if (pc == null || !meetingButton.ShouldShowButtonFor(pc)) continue;
            GameObject template = pva.Buttons.transform.Find("CancelButton").gameObject;
            GameObject targetBox = UnityEngine.Object.Instantiate(template, pva.transform);
            targetBox.name = "Custom Meeting Button";
            targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1.31f);
            SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
            renderer.sprite = CustomButton.GetSprite(meetingButton.ButtonName);
            PassiveButton button = targetBox.GetComponent<PassiveButton>();
            button.OnClick = new();
            button.OnClick.AddListener((Action)(() =>
            {
                if (meetingButton.OnClickButtonLocal(pc))
                {
                    if (AmongUsClient.Instance.AmHost) meetingButton.OnClickButton(pc);
                    else
                    {
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.OnClickMeetingButton, SendOption.Reliable, -1);
                        writer.Write(pc.PlayerId);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                    }
                }
            }));
        }
        ButtonCreated = true;
    }
}
