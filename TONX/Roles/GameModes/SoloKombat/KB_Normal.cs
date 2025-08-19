using AmongUs.GameOptions;
using Hazel;
using TONX.Roles.Core.Interfaces;
using UnityEngine;
using static TONX.GameModes.SoloKombatManager;

namespace TONX.Roles.GameMode;

public sealed class KB_Normal : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(KB_Normal),
            player => new KB_Normal(player),
            CustomRoles.KB_Normal,
            () => RoleTypes.Impostor,
            CustomRoleTypes.GameMode,
            100000,
            null,
            "个人竞技|挑战",
            "#f55252",
            true
        );
    public KB_Normal(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
    {
        _HPMax = _HP = KB_HPMax.GetFloat();
        HPReco = KB_RecoverPerSecond.GetFloat();
        ATK = KB_ATK.GetFloat();
        DF = 0F;

        Score = 0;
        _LastHurt = Utils.GetTimeStamp();
    }

    public bool CanUseSabotageButton() => false;
    public bool CanUseKillButton() => true;
    public float CalculateKillCooldown() => KB_ATKCooldown.GetFloat();

    private float _HP;
    private float _HPMax;
    private long _LastHurt;
    private int _BackCountdown;
    public float HPReco { get; private set; }
    public float ATK { get; private set; }
    public float DF { get; private set; }
    public int Score { get; private set; }

    private enum RoleRpcType
    {
        SyncKBPlayer,
        SyncKBBackCountdown,
    }

    private void SendRPCSyncKBPlayer()
    {
        var sender = CreateSender();
        sender.Writer.Write((byte)RoleRpcType.SyncKBPlayer);
        sender.Writer.Write(_HPMax);
        sender.Writer.Write(_HP);
        sender.Writer.Write(HPReco);
        sender.Writer.Write(ATK);
        sender.Writer.Write(DF);
        sender.Writer.Write(Score);
    }
    private void SendRPCSyncKBBackCountdown()
    {
        var sender = CreateSender();
        sender.Writer.Write((byte)RoleRpcType.SyncKBBackCountdown);
        sender.Writer.Write(_BackCountdown);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        var rpcType = (RoleRpcType)reader.ReadByte();
        switch (rpcType)
        {
            case RoleRpcType.SyncKBBackCountdown:
                _BackCountdown = reader.ReadInt32();
                break;
            case RoleRpcType.SyncKBPlayer:
                _HPMax = reader.ReadSingle();
                _HP = reader.ReadSingle();
                HPReco = reader.ReadSingle();
                ATK = reader.ReadSingle();
                DF = reader.ReadSingle();
                Score = reader.ReadInt32();
                break;
        }
    }
    public override void OnSecondsUpdate(PlayerControl player, long now)
    {
        if (!GameStates.IsInTask) return;
        if (!AmongUsClient.Instance.AmHost) return;

        if (_LastHurt + KB_RecoverAfterSecond.GetInt() < Utils.GetTimeStamp()
            && _HP < _HPMax
            && !player.Data.IsDead
            && !player.inVent)
        {
            _HP += HPReco;
            _HP = Math.Min(_HPMax, _HP);
            SendRPCSyncKBPlayer();
        }
        if (_BackCountdown > 0)
        {
            _BackCountdown--;
            if (_BackCountdown <= 0) OnPlayerBack(player);
            SendRPCSyncKBBackCountdown();
        }
        if (_BackCountdown > 0)
        {
            player.Notify(string.Format(GetString("KBBackCountDown"), _BackCountdown));
        }
        Utils.NotifyRoles(player);
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        var role = seen.GetRoleClass() as KB_Normal;
        return role == null ? "" : GetHealthText(role);
    }
    public static string GetHealthText(KB_Normal role)
    {
        return !role.Player.Data.IsDead ? Utils.ColorString(GetHealthColor(role), $"{(int)role._HP}/{(int)role._HPMax}") : "";
    }
    private static Color32 GetHealthColor(KB_Normal role)
    {
        var x = (int)(role._HP / role._HPMax * 10 * 50);
        var R = 255; var G = 255; var B = 0;
        if (x > 255) R -= x - 255; else G = x;
        return new Color32((byte)R, (byte)G, (byte)B, byte.MaxValue);
    }
    public override string GetProgressText(bool comms = false) => GetDisplayScore(Player.PlayerId);
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = (info.AttemptKiller, info.AttemptTarget);
        if (target.GetRoleClass() is not KB_Normal targetRole) return false;

        var dmg = ATK - targetRole.DF;
        targetRole._HP = Math.Max(0f, targetRole._HP - dmg);

        targetRole._LastHurt = Utils.GetTimeStamp();

        killer.SetKillCooldownV2(KB_ATKCooldown.GetFloat(), target);
        RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
        RPC.PlaySoundRPC(target.PlayerId, Sounds.KillSound);

        targetRole.SendRPCSyncKBPlayer();
        Utils.NotifyRoles(killer);
        Utils.NotifyRoles(target);

        if (targetRole._HP <= 0f)
        {
            RewardKiller(killer);
            return true;
        }

        return false;
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        _BackCountdown = KB_ResurrectionWaitingTime.GetInt();
        SendRPCSyncKBBackCountdown();
        return true;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        return Utils.ColorString(Color.white, string.Format(GetString("KBTimeRemain"), RoundTime.ToString()));
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("DemonButtonText");
        return true;
    }
    private void RewardKiller(PlayerControl killer)
    {
        if (PlayerControl.LocalPlayer.Is(CustomRoles.GM)) PlayerControl.LocalPlayer.KillFlash();

        Score++;

        var addRate = IRandom.Instance.Next(3, 5 + GetRankOfScore(killer.PlayerId)) / 100f;
        addRate *= KB_KillBonusMultiplier.GetFloat();
        float addin;
        switch (IRandom.Instance.Next(0, 3))
        {
            case 0:
                addin = _HPMax * addRate;
                _HPMax += addin;
                killer.Notify(string.Format(GetString("KB_Buff_HPMax"), addin.ToString("0.0#####")));
                break;
            case 1:
                addin = HPReco * addRate * 2;
                HPReco += addin;
                killer.Notify( string.Format(GetString("KB_Buff_HPReco"), addin.ToString("0.0#####")));
                break;
            case 2:
                addin = ATK * addRate;
                ATK += addin;
                killer.Notify(string.Format(GetString("KB_Buff_ATK"), addin.ToString("0.0#####")));
                break;
        }
    }
    private void OnPlayerBack(PlayerControl player)
    {
        _BackCountdown = -1;
        _HP = _HPMax;
        SendRPCSyncKBPlayer();
        player.RpcRevive();
        _LastHurt = Utils.GetTimeStamp();
        player.MarkDirtySettings();
        RPC.PlaySoundRPC(player.PlayerId, Sounds.TaskComplete);
        player.SetKillCooldown();
        PlayerRandomSpawn();
    }
    private void PlayerRandomSpawn()
    {
        RandomSpawn.SpawnMap map;
        switch (Main.NormalOptions.MapId)
        {
            case 0:
                map = new RandomSpawn.SkeldSpawnMap();
                map.RandomTeleport(Player);
                break;
            case 1:
                map = new RandomSpawn.MiraHQSpawnMap();
                map.RandomTeleport(Player);
                break;
            case 2:
                map = new RandomSpawn.PolusSpawnMap();
                map.RandomTeleport(Player);
                break;
            case 4:
                map = new RandomSpawn.AirshipSpawnMap();
                map.RandomTeleport(Player);
                break;
            case 5:
                map = new RandomSpawn.FungleSpawnMap();
                map.RandomTeleport(Player);
                break;
        }
    }
}