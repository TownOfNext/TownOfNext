using TONX.Attributes;
using UnityEngine;
using static TONX.Options;

namespace TONX.Roles.AddOns.Common;
public static class Bewilder
{
    private static readonly int Id = 81200;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Bewilder);
    private static List<byte> playerIdList = new();

    public static OptionItem OptionVision;

    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.Bewilder);
        AddOnsAssignData.Create(Id + 10, CustomRoles.Bewilder, true, true, true);
        OptionVision = FloatOptionItem.Create(Id + 20, "BewilderVision", new(0f, 5f, 0.05f), 0.6f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bewilder])
            .SetValueFormat(OptionFormat.Multiplier);
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