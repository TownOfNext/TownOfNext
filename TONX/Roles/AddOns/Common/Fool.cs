using TONX.Attributes;
using UnityEngine;
using static TONX.Options;

namespace TONX.Roles.AddOns.Common;
public static class Fool
{
    private static readonly int Id = 81300;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Fool);
    private static List<byte> playerIdList = new();

    public static OptionItem OptionImpFoolCanNotSabotage;
    public static OptionItem OptionImpFoolCanNotOpenDoor;

    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.Fool);
        AddOnsAssignData.Create(Id + 10, CustomRoles.Fool, true, true, true);
        OptionImpFoolCanNotSabotage = BooleanOptionItem.Create(Id + 20, "ImpFoolCanNotSabotage", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fool]);
        OptionImpFoolCanNotOpenDoor = BooleanOptionItem.Create(Id + 21, "FoolCanNotOpenDoor", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Fool]);
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