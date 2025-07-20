using HarmonyLib;

using TONX.Roles.Core;

namespace TONX;

//参考：https://github.com/yukieiji/ExtremeRoles/blob/master/ExtremeRoles/Patches/AirShipStatusPatch.cs
[HarmonyPatch(typeof(AirshipStatus), nameof(AirshipStatus.PrespawnStep))]
public static class AirshipStatusPrespawnStepPatch
{
    public static bool Prefix()
    {
        if (PlayerControl.LocalPlayer.Is(CustomRoles.GM))
        {
            RandomSpawn.AirshipSpawn(PlayerControl.LocalPlayer);
            // GM跳过选择出生地
            return false;
        }
        return true;
    }
}