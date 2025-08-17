using TONX.Attributes;
using TONX.Modules;
using UnityEngine;
using static TONX.Options;

namespace TONX.Roles.AddOns.Common;
public static class Bait
{
    private static readonly int Id = 81700;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Bait);
    private static List<byte> playerIdList = new();

    public static OptionItem OptionReportDelayMin;
    public static OptionItem OptionReportDelayMax;
    public static OptionItem OptionDelayNotifyForKiller;

    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.Bait);
        AddOnsAssignData.Create(Id + 10, CustomRoles.Bait, true, true, true);
        OptionReportDelayMin = FloatOptionItem.Create(Id + 20, "BaitDelayMin", new(0f, 5f, 1f), 0f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bait])
            .SetValueFormat(OptionFormat.Seconds);
        OptionReportDelayMax = FloatOptionItem.Create(Id + 21, "BaitDelayMax", new(0f, 10f, 1f), 0f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bait])
            .SetValueFormat(OptionFormat.Seconds);
        OptionDelayNotifyForKiller = BooleanOptionItem.Create(Id + 22, "BaitDelayNotify", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bait]);

    }
    [GameModuleInitializer]
    public static void Init()
    {
        playerIdList = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
    public static void OnMurderPlayerOthers(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (!playerIdList.Contains(target.PlayerId) || info.IsSuicide) return;
        if (!info.IsSuicide)
        {
            killer.RPCPlayCustomSound("Congrats");
            target.RPCPlayCustomSound("Congrats");
            float delay;
            if (OptionReportDelayMax.GetFloat() < OptionReportDelayMin.GetFloat()) delay = 0f;
            else delay = IRandom.Instance.Next((int)OptionReportDelayMin.GetFloat(), (int)OptionReportDelayMax.GetFloat() + 1);
            delay = Math.Max(delay, 0.15f);
            if (delay > 0.15f && OptionDelayNotifyForKiller.GetBool()) killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Bait), string.Format(GetString("KillBaitNotify"), (int)delay)), delay);
            Logger.Info($"{killer.GetNameWithRole()} Killed Bait => {target.GetNameWithRole()}", "Bait.OnMurderPlayerAsTarget");
            _ = new LateTask(() => { if (GameStates.IsInTask) killer.CmdReportDeadBody(target.Data); }, delay, "Bait Self Report");
        }
    }
}