using static TONX.Options;

namespace TONX.Roles.AddOns;

public class AddOnsAssignData
{
    static Dictionary<CustomRoles, AddOnsAssignData> AllData = new();
    public CustomRoles Role { get; private set; }
    public int IdStart { get; private set; }
    public (bool Crewmate, bool Impostor, bool Neutral) AssignTeams { get; private set; }
    public List<CustomRoles> Conflicts { get; private set; }
    public bool CreateAssignOptions { get; private set; }
    OptionItem CrewmateMaximum;
    OptionItem CrewmateFixedRole;
    OptionItem CrewmateAssignTarget;
    OptionItem ImpostorMaximum;
    OptionItem ImpostorFixedRole;
    OptionItem ImpostorAssignTarget;
    OptionItem NeutralMaximum;
    OptionItem NeutralFixedRole;
    OptionItem NeutralAssignTarget;
    static readonly CustomRoles[] InvalidRoles =
    {
        CustomRoles.GuardianAngel,
        CustomRoles.NotAssigned,
        CustomRoles.LazyGuy,
        CustomRoles.GM,
    };
    static bool CheckRoleConflict(PlayerControl pc, CustomRoles role)
    {
        if (role is CustomRoles.Madmate && !Utils.CanBeMadmate(pc)) return false;
        if (role is CustomRoles.Reach && !pc.CanUseKillButton()) return false;

        var data = GetAddOnsAssignData(role);
        if (data == null) return true;
        if ((!data.AssignTeams.Crewmate && pc.GetCustomRole().IsCrewmate())
            || (!data.AssignTeams.Impostor && pc.GetCustomRole().IsImpostor())
            || (!data.AssignTeams.Neutral && pc.GetCustomRole().IsNeutral()))
            return false;
        foreach (var c in data.Conflicts) if (pc.Is(c)) return false;
        return true;
    }
    static readonly IEnumerable<CustomRoles> ValidRoles = CustomRolesHelper.AllRoles.Where(role => !InvalidRoles.Contains(role));
    static CustomRoles[] CrewmateRoles = ValidRoles.Where(role => role.IsCrewmate()).ToArray();
    static CustomRoles[] ImpostorRoles = ValidRoles.Where(role => role.IsImpostor()).ToArray();
    static CustomRoles[] NeutralRoles = ValidRoles.Where(role => role.IsNeutral()).ToArray();

    public AddOnsAssignData(int idStart, CustomRoles role, bool assignCrewmate, bool assignImpostor, bool assignNeutral, List<CustomRoles> conflicts = null, bool createAssignOptions = true)
    {
        IdStart = idStart;
        Role = role;
        AssignTeams = (assignCrewmate, assignImpostor, assignNeutral);
        conflicts ??= new();
        Conflicts = conflicts;
        CreateAssignOptions = createAssignOptions;

        if (!AllData.ContainsKey(role)) AllData.Add(role, this);
        else Logger.Warn("重複したCustomRolesを対象とするAddOnsAssignDataが作成されました", "AddOnsAssignData");
    }
    public static AddOnsAssignData Create(SimpleRoleInfo roleInfo, int idStart, CustomRoles role, bool assignCrewmate, bool assignImpostor, bool assignNeutral, List<CustomRoles> conflicts = null, bool createAssignOptions = true)
        => new(roleInfo.ConfigId + idStart, role, assignCrewmate, assignImpostor, assignNeutral, conflicts, createAssignOptions);
    public static void CreateAddonsAssignOptions(SimpleRoleInfo info, AddOnsAssignData data)
    {
        var role = info.RoleName;
        var tab = info.Experimental ? TabGroup.OtherRoles : TabGroup.Addons;
        if (data == null || !data.CreateAssignOptions) return;

        if (data.AssignTeams.Crewmate) CreateOptionItem(ref data.CrewmateMaximum, ref data.CrewmateFixedRole, ref data.CrewmateAssignTarget, CrewmateRoles, CustomRoleTypes.Crewmate, "TeamCrewmate");
        if (data.AssignTeams.Impostor) CreateOptionItem(ref data.ImpostorMaximum, ref data.ImpostorFixedRole, ref data.ImpostorAssignTarget, ImpostorRoles, CustomRoleTypes.Impostor, "TeamImpostor");
        if (data.AssignTeams.Neutral) CreateOptionItem(ref data.NeutralMaximum, ref data.NeutralFixedRole, ref data.NeutralAssignTarget, NeutralRoles, CustomRoleTypes.Neutral, "TeamNeutral");

        void CreateOptionItem(ref OptionItem maximum, ref OptionItem fixedRole, ref OptionItem assignTarget, CustomRoles[] roles, CustomRoleTypes team, string teamName)
        {
            maximum = IntegerOptionItem.Create(data.IdStart++, "RoleTypesMaximum", new(0, 15, 1), 1, tab, false)
                .SetParent(CustomRoleSpawnChances[role])
                .SetValueFormat(OptionFormat.Players);
            maximum.ReplacementDictionary = new Dictionary<string, Func<string>> { { "%roleTypes%", () => Utils.ColorString(Utils.GetCustomRoleTypeColor(team), GetString(teamName)) } };
            fixedRole = BooleanOptionItem.Create(data.IdStart++, "FixedRole", false, tab, false)
                .SetParent(maximum);
            var StringsArray = roles.Select(role => role.ToString()).ToArray();
            assignTarget = StringOptionItem.Create(data.IdStart++, "Role", StringsArray, 0, tab, false)
                .SetParent(fixedRole);
        }
    }
    ///<summary>
    ///AddOnsAssignDataが存在する属性を一括で割り当て
    ///</summary>
    public static void AssignAddOnsFromList()
    {
        foreach (var kvp in AllData)
        {
            var (role, data) = kvp;
            var assignTargetList = AssignTargetList(data);

            foreach (var pc in assignTargetList)
            {
                PlayerState.GetByPlayerId(pc.PlayerId).SetSubRole(role);
                Logger.Info($"注册附加职业：{pc?.Data?.PlayerName}（{pc.GetCustomRole()}）=> {role}", "AssignCustomSubRoles");
            }
        }
    }
    ///<summary>
    ///アサインするプレイヤーのList
    ///</summary>
    private static List<PlayerControl> AssignTargetList(AddOnsAssignData data)
    {
        var rnd = IRandom.Instance;
        var candidates = new List<PlayerControl>();
        var validPlayers = Main.AllPlayerControls.Where(pc => ValidRoles.Contains(pc.GetCustomRole()) && pc.GetCustomSubRoles()?.Count < AddonsNumLimit.GetInt() && CheckRoleConflict(pc, data.Role) && rnd.Next(0, 100) < GetRoleChance(data.Role));

        SelectCandidates(data.CrewmateMaximum, data.CrewmateFixedRole, data.CrewmateAssignTarget, CrewmateRoles, CustomRoleTypes.Crewmate);
        SelectCandidates(data.ImpostorMaximum, data.ImpostorFixedRole, data.ImpostorAssignTarget, ImpostorRoles, CustomRoleTypes.Impostor);
        SelectCandidates(data.NeutralMaximum, data.NeutralFixedRole, data.NeutralAssignTarget, NeutralRoles, CustomRoleTypes.Neutral);

        void SelectCandidates(OptionItem maximum, OptionItem fixedRole, OptionItem assignTarget, CustomRoles[] roles, CustomRoleTypes team)
        {
            if (maximum != null)
            {
                var Maximum = maximum.GetInt();
                if (Maximum > 0)
                {
                    var players = validPlayers.Where(pc
                        => fixedRole.GetBool() ? pc.Is(roles[assignTarget.GetValue()]) : pc.Is(team)).ToList();
                    for (var i = 0; i < Maximum; i++)
                    {
                        if (players.Count == 0) break;
                        var selected = players[rnd.Next(players.Count)];
                        candidates.Add(selected);
                        players.Remove(selected);
                    }
                }
            }
        }

        while (candidates.Count > data.Role.GetCount())
            candidates.RemoveAt(rnd.Next(candidates.Count));

        return candidates;
    }
    public static AddOnsAssignData GetAddOnsAssignData(CustomRoles role) => AllData.ContainsKey(role) ? AllData[role] : null;
    public static void RemoveImcompatibleAddons(PlayerControl player)
    {
        var state = PlayerState.GetByPlayerId(player.PlayerId);
        var remove = state.SubRoles.Where(r => !CheckRoleConflict(player, r)).ToList();
        remove.ForEach(r => player.RpcRemoveCustomSubRole(r, false));
    }
}