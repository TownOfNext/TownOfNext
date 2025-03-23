using AmongUs.GameOptions;
using TONX.Modules;
using TONX.Roles.Core;
using UnityEngine;
using static TONX.Translator;

namespace TONX.Roles.Crewmate;
public sealed class Veteran : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Veteran),
            player => new Veteran(player),
            CustomRoles.Veteran,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            21800,
            SetupOptionItem,
            "ve",
            "#a77738"
        );
    public Veteran(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionSkillCooldown;
    static OptionItem OptionSkillDuration;
    static OptionItem OptionSkillNums;
    enum OptionName
    {
        VeteranSkillCooldown,
        VeteranSkillDuration,
        VeteranSkillMaxOfUseage,
    }

    private int SkillLimit;
    private float SkillTimer;
    private static void SetupOptionItem()
    {
        OptionSkillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.VeteranSkillCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSkillDuration = FloatOptionItem.Create(RoleInfo, 11, OptionName.VeteranSkillDuration, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSkillNums = IntegerOptionItem.Create(RoleInfo, 12, OptionName.VeteranSkillMaxOfUseage, new(1, 99, 1), 5, false)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Add()
    {
        SkillLimit = OptionSkillNums.GetInt();
        SkillTimer = -1f;
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown =
            SkillTimer >= 0 ? OptionSkillDuration.GetFloat() :
            (SkillLimit <= 0 ? 255f : OptionSkillCooldown.GetFloat());
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }
    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("VeteranVetnButtonText");
        return true;
    }
    public override bool GetAbilityButtonSprite(out string buttonName)
    {
        buttonName = "Veteran";
        return true;
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (SkillLimit >= 1)
        {
            SkillLimit--;
            SkillTimer = 0f;
            if (!Player.IsModClient()) Player.RpcProtectedMurderPlayer(Player);
            Player.RPCPlayCustomSound("Gunload");
            Player.Notify(GetString("VeteranOnGuard"), OptionSkillDuration.GetFloat());
            Player.MarkDirtySettings();
        }
        else
        {
            Player.Notify(GetString("SkillMaxUsage"));
        }
        return false;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (SkillTimer == -1f) return;
        if (SkillTimer > OptionSkillDuration.GetFloat())
        {
            SkillTimer = -1f;
            player.RpcProtectedMurderPlayer();
            player.SyncSettings();
            player.RpcResetAbilityCooldown();
            player.Notify(string.Format(GetString("VeteranOffGuard"), SkillLimit));
        }
        else SkillTimer += Time.fixedDeltaTime;
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        if (info.IsSuicide) return true;
        if (SkillTimer >= 0 && SkillTimer <= OptionSkillDuration.GetFloat())
        {
            var (killer, target) = info.AttemptTuple;
            target.RpcMurderPlayerV2(killer);
            Logger.Info($"{target.GetRealName()} 老兵反弹击杀：{killer.GetRealName()}", "Veteran.OnCheckMurderAsTarget");
            return false;
        }
        return true;
    }
}