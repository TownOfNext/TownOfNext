using AmongUs.GameOptions;
using TONX.Modules;
using UnityEngine;

namespace TONX;

[HarmonyPatch(typeof(IntroCutscene))]
class IntroCutscenePatch
{
    // 通过Patch原函数MoveNext的方法解决状态机无法打补丁的问题
    [HarmonyPatch(typeof(IntroCutscene._ShowRole_d__41), nameof(IntroCutscene._ShowRole_d__41.MoveNext)), HarmonyPostfix]
    public static void ShowRole_Postfix(IntroCutscene._ShowRole_d__41 __instance)
    {
        if (!GameStates.IsModHost) return;
        var introCutscene = __instance.__4__this;
        _ = new LateTask(() =>
        {
            var role = PlayerControl.LocalPlayer.GetCustomRole();
            if (!role.IsVanilla())
            {
                introCutscene.YouAreText.color = Utils.GetRoleColor(role);
                introCutscene.RoleText.text = Utils.GetRoleName(role);
                introCutscene.RoleText.color = Utils.GetRoleColor(role);
                introCutscene.RoleText.fontWeight = TMPro.FontWeight.Thin;
                introCutscene.RoleText.SetOutlineColor(Utils.ShadeColor(Utils.GetRoleColor(role), 0.1f).SetAlpha(0.38f));
                introCutscene.RoleText.SetOutlineThickness(0.17f);
                introCutscene.RoleBlurbText.color = Utils.GetRoleColor(role);
                introCutscene.RoleBlurbText.text = PlayerControl.LocalPlayer.GetRoleInfo();
            }
            foreach (var subRole in PlayerState.GetByPlayerId(PlayerControl.LocalPlayer.PlayerId).SubRoles)
                introCutscene.RoleBlurbText.text += "\n" + Utils.ColorString(Utils.GetRoleColor(subRole), GetString($"{subRole}Info"));
            if (!PlayerControl.LocalPlayer.Is(CustomRoles.Lovers) && !PlayerControl.LocalPlayer.Is(CustomRoles.Neptune) && CustomRoles.Neptune.IsExist())
                introCutscene.RoleBlurbText.text += "\n" + Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), GetString($"{CustomRoles.Lovers}Info"));
            introCutscene.RoleText.text += Utils.GetSubRolesText(PlayerControl.LocalPlayer.PlayerId, false, true);
        }, 0.0001f, "Override Role Text");
    }
    [HarmonyPatch(nameof(IntroCutscene.CoBegin)), HarmonyPrefix]
    public static void CoBegin_Prefix()
    {
        var logger = Logger.Handler("Info");
        logger.Info("------------显示名称------------");
        foreach (var pc in Main.AllPlayerControls)
        {
            logger.Info($"{(pc.AmOwner ? "[*]" : ""),-3}{pc.PlayerId,-2}:{pc.name.PadRightV2(20)}:{pc.cosmetics.nameText.text}({Palette.ColorNames[pc.Data.DefaultOutfit.ColorId].ToString().Replace("Color", "")})");
            pc.cosmetics.nameText.text = pc.name;
        }
        logger.Info("------------职业分配------------");
        foreach (var pc in Main.AllPlayerControls)
        {
            logger.Info($"{(pc.AmOwner ? "[*]" : ""),-3}{pc.PlayerId,-2}:{pc?.Data?.PlayerName?.PadRightV2(20)}:{pc.GetAllRoleName().RemoveHtmlTags()}");
        }
        logger.Info("------------运行环境------------");
        foreach (var pc in Main.AllPlayerControls)
        {
            try
            {
                var text = pc.AmOwner ? "[*]" : "   ";
                text += $"{pc.PlayerId,-2}:{pc.Data?.PlayerName?.PadRightV2(20)}:{pc.GetClient()?.PlatformData?.Platform.ToString()?.Replace("Standalone", ""),-11}";
                if (Main.playerVersion.TryGetValue(pc.PlayerId, out PlayerVersion pv))
                    text += $":Mod({pv.forkId}/{pv.version}:{pv.tag})";
                else text += ":Vanilla";
                logger.Info(text);
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Platform");
            }
        }
        logger.Info("------------基本设置------------");
        var tmp = GameOptionsManager.Instance.CurrentGameOptions.ToHudString(GameData.Instance ? GameData.Instance.PlayerCount : 10).Split("\r\n").Skip(1);
        foreach (var t in tmp) logger.Info(t);
        logger.Info("------------详细设置------------");
        foreach (var o in OptionItem.AllOptions)
            if (!o.IsHiddenOn(Options.CurrentGameMode) && (o.Parent == null ? !o.GetString().Equals("0%") : o.Parent.GetBool()))
                logger.Info(
                    $"{(o.Parent == null ? o.GetName(true, true).RemoveHtmlTags().PadRightV2(40) : $"┗ {o.GetName(true, true).RemoveHtmlTags()}".PadRightV2(41))}:{o.GetString().RemoveHtmlTags()}"
                    );
        logger.Info("-------------其它信息-------------");
        logger.Info($"玩家人数: {Main.AllPlayerControls.Count()}");
        Main.AllPlayerControls.Do(x => PlayerState.GetByPlayerId(x.PlayerId).InitTask(x));
        GameData.Instance.RecomputeTaskCounts();
        TaskState.InitialTotalTasks = GameData.Instance.TotalTasks;

        Utils.NotifyRoles();

        GameStates.InGame = true;
    }
    [HarmonyPatch(nameof(IntroCutscene.BeginCrewmate)), HarmonyPrefix]
    public static bool BeginCrewmate_Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
    {
        if (PlayerControl.LocalPlayer.Is(CustomRoles.CrewPostor))
        {
            teamToDisplay = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            teamToDisplay.Add(PlayerControl.LocalPlayer);
            foreach (var pc in Main.AllPlayerControls.Where(x => !x.AmOwner && x.GetCustomRole().IsImpostor())) teamToDisplay.Add(pc);
            __instance.BeginImpostor(teamToDisplay);
            __instance.overlayHandle.color = Palette.ImpostorRed;
            return false;
        }
        if (PlayerControl.LocalPlayer.Is(CustomRoleTypes.Neutral))
        {
            teamToDisplay = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            teamToDisplay.Add(PlayerControl.LocalPlayer);
        }
        else if (PlayerControl.LocalPlayer.Is(CustomRoles.Madmate))
        {
            teamToDisplay = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            teamToDisplay.Add(PlayerControl.LocalPlayer);
            __instance.BeginImpostor(teamToDisplay);
            __instance.overlayHandle.color = Palette.ImpostorRed;
            return false;
        }
        return true;
    }
    [HarmonyPatch(nameof(IntroCutscene.BeginCrewmate)), HarmonyPostfix]
    public static void BeginCrewmate_Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
    {
        //チーム表示変更
        CustomRoles role = PlayerControl.LocalPlayer.GetCustomRole();

        __instance.ImpostorText.gameObject.SetActive(false);
        switch (role.GetCustomRoleTypes())
        {
            case CustomRoleTypes.Impostor:
                __instance.TeamTitle.text = GetString("TeamImpostor");
                __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(255, 25, 25, byte.MaxValue);
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                break;
            case CustomRoleTypes.Crewmate:
                __instance.TeamTitle.text = GetString("TeamCrewmate");
                __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(140, 255, 255, byte.MaxValue);
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Crewmate);
                break;
            case CustomRoleTypes.Neutral:
                __instance.TeamTitle.text = GetString("TeamNeutral");
                __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(255, 171, 27, byte.MaxValue);
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Shapeshifter);
                break;
        }
        switch (role)
        {
            case CustomRoles.GM:
                __instance.TeamTitle.text = Utils.GetRoleName(role);
                __instance.TeamTitle.color = Utils.GetRoleColor(role);
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(role);
                PlayerControl.LocalPlayer.Data.Role.IntroSound = DestroyableSingleton<HudManager>.Instance.TaskCompleteSound;
                break;
        }

        if (role.GetRoleInfo()?.IntroSound is AudioClip introSound)
        {
            PlayerControl.LocalPlayer.Data.Role.IntroSound = introSound;
        }

        if (PlayerControl.LocalPlayer.Is(CustomRoles.Madmate))
        {
            __instance.TeamTitle.text = GetString("TeamImpostor");
            __instance.TeamTitle.color = __instance.BackgroundBar.material.color = new Color32(255, 25, 25, byte.MaxValue);
            PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
        }

        if (Options.CurrentGameMode == CustomGameMode.SoloKombat)
        {
            var color = ColorUtility.TryParseHtmlString("#f55252", out var c) ? c : new(255, 255, 255, 255);
            __instance.TeamTitle.text = Utils.GetRoleName(role);
            __instance.TeamTitle.color = Utils.GetRoleColor(role);
            __instance.ImpostorText.gameObject.SetActive(true);
            __instance.ImpostorText.text = GetString("ModeSoloKombat");
            __instance.BackgroundBar.material.color = color;
            PlayerControl.LocalPlayer.Data.Role.IntroSound = DestroyableSingleton<HnSImpostorScreamSfx>.Instance.HnSOtherImpostorTransformSfx;
        }

        if (RoleDraftManager.RoleDraftState == RoleDraftState.ReadyToDraft)
        {
            __instance.TeamTitle.text = GetString("RoleDraft");
            __instance.TeamTitle.color = Color.gray;
            __instance.ImpostorText.gameObject.SetActive(false);
            __instance.BackgroundBar.material.color = Color.gray;
            PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Crewmate);
        }

        if (Input.GetKey(KeyCode.RightShift))
        {
            __instance.TeamTitle.text = "明天就跑路啦";
            __instance.ImpostorText.gameObject.SetActive(true);
            __instance.ImpostorText.text = "嘿嘿嘿嘿嘿嘿";
            __instance.TeamTitle.color = Color.cyan;
            StartFadeIntro(__instance, Color.cyan, Color.yellow);
        }
        if (Input.GetKey(KeyCode.RightControl))
        {
            __instance.TeamTitle.text = "警告";
            __instance.ImpostorText.gameObject.SetActive(true);
            __instance.ImpostorText.text = "请远离无知的玩家";
            __instance.TeamTitle.color = Color.magenta;
            StartFadeIntro(__instance, Color.magenta, Color.magenta);
        }
    }
    public static AudioClip GetIntroSound(RoleTypes roleType)
    {
        return RoleManager.Instance.AllRoles.FirstOrDefault(role => role.Role == roleType).IntroSound;
    }
    private static async void StartFadeIntro(IntroCutscene __instance, Color start, Color end)
    {
        await Task.Delay(1000);
        int milliseconds = 0;
        while (true)
        {
            await Task.Delay(20);
            milliseconds += 20;
            float time = milliseconds / (float)500;
            Color LerpingColor = Color.Lerp(start, end, time);
            if (__instance == null || milliseconds > 500)
            {
                Logger.Info("ループを終了します", "StartFadeIntro");
                break;
            }
            __instance.BackgroundBar.material.color = LerpingColor;
        }
    }
    [HarmonyPatch(nameof(IntroCutscene.BeginImpostor)), HarmonyPrefix]
    public static bool BeginImpostor_Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
    {
        var role = PlayerControl.LocalPlayer.GetCustomRole();
        if (role is CustomRoles.CrewPostor)
        {
            yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            yourTeam.Add(PlayerControl.LocalPlayer);
            foreach (var pc in Main.AllPlayerControls.Where(x => !x.AmOwner && x.GetCustomRole().IsImpostor())) yourTeam.Add(pc);
            __instance.overlayHandle.color = Palette.ImpostorRed;
            return true;
        }
        if (PlayerControl.LocalPlayer.Is(CustomRoles.Madmate))
        {
            yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            yourTeam.Add(PlayerControl.LocalPlayer);
            __instance.overlayHandle.color = Palette.ImpostorRed;
            return true;
        }
        if (role.IsCrewmate() && role.GetRoleInfo().IsDesyncImpostor)
        {
            yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            yourTeam.Add(PlayerControl.LocalPlayer);
            foreach (var pc in Main.AllPlayerControls.Where(x => !x.AmOwner)) yourTeam.Add(pc);
            __instance.BeginCrewmate(yourTeam);
            __instance.overlayHandle.color = Palette.CrewmateBlue;
            return false;
        }
        BeginCrewmate_Prefix(__instance, ref yourTeam);
        return true;
    }
    [HarmonyPatch(nameof(IntroCutscene.BeginImpostor)), HarmonyPostfix]
    public static void BeginImpostor_Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
    {
        BeginCrewmate_Postfix(__instance, ref yourTeam);
    }
    [HarmonyPatch(nameof(IntroCutscene.OnDestroy)), HarmonyPostfix]
    public static void OnDestroy_Postfix(IntroCutscene __instance)
    {
        if (!GameStates.IsInGame) return;
        Main.isFirstTurn = true;
        var mapId = Main.NormalOptions.MapId;
        // エアシップではまだ湧かない
        if ((MapNames)mapId != MapNames.Airship)
        {
            foreach (var state in PlayerState.AllPlayerStates.Values)
            {
                state.HasSpawned = true;
            }
        }

        if (AmongUsClient.Instance.AmHost)
        {
            if (mapId != 4)
            {
                Main.AllPlayerControls.Do(pc =>
                {
                    pc.GetRoleClass()?.OnSpawn(true);
                    pc.SyncSettings();
                    pc.RpcResetAbilityCooldown();
                });
                if (Options.FixFirstKillCooldown.GetBool() && Options.CurrentGameMode != CustomGameMode.SoloKombat)
                    _ = new LateTask(() =>
                    {
                        if (GameStates.IsInTask)
                        {
                            Main.AllPlayerControls.Do(x => x.ResetKillCooldown());
                            Main.AllPlayerControls.Where(x => (Main.AllPlayerKillCooldown[x.PlayerId] - 2f) > 0f).Do(pc => pc.SetKillCooldownV2(Main.AllPlayerKillCooldown[pc.PlayerId] - 2f));
                        }
                    }, 2f, "FixKillCooldownTask");
                _ = new LateTask(() =>
                {
                    CustomRoleManager.AllActiveRoles.Values.Do(x => x?.OnGameStart());
                }, 0.1f, "RoleClassOnGameStartTask");
            }
            // _ = new LateTask(() => Main.AllPlayerControls.Do(pc => pc.RpcSetRoleDesync(RoleTypes.Shapeshifter, -3)), 2f, "SetImpostorForServer");
            if (PlayerControl.LocalPlayer.Is(CustomRoles.GM))
            {
                PlayerControl.LocalPlayer.RpcExile();
                PlayerState.GetByPlayerId(PlayerControl.LocalPlayer.PlayerId).SetDead();
            }
            if (RandomSpawn.IsRandomSpawn())
            {
                RandomSpawn.SpawnMap map;
                switch (mapId)
                {
                    case 0:
                        map = new RandomSpawn.SkeldSpawnMap();
                        Main.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                    case 1:
                        map = new RandomSpawn.MiraHQSpawnMap();
                        Main.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                    case 2:
                        map = new RandomSpawn.PolusSpawnMap();
                        Main.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                    case 5:
                        map = new RandomSpawn.FungleSpawnMap();
                        Main.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                }
            }

            // そのままだとホストのみDesyncImpostorの暗室内での視界がクルー仕様になってしまう
            var roleInfo = PlayerControl.LocalPlayer.GetCustomRole().GetRoleInfo();
            var amDesyncImpostor = roleInfo?.IsDesyncImpostor == true;
            if (amDesyncImpostor)
            {
                PlayerControl.LocalPlayer.Data.Role.AffectedByLightAffectors = false;
            }
        }
        Logger.Info("OnDestroy", "IntroCutscene");

        GameStates.InTask = true;
        Logger.Info("タスクフェイズ開始", "Phase");

        if (Options.EnableRoleDraftMode.GetBool() && AmongUsClient.Instance.AmHost) PlayerControl.LocalPlayer.NoCheckStartMeeting(null, true);
    }
}