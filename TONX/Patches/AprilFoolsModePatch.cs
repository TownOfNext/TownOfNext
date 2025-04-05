using HarmonyLib;

namespace TONX;

public static class AprilFoolsModePatch
{
    public static bool FlipSkeld => AprilFoolsMode.ShouldFlipSkeld();
    public static bool HorseMode => AprilFoolsMode.ShouldFlipSkeld();
    public static bool LongMode => AprilFoolsMode.ShouldFlipSkeld();
}