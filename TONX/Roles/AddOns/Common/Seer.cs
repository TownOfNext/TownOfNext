using TONX.Attributes;
using UnityEngine;
using static TONX.Options;

namespace TONX.Roles.AddOns.Common;
public static class Seer
{
    private static readonly int Id = 80900;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Seer);
    private static List<byte> playerIdList = new();

    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.Seer);
        AddOnsAssignData.Create(Id + 10, CustomRoles.Seer, true, true, true);
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