using AmongUs.GameOptions;

namespace TONX.Modules;

internal static class CustomRoleSelector
{
    public static Dictionary<PlayerControl, CustomRoles> RoleResult;
    public static IReadOnlyList<CustomRoles> AllRoles => RoleResult.Values.ToList();

    public static void SelectCustomRoles()
    {
        // 开始职业抽取
        RoleResult = new();
        var rd = IRandom.Instance;
        int playerCount = Main.AllAlivePlayerControls.Count();
        int optImpNum = Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors);
        int optNeutralNum = 0;
        if (Options.NeutralRolesMaxPlayer.GetInt() > 0 && Options.NeutralRolesMaxPlayer.GetInt() >= Options.NeutralRolesMinPlayer.GetInt())
            optNeutralNum = rd.Next(Options.NeutralRolesMinPlayer.GetInt(), Options.NeutralRolesMaxPlayer.GetInt() + 1);

        int readyRoleNum = 0;

        List<CustomRoles> rolesToAssign = new();

        List<CustomRoles> roleList = new();
        List<CustomRoles> roleOnList = new();
        List<CustomRoles> ImpOnList = new();
        List<CustomRoles> NeutralOnList = new();

        List<CustomRoles> roleRateList = new();
        List<CustomRoles> ImpRateList = new();
        List<CustomRoles> NeutralRateList = new();

        if (Options.CurrentGameMode == CustomGameMode.SoloKombat)
        {
            RoleResult = new();
            foreach (var pc in Main.AllAlivePlayerControls) RoleResult.Add(pc, pc.PlayerId == 0 && Options.EnableGM.GetBool() ? CustomRoles.GM : CustomRoles.KB_Normal);
            return;
        }

        foreach (var cr in Enum.GetValues(typeof(CustomRoles)))
        {
            CustomRoles role = (CustomRoles)Enum.Parse(typeof(CustomRoles), cr.ToString());
            if (role.IsGameModeRole() || !role.IsValid()) continue;
            if (role is CustomRoles.Crewmate or CustomRoles.Impostor) continue;
            if (role.IsVanilla())
            {
                if (Options.DisableVanillaRoles.GetBool() || role.GetCount() == 0 || rd.Next(0, 100) > role.GetChance()) continue;
            }
            else
            {
                if (role.IsAddon() || !Options.CustomRoleSpawnChances.TryGetValue(role, out var option) || option.Selections.Length != 3) continue;
                if (role is CustomRoles.Mare or CustomRoles.Concealer && Main.NormalOptions.MapId == 5) continue;
            }
            for (int i = 0; i < role.GetAssignCount(); i++)
                roleList.Add(role);
        }

        // 职业设置为：优先
        foreach (var role in roleList.Where(x => Options.GetRoleChance(x) == 2).Concat(roleList.Where(x => x.IsVanilla())))
        {
            if (role.IsImpostor()) ImpOnList.Add(role);
            else if (role.IsNeutral()) NeutralOnList.Add(role);
            else roleOnList.Add(role);
        }
        // 职业设置为：启用
        foreach (var role in roleList.Where(x => Options.GetRoleChance(x) == 1))
        {
            if (role.IsImpostor()) ImpRateList.Add(role);
            else if (role.IsNeutral()) NeutralRateList.Add(role);
            else roleRateList.Add(role);
        }

        if (Options.EnableRoleDraftMode.GetBool())
        {
            var rolesLists = new List<List<CustomRoles>> { roleRateList, roleOnList, ImpRateList, ImpOnList, NeutralRateList, NeutralOnList };
            if (!Options.DisableHiddenRoles.GetBool())
            {
                foreach (var role in EnumHelper.GetAllValues<CustomRoles>())
                {
                    if (!role.IsHidden(out var hiddenRoleInfo) || hiddenRoleInfo.TargetRole == null) continue;
                    if (rd.Next(0, 100) < hiddenRoleInfo.Probability)
                    {
                        foreach (var list in rolesLists) if (list.Remove(hiddenRoleInfo.TargetRole.Value)) list.Add(role);
                    }
                }
            }
            RoleDraftManager.Init(rolesLists, optImpNum, optNeutralNum);
            Logger.Info("已启用轮抽选角", "Role Draft");
            return;
        }

        void SelectRoles(string team, List<CustomRoles> currentRoleList, int optRoleNum, int lastReadyRoleNum, out int readyCurrentTeamRoleNum)
        {
            readyCurrentTeamRoleNum = 0;
            if (lastReadyRoleNum >= optRoleNum) return;
            while (currentRoleList.Count > 0)
            {
                if (readyRoleNum >= playerCount) return;
                var select = currentRoleList[rd.Next(0, currentRoleList.Count)];
                currentRoleList.Remove(select);
                rolesToAssign.Add(select);
                readyRoleNum++;
                readyCurrentTeamRoleNum++;
                Logger.Info(select.ToString() + $" 加入{team}职业待选列表", "CustomRoleSelector");
                if (readyCurrentTeamRoleNum >= optRoleNum) return;
            }
        }

        SelectRoles("内鬼(优先)", ImpOnList, optImpNum, 0, out var readyImpNum); // 抽取优先职业（内鬼）
        SelectRoles("内鬼(启用)", ImpRateList, optImpNum, readyImpNum, out _); // 优先职业不足以分配，开始分配启用的职业（内鬼）
        SelectRoles("中立(优先)", NeutralOnList, optNeutralNum, 0, out var readyNeutralNum); // 抽取优先职业（中立）
        SelectRoles("中立(启用)", NeutralRateList, optNeutralNum, readyNeutralNum, out _); // 优先职业不足以分配，开始分配启用的职业（中立）
        SelectRoles("船员(优先)", roleOnList, playerCount, 0, out _); // 抽取优先职业（船员）
        SelectRoles("船员(启用)", roleRateList, playerCount, 0, out _); // 优先职业不足以分配，开始分配启用的职业（船员）

        // 职业抽取结束

        // 隐藏职业
        if (!Options.DisableHiddenRoles.GetBool())
        {
            foreach (var role in EnumHelper.GetAllValues<CustomRoles>())
            {
                if (!role.IsHidden(out var hiddenRoleInfo) || hiddenRoleInfo.TargetRole == null) continue;
                if (rd.Next(0, 100) < hiddenRoleInfo.Probability && rolesToAssign.Remove(hiddenRoleInfo.TargetRole.Value)) 
                    rolesToAssign.Add(role);
            }
        }

        // Dev Roles List Edit
        foreach (var dr in Main.DevRole)
        {
            if (dr.Key == PlayerControl.LocalPlayer.PlayerId && Options.EnableGM.GetBool()) continue;
            if (rolesToAssign.Contains(dr.Value))
            {
                rolesToAssign.Remove(dr.Value);
                rolesToAssign.Insert(0, dr.Value);
                Logger.Info("职业列表提高优先：" + dr.Value, "Dev Role");
                continue;
            }
            for (int i = 0; i < rolesToAssign.Count; i++)
            {
                var role = rolesToAssign[i];
                if (Options.GetRoleChance(dr.Value) != Options.GetRoleChance(role)) continue;
                if (
                    (dr.Value.IsImpostor() && role.IsImpostor()) ||
                    (dr.Value.IsNeutral() && role.IsNeutral()) ||
                    (dr.Value.IsCrewmate() & role.IsCrewmate())
                    )
                {
                    rolesToAssign.RemoveAt(i);
                    rolesToAssign.Insert(0, dr.Value);
                    Logger.Info("覆盖职业列表：" + i + " " + role.ToString() + " => " + dr.Value, "Dev Role");
                    break;
                }
            }
        }

        var AllPlayer = Main.AllAlivePlayerControls.ToList();

        while (AllPlayer.Count > 0 && rolesToAssign.Count > 0)
        {
            PlayerControl delPc = null;
            foreach (var pc in AllPlayer)
                foreach (var dr in Main.DevRole.Where(x => pc.PlayerId == x.Key))
                {
                    if (dr.Key == PlayerControl.LocalPlayer.PlayerId && Options.EnableGM.GetBool()) continue;
                    var id = rolesToAssign.IndexOf(dr.Value);
                    if (id == -1) continue;
                    RoleResult.Add(pc, rolesToAssign[id]);
                    Logger.Info($"职业优先分配：{AllPlayer[0].GetRealName()} => {rolesToAssign[id]}", "CustomRoleSelector");
                    delPc = pc;
                    rolesToAssign.RemoveAt(id);
                    goto EndOfWhile;
                }

            var roleId = rd.Next(0, rolesToAssign.Count);
            RoleResult.Add(AllPlayer[0], rolesToAssign[roleId]);
            Logger.Info($"职业分配：{AllPlayer[0].GetRealName()} => {rolesToAssign[roleId]}", "CustomRoleSelector");
            AllPlayer.RemoveAt(0);
            rolesToAssign.RemoveAt(roleId);

        EndOfWhile:
            if (delPc != null)
            {
                AllPlayer.Remove(delPc);
                Main.DevRole.Remove(delPc.PlayerId);
            }
        }

        if (AllPlayer.Count > 0)
            Logger.Error("职业分配错误：存在未被分配职业的玩家", "CustomRoleSelector");
        if (rolesToAssign.Count > 0)
            Logger.Error("职业分配错误：存在未被分配的职业", "CustomRoleSelector");

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
        if (Options.CurrentGameMode == CustomGameMode.SoloKombat) return;

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
