using TONX.Attributes;

namespace TONX.GameModes.Core;

public static class CustomGameModeManager
{
    public static Type[] AllModesClassType;
    public static Dictionary<CustomGameMode, GameModeInfo> AllModesInfo = new(CustomGameModesHelper.AllModes.Length);
    public static Dictionary<CustomGameMode, GameModeBase> AllModesClass = new(1);

    public static GameModeInfo GetModeInfo(this CustomGameMode mode) => AllModesInfo.ContainsKey(mode) ? AllModesInfo[mode] : null;
    public static GameModeBase GetModeClass(this CustomGameMode mode) => AllModesClass.ContainsKey(mode) ? AllModesClass[mode] : null;

    // ==初始化处理 ==
    [GameModuleInitializer]
    public static void Initialize()
    {
        AllModesClass.Clear();
        Options.CurrentGameMode.GetModeInfo()?.CreateInstance().Add();
    }
    /// <summary>
    /// 全部对象的销毁事件
    /// </summary>
    public static void Dispose()
    {
        Logger.Info($"Dispose ActiveModes", "CustomGameModeManager");
        AllModesClass.Values.ToArray().Do(modeClass => modeClass.Dispose());
    }

    private static Dictionary<byte, long> LastSecondsUpdate = new();
    public static void OnFixedUpdate(PlayerControl player)
    {
        if (GameStates.IsInTask)
        {
            var now = Utils.GetTimeStamp();
            LastSecondsUpdate.TryAdd(player.PlayerId, 0);
            if (LastSecondsUpdate[player.PlayerId] != now)
            {
                Options.CurrentGameMode.GetModeClass()?.OnSecondsUpdate(player, now);
                LastSecondsUpdate[player.PlayerId] = now;
            }
            Options.CurrentGameMode.GetModeClass()?.OnFixedUpdate(player);
        }
    }
}

[Flags]
public enum CustomGameMode
{
    Standard = 0x01,
    SoloKombat = 0x02,
    RoleDraft = 0x03,
    All = int.MaxValue
}