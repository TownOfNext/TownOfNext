using AmongUs.GameOptions;

namespace TONX.Modules;

internal static class CustomRoleSelector
{
    public static Dictionary<PlayerControl, CustomRoles> RoleResult;
    public static IReadOnlyList<CustomRoles> AllRoles => RoleResult.Values.ToList();

    public static void SelectCustomRoles()
    {
        RoleResult = new();
        Options.CurrentGameMode.GetModeClass()?.SelectCustomRoles(ref RoleResult);
    }

    public static int addScientistNum = 0;
    public static int addEngineerNum = 0;
    public static int addNoisemakerNum = 0; 
    public static int addTrackerNum = 0;
    public static int addDetectiveNum = 0; 
    public static int addShapeshifterNum = 0;
    public static int addPhantomNum = 0;
    public static int addViperNum = 0;
    public static void CalculateVanillaRoleCount()
    {
        // 计算原版特殊职业数量
        addScientistNum = 0;
        addEngineerNum = 0;
        addNoisemakerNum = 0;
        addTrackerNum = 0;
        addDetectiveNum = 0;
        addShapeshifterNum = 0;
        addPhantomNum = 0;
        addViperNum = 0;

        foreach (var role in AllRoles)
        {
            switch (role.GetRoleInfo()?.BaseRoleType.Invoke())
            {
                case RoleTypes.Scientist: addScientistNum++; break;
                case RoleTypes.Engineer: addEngineerNum++; break;
                case RoleTypes.Noisemaker: addNoisemakerNum++; break;
                case RoleTypes.Tracker: addTrackerNum++; break;
                case RoleTypes.Detective: addDetectiveNum++; break;
                case RoleTypes.Shapeshifter: addShapeshifterNum++; break;
                case RoleTypes.Phantom: addPhantomNum++; break;
                case RoleTypes.Viper: addViperNum++; break;
            }
        }
    }
    public static int GetRoleTypesCount(RoleTypes type)
    {
        return type switch
        {
            RoleTypes.Scientist => addScientistNum,
            RoleTypes.Engineer => addEngineerNum,
            RoleTypes.Noisemaker => addNoisemakerNum,
            RoleTypes.Tracker => addTrackerNum,
            RoleTypes.Detective => addDetectiveNum,
            RoleTypes.Shapeshifter => addShapeshifterNum,
            RoleTypes.Phantom => addPhantomNum,
            RoleTypes.Viper => addViperNum,
            _ => 0
        };
    }
    public static int GetAssignCount(this CustomRoles role)
    {
        int maximumCount = role.GetCount();
        int assignUnitCount = CustomRoleManager.GetRoleInfo(role)?.AssignUnitCount ??
            role switch
            {
                CustomRoles.Lovers => 2,
                _ => 1,
            };
        return maximumCount / assignUnitCount;
    }

    public static List<CustomRoles> AddonRolesList = new();
    public static void SelectAddonRoles()
    {
        if (!Options.CurrentGameMode.GetModeClass()?.ShouldAssignAddons() ?? true) return;

        AddonRolesList = new();
        foreach (var cr in Enum.GetValues(typeof(CustomRoles)))
        {
            CustomRoles role = (CustomRoles)Enum.Parse(typeof(CustomRoles), cr.ToString());
            if (!role.IsAddon()) continue;
            if (role is CustomRoles.Lovers or CustomRoles.LastImpostor or CustomRoles.Workhorse or CustomRoles.Madmate) continue;
            AddonRolesList.Add(role);
        }
    }
}
