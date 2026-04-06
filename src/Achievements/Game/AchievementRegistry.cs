using TONX.Attributes;
using TONX.Achievements.Core.Base;
using TONX.Achievements.Core.Interfaces;
using TONX.Achievements.Roles.Crewmate.Criminologist;

namespace TONX.Achievements.Game;

public static class AchievementRegistry
{
    private static readonly Dictionary<int, AchievementBase> All = new();
    
    [PluginModuleInitializer]
    public static void Initialize()
    {
        All.Clear();
        // 公用成就(0-999)

        // 船员职业成就 (1000–1999)
        Register(new SelfVerify());

        // 内鬼职业成就 (2000–2999)

        // 中立职业成就 (3000–3999)

        // 附加身份成就 (4000–4999)

        Logger.Info($"{All.Count} achievements registered", "AchievementRegistry");
    }
    
    public static AchievementBase GetById(int id)
        => All.TryGetValue(id, out var a) ? a : null;
    
    public static IReadOnlyCollection<AchievementBase> GetAll()
        => All.Values.ToList().AsReadOnly();

    private static void Register(AchievementBase achievement)
    {
        if (All.ContainsKey(achievement.Id))
        {
            Logger.Error($"Checked the same id：{achievement.Id}({achievement.Name}), Skip.", "AchievementRegistry");
            return;
        }
        All[achievement.Id] = achievement;
        Logger.Info($"Registry successfully [{achievement.Id}] {achievement.Name}", "AchievementRegistry");
    }
}
