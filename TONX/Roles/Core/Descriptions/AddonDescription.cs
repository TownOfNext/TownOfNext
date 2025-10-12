using System.Text;

namespace TONX.Roles.Core.Descriptions;

public static class AddonDescription
{
    public static string FullFormatHelpByPlayer(PlayerControl player, bool withSettings = true)
    {
        var builder = new StringBuilder(512);
        var subRoles = player?.GetCustomSubRoles();
        if (CustomRoles.Neptune.IsExist() && !subRoles.Contains(CustomRoles.Lovers) && !player.Is(CustomRoles.GM) && !player.Is(CustomRoles.Neptune))
        {
            subRoles.Add(CustomRoles.Lovers);
        }

        foreach (var subRole in subRoles)
        {
            if (subRoles.IndexOf(subRole) != 0) builder.AppendFormat("<size={0}>\n", BlankLineSize);
            builder.AppendFormat("<size={0}>{1}\n", FirstHeaderSize, GetRoleString(subRole.ToString()).Color(Utils.GetRoleColor(subRole).ToReadableColor()));
            builder.AppendFormat("<size={0}>{1}\n", BodySize, GetString($"{subRole}InfoLong"));
            // 职业设定
            if (withSettings && Options.CustomRoleSpawnChances.TryGetValue(subRole, out var opt))
                Utils.ShowChildrenSettings(opt, ref builder, forChat: true);
        }

        return builder.ToString();
    }
    public static string FullFormatHelpBySubRole(CustomRoles subRole, bool withSettings = true)
    {
        var builder = new StringBuilder(512);
        builder.AppendFormat("<size={0}>\n", BlankLineSize);
        builder.AppendFormat("<size={0}>{1}\n", FirstHeaderSize, GetRoleString(subRole.ToString()).Color(Utils.GetRoleColor(subRole).ToReadableColor()));
        builder.AppendFormat("<size={0}>{1}\n", BodySize, GetString($"{subRole}InfoLong"));
        // 职业设定
        if (withSettings && Options.CustomRoleSpawnChances.TryGetValue(subRole, out var opt))
            Utils.ShowChildrenSettings(opt, ref builder, forChat: true);


        return builder.ToString();
    }
    public const string FirstHeaderSize = "130%";
    public const string SecondHeaderSize = "100%";
    public const string BodySize = "70%";
    public const string BlankLineSize = "30%";
}
