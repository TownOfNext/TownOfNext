using TONX.Attributes;
using UnityEngine;
using static TONX.Options;

namespace TONX.Roles.AddOns.Common;
public static class Beartrap
{
    private static readonly int Id = 81800;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Beartrap);
    private static List<byte> playerIdList = new();

    public static OptionItem OptionBlockMoveTime;

    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.Beartrap);
        AddOnsAssignData.Create(Id + 10, CustomRoles.Beartrap, true, true, true);
        OptionBlockMoveTime = FloatOptionItem.Create(Id + 20, "BeartrapBlockMoveTime", new(1f, 180f, 1f), 5f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Beartrap])
            .SetValueFormat(OptionFormat.Seconds);
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

        var tmpSpeed = Main.AllPlayerSpeed[killer.PlayerId];
        Main.AllPlayerSpeed[killer.PlayerId] = Main.MinSpeed;    //tmpSpeed����ۤɂ�������ΤǴ��뤷�Ƥ��ޤ���
        ReportDeadBodyPatch.CanReport[killer.PlayerId] = false;
        killer.MarkDirtySettings();
        _ = new LateTask(() =>
        {
            Main.AllPlayerSpeed[killer.PlayerId] = tmpSpeed;
            ReportDeadBodyPatch.CanReport[killer.PlayerId] = true;
            killer.MarkDirtySettings();
            RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
        }, OptionBlockMoveTime.GetFloat(), "Beartrap BlockMove");
    }
}