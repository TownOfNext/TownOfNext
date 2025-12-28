namespace TONX.Roles.Core;

public class HiddenRoleInfo(int probability, CustomRoles? targetRole)
{
    public readonly int Probability = probability;
    public CustomRoles? TargetRole = targetRole;
}