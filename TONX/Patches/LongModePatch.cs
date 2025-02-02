using HarmonyLib;

namespace TONX;

//来源：https://github.com/ykundesu/SuperNewRoles/blob/master/SuperNewRoles/Patches/LongModePatch.cs
[HarmonyPatch(typeof(AprilFoolsMode), nameof(AprilFoolsMode.ShouldLongAround))]
public static class LongModePatch
{
    public static bool Prefix(ref bool __result)
    {
        __result = Main.LongMode.Value;
        return false;
    }
}