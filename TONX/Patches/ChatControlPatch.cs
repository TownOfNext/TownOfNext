using AmongUs.Data;
using TMPro;
using UnityEngine;

namespace TONX;

[HarmonyPatch(typeof(ChatController))]
public static class SendTargetPatch
{
    public enum SendTargets
    {
        Default,
        All,
        Dead
    }
    public static SendTargets SendTarget = SendTargets.Default;
    public static GameObject SendTargetShower;
    [HarmonyPatch(nameof(ChatController.Awake)), HarmonyPostfix]
    public static void Awake_Postfix(ChatController __instance)
    {
        __instance.freeChatField.textArea.SetText("");
        if (SendTargetShower != null) return;
        SendTargetShower = UnityEngine.Object.Instantiate(__instance.freeChatField.charCountText.gameObject, __instance.freeChatField.charCountText.transform.parent);
        SendTargetShower.name = "TONX Send Target Shower";
        SendTargetShower.transform.localPosition = new Vector3(1.95f, 0.5f, 0f);
        SendTargetShower.GetComponent<RectTransform>().sizeDelta = new Vector2(5f, 0.1f);
        var tmp = SendTargetShower.GetComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.outlineWidth = 1f;
    }
    [HarmonyPatch(nameof(ChatController.Update)), HarmonyPostfix]
    public static void Update_Postfix(ChatController __instance)
    {
        if (SendTargetShower == null) return;
        string text = GetString($"SendTargets.{Enum.GetName(SendTarget)}");
        if (AmongUsClient.Instance.AmHost && GameStates.IsInGame && __instance.IsOpenOrOpening)
        {
            text += "<size=75%>" + GetString("SendTargetSwitchNotice") + "</size>";
            if (Input.GetKey(KeyCode.LeftShift)) SendTarget = SendTargets.All;
            else if (Input.GetKey(KeyCode.LeftControl)) SendTarget = SendTargets.Dead;
            else SendTarget = SendTargets.Default;
        }
        else SendTarget = SendTargets.Default;
        SendTargetShower?.GetComponent<TextMeshPro>()?.SetText(text);
        SendTargetShower?.SetActive(!SendTargetShower.transform.parent.parent.FindChild("RateMessage (TMP)").gameObject.activeSelf);
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
public static class ChatControllerUpdatePatch
{
    public static int CurrentHistorySelection = -1;
    public static void Prefix()
    {
        if (AmongUsClient.Instance.AmHost && DataManager.Settings.Multiplayer.ChatMode == InnerNet.QuickChatModes.QuickChatOnly)
            DataManager.Settings.Multiplayer.ChatMode = InnerNet.QuickChatModes.FreeChatOrQuickChat; //コマンドを打つためにホストのみ常時フリーチャット開放
    }
    public static void Postfix(ChatController __instance)
    {
        if (!__instance.freeChatField.textArea.hasFocus) return;
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C))
            ClipboardHelper.PutClipboardString(__instance.freeChatField.textArea.text);
        // if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
            // __instance.freeChatField.textArea.SetText(__instance.freeChatField.textArea.text + GUIUtility.systemCopyBuffer);
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.X))
        {
            ClipboardHelper.PutClipboardString(__instance.freeChatField.textArea.text);
            __instance.freeChatField.textArea.SetText("");
        }
        if (Input.GetKeyDown(KeyCode.UpArrow) && ChatCommands.SentHistory.Count > 0)
        {
            CurrentHistorySelection = Mathf.Clamp(--CurrentHistorySelection, 0, ChatCommands.SentHistory.Count - 1);
            __instance.freeChatField.textArea.SetText(ChatCommands.SentHistory[CurrentHistorySelection]);
        }
        if (Input.GetKeyDown(KeyCode.DownArrow) && ChatCommands.SentHistory.Count > 0)
        {
            CurrentHistorySelection++;
            if (CurrentHistorySelection < ChatCommands.SentHistory.Count)
                __instance.freeChatField.textArea.SetText(ChatCommands.SentHistory[CurrentHistorySelection]);
            else __instance.freeChatField.textArea.SetText("");
        }
    }
}