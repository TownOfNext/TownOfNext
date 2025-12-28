namespace TONX;

static class CustomGameModesHelper
{
    public static readonly CustomGameMode[] AllModes = EnumHelper.GetAllValues<CustomGameMode>().Where(m => m is not CustomGameMode.All).ToArray();
    public static bool IsEnable(this CustomGameMode mode) => Options.CurrentGameMode == mode;
}