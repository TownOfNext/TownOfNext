namespace TONX;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnDisconnected))]
internal class OnDisconnectedPatch
{
    public static void Postfix(AmongUsClient __instance)
    {
        Main.VisibleTasksCount = false;
    }
}

[HarmonyPatch(typeof(DisconnectPopup), nameof(DisconnectPopup.DoShow))]
internal class ShowDisconnectPopupPatch
{
    public static DisconnectReasons Reason;
    public static string StringReason;
    public static string ReasonByHost = string.Empty;
    public static void Postfix(DisconnectPopup __instance)
    {
        _ = new LateTask(() =>
        {
            if (__instance == null) return;
            try
            {

                void SetText(string text)
                {
                    if (__instance?._textArea?.text != null)
                        __instance._textArea.text = text;
                }

                if (!string.IsNullOrEmpty(ReasonByHost))
                    SetText(string.Format(GetString("DCNotify.Description"), ReasonByHost));
                else switch (Reason)
                    {
                        case DisconnectReasons.Hacking:
                            SetText(GetString("DCNotify.Hacking"));
                            break;
                        case DisconnectReasons.Banned:
                            SetText(GetString("DCNotify.Banned"));
                            break;
                        case DisconnectReasons.Kicked:
                            SetText(GetString("DCNotify.Kicked"));
                            break;
                        case DisconnectReasons.GameNotFound:
                            SetText(GetString("DCNotify.GameNotFound"));
                            break;
                        case DisconnectReasons.GameStarted:
                            SetText(GetString("DCNotify.GameStarted"));
                            break;
                        case DisconnectReasons.GameFull:
                            SetText(GetString("DCNotify.GameFull"));
                            break;
                        case DisconnectReasons.IncorrectVersion:
                            SetText(GetString("DCNotify.IncorrectVersion"));
                            break;
                        case DisconnectReasons.Error:
                            if (StringReason.Contains("Failed to send message")) SetText(GetString("DCNotify.DCFromServer"));
                            break;
                        case DisconnectReasons.Custom:
                            if (StringReason.Contains("Reliable packet")) SetText(GetString("DCNotify.DCFromServer"));
                            else if (StringReason.Contains("remote has not responded to")) SetText(GetString("DCNotify.DCFromServer"));
                            break;
                    }
            }
            catch { }
        }, 0.01f, "Override Disconnect Text");
    }
}