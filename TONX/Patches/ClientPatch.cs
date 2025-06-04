using HarmonyLib;
using Hazel;
using InnerNet;
using System.Linq;
using TONX.Modules;
using UnityEngine;
using System.Collections.Generic;
using static TONX.Translator;

namespace TONX;

[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.MakePublic))]
internal class MakePublicPatch
{
    public static bool Prefix(GameStartManager __instance)
    {
        // 定数設定による公開ルームブロック
        if (!Main.AllowPublicRoom)
        {
            var message = GetString("DisabledByProgram");
            Logger.Info(message, "MakePublicPatch");
            Logger.SendInGame(message);
            return false;
        }
        if (ModUpdater.isBroken || (ModUpdater.hasUpdate && ModUpdater.forceUpdate) || !VersionChecker.IsSupported || !Main.IsPublicAvailableOnThisVersion)
        {
            var message = "";
            if (!Main.IsPublicAvailableOnThisVersion) message = GetString("PublicNotAvailableOnThisVersion");
            if (ModUpdater.isBroken) message = GetString("ModBrokenMessage");
            if (ModUpdater.hasUpdate) message = GetString("CanNotJoinPublicRoomNoLatest");
            Logger.Info(message, "MakePublicPatch");
            Logger.SendInGame(message);
            return false;
        }
        return true;
    }
}
[HarmonyPatch(typeof(FindGameButton), nameof(FindGameButton.OnClick))]
class FindGameButtonOnClickPatch
{
    public static bool Prefix(FindGameButton __instance)
    {
        if (!(ModUpdater.hasUpdate || ModUpdater.isBroken || !VersionChecker.IsSupported || !Main.IsPublicAvailableOnThisVersion)) return true;
        string message = "";
        if (ModUpdater.hasUpdate)
        {
            message = GetString("CanNotJoinPublicRoomNoLatest");
        }
        else if (ModUpdater.isBroken)
        {
            message = GetString("ModBrokenMessage");
        }
        else if (!VersionChecker.IsSupported)
        {
            message = GetString("UnsupportedVersion");
        }
        else if (!Main.IsPublicAvailableOnThisVersion)
        {
            message = GetString("PublicNotAvailableOnThisVersion");
        }
        DisconnectPopup.Instance.ShowCustom(message);
        return false;
    }
}
[HarmonyPatch(typeof(SplashManager), nameof(SplashManager.Update))]
internal class SplashLogoAnimatorPatch
{
    public static void Prefix(SplashManager __instance)
    {
        if (DebugModeManager.AmDebugger)
        {
            __instance.sceneChanger.AllowFinishLoadingScene();
            __instance.startedSceneLoad = true;
        }
    }
}
[HarmonyPatch(typeof(EOSManager), nameof(EOSManager.IsAllowedOnline))]
internal class RunLoginPatch
{
    public static void Prefix(ref bool canOnline)
    {
        // Ref: https://github.com/0xDrMoe/TownofHost-Enhanced/blob/main/Patches/ClientPatch.cs
        var friendCode = EOSManager.Instance?.friendCode;
        canOnline = !string.IsNullOrEmpty(friendCode) && !BanManager.CheckEACStatus(friendCode, null);

#if DEBUG
        // 如果您希望在调试版本公开您的房间，请仅用于测试用途
        // 如果您修改了代码，请在房间公告内表明这是修改版本，并给出修改作者
        // If you wish to make your lobby public in a debug build, please use it only for testing purposes
        // If you modify the code, please indicate in the lobby announcement that this is a modified version and provide the author of the modification
        canOnline = System.Environment.UserName == "Leever";
#endif
    }
}
[HarmonyPatch(typeof(BanMenu), nameof(BanMenu.SetVisible))]
internal class BanMenuSetVisiblePatch
{
    public static bool Prefix(BanMenu __instance, bool show)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        show &= PlayerControl.LocalPlayer && PlayerControl.LocalPlayer.Data != null;
        __instance.BanButton.gameObject.SetActive(AmongUsClient.Instance.CanBan());
        __instance.KickButton.gameObject.SetActive(AmongUsClient.Instance.CanKick());
        __instance.MenuButton.gameObject.SetActive(show);
        return false;
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.CanBan))]
internal class InnerNetClientCanBanPatch
{
    public static bool Prefix(InnerNetClient __instance, ref bool __result)
    {
        __result = __instance.AmHost;
        return false;
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.KickPlayer))]
internal class KickPlayerPatch
{
    public static bool Prefix(InnerNetClient __instance, int clientId, bool ban)
    {
        if (Main.AllPlayerControls.Where(p => p.IsDev()).Any(p => AmongUsClient.Instance.GetRecentClient(clientId).FriendCode == p.FriendCode))
        {
            Logger.SendInGame(GetString("Warning.CantKickDev"));
            return false;
        }
        if (!AmongUsClient.Instance.AmHost) return true;

        if (!OnPlayerLeftPatch.ClientsProcessed.Contains(clientId))
        {
            OnPlayerLeftPatch.Add(clientId);
            if (ban)
            {
                BanManager.AddBanPlayer(AmongUsClient.Instance.GetRecentClient(clientId));
                RPC.NotificationPop(string.Format(GetString("PlayerBanByHost"), AmongUsClient.Instance.GetRecentClient(clientId).PlayerName));
            }
            else
            {
                RPC.NotificationPop(string.Format(GetString("PlayerKickByHost"), AmongUsClient.Instance.GetRecentClient(clientId).PlayerName));
            }
        }
        return true;
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.SendAllStreamedObjects))]
internal class InnerNetObjectSerializePatch
{
    public static void Prefix(InnerNetClient __instance)
    {
        if (AmongUsClient.Instance.AmHost)
            GameOptionsSender.SendAllGameOptions();
    }
}
[HarmonyPatch]
class InnerNetClientPatch
{
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.HandleMessage)), HarmonyPrefix]
    public static bool HandleMessagePatch(InnerNetClient __instance, MessageReader reader, SendOption sendOption)
    {
        if (DebugModeManager.IsDebugMode)
        {
            Logger.Info($"HandleMessagePatch:Packet({reader.Length}) ,SendOption:{sendOption}", "InnerNetClient");
        }
        else if (reader.Length > 1000)
        {
            Logger.Info($"HandleMessagePatch:Large Packet({reader.Length})", "InnerNetClient");
        }
        return true;
    }
    static Dictionary<int, int> messageCount = new(10);
    const int warningThreshold = 100;
    static int peak = warningThreshold;
    static float timer = 0f;
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.FixedUpdate)), HarmonyPrefix]
    public static void FixedUpdatePatch(InnerNetClient __instance)
    {
        int last = (int)timer % 10;
        timer += Time.fixedDeltaTime;
        int current = (int)timer % 10;
        if (last != current)
        {
            messageCount[current] = 0;
        }
    }

    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.SendOrDisconnect)), HarmonyPrefix]
    public static bool SendOrDisconnectPatch(InnerNetClient __instance, MessageWriter msg)
    {
        //分割するサイズ。大きすぎるとリトライ時不利、小さすぎると受信パケット取りこぼしが発生しうる。
        var limitSize = 1000;
 
        if (DebugModeManager.IsDebugMode)
        {
            Logger.Info($"SendOrDisconnectPatch:Packet({msg.Length}) ,SendOption:{msg.SendOption}", "InnerNetClient");
        }
        else if (msg.Length > limitSize)
        {
            Logger.Info($"SendOrDisconnectPatch:Large Packet({msg.Length})", "InnerNetClient");
        }
    //メッセージピークのログ出力
        if (msg.SendOption == SendOption.Reliable)
        {
            int last = (int)timer % 10;
            messageCount[last]++;
            int totalMessages = 0;
            foreach (var count in messageCount.Values)
            {
                totalMessages += count;
            }
            if (totalMessages > warningThreshold)
            {
                if (peak > totalMessages)
                {
                    Logger.Warn($"SendOrDisconnectPatch:Packet Spam Detected ({peak})", "InnerNetClient");
                    peak = warningThreshold;
                }
                else
                {
                    peak = totalMessages;
                }
            }
        }
        if (!Options.FixSpawnPacketSize.GetBool()) return true;

        //ラージパケットを分割(9人以上部屋で落ちる現象の対策コード)

        //メッセージが大きすぎる場合は分割して送信を試みる
        if (msg.Length > limitSize)
        {
            var writer = MessageWriter.Get(msg.SendOption);
            var reader = MessageReader.Get(msg.ToByteArray(false));

            //Tagレベルの処理
            while (reader.Position < reader.Length)
            {
                //Logger.Info($"SendOrDisconnectPatch:reader {reader.Position} / {reader.Length}", "InnerNetClient");

                var partMsg = reader.ReadMessage();
                var tag = partMsg.Tag;

                //Logger.Info($"SendOrDisconnectPatch:partMsg Tag={tag} Length={partMsg.Length}", "InnerNetClient");

                //TagがGameData,GameDataToの場合のみ分割処理
                //それ以外では多分分割しなくても問題ない
                if (tag is 5 or 6 && partMsg.Length > limitSize)
                {
                    //分割を試みる
                    DivideLargeMessage(__instance, writer, partMsg);
                }
                else
                {
                    //そのまま追加
                    WriteMessage(writer, partMsg);
                }

                //送信サイズが制限を超えた場合は送信
                if (writer.Length > limitSize)
                {
                    Send(__instance, writer);
                    writer.Clear(writer.SendOption);
                }
            }

            //残りの送信
            if (writer.HasBytes(7))
            {
                Send(__instance, writer);
            }

            writer.Recycle();
            reader.Recycle();
            return false;
        }
        return true;
    }
    private static void DivideLargeMessage(InnerNetClient __instance, MessageWriter writer, MessageReader partMsg)
    {
        var tag = partMsg.Tag;
        var GameId = partMsg.ReadInt32();
        var ClientId = -1;

        //元と同じTagを開く
        writer.StartMessage(tag);
        writer.Write(GameId);
        if (tag == 6)
        {
            ClientId = partMsg.ReadPackedInt32();
            writer.WritePacked(ClientId);
        }

        //Flag単位の処理
        while (partMsg.Position < partMsg.Length)
        {
            var subMsg = partMsg.ReadMessage();
            var subLength = subMsg.Length;

            //加算すると制限を超える場合は先に送信
            if (writer.Length + subLength > 500)
            {
                writer.EndMessage();
                Send(__instance, writer);
                //再度Tagを開く
                writer.Clear(writer.SendOption);
                writer.StartMessage(tag);
                writer.Write(GameId);
                if (tag == 6)
                {
                    writer.WritePacked(ClientId);
                }
            }
            //メッセージの出力
            WriteMessage(writer, subMsg);
        }
        writer.EndMessage();
    }

    private static void WriteMessage(MessageWriter writer, MessageReader reader)
    {
        writer.Write((ushort)reader.Length);
        writer.Write(reader.Tag);
        writer.Write(reader.ReadBytes(reader.Length));
    }

    private static void Send(InnerNetClient __instance, MessageWriter writer)
    {
        Logger.Info($"SendOrDisconnectPatch: SendMessage Length={writer.Length}", "InnerNetClient");
        var err = __instance.connection.Send(writer);
        if (err != SendErrors.None)
        {
            Logger.Info($"SendOrDisconnectPatch: SendMessage Error={err}", "InnerNetClient");
        }
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.Connect))]
public static class InnerNetClientConnectPatch
{
    public static IRegionInfo CurrentFindGameListFilteredClientRegion;
    public static int CurrentFindGameListFilteredClientGameId;
    public static void Postfix(InnerNetClient __instance)
    {
        if (FindAGameManager.Instance.isActiveAndEnabled)
        {
            CurrentFindGameListFilteredClientRegion = ServerManager.Instance.CurrentRegion;
            CurrentFindGameListFilteredClientGameId = __instance.GameId;
        }
    }
}