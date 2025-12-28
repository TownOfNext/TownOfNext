using System.Text;

namespace TONX.Roles.Core.Descriptions;

public static class AddonDescription
{
    public static string FullFormatHelpByPlayer(PlayerControl player)
    {
        var builder = new StringBuilder(512);
        var subRoles = player?.GetCustomSubRoles();
        if (CustomRoles.Neptune.IsExist() && !subRoles.Contains(CustomRoles.Lovers) && !player.Is(CustomRoles.GM) && !player.Is(CustomRoles.Neptune))
        {
            subRoles.Add(CustomRoles.Lovers);
        }

        foreach (var subRole in subRoles)
        {
            if (subRoles.IndexOf(subRole) != 0) builder.AppendFormat("<size={0}>\n", RoleDescription.BlankLineSize);
            var description = subRole.GetRoleInfo()?.Description;
            if (description != null) builder.Append(description.FullFormatHelp);
        }

        return builder.ToString();
    }
}
