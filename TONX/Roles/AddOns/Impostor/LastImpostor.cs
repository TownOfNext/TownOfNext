using TONX.Attributes;
using TONX.Roles.Core.Interfaces;
using static TONX.Options;

namespace TONX.Roles.AddOns.Impostor;
public static class LastImpostor
{
    private static readonly int Id = 80000;
    public static byte currentId = byte.MaxValue;
    public static OptionItem KillCooldown;
    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.LastImpostor, RoleSpwanToggle, false);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 1f), 15f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastImpostor])
            .SetValueFormat(OptionFormat.Seconds);
    }
    [GameModuleInitializer]
    public static void Init() => currentId = byte.MaxValue;
    public static void Add(byte id) => currentId = id;
    public static void SetKillCooldown()
    {
        if (currentId == byte.MaxValue) return;
        if (!Main.AllPlayerKillCooldown.TryGetValue(currentId, out var x) || KillCooldown.GetFloat() >= x) return;
        Main.AllPlayerKillCooldown[currentId] = KillCooldown.GetFloat();
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
        if (currentId != byte.MaxValue) return;
        if (CurrentGameMode != CustomGameMode.Standard
        || !CustomRoles.LastImpostor.IsEnable() || Main.AliveImpostorCount != 1)
            return;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (CanBeLastImpostor(pc))
            {
                pc.RpcSetCustomRole(CustomRoles.LastImpostor);
                Add(pc.PlayerId);
                SetKillCooldown();
                pc.SyncSettings();
                Utils.NotifyRoles();
                break;
            }
        }
    }
}