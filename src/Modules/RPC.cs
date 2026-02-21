using AmongUs.GameOptions;
using Hazel;
using TONX.Modules;
using TONX.Roles.Core.Interfaces;
using TONX.Roles.Crewmate;

namespace TONX;

public enum CustomRPC
{
    VersionCheck = 80,
    RequestRetryVersionCheck = 81,
    SyncCustomSettings = 100,
    SetDeathReason,
    EndGame,
    PlaySound,
    SetCustomRole,
    SetNameColorData,
    SetLoversPlayers,
    SetRealKiller,
    CustomRoleSync,
    RemoveSubRole,

    //TONX
    AntiBlackout,
    RestTONXSetting,
    PlayCustomSound,
    SetKillTimer,
    SyncAllPlayerNames,
    SyncNameNotify,
    ShowPopUp,
    KillFlash,
    NotificationPop,
    SetKickReason,
    SyncRolesRecord,
    SyncTaskState,
    Revive,

    //Roles
    Guess,
    OnClickMeetingButton,
    SuicideWithAnime,
    SetMedicProtectList,
    //SetDrawPlayer,
    //SetCurrentDrawTarget,
    //SyncPelicanEatenPlayers,
    //VigilanteKill,
    //SetDemonHealth,
    //SetDeceiverSellLimit,
    //SetMedicProtectLimit,
    //SetGangsterRecruitLimit,
    //SetGhostPlayer,
    //SetStalkerrKillCount,
    //SetCursedWolfSpellCount,
    //SetCollectorVotes,
    //SetQuickShooterShotLimit,
    //SetEraseLimit,
    //SetMarkedPlayer,
    //SetConcealerTimer,

    //SetHackerHackLimit,
    //SyncPsychicRedList,
    //SetMorticianArrow,
    //SetSwooperTimer,
    //SetBKTimer,
    //SyncFollowerTargetAndTimes,
    //SetSuccubusCharmLimit,
    //SyncPuppeteerList,
    //SyncWarlock,
    //SyncEscapist,
    //SyncMarioVentedTimes,

    //SoloKombat
    SyncKBPlayer,
    SyncKBBackCountdown,
    SyncKBNameNotify,
}
public enum Sounds
{
    KillSound,
    TaskComplete,
    TaskUpdateSound,
    ImpTransform,

    Test,
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
internal class RPCHandlerPatch
{
    public static bool TrustedRpc(byte id)
    => (CustomRPC)id is CustomRPC.VersionCheck or CustomRPC.RequestRetryVersionCheck or CustomRPC.AntiBlackout or CustomRPC.Guess or CustomRPC.OnClickMeetingButton;
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        var rpcType = (RpcCalls)callId;
        MessageReader subReader = MessageReader.Get(reader);
        if (EAC.ReceiveRpc(__instance, callId, reader)) return false;
        Logger.Info(
            $"{__instance?.Data?.PlayerId}({(__instance?.Data?.PlayerId == 0 ? "Host" : __instance?.Data?.PlayerName)}):{callId}({RPC.GetRpcName(callId)})",
            "ReceiveRPC");
        switch (rpcType)
        {
            case RpcCalls.SetName: //SetNameRPC
                subReader.ReadUInt32();
                string name = subReader.ReadString();
                if (subReader.BytesRemaining > 0 && subReader.ReadBoolean()) return false;
                Logger.Info("RPC名称修改:" + __instance.GetNameWithRole() + " => " + name, "SetName");
                break;
            case RpcCalls.SetRole: //SetNameRPC
                var role = (RoleTypes)subReader.ReadUInt16();
                Logger.Info("RPC设置职业:" + __instance.GetRealName() + " => " + role, "SetRole");
                break;
            case RpcCalls.SendChat:
                var text = subReader.ReadString();
                if (string.IsNullOrEmpty(text) || text.EndsWith('\0')) return false;
                Logger.Info($"{__instance.GetNameWithRole()}:{text}", "ReceiveChat");
                ChatCommands.OnReceiveChat(__instance, text, out var canceled);
                if (canceled) return false;
                break;
            case RpcCalls.StartMeeting:
                var p = Utils.GetPlayerById(subReader.ReadByte());
                Logger.Info($"{__instance.GetNameWithRole()} => {p?.GetNameWithRole() ?? "null"}", "StartMeeting");
                break;
        }

        if (__instance?.PlayerId != 0
            && Enum.IsDefined(typeof(CustomRPC), (int)callId)
            && !TrustedRpc(callId)) //ホストではなく、CustomRPCで、VersionCheckではない
        {
            Logger.Warn($"{__instance?.Data?.PlayerName}:{callId}({RPC.GetRpcName(callId)}) 已取消，因为它是由主机以外的其他人发送的。",
                "CustomRPC");
            if (AmongUsClient.Instance.AmHost)
            {
                if (!EAC.ReceiveInvalidRpc(__instance, callId)) return false;
                Utils.KickPlayer(__instance.GetClientId(), false, "InvalidRPC");
                Logger.Warn($"收到来自 {__instance?.Data?.PlayerName} 的不受信用的RPC，因此将其踢出。", "Kick");
                RPC.NotificationPop(string.Format(GetString("Warning.InvalidRpc"), __instance?.Data?.PlayerName));
            }
            return false;
        }
        return true;
    }
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        //CustomRPC以外は処理しない
        if (callId < (byte)CustomRPC.VersionCheck) return;

        var rpcType = (CustomRPC)callId;
        switch (rpcType)
        {
            case CustomRPC.AntiBlackout:
                if (Options.EndWhenPlayerBug.GetBool())
                {
                    Logger.Fatal($"{__instance?.Data?.PlayerName}({__instance.PlayerId}): {reader.ReadString()} 错误，根据设定终止游戏", "Anti-black");
                    ChatUpdatePatch.DoBlockChat = true;
                    Main.OverrideWelcomeMsg = string.Format(GetString("RpcAntiBlackOutNotifyInLobby"), __instance?.Data?.PlayerName, GetString("EndWhenPlayerBug"));
                    _ = new LateTask(() =>
                    {
                        Logger.SendInGame(string.Format(GetString("RpcAntiBlackOutEndGame"), __instance?.Data?.PlayerName), true);
                    }, 3f, "Anti-Black Msg SendInGame");
                    _ = new LateTask(() =>
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Error);
                        GameManager.Instance.LogicFlow.CheckEndCriteria();
                        RPC.ForceEndGame(CustomWinner.Error);
                    }, 5.5f, "Anti-Black End Game");
                }
                else
                {
                    Logger.Fatal($"{__instance?.Data?.PlayerName}({__instance.PlayerId}): Change Role Setting Postfix 错误，根据设定继续游戏", "Anti-black");
                    _ = new LateTask(() =>
                    {
                        Logger.SendInGame(string.Format(GetString("RpcAntiBlackOutIgnored"), __instance?.Data?.PlayerName), true);
                    }, 3f, "Anti-Black Msg SendInGame");
                }
                break;
            case CustomRPC.VersionCheck:
                Version version = Version.Parse(reader.ReadString());
                string tag = reader.ReadString();
                string forkId = reader.ReadString();
                RPC.ProceedVersionCheck(__instance, version, tag, forkId);
                break;
            case CustomRPC.RequestRetryVersionCheck:
                RPC.RpcVersionCheck();
                break;
            case CustomRPC.SyncCustomSettings:
                if (AmongUsClient.Instance.AmHost) break;
                List<OptionItem> list = new();
                var startAmount = reader.ReadInt32();
                var lastAmount = reader.ReadInt32();
                for (var i = startAmount; i < OptionItem.AllOptions.Count && i <= lastAmount; i++)
                    list.Add(OptionItem.AllOptions[i]);
                Logger.Info($"{startAmount}-{lastAmount}:{list.Count}/{OptionItem.AllOptions.Count}", "SyncCustomSettings");
                foreach (var co in list) co.SetValue(reader.ReadPackedInt32());
                break;
            case CustomRPC.SetDeathReason:
                RPC.GetDeathReason(reader);
                break;
            case CustomRPC.EndGame:
                RPC.EndGame(reader);
                break;
            case CustomRPC.PlaySound:
                byte playerID = reader.ReadByte();
                Sounds sound = (Sounds)reader.ReadByte();
                RPC.PlaySound(playerID, sound);
                break;
            case CustomRPC.ShowPopUp:
                string msg = reader.ReadString();
                HudManager.Instance.ShowPopUp(msg);
                break;
            case CustomRPC.SetCustomRole:
                byte CustomRoleTargetId = reader.ReadByte();
                CustomRoles role = (CustomRoles)reader.ReadPackedInt32();
                RPC.SetCustomRole(CustomRoleTargetId, role);
                break;
            case CustomRPC.SetNameColorData:
                NameColorManager.ReceiveRPC(reader);
                break;
            case CustomRPC.SetLoversPlayers:
                Main.LoversPlayers.Clear();
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                    Main.LoversPlayers.Add(Utils.GetPlayerById(reader.ReadByte()));
                break;
            case CustomRPC.SetRealKiller:
                byte targetId = reader.ReadByte();
                byte killerId = reader.ReadByte();
                RPC.SetRealKiller(targetId, killerId);
                break;
            case CustomRPC.PlayCustomSound:
                CustomSoundsManager.ReceiveRPC(reader);
                break;
            case CustomRPC.RestTONXSetting:
                OptionItem.AllOptions.ToArray().Where(x => x.Id > 0).Do(x => x.SetValue(x.DefaultValue, false));
                break;
            case CustomRPC.SuicideWithAnime:
                var playerId = reader.ReadByte();
                var pc = Utils.GetPlayerById(playerId);
                pc?.RpcSuicideWithAnime(true);
                break;
            case CustomRPC.SetKillTimer:
                float time = reader.ReadSingle();
                PlayerControl.LocalPlayer.SetKillTimer(time);
                break;
            case CustomRPC.SyncAllPlayerNames:
                Main.AllPlayerNames = new();
                int num = reader.ReadInt32();
                for (int i = 0; i < num; i++)
                    Main.AllPlayerNames.TryAdd(reader.ReadByte(), reader.ReadString());
                break;
            case CustomRPC.SyncNameNotify:
                NameNotifyManager.ReceiveRPC(reader);
                break;
            case CustomRPC.KillFlash:
                Utils.FlashColor(new(1f, 0f, 0f, 0.3f));
                if (Constants.ShouldPlaySfx()) RPC.PlaySound(PlayerControl.LocalPlayer.PlayerId, Sounds.KillSound);
                break;
            case CustomRPC.OnClickMeetingButton:
                var target = Utils.GetPlayerById(reader.ReadByte());
                if (AmongUsClient.Instance.AmHost && (GameStates.IsDiscussing || GameStates.IsVoting))
                    if (__instance.GetRoleClass() is IMeetingButton meetingButton) meetingButton.OnClickButton(target);
                break;
            case CustomRPC.Guess:
                GuesserHelper.ReceiveRPC(reader, __instance);
                break;
            case CustomRPC.SetMedicProtectList:
                Medic.ReceiveRPC_SyncList(reader);
                break;
            case CustomRPC.NotificationPop:
                NotificationPopperPatch.AddItem(reader.ReadString());
                break;
            case CustomRPC.SetKickReason:
                ShowDisconnectPopupPatch.ReasonByHost = reader.ReadString();
                break;
            case CustomRPC.CustomRoleSync:
                CustomRoleManager.DispatchRpc(reader);
                break;
            case CustomRPC.SyncRolesRecord:
                Utils.SyncRolesRecord(reader.ReadByte());
                break;
            case CustomRPC.RemoveSubRole:
                byte CustomRoleTargetId2 = reader.ReadByte();
                CustomRoles role2 = (CustomRoles)reader.ReadPackedInt32();
                PlayerState.GetByPlayerId(CustomRoleTargetId2).RemoveSubRole(role2, false);
                break;
            case CustomRPC.SyncTaskState:
                byte id = reader.ReadByte();
                TaskState taskState = Utils.GetPlayerById(id).GetPlayerTaskState();
                taskState.AllTasksCount = reader.ReadInt32();
                taskState.CompletedTasksCount = reader.ReadInt32();
                taskState.hasTasks = reader.ReadBoolean();
                break;
            case CustomRPC.Revive:
                byte reviveId = reader.ReadByte();
                PlayerState playerState = PlayerState.GetByPlayerId(reviveId);
                playerState.DeathReason = CustomDeathReason.etc;
                playerState.IsDead = false;
                playerState.RealKiller = (DateTime.MinValue, byte.MaxValue);
                break;
        }
    }
}

internal static class RPC
{
    public static async void ProceedVersionCheck(PlayerControl sender, Version version, string tag, string forkId)
    {
        try
        {
            while (sender == null || sender.GetClient() == null) await Task.Delay(500);

            var clientId = sender.GetClientId();
            Main.playerVersion.Remove(clientId);
            Main.playerVersion[clientId] = new PlayerVersion(version, tag, forkId);

            if (Main.VersionCheat.Value && sender.PlayerId == 0) RpcVersionCheck();
            if (Main.VersionCheat.Value && AmongUsClient.Instance.AmHost)
                Main.playerVersion[clientId] = Main.playerVersion[clientId];

            // Kick Unmatched Player
            if (AmongUsClient.Instance.AmHost && tag != $"{Main.GitCommit}({Main.GitBranch})" && forkId != Main.ForkId)
            {
                _ = new LateTask(() =>
                {
                    if (sender?.Data?.Disconnected is not null and not true)
                    {
                        var msg = string.Format(GetString("KickBecauseDiffrentVersionOrMod"), sender?.Data?.PlayerName);
                        Logger.Warn(msg, "Version Kick");
                        NotificationPop(msg);
                        Utils.KickPlayer(clientId, false, "ModVersionIncorrect");
                    }
                }, 5f, "Kick");
            }
        }
        catch
        {
            Logger.Warn($"{sender?.Data?.PlayerName}({sender.PlayerId}): バージョン情報が無効です", "RpcVersionCheck");
            _ = new LateTask(() =>
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RequestRetryVersionCheck, SendOption.Reliable, sender.GetClientId());
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }, 1f, "Retry Version Check Task");
        }
    }
    // 来源：https://github.com/music-discussion/TownOfHost-TheOtherRoles/blob/main/Modules/RPC.cs
    public static void SyncCustomSettingsRPC(int targetId = -1)
    {
        if (targetId != -1)
        {
            var client = Utils.GetClientById(targetId);
            if (client == null || client.Character == null || !Main.playerVersion.ContainsKey(client.Id)) return;
        }
        if (!AmongUsClient.Instance.AmHost || PlayerControl.AllPlayerControls.Count <= 1 || (AmongUsClient.Instance.AmHost == false && PlayerControl.LocalPlayer == null)) return;
        var amount = OptionItem.AllOptions.Count;
        int divideBy = amount / 10;
        for (var i = 0; i <= 10; i++)
            SyncOptionsBetween(i * divideBy, (i + 1) * divideBy, targetId);
    }
    public static void SyncCustomSettingsRPCforOneOption(OptionItem option)
    {
        List<OptionItem> allOptions = new(OptionItem.AllOptions);
        var placement = allOptions.IndexOf(option);
        if (placement != -1)
            SyncOptionsBetween(placement, placement);
    }
    static void SyncOptionsBetween(int startAmount, int lastAmount, int targetId = -1)
    {
        //判断发送请求是否有效
        if (
            Main.AllPlayerControls.Count() <= 1 ||
            AmongUsClient.Instance.AmHost == false ||
            PlayerControl.LocalPlayer == null
        ) return;
        //判断发送目标是否有效
        if (targetId != -1)
        {
            var client = Utils.GetClientById(targetId);
            if (client == null || client.Character == null || !Main.playerVersion.ContainsKey(client.Id))
                return;
        }

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncCustomSettings, SendOption.Reliable, targetId);
        List<OptionItem> list = new();
        writer.Write(startAmount);
        writer.Write(lastAmount);
        for (var i = startAmount; i < OptionItem.AllOptions.Count && i <= lastAmount; i++)
            list.Add(OptionItem.AllOptions[i]);
        Logger.Info($"{startAmount}-{lastAmount}:{list.Count}/{OptionItem.AllOptions.Count}", "SyncCustomSettings");
        foreach (var co in list) writer.WritePacked(co.GetValue());
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void PlaySoundRPC(byte PlayerID, Sounds sound)
    {
        if (AmongUsClient.Instance.AmHost)
            PlaySound(PlayerID, sound);
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlaySound, SendOption.Reliable, -1);
        writer.Write(PlayerID);
        writer.Write((byte)sound);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SyncAllPlayerNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncAllPlayerNames, SendOption.Reliable, -1);
        writer.Write(Main.AllPlayerNames.Count);
        foreach (var name in Main.AllPlayerNames)
        {
            writer.Write(name.Key);
            writer.Write(name.Value);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ShowPopUp(this PlayerControl pc, string msg)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ShowPopUp, SendOption.Reliable, pc.GetClientId());
        writer.Write(msg);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ExileAsync(PlayerControl player)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Exiled, SendOption.Reliable, -1);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        player.Exiled();
    }
    public static async void RpcVersionCheck()
    {
        try
        {
            while (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.GetClient() == null) await Task.Delay(500);

            Main.playerVersion.TryAdd(PlayerControl.LocalPlayer.GetClientId(), new PlayerVersion(Main.PluginVersion, $"{Main.GitCommit}({Main.GitBranch})", Main.ForkId));
            if (Main.playerVersion.ContainsKey(Main.HostClientId) || !Main.VersionCheat.Value)
            {
                bool cheating = Main.VersionCheat.Value;
                _ = new LateTask(() => // 利用LateTask使RPC相关操作在主线程进行
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.VersionCheck, SendOption.Reliable);
                    writer.Write(cheating ? Main.playerVersion[Main.HostClientId].version.ToString() : Main.PluginVersion);
                    writer.Write(cheating ? Main.playerVersion[Main.HostClientId].tag : $"{Main.GitCommit}({Main.GitBranch})");
                    writer.Write(cheating ? Main.playerVersion[Main.HostClientId].forkId : Main.ForkId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                }, 0f, "RpcVersionCheck");
            }
        }
        catch
        {
            Logger.Warn($"{PlayerControl.LocalPlayer?.Data?.PlayerName}({PlayerControl.LocalPlayer.PlayerId}): 本地版本信息无效", "RpcVersionCheck");
        }
    }
    public static void SendDeathReason(byte playerId, CustomDeathReason deathReason)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDeathReason, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write((int)deathReason);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void GetDeathReason(MessageReader reader)
    {
        var playerId = reader.ReadByte();
        var deathReason = (CustomDeathReason)reader.ReadInt32();
        var state = PlayerState.GetByPlayerId(playerId);
        state.DeathReason = deathReason;
        state.IsDead = true;
    }
    public static void ForceEndGame(CustomWinner win)
    {
        if (ShipStatus.Instance == null) return;
        try { CustomWinnerHolder.ResetAndSetWinner(win); }
        catch { }
        if (AmongUsClient.Instance.AmHost)
        {
            ShipStatus.Instance.enabled = false;
            try { GameManager.Instance.LogicFlow.CheckEndCriteria(); }
            catch { }
            try { GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false); }
            catch { }
        }
    }
    public static void EndGame(MessageReader reader)
    {
        try
        {
            CustomWinnerHolder.ReadFrom(reader);
        }
        catch (Exception ex)
        {
            Logger.Error($"正常にEndGameを行えませんでした。\n{ex}", "EndGame", false);
        }
    }
    public static void PlaySound(byte playerID, Sounds sound)
    {
        if (PlayerControl.LocalPlayer.PlayerId == playerID)
        {
            switch (sound)
            {
                case Sounds.KillSound:
                    SoundManager.Instance.PlaySound(PlayerControl.LocalPlayer.KillSfx, false, 1f);
                    break;
                case Sounds.TaskComplete:
                    SoundManager.Instance.PlaySound(DestroyableSingleton<HudManager>.Instance.TaskCompleteSound, false, 1f);
                    break;
                case Sounds.TaskUpdateSound:
                    SoundManager.Instance.PlaySound(DestroyableSingleton<HudManager>.Instance.TaskUpdateSound, false, 1f);
                    break;
                case Sounds.ImpTransform:
                    SoundManager.Instance.PlaySound(DestroyableSingleton<HnSImpostorScreamSfx>.Instance.HnSOtherImpostorTransformSfx, false, 0.8f);
                    break;
            }
        }
    }
    public static void SetCustomRole(byte targetId, CustomRoles role)
    {
        if (role < CustomRoles.NotAssigned)
        {
            CustomRoleManager.GetByPlayerId(targetId)?.Dispose();
            PlayerState.GetByPlayerId(targetId).SetMainRole(role);
        }
        else
        {
            PlayerState.GetByPlayerId(targetId).SetSubRole(role);
        }
        CustomRoleManager.CreateInstance(role, Utils.GetPlayerById(targetId));

        HudManager.Instance.SetHudActive(true);
        if (PlayerControl.LocalPlayer.PlayerId == targetId) RemoveDisableDevicesPatch.UpdateDisableDevices();
    }
    public static void SyncLoversPlayers()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetLoversPlayers, SendOption.Reliable, -1);
        writer.Write(Main.LoversPlayers.Count);
        foreach (var lp in Main.LoversPlayers)
        {
            writer.Write(lp.PlayerId);
        }
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SendRpcLogger(uint targetNetId, byte callId, int targetClientId = -1)
    {
        if (!DebugModeManager.AmDebugger) return;
        string rpcName = GetRpcName(callId);
        string from = targetNetId.ToString();
        string target = targetClientId.ToString();
        try
        {
            target = targetClientId < 0 ? "All" : AmongUsClient.Instance.GetClient(targetClientId).PlayerName;
            from = Main.AllPlayerControls.FirstOrDefault(c => c.NetId == targetNetId)?.Data?.PlayerName;
        }
        catch { }
        Logger.Info($"FromNetID:{targetNetId}({from}) TargetClientID:{targetClientId}({target}) CallID:{callId}({rpcName})", "SendRPC");
    }
    public static string GetRpcName(byte callId)
    {
        string rpcName;
        if ((rpcName = Enum.GetName(typeof(RpcCalls), callId)) != null) { }
        else if ((rpcName = Enum.GetName(typeof(CustomRPC), callId)) != null) { }
        else rpcName = callId.ToString();
        return rpcName;
    }
    public static void SetRealKiller(byte targetId, byte killerId)
    {
        var state = PlayerState.GetByPlayerId(targetId);
        state.RealKiller.Item1 = DateTime.Now;
        state.RealKiller.Item2 = killerId;

        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetRealKiller, SendOption.Reliable, -1);
        writer.Write(targetId);
        writer.Write(killerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void NotificationPop(string text)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.NotificationPop, SendOption.Reliable, -1);
        writer.Write(text);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        NotificationPopperPatch.AddItem(text);
    }
    public static void SyncRolesRecord(byte playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRolesRecord, SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void SyncTaskState(byte playerId, int allTasksCount, int completedTasksCount, bool hastasks)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncTaskState, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(allTasksCount);
        writer.Write(completedTasksCount);
        writer.Write(hastasks);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void Revive(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.Revive, SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
}
[HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.StartRpcImmediately))]
internal class StartRpcImmediatelyPatch
{
    public static void Prefix(InnerNet.InnerNetClient __instance, [HarmonyArgument(0)] uint targetNetId, [HarmonyArgument(1)] byte callId, [HarmonyArgument(3)] int targetClientId = -1)
    {
        RPC.SendRpcLogger(targetNetId, callId, targetClientId);
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRoleRpc))]
internal class RoleRPCHandlerPatch
{
    public static bool Prefix(PlayerControl __instance)
    {
        var isNull = __instance?.Data?.Role == null;                                                        // HandleRoleRpc中树懒不检验Data.Role是否为空
        if (isNull) Logger.Info($"{__instance?.Data?.PlayerName}: Null Role Data", "HandleRoleRpc.Prefix"); // 用于临时修复原版问题
        return !isNull;                                                                                     // 如果Data.Role为null则不接收职业(幻象师)的Rpc
    }
}