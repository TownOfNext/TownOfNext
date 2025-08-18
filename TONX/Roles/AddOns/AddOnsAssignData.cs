using UnityEngine;
using static TONX.Options;

namespace TONX.Roles.AddOns;
public class AddOnsAssignData
{
    static Dictionary<CustomRoles, AddOnsAssignData> AllData = new();
    public CustomRoles Role { get; private set; }
    public int IdStart { get; private set; }
    public List<CustomRoleTypes> AssignTypes { get; private set; }
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
        if (role is CustomRoles.Lighter && (!pc.GetCustomRole().IsCrewmate() || pc.Is(CustomRoles.Bewilder))) return false;
        if (role is CustomRoles.Bewilder && (pc.GetCustomRole().IsImpostor() || pc.Is(CustomRoles.Lighter))) return false;
        if (role is CustomRoles.Neptune && (pc.Is(CustomRoles.Lovers) || pc.Is(CustomRoles.Hater))) return false;
        if (role is CustomRoles.Madmate && !Utils.CanBeMadmate(pc)) return false;
        if (role is CustomRoles.Oblivious && (pc.Is(CustomRoles.Detective) || pc.Is(CustomRoles.Cleaner) || pc.Is(CustomRoles.Mortician) || pc.Is(CustomRoles.Medium))) return false;
        if (role is CustomRoles.Fool && pc.Is(CustomRoles.Repairman)) return false;
        if (role is CustomRoles.Tiebreaker && pc.Is(CustomRoles.Dictator)) return false;
        if (role is CustomRoles.YouTuber && (!pc.GetCustomRole().IsCrewmate() || pc.Is(CustomRoles.Madmate) || pc.Is(CustomRoles.Sheriff))) return false;
        if (role is CustomRoles.Egoist && (pc.GetCustomRole().IsNeutral() || pc.Is(CustomRoles.Madmate))) return false;
        if (role is CustomRoles.TicketsStealer or CustomRoles.Mimic && !pc.GetCustomRole().IsImpostor()) return false;
        if (role is CustomRoles.TicketsStealer && (pc.Is(CustomRoles.Bomber) || pc.Is(CustomRoles.BoobyTrap) || pc.Is(CustomRoles.Capitalist))) return false;
        if (role is CustomRoles.Mimic && pc.Is(CustomRoles.Mafia)) return false;
        if (role is CustomRoles.Schizophrenic && ((!pc.GetCustomRole().IsImpostor() && !pc.GetCustomRole().IsCrewmate()) || pc.Is(CustomRoles.Madmate))) return false;
        if (role is CustomRoles.Seer && pc.Is(CustomRoles.Mortician)) return false;
        if (role is CustomRoles.Reach && !pc.CanUseKillButton()) return false;
        if (role is CustomRoles.Flashman && pc.Is(CustomRoles.Swooper)) return false;
        return true;
    }
    static readonly IEnumerable<CustomRoles> ValidRoles = CustomRolesHelper.AllRoles.Where(role => !InvalidRoles.Contains(role));
    static CustomRoles[] CrewmateRoles = ValidRoles.Where(role => role.IsCrewmate()).ToArray();
    static CustomRoles[] ImpostorRoles = ValidRoles.Where(role => role.IsImpostor()).ToArray();
    static CustomRoles[] NeutralRoles = ValidRoles.Where(role => role.IsNeutral()).ToArray();

    public AddOnsAssignData(int idStart, TabGroup tab, CustomRoles role, bool assignCrewmate, bool assignImpostor, bool assignNeutral)
    {
        IdStart = idStart;
        Role = role;
        AssignTypes = new();
        if (assignCrewmate)
        {
            AssignTypes.Add(CustomRoleTypes.Crewmate);
            CrewmateMaximum = IntegerOptionItem.Create(idStart++, "RoleTypesMaximum", new(0, 15, 1), 1, tab, false)
                .SetParent(CustomRoleSpawnChances[role])
                .SetValueFormat(OptionFormat.Players);
            CrewmateMaximum.ReplacementDictionary = new Dictionary<string, string> { { "%roleTypes%", Utils.ColorString(new Color32(140, 255, 255, byte.MaxValue), GetString("TeamCrewmate")) } };
            CrewmateFixedRole = BooleanOptionItem.Create(idStart++, "FixedRole", false, tab, false)
                .SetParent(CrewmateMaximum);
            var crewmateStringArray = CrewmateRoles.Select(role => role.ToString()).ToArray();
            CrewmateAssignTarget = StringOptionItem.Create(idStart++, "Role", crewmateStringArray, 0, tab, false)
                .SetParent(CrewmateFixedRole);
        }

        if (assignImpostor)
        {
            AssignTypes.Add(CustomRoleTypes.Impostor);
            ImpostorMaximum = IntegerOptionItem.Create(idStart++, "RoleTypesMaximum", new(0, 3, 1), 1, tab, false)
                .SetParent(CustomRoleSpawnChances[role])
                .SetValueFormat(OptionFormat.Players);
            ImpostorMaximum.ReplacementDictionary = new Dictionary<string, string> { { "%roleTypes%", Utils.ColorString(new Color32(247, 70, 49, byte.MaxValue), GetString("TeamImpostor")) } };
            ImpostorFixedRole = BooleanOptionItem.Create(idStart++, "FixedRole", false, tab, false)
                .SetParent(ImpostorMaximum);
            var impostorStringArray = ImpostorRoles.Select(role => role.ToString()).ToArray();
            ImpostorAssignTarget = StringOptionItem.Create(idStart++, "Role", impostorStringArray, 0, tab, false)
                .SetParent(ImpostorFixedRole);
        }

        if (assignNeutral)
        {
            AssignTypes.Add(CustomRoleTypes.Neutral);
            NeutralMaximum = IntegerOptionItem.Create(idStart++, "RoleTypesMaximum", new(0, 15, 1), 1, tab, false)
                .SetParent(CustomRoleSpawnChances[role])
                .SetValueFormat(OptionFormat.Players);
            NeutralMaximum.ReplacementDictionary = new Dictionary<string, string> { { "%roleTypes%", Utils.ColorString(new Color32(255, 171, 27, byte.MaxValue), GetString("TeamNeutral")) } };
            NeutralFixedRole = BooleanOptionItem.Create(idStart++, "FixedRole", false, tab, false)
                .SetParent(NeutralMaximum);
            var neutralStringsArray = NeutralRoles.Select(role => role.ToString()).ToArray();
            NeutralAssignTarget = StringOptionItem.Create(idStart++, "Role", neutralStringsArray, 0, tab, false)
                .SetParent(NeutralFixedRole);
        }

        if (!AllData.ContainsKey(role)) AllData.Add(role, this);
        else Logger.Warn("重複したCustomRolesを対象とするAddOnsAssignDataが作成されました", "AddOnsAssignData");
    }
    public static AddOnsAssignData Create(int idStart, TabGroup tab, CustomRoles role, bool assignCrewmate, bool assignImpostor, bool assignNeutral)
        => new(idStart, tab, role, assignCrewmate, assignImpostor, assignNeutral);
    public static AddOnsAssignData Create(int idStart, CustomRoles role, bool assignCrewmate, bool assignImpostor, bool assignNeutral)
        => new(idStart, TabGroup.Addons, role, assignCrewmate, assignImpostor, assignNeutral);
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

        if (data.CrewmateMaximum != null)
        {
            var crewmateMaximum = data.CrewmateMaximum.GetInt();
            if (crewmateMaximum > 0)
            {
                var crewmates = validPlayers.Where(pc
                    => data.CrewmateFixedRole.GetBool() ? pc.Is(CrewmateRoles[data.CrewmateAssignTarget.GetValue()]) : pc.Is(CustomRoleTypes.Crewmate)).ToList();
                for (var i = 0; i < crewmateMaximum; i++)
                {
                    if (crewmates.Count == 0) break;
                    var selectedCrewmate = crewmates[rnd.Next(crewmates.Count)];
                    candidates.Add(selectedCrewmate);
                    crewmates.Remove(selectedCrewmate);
                }
            }
        }

        if (data.ImpostorMaximum != null)
        {
            var impostorMaximum = data.ImpostorMaximum.GetInt();
            if (impostorMaximum > 0)
            {
                var impostors = validPlayers.Where(pc
                    => data.ImpostorFixedRole.GetBool() ? pc.Is(ImpostorRoles[data.ImpostorAssignTarget.GetValue()]) : pc.Is(CustomRoleTypes.Impostor)).ToList();
                for (var i = 0; i < impostorMaximum; i++)
                {
                    if (impostors.Count == 0) break;
                    var selectedImpostor = impostors[rnd.Next(impostors.Count)];
                    candidates.Add(selectedImpostor);
                    impostors.Remove(selectedImpostor);
                }
            }
        }

        if (data.NeutralMaximum != null)
        {
            var neutralMaximum = data.NeutralMaximum.GetInt();
            if (neutralMaximum > 0)
            {
                var neutrals = validPlayers.Where(pc
                    => data.NeutralFixedRole.GetBool() ? pc.Is(NeutralRoles[data.NeutralAssignTarget.GetValue()]) : pc.Is(CustomRoleTypes.Neutral)).ToList();
                for (var i = 0; i < neutralMaximum; i++)
                {
                    if (neutrals.Count == 0) break;
                    var selectedNeutral = neutrals[rnd.Next(neutrals.Count)];
                    candidates.Add(selectedNeutral);
                    neutrals.Remove(selectedNeutral);
                }
            }
        }

        while (candidates.Count > data.Role.GetCount())
            candidates.RemoveAt(rnd.Next(candidates.Count));

        return candidates;
    }
    public static AddOnsAssignData GetAddonAssignData(CustomRoles role)
    {
        if (!role.IsAddon()) return null;
        return AllData.ContainsKey(role) ? AllData[role] : null;
    }
}