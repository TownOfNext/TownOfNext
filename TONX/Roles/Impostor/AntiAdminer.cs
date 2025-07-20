using AmongUs.GameOptions;
using HarmonyLib;
using System;
using System.Linq;
using TONX.Roles.Core;
using TONX.Roles.Core.Interfaces;
using UnityEngine;
using static TONX.Translator;

// 参考: https://github.com/ykundesu/SuperNewRoles/blob/master/SuperNewRoles/Mode/SuperHostRoles/BlockTool.cs
// 贡献：https://github.com/Yumenopai/TownOfHost_Y/tree/AntiAdminer
namespace TONX.Roles.Impostor;
public sealed class AntiAdminer : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(AntiAdminer),
            player => new AntiAdminer(player),
            CustomRoles.AntiAdminer,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            2900,
            SetupOptionItem,
            "aa|監視者|监管"
        );
    public AntiAdminer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        IsAdminWatch = false;
        IsVitalWatch = false;
        IsDoorLogWatch = false;
        IsCameraWatch = false;
    }

    static OptionItem OptionCanCheckCamera;
    enum OptionName
    {
        CanCheckCamera
    }

    public static bool IsAdminWatch;
    public static bool IsVitalWatch;
    public static bool IsDoorLogWatch;
    public static bool IsCameraWatch;

    private static void SetupOptionItem()
    {
        OptionCanCheckCamera = BooleanOptionItem.Create(RoleInfo, 10, OptionName.CanCheckCamera, true, false);
    }

    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!GameStates.IsInTask || isForMeeting || !Is(seer) || !Is(seen)) return "";

        OnFixedUpdate(Player);

        string suffix = "";
        if (IsAdminWatch) suffix += "★" + GetString("AntiAdminerAD");
        if (IsVitalWatch) suffix += "★" + GetString("AntiAdminerVI");
        if (IsDoorLogWatch) suffix += "★" + GetString("AntiAdminerDL");
        if (IsCameraWatch) suffix += "★" + GetString("AntiAdminerCA");

        return suffix;
    }
    private static int Count;
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost && !PlayerControl.LocalPlayer.Is(CustomRoles.AntiAdminer)) return;
        Count = Count > 5 ? 0 : ++Count;
        if (Count != 0) return;

        bool Admin = false, Camera = false, DoorLog = false, Vital = false;
        foreach (PlayerControl pc in Main.AllAlivePlayerControls)
        {
            if (pc.IsEaten() || pc.inVent || pc.Is(CustomRoleTypes.Impostor) || pc.Is(CustomRoles.Madmate)) continue;
            try
            {
                Vector2 PlayerPos = pc.GetTruePosition();

                switch (Main.NormalOptions.MapId)
                {
                    case 0:
                        if (!Options.DisableSkeldAdmin.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["SkeldAdmin"]) <= DisableDevice.UsableDistance();
                        if (!Options.DisableSkeldCamera.GetBool())
                            Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["SkeldCamera"]) <= DisableDevice.UsableDistance();
                        break;
                    case 1:
                        if (!Options.DisableMiraHQAdmin.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["MiraHQAdmin"]) <= DisableDevice.UsableDistance();
                        if (!Options.DisableMiraHQDoorLog.GetBool())
                            DoorLog |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["MiraHQDoorLog"]) <= DisableDevice.UsableDistance();
                        break;
                    case 2:
                        if (!Options.DisablePolusAdmin.GetBool())
                        {
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusLeftAdmin"]) <= DisableDevice.UsableDistance();
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusRightAdmin"]) <= DisableDevice.UsableDistance();
                        }
                        if (!Options.DisablePolusCamera.GetBool())
                            Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusCamera"]) <= DisableDevice.UsableDistance();
                        if (!Options.DisablePolusVital.GetBool())
                            Vital |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["PolusVital"]) <= DisableDevice.UsableDistance();
                        break;
                    case 4:
                        if (!Options.DisableAirshipCockpitAdmin.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipCockpitAdmin"]) <= DisableDevice.UsableDistance();
                        if (!Options.DisableAirshipRecordsAdmin.GetBool())
                            Admin |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipRecordsAdmin"]) <= DisableDevice.UsableDistance();
                        if (!Options.DisableAirshipCamera.GetBool())
                            Camera |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipCamera"]) <= DisableDevice.UsableDistance();
                        if (!Options.DisableAirshipVital.GetBool())
                            Vital |= Vector2.Distance(PlayerPos, DisableDevice.DevicePos["AirshipVital"]) <= DisableDevice.UsableDistance();
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString(), "AntiAdmin.OnFixedUpdate");
            }
        }

        var isChange = false;

        isChange |= IsAdminWatch != Admin;
        IsAdminWatch = Admin;
        isChange |= IsVitalWatch != Vital;
        IsVitalWatch = Vital;
        isChange |= IsDoorLogWatch != DoorLog;
        IsDoorLogWatch = DoorLog;
        if (OptionCanCheckCamera.GetBool())
        {
            isChange |= IsCameraWatch != Camera;
            IsCameraWatch = Camera;
        }

        if (isChange)
        {
            Main.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.AntiAdminer)).Do(x => Utils.NotifyRoles(x));
        }
    }
}