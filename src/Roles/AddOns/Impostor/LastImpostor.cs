using TONX.Roles.Core.Interfaces;
using static TONX.Options;

namespace TONX.Roles.AddOns.Impostor;
public sealed class LastImpostor : AddonBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(LastImpostor),
            player => new LastImpostor(player),
            CustomRoles.LastImpostor,
            80000,
            SetupCustomOption,
            "li|绝境",
            "#ff1919",
            assignMode: RoleAssignMode.Toggle
        );
    public LastImpostor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public static OptionItem KillCooldown;
    enum OptionName
    {
        KillCooldown
    }

    private static void SetupCustomOption()
    {
        KillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.KillCooldown, new(0f, 180f, 1f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    
    public static void SetKillCooldown(PlayerControl player)
    {
        if (!Main.AllPlayerKillCooldown.TryGetValue(player.PlayerId, out var x) || KillCooldown.GetFloat() >= x) return;
        Main.AllPlayerKillCooldown[player.PlayerId] = KillCooldown.GetFloat();
    }
    public static bool CanBeLastImpostor(PlayerControl pc)
    {
        if (!pc.IsAlive() || pc.Is(CustomRoles.LastImpostor) || !pc.Is(CustomRoleTypes.Impostor))
        {
            return false;
        }
        if (pc.GetRoleClass() is IImpostor impostor)
        {
            return impostor.CanBeLastImpostor;
        }
        return true;
    }
    public static void SetSubRole()
    {
        //ラストインポスターがすでにいれば処理不要
        if (CustomRoles.LastImpostor.IsExist(true)) return;
        if (CurrentGameMode != CustomGameMode.Standard
        || !CustomRoles.LastImpostor.IsEnable() || Main.AliveImpostorCount != 1)
            return;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (CanBeLastImpostor(pc))
            {
                pc.RpcSetCustomRole(CustomRoles.LastImpostor);
                SetKillCooldown(pc);
                pc.SyncSettings();
                Utils.NotifyRoles();
                break;
            }
        }
    }
}