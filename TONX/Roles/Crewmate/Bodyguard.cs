using AmongUs.GameOptions;
using UnityEngine;

namespace TONX.Roles.Crewmate;
public sealed class Bodyguard : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Bodyguard),
            player => new Bodyguard(player),
            CustomRoles.Bodyguard,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            21900,
            SetupOptionItem,
            "bg|保鏢",
            "#185abd"
        );
    public Bodyguard(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CustomRoleManager.OnCheckMurderPlayerOthers_After.Add(OnCheckMurderPlayerOthers_After);
    }

    static OptionItem OptionRadius;
    enum OptionName
    {
        BodyguardProtectRadius
    }

    private static void SetupOptionItem()
    {
        OptionRadius = FloatOptionItem.Create(RoleInfo, 10, OptionName.BodyguardProtectRadius, new(0.5f, 5f, 0.5f), 1.5f, false)
            .SetValueFormat(OptionFormat.Multiplier);
    }
    private static bool OnCheckMurderPlayerOthers_After(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (info.IsSuicide || target.Is(CustomRoles.Bodyguard)) return true;

        foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != target.PlayerId))
        {
            var pos = target.transform.position;
            var dis = Vector2.Distance(pos, pc.transform.position);
            if (dis > OptionRadius.GetFloat()) continue;
            if (pc.Is(CustomRoles.Bodyguard))
            {
                var roleClass = pc.GetRoleClass() as Bodyguard;
                if (pc.Is(CustomRoles.Madmate) && killer.GetCustomRole().IsImpostorTeam())
                    Logger.Info($"{pc.GetRealName()} 是个叛徒，所以他选择无视杀人现场", "Bodyguard.OnCheckMurderPlayerOthers_After");
                else
                {
                    pc.SetDeathReason(CustomDeathReason.Sacrifice);
                    pc.RpcMurderPlayerV2(killer);
                    killer.RpcMurderPlayerV2(pc);
                    Logger.Info($"{pc.GetRealName()} 挺身而出与歹徒 {killer.GetRealName()} 同归于尽", "Bodyguard.OnCheckMurderPlayerOthers_After");
                    return false;
                }
            }
        }
        return true;
    }

}