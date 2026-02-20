public class AvailableRolesData(
    int impNum,
    int neutralNum,
    List<CustomRoles> roleOn,
    List<CustomRoles> impOn,
    List<CustomRoles> neutralOn,
    List<CustomRoles> roleRate,
    List<CustomRoles> impRate,
    List<CustomRoles> neutralRate
)
{
    public int optImpNum = impNum;
    public int optNeutralNum = neutralNum;

    public List<CustomRoles> roleOnList = roleOn;
    public List<CustomRoles> ImpOnList = impOn;
    public List<CustomRoles> NeutralOnList = neutralOn;

    public List<CustomRoles> roleRateList = roleRate;
    public List<CustomRoles> ImpRateList = impRate;
    public List<CustomRoles> NeutralRateList = neutralRate;

    public bool ReplaceRole(CustomRoles oldRole, CustomRoles newRole)
    {
        var lists = new[] { roleOnList, ImpOnList, NeutralOnList, roleRateList, ImpRateList, NeutralRateList };
        foreach (var list in lists)
        {
            if (list.Remove(oldRole))
            {
                list.Add(newRole);
                return true;
            }
        }
        return false;
    }
    public bool RemoveRole(CustomRoles role)
    {
        var lists = new[] { roleOnList, ImpOnList, NeutralOnList, roleRateList, ImpRateList, NeutralRateList };
        foreach (var list in lists)
        {
            if (list.Remove(role)) return true;
        }
        return false;
    }
}