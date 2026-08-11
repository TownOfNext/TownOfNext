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
        //这咋一股COG味。
        All.Clear();

        var types = System.Reflection.Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(AchievementBase)));

        foreach (var type in types)
        {
            var instance = (AchievementBase)Activator.CreateInstance(type);
            Register(instance);
        }

        Logger.Info($"{All.Count} Achievements Registry", "AchievementRegistry");
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
