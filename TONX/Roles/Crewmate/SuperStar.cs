using AmongUs.GameOptions;
using UnityEngine;

namespace TONX.Roles.Crewmate;
public sealed class SuperStar : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SuperStar),
            player => new SuperStar(player),
            CustomRoles.SuperStar,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20300,
            SetupOptionItem,
            "ss|明星",
            "#f6f657"
        );
    public SuperStar(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CustomRoleManager.MarkOthers.Add(MarkOthers);
    }

    public static OptionItem OptionEveryoneKnowSuperStar;
    enum OptionName
    {
        EveryOneKnowSuperStar,
    }

    private static void SetupOptionItem()
    {
        OptionEveryoneKnowSuperStar = BooleanOptionItem.Create(RoleInfo, 10, OptionName.EveryOneKnowSuperStar, true, false);
    }
    public static string MarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        return (seen.Is(CustomRoles.SuperStar) && OptionEveryoneKnowSuperStar.GetBool()) ? Utils.ColorString(RoleInfo.RoleColor, "★") : "";
    }
    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seer, ref bool enabled, ref Color roleColor, ref string roleText)
        => enabled |= OptionEveryoneKnowSuperStar.GetBool();
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (info.IsSuicide) return true;
        return !Main.AllAlivePlayerControls.Any(pc =>
            !Is(pc) &&
            pc != killer &&
            Vector2.Distance(pc.GetTruePosition(), target.GetTruePosition()) < 2f
            );
    }
    public static bool KnowTargetRoleColor(PlayerControl target, bool isMeeting)
        => target.Is(CustomRoles.SuperStar) && OptionEveryoneKnowSuperStar.GetBool();
}