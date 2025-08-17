using AmongUs.GameOptions;
using TONX.Roles.Core.Interfaces;

namespace TONX.Roles.Impostor;
public sealed class Arrogance : RoleBase, IImpostor
{

    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Arrogance),
            player => new Arrogance(player),
            CustomRoles.Arrogance,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            3000,
            SetupOptionItem,
            "ag|狂妄殺手|狂妄"
        );
    public Arrogance(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem DefaultKillCooldown;
    static OptionItem ReduceKillCooldown;
    static OptionItem MinKillCooldown;
    enum OptionName
    {
        ArroganceDefaultKillCooldown,
        ArroganceReduceKillCooldown,
        ArroganceMinKillCooldown,
    }
    private float KillCooldown;
    private static void SetupOptionItem()
    {
        DefaultKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.ArroganceDefaultKillCooldown, new(2.5f, 180f, 2.5f), 65f, false)
            .SetValueFormat(OptionFormat.Seconds);
        ReduceKillCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.ArroganceReduceKillCooldown, new(2.5f, 180f, 2.5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
        MinKillCooldown = FloatOptionItem.Create(RoleInfo, 12, OptionName.ArroganceMinKillCooldown, new(2.5f, 180f, 2.5f), 2.5f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add()
    {
        KillCooldown = DefaultKillCooldown.GetFloat();
    }
    public float CalculateKillCooldown() => KillCooldown;

    public void BeforeMurderPlayerAsKiller(MurderInfo info)
    {
        if (info.IsSuicide) return;
        var killer = info.AttemptKiller;
        KillCooldown = Math.Clamp(KillCooldown - ReduceKillCooldown.GetFloat(), MinKillCooldown.GetFloat(), DefaultKillCooldown.GetFloat());
        killer.ResetKillCooldown();
        killer.SyncSettings();
    }
}