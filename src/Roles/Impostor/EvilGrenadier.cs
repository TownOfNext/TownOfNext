using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using TONX.Modules;
using TONX.Roles.Core.Interfaces;

namespace TONX.Roles.Impostor;
public sealed class EvilGrenadier : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EvilGrenadier),
            player => new EvilGrenadier(player),
            CustomRoles.EvilGrenadier,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            5500,
            SetupOptionItem,
            "gr|擲雷兵|掷雷|闪光弹"
        );
    public EvilGrenadier(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CustomRoleManager.MarkOthers.Add(GetSuffixOthers);
    }

    static OptionItem OptionSkillCooldown;
    static OptionItem OptionSkillDuration;
    static OptionItem OptionSkillRange;
    enum OptionName
    {
        EvilGrenadierSkillCooldown,
        EvilGrenadierSkillDuration,
        EvilGrenadierSkillRange,
    }
    
    private long BlindingStartTime;
    private List<byte> Blinds;
    private static void SetupOptionItem()
    {
        OptionSkillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.EvilGrenadierSkillCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSkillDuration = FloatOptionItem.Create(RoleInfo, 11, OptionName.EvilGrenadierSkillDuration, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSkillRange = FloatOptionItem.Create(RoleInfo, 12, OptionName.EvilGrenadierSkillRange, new(0f, 50f, 2.5f), 10f, false)
            .SetValueFormat(OptionFormat.Multiplier);
    }

    public override void Add()
    {
        OptionSkillDuration.GetFloat();
        BlindingStartTime = 0;
        Blinds = new();
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.PhantomCooldown = BlindingStartTime != 0 ?
            OptionSkillDuration.GetFloat() + 1 : OptionSkillCooldown.GetFloat();
        AURoleOptions.PhantomDuration = 1f;
    }

    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(Blinds.Count);
        for (int i = 0; i < Blinds.Count; i++)
            sender.Writer.Write(Blinds[i]);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        int count = reader.ReadInt32();
        Blinds = new();
        for (int i = 0; i < count; i++)
            Blinds.Add(reader.ReadByte());
    }

    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("GrenadierVetnButtonText");
        return true;
    }
    public override bool OnCheckVanish()
    {
        if (BlindingStartTime != 0) return false;
        BlindingStartTime = Utils.GetTimeStamp();
        foreach (var pc in Main.AllAlivePlayerControls.Where(x => !x.IsImpTeam()))
        {
            OnBlinding(pc);
        }
        SendRPC();
        Player.RPCPlayCustomSound("FlashBang");
        if (!Player.IsModClient()) Player.RpcProtectedMurderPlayer();
        foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.IsImpTeam()))
            pc.Notify(GetString("GrenadierSkillInUse"), OptionSkillDuration.GetFloat());
        Utils.MarkEveryoneDirtySettings();
        Player.SyncSettings();
        Player.RpcResetAbilityCooldown();
        return false;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (BlindingStartTime == 0) return;
        if (BlindingStartTime + (long)OptionSkillDuration.GetFloat() < Utils.GetTimeStamp())
        {
            Blinds = new();
            SendRPC();
            BlindingStartTime = 0;
            Player.RpcProtectedMurderPlayer();
            Player.Notify(GetString("GrenadierSkillStop"));
            Utils.MarkEveryoneDirtySettings();
            Player.SyncSettings();
            Player.RpcResetAbilityCooldown();
        }
    }
    void OnBlinding(PlayerControl pc)
    {
        var posi = Player.transform.position;
        var diss = Vector2.Distance(posi, pc.transform.position);
        if (pc.IsAlive() && pc != Player && diss <= OptionSkillRange.GetFloat())
        {
            if (pc.IsModClient())
            {
                pc.RPCPlayCustomSound("FlashBang");
            }
            Blinds.Add(pc.PlayerId);
        }
    }
    public static string GetSuffixOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (IsBlinding(seer))
            return "<size=1000><color=#ffffff>●</color></size>";
        return "";
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        seen ??= seer;
        if (Blinds.Contains(seen.PlayerId) && seen.IsAlive())
            return Utils.ColorString(RoleInfo.RoleColor, "●");
        return "";
    }

    // 全部模组端都会调用，确保所有变量列表都已同步
    public static bool IsBlinding(PlayerControl target)
    {
        foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.EvilGrenadier)))
        {
            if (pc.GetRoleClass() is not EvilGrenadier roleClass) continue;
            if (roleClass.Blinds.Contains(target.PlayerId) && target.IsAlive()) return true;
        }
        return false;
    }
}