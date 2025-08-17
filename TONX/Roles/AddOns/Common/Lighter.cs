using TONX.Attributes;
using UnityEngine;
using static TONX.Options;

namespace TONX.Roles.AddOns.Common;
public static class Lighter
{
    private static readonly int Id = 82100;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Lighter);
    private static List<byte> playerIdList = new();

    public static OptionItem OptionVistion;

    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.Lighter);
        AddOnsAssignData.Create(Id + 10, CustomRoles.Lighter, true, true, true);
        OptionVistion = FloatOptionItem.Create(Id + 20, "LighterVision", new(0.5f, 5f, 0.25f), 1.5f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lighter])
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