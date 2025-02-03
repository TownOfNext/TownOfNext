namespace TONX.Roles.Core.Interfaces;

/// <summary>
/// 持有赌怪能力的接口
/// 为什么要搞个接口？我也不知道 :P
/// </summary>
public interface IGuesser
{
    public static string GetFormatString()
    {
        string text = GetString("PlayerIdList");
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            string id = pc.PlayerId.ToString();
            string name = pc.GetRealName();
            text += $"\n{id} → {name}";
        }
        return text;
    }
}