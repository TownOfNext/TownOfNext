using TONX.Attributes;
using UnityEngine;
using static TONX.Options;

namespace TONX.Roles.AddOns.Crewmate;
public static class YouTuber
{
    private static readonly int Id = 80700;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.YouTuber);
    private static List<byte> playerIdList = new();

    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.YouTuber);
        AddOnsAssignData.Create(Id + 10, CustomRoles.YouTuber, true, false, false);
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