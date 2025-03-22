using AmongUs.GameOptions;
using HarmonyLib;
using System.Linq;
using TONX.Modules;
using TONX.Roles.Core;
using UnityEngine;
using static TONX.Translator;

namespace TONX.Roles.Crewmate;
public sealed class Grenadier : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Grenadier),
            player => new Grenadier(player),
            CustomRoles.Grenadier,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            22000,
            SetupOptionItem,
            "gr|擲雷兵|掷雷|闪光弹",
            "#3c4a16"
        );
    public Grenadier(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionSkillCooldown;
    static OptionItem OptionSkillDuration;
    public static OptionItem OptionCauseVision;
    static OptionItem OptionCanAffectNeutral;
    enum OptionName
    {
        GrenadierSkillCooldown,
        GrenadierSkillDuration,
        GrenadierCauseVision,
        GrenadierCanAffectNeutral,
    }

    private float SkillTimer;
    private float MadSkillTimer;
    private static void SetupOptionItem()
    {
        OptionSkillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.GrenadierSkillCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSkillDuration = FloatOptionItem.Create(RoleInfo, 11, OptionName.GrenadierSkillDuration, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCauseVision = FloatOptionItem.Create(RoleInfo, 12, OptionName.GrenadierCauseVision, new(0f, 5f, 0.05f), 0.3f, false)
            .SetValueFormat(OptionFormat.Multiplier);
        OptionCanAffectNeutral = BooleanOptionItem.Create(RoleInfo, 13, OptionName.GrenadierCanAffectNeutral, false, false);
    }
    public override void Add()
    {
        SkillTimer = -1f;
        MadSkillTimer = -1f;
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = SkillTimer >= 0 || MadSkillTimer >= 0 ?
            OptionSkillDuration.GetFloat() : OptionSkillCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }
    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("GrenadierVetnButtonText");
        return true;
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (Player.Is(CustomRoles.Madmate))
        {
            MadSkillTimer = 0f;
            Main.AllPlayerControls.Where(x => x.IsModClient()).Where(x => !x.IsImp() && !x.Is(CustomRoles.Madmate)).Do(x => x.RPCPlayCustomSound("FlashBang"));
        }
        else
        {
            SkillTimer = 0f;
            Main.AllPlayerControls.Where(x => x.IsModClient()).Where(x => x.IsImp() || (x.IsNeutral() && OptionCanAffectNeutral.GetBool())).Do(x => x.RPCPlayCustomSound("FlashBang"));
        }
        if (!Player.IsModClient()) Player.RpcProtectedMurderPlayer();
        Player.RPCPlayCustomSound("FlashBang");
        Player.Notify(GetString("GrenadierSkillInUse"), OptionSkillDuration.GetFloat());
        Utils.MarkEveryoneDirtySettings();
        return false;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (SkillTimer == -1 && MadSkillTimer == -1) return;
        if (SkillTimer > OptionSkillDuration.GetFloat())
        {
            SkillTimer = -1f;
            Player.RpcProtectedMurderPlayer();
            Player.SyncSettings();
            Player.RpcResetAbilityCooldown();
            Player.Notify(GetString("GrenadierSkillStop"));
            Utils.MarkEveryoneDirtySettings();
        }
        if (MadSkillTimer > OptionSkillDuration.GetFloat())
        {
            MadSkillTimer = -1f;
            Player.RpcProtectedMurderPlayer();
            Player.SyncSettings();
            Player.RpcResetAbilityCooldown();
            Player.Notify(GetString("GrenadierSkillStop"));
            Utils.MarkEveryoneDirtySettings();
        }
        if (SkillTimer >= 0) SkillTimer += Time.fixedDeltaTime;
        if (MadSkillTimer >= 0) MadSkillTimer += Time.fixedDeltaTime;
    }
    public static bool IsBlinding(PlayerControl target)
    {
        foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Grenadier)))
        {
            if (pc.GetRoleClass() is not Grenadier roleClass) continue;
            if (roleClass.SkillTimer >= 0 && roleClass.SkillTimer <= OptionSkillDuration.GetFloat())
            {
                if (target.IsImp() || target.Is(CustomRoles.Madmate)
                    || (target.IsNeutral() && OptionCanAffectNeutral.GetBool()))
                {
                    return true;
                }
            }
            else if (roleClass.MadSkillTimer >= 0 && roleClass.MadSkillTimer <= OptionSkillDuration.GetFloat())
            {
                if (!target.IsImp() && !target.Is(CustomRoles.Madmate))
                    return true;
            }
        }
        return false;
    }
}