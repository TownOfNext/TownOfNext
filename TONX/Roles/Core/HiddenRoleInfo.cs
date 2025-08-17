namespace TONX.Roles.Core;

public class HiddenRoleInfo(int probability, CustomRoles? targetRole)
{
    public int Probability = probability;
    public CustomRoles? TargetRole = targetRole;
}