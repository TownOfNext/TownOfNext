using TONX.Attributes;
using UnityEngine;
using static TONX.Options;

namespace TONX.Roles.AddOns.Common;
public static class Egoist
{
    private static readonly int Id = 80800;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Egoist);
    private static List<byte> playerIdList = new();

    public static OptionItem OptionImpEgoVisibalToAllies;

    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.Egoist);
        AddOnsAssignData.Create(Id + 10, CustomRoles.Egoist, true, true, false);
        OptionImpEgoVisibalToAllies = BooleanOptionItem.Create(Id + 20, "ImpEgoistVisibalToAllies", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Egoist]);
    }
    [GameModuleInitializer]
    public static void Init()
    {
        playerIdList = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);

}