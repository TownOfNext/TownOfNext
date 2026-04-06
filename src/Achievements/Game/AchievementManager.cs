using System.Text;
using System.Text.Json;
using TONX.Achievements.Player;

namespace TONX.Achievements.Game;

public static class AchievementManager
{
    public static string ServerBaseUrl { get; set; } = "https://achievement.tonx.cc";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };
    
    public static async Task TriggerUnlockAsync(string friendCode, int achievementId, string achievementName, string description = "")
    {
        PlayerAchievementData.AddToCache(friendCode, new AchievementRecord
        {
            Id = achievementId,
            Name = achievementName,
            UnlockedAt = DateTime.UtcNow.ToString("o")
        });

        try
        {
            var body = JsonSerializer.Serialize(new
            {
                friend_code = friendCode,
                achievement_id = achievementId,
                achievement_name = achievementName,
                description = description
            });
            var content  = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await Http.PostAsync($"{ServerBaseUrl}/api/achievements/unlock", content);

            if (response.IsSuccessStatusCode)
                Logger.Info($"Unlock Successfully：friendCode={friendCode} id={achievementId}", "AchievementManager");
        }
        catch (Exception ex)
        {
            Logger.Warn(ex.Message, "AchievementManager");
        }
    }
    
    public static async Task FetchAndDisplayAchievementsAsync(PlayerControl player)
    {
        if (player == null) return;
        string friendCode = player.FriendCode.Replace("#","%23");// #需要转成Url编码

        Utils.SendMessage(GetString("Achievement.LoadingFromServer"), player.PlayerId, $"<color=#FFD700>{GetString("AchievementMsgTitle")}</color>");

        try
        {
            var response = await Http.GetAsync($"{ServerBaseUrl}/api/achievements/{Uri.EscapeDataString(friendCode)}");
            if (!response.IsSuccessStatusCode)
            {
                Utils.SendMessage(string.Format(GetString("Achievement.LoadingFromServer.Fail"),(int)response.StatusCode), player.PlayerId, $"<color=#FFD700>{GetString("AchievementMsgTitle")}</color>");
                return;
            }

            string json = await response.Content.ReadAsStringAsync();
            var records = JsonSerializer.Deserialize<List<AchievementRecord>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            
            PlayerAchievementData.UpdateCache(friendCode, records);

            if (!records.Any())
            {
                Utils.SendMessage(GetString("Achievement.LoadingFromServer.Empty"), player.PlayerId, $"<color=#FFD700>{GetString("AchievementMsgTitle")}</color>");
                return;
            }
            
            var sb = new StringBuilder();
            sb.AppendLine($"{string.Format(GetString("Achievement.Info.AllCount"),records.Count)}\n");
            foreach (var r in records)
            {
                var def = AchievementRegistry.GetById(r.Id);
                string colorHex = def?.TitleColorHex ?? "#FFFFFF";
                sb.AppendLine($"<color={colorHex}>【{r.Id}】{r.Name}</color>");
                sb.AppendLine($"<size=70%>{string.Format(GetString("Achievement.Info.UnlockedAt"),r.UnlockedAt)}</size>\n");
            }
            sb.AppendLine($"\n{GetString("Achievement.Tip1")}");

            Utils.SendMessage(sb.ToString().TrimEnd(), player.PlayerId, $"<color=#FFD700>{GetString("AchievementMsgTitle")}</color>");
        }
        catch (Exception ex)
        {
            Logger.Warn($"FetchAndDisplay Error：{ex.Message}", "AchievementManager");
            Utils.SendMessage(GetString("Achievement.LoadingFromServer.ServerBomb"), player.PlayerId, $"<color=#FF4444>{GetString("AchievementMsgTitle")}</color>");
        }
    }
    
    public static async Task WearTitleAsync(PlayerControl player, int achievementId)
    {
        if (player == null) return;
        
        if (achievementId <= 0)
        {
            AchievementTitleHandler.SendTitleSyncRpc(player.PlayerId, 0);
            Utils.SendMessage(GetString("Achievement.Command.UnWear"), player.PlayerId, $"<color=#FFD700>{GetString("AchievementMsgTitle")}</color>");
            return;
        }

        var def = AchievementRegistry.GetById(achievementId);
        if (def == null)
        {
            Utils.SendMessage(string.Format(GetString("Achievement.Command.Fail"),achievementId), player.PlayerId, $"<color=#FFD700>{GetString("AchievementMsgTitle")}</color>");
            return;
        }

        bool owned = PlayerAchievementData.HasAchievement(player.FriendCode, achievementId);

        if (!owned)
        {
            try
            {
                var response = await Http.GetAsync($"{ServerBaseUrl}/api/achievements/{Uri.EscapeDataString(player.FriendCode.Replace("#","%23"))}");
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var records = JsonSerializer.Deserialize<List<AchievementRecord>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                    PlayerAchievementData.UpdateCache(player.FriendCode, records);
                    owned = records.Any(r => r.Id == achievementId);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"WearTitle Verify Error：{ex.Message}", "AchievementManager");
                Utils.SendMessage(GetString("Achievement.LoadingFromServer.ServerBomb"), player.PlayerId, $"<color=#FF4444>{GetString("AchievementMsgTitle")}</color>");
                return;
            }
        }

        if (!owned)
        {
            Utils.SendMessage(string.Format(GetString("Achievement.Command.NoBelong"),def.Id), player.PlayerId, $"<color=#FFD700>{GetString("AchievementMsgTitle")}</color>");
            return;
        }
        
        AchievementTitleHandler.SendTitleSyncRpc(player.PlayerId, achievementId);
        Utils.SendMessage(string.Format(GetString("Achievement.Command.Wear"),def.TitleColorHex,def.TitleDisplay),
            player.PlayerId, $"<color=#FFD700>{GetString("AchievementMsgTitle")}</color>");
    }
}
