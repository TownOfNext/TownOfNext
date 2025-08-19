using TONX.Attributes;
using TONX.Roles.GameMode;
using UnityEngine;

namespace TONX.GameModes;

internal static class SoloKombatManager
{
    public static int RoundTime;

    //Options
    public static OptionItem KB_GameTime;
    public static OptionItem KB_ATKCooldown;
    public static OptionItem KB_HPMax;
    public static OptionItem KB_ATK;
    public static OptionItem KB_RecoverAfterSecond;
    public static OptionItem KB_RecoverPerSecond;
    public static OptionItem KB_ResurrectionWaitingTime;
    public static OptionItem KB_KillBonusMultiplier;

    public static void SetupCustomOption()
    {
        TextOptionItem.Create(10_100_001, "MenuTitle.GameMode", TabGroup.GameSettings)
            .SetGameMode(CustomGameMode.SoloKombat)
            .SetColor(Utils.GetRoleColor(CustomRoles.KB_Normal));
        KB_GameTime = IntegerOptionItem.Create(10_000_001, "KB_GameTime", new(30, 300, 5), 180, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.SoloKombat)
            .SetColor(Utils.GetRoleColor(CustomRoles.KB_Normal))
            .SetValueFormat(OptionFormat.Seconds)
            .SetHeader(true);
        KB_ATKCooldown = FloatOptionItem.Create(10_000_002, "KB_ATKCooldown", new(1f, 10f, 0.1f), 1f, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.SoloKombat)
            .SetColor(Utils.GetRoleColor(CustomRoles.KB_Normal))
            .SetValueFormat(OptionFormat.Seconds);
        KB_HPMax = FloatOptionItem.Create(10_000_003, "KB_HPMax", new(10f, 990f, 5f), 100f, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.SoloKombat)
            .SetColor(Utils.GetRoleColor(CustomRoles.KB_Normal))
            .SetValueFormat(OptionFormat.Health);
        KB_ATK = FloatOptionItem.Create(10_000_004, "KB_ATK", new(1f, 100f, 1f), 8f, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.SoloKombat)
            .SetColor(Utils.GetRoleColor(CustomRoles.KB_Normal))
            .SetValueFormat(OptionFormat.Health);
        KB_RecoverPerSecond = FloatOptionItem.Create(10_000_005, "KB_RecoverPerSecond", new(1f, 180f, 1f), 2f, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.SoloKombat)
            .SetColor(Utils.GetRoleColor(CustomRoles.KB_Normal))
            .SetValueFormat(OptionFormat.Health);
        KB_RecoverAfterSecond = IntegerOptionItem.Create(10_000_006, "KB_RecoverAfterSecond", new(0, 60, 1), 8, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.SoloKombat)
            .SetColor(Utils.GetRoleColor(CustomRoles.KB_Normal))
            .SetValueFormat(OptionFormat.Seconds);
        KB_ResurrectionWaitingTime = IntegerOptionItem.Create(10_000_007, "KB_ResurrectionWaitingTime", new(5, 990, 1), 15, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.SoloKombat)
            .SetColor(Utils.GetRoleColor(CustomRoles.KB_Normal))
            .SetValueFormat(OptionFormat.Seconds);
        KB_KillBonusMultiplier = FloatOptionItem.Create(10_000_008, "KB_KillBonusMultiplier", new(0.25f, 5f, 0.25f), 1.25f, TabGroup.GameSettings, false)
            .SetGameMode(CustomGameMode.SoloKombat)
            .SetColor(Utils.GetRoleColor(CustomRoles.KB_Normal))
            .SetValueFormat(OptionFormat.Multiplier);
    }

    [GameModuleInitializer]
    public static void Init()
    {
        if (Options.CurrentGameMode != CustomGameMode.SoloKombat) return;
        RoundTime = KB_GameTime.GetInt() + 8;
    }
    
    private static Dictionary<byte, int> KBScore = new();
    public static string GetDisplayScore(byte playerId)
    {
        int rank = GetRankOfScore(playerId);
        string score = KBScore.TryGetValue(playerId, out var s) ? $"{s}" : "Invalid";
        string text = string.Format(GetString("KBDisplayScore"), rank.ToString(), score);
        Color color = Utils.GetRoleColor(CustomRoles.KB_Normal);
        return Utils.ColorString(color, text);
    }
    public static int GetRankOfScore(byte playerId)
    {
        if (!GameStates.IsLobby)
        {
            foreach (var player in Main.AllPlayerControls)
            {
                var role = player.GetRoleClass() as KB_Normal;
                KBScore.TryAdd(player.PlayerId, role?.Score ?? -255);
                KBScore[player.PlayerId] = role?.Score ?? -255;
            }
        }
        try
        {
            int ms = KBScore[playerId];
            int rank = 1 + KBScore.Values.Where(x => x > ms).Count();
            rank += KBScore.Where(x => x.Value == ms).ToList().IndexOf(new(playerId, ms));
            return rank;
        }
        catch
        {
            return Main.AllPlayerControls.Count();
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class FixedUpdatePatch
    {
        private static long LastFixedUpdate;
        public static void Postfix()
        {
            if (!GameStates.IsInTask || Options.CurrentGameMode != CustomGameMode.SoloKombat) return;

            if (!AmongUsClient.Instance.AmHost) return;
            if (LastFixedUpdate == Utils.GetTimeStamp()) return;
            LastFixedUpdate = Utils.GetTimeStamp();
            // 减少全局倒计时
            RoundTime--;
        }
    }
}