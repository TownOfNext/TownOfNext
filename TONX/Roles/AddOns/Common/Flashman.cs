using TONX.Attributes;
using UnityEngine;
using static TONX.Options;

namespace TONX.Roles.AddOns.Common;
public static class Flashman
{
    private static readonly int Id = 80500;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Flashman);
    private static List<byte> playerIdList = new();

    public static OptionItem OptionSpeed;

    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.OtherRoles, CustomRoles.Flashman);
        AddOnsAssignData.Create(Id + 10, TabGroup.OtherRoles, CustomRoles.Flashman, true, true, true);
        OptionSpeed = FloatOptionItem.Create(Id + 20, "FlashmanSpeed", new(0.25f, 5f, 0.25f), 2.5f, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Flashman])
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