using System.Text;
using TONX.Attributes;
using TONX.Roles.Core.Interfaces;
using UnityEngine;
using TONX.Modules;
using TONX.GameModes;
using TONX.Roles.GameMode;

namespace TONX;

class HudManagerInitializePatch
{
    public static PassiveButton RoleInfoButton;
    [GameModuleInitializer]
    public static void CreateRoleInfoButton()
    {
        if (!GameStates.IsModHost || Options.CurrentGameMode == CustomGameMode.SoloKombat) return;
        var template = HudManager.Instance.MapButton;
        RoleInfoButton = UnityEngine.Object.Instantiate(template, template.transform.parent);
        RoleInfoButton.OnClick.AddListener((Action)(() =>
        {
            if (GameStates.IsInGame && (GameStates.IsCanMove || GameStates.IsMeeting) && Options.CurrentGameMode == CustomGameMode.Standard)
            {
                if (InGameRoleInfoMenu.Showing) InGameRoleInfoMenu.Hide();
                else
                {
                    InGameRoleInfoMenu.SetRoleInfoRef(PlayerControl.LocalPlayer);
                    InGameRoleInfoMenu.Show();
                }
                RoleInfoButton.SelectButton(InGameRoleInfoMenu.Showing);
            }
        }));
        RoleInfoButton.inactiveSprites.GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite("TONX.Resources.Images.UI.RoleInfoButton-inactive.png", 100f);
        RoleInfoButton.activeSprites.GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite("TONX.Resources.Images.UI.RoleInfoButton-active.png", 100f);
        RoleInfoButton.selectedSprites.GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite("TONX.Resources.Images.UI.RoleInfoButton-selected.png", 100f);
        RoleInfoButton.gameObject.SetActive(true);
    }
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
class HudManagerPatch
{
    public static bool ShowDebugText = false;
    public static int LastCallNotifyRolesPerSecond = 0;
    public static int NowCallNotifyRolesCount = 0;
    public static int LastSetNameDesyncCount = 0;
    public static int LastFPS = 0;
    public static int NowFrameCount = 0;
    public static float FrameRateTimer = 0.0f;
    public static TMPro.TextMeshPro LowerInfoText;
    public static void Postfix(HudManager __instance)
    {
        if (!GameStates.IsModHost) return;

        if (HudManagerInitializePatch.RoleInfoButton != null)
        {
            var template = HudManager.Instance.MapButton;
            var RoleInfoButton = HudManagerInitializePatch.RoleInfoButton;
            RoleInfoButton.gameObject.SetActive(template.isActiveAndEnabled);
            RoleInfoButton.transform.localPosition = template.transform.localPosition - new Vector3(0f, 0.8f, 0f);
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        //壁抜け
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            if ((!AmongUsClient.Instance.IsGameStarted || !GameStates.IsOnlineGame)
                && player.CanMove)
            {
                player.Collider.offset = new Vector2(0f, 127f);
            }
        }
        //壁抜け解除
        if (player.Collider.offset.y == 127f)
        {
            if (!Input.GetKey(KeyCode.LeftControl) || (AmongUsClient.Instance.IsGameStarted && GameStates.IsOnlineGame))
            {
                player.Collider.offset = new Vector2(0f, -0.3636f);
            }
        }

        //ゲーム中でなければ以下は実行されない
        if (!AmongUsClient.Instance.IsGameStarted) return;

        Utils.CountAlivePlayers();

        if (SetHudActivePatch.IsActive)
        {
            if (player.IsAlive())
            {
                //CustomRoleManager.AllActiveRoles.Do(r => Logger.Test(r.Key + " - " + r.Value.MyState.GetCustomRole().ToString()));
                var roleClass = player.GetRoleClass();
                if (roleClass != null)
                {
                    var killLabel = (roleClass as IKiller)?.OverrideKillButtonText(out string text) == true ? text : "";
                    if (killLabel != "") __instance.KillButton.OverrideText(killLabel);
                    var reportLabel = roleClass?.GetReportButtonText() ?? "";
                    if (reportLabel != "") __instance.ReportButton.OverrideText(reportLabel);
                    if (roleClass.HasAbility)
                    {
                        if (roleClass.GetAbilityButtonText(out var abilityLabel)) __instance.AbilityButton.OverrideText(abilityLabel);
                        __instance.AbilityButton.ToggleVisible(roleClass.CanUseAbilityButton() && GameStates.IsInTask);
                        int uses = roleClass.OverrideAbilityButtonUsesRemaining();
                        if (uses != -1) __instance.AbilityButton.SetUsesRemaining(uses);
                        else __instance.AbilityButton.SetInfiniteUses();
                    }
                }

                //バウンティハンターのターゲットテキスト
                if (LowerInfoText == null)
                {
                    LowerInfoText = UnityEngine.Object.Instantiate(__instance.KillButton.buttonLabelText);
                    LowerInfoText.transform.parent = __instance.transform;
                    LowerInfoText.transform.localPosition = new Vector3(0, -2f, 0);
                    LowerInfoText.alignment = TMPro.TextAlignmentOptions.Center;
                    LowerInfoText.overflowMode = TMPro.TextOverflowModes.Overflow;
                    LowerInfoText.enableWordWrapping = false;
                    LowerInfoText.color = Palette.EnabledColor;
                    LowerInfoText.fontSizeMin = 2.0f;
                    LowerInfoText.fontSizeMax = 2.0f;
                }
                LowerInfoText.text = roleClass?.GetLowerText(player, isForMeeting: GameStates.IsMeeting, isForHud: true) ?? "";
                LowerInfoText.enabled = LowerInfoText.text != "";

                if ((!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay) || GameStates.IsMeeting)
                {
                    LowerInfoText.enabled = false;
                }

                if (player.CanUseKillButton())
                {
                    __instance.KillButton.ToggleVisible(player.IsAlive() && GameStates.IsInTask);
                    player.Data.Role.CanUseKillButton = true;
                }
                else
                {
                    __instance.KillButton.SetDisabled();
                    __instance.KillButton.ToggleVisible(false);
                }

                bool CanUseVent = player.CanUseImpostorVentButton();
                __instance.ImpostorVentButton.ToggleVisible(CanUseVent);
                player.Data.Role.CanVent = CanUseVent;
            }
            else
            {
                __instance.ReportButton.Hide();
                __instance.ImpostorVentButton.Hide();
                __instance.KillButton.Hide();
                __instance.AbilityButton.Show();
                __instance.AbilityButton.OverrideText(GetString(StringNames.HauntAbilityName));
                __instance.AbilityButton.SetInfiniteUses();

                if (Options.CurrentGameMode == CustomGameMode.SoloKombat)
                {
                    if (LowerInfoText == null)
                    {
                        LowerInfoText = UnityEngine.Object.Instantiate(__instance.KillButton.buttonLabelText);
                        LowerInfoText.transform.parent = __instance.transform;
                        LowerInfoText.transform.localPosition = new Vector3(0, -2f, 0);
                        LowerInfoText.alignment = TMPro.TextAlignmentOptions.Center;
                        LowerInfoText.overflowMode = TMPro.TextOverflowModes.Overflow;
                        LowerInfoText.enableWordWrapping = false;
                        LowerInfoText.color = Palette.EnabledColor;
                        LowerInfoText.fontSizeMin = 2.0f;
                        LowerInfoText.fontSizeMax = 2.0f;
                    }
                    LowerInfoText.text = string.Format(GetString("KBTimeRemain"), SoloKombatManager.RoundTime.ToString());
                    LowerInfoText.enabled = true;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Y) && AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay)
        {
            __instance.ToggleMapVisible(new MapOptions()
            {
                Mode = MapOptions.Modes.Sabotage,
                AllowMovementWhileMapOpen = true
            });
            if (player.AmOwner)
            {
                player.MyPhysics.inputHandler.enabled = true;
                ConsoleJoystick.SetMode_Task();
            }
        }

        if (AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame) RepairSender.enabled = false;
        if (Input.GetKeyDown(KeyCode.RightShift) && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
        {
            RepairSender.enabled = !RepairSender.enabled;
            RepairSender.Reset();
        }
        if (RepairSender.enabled && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0)) RepairSender.Input(0);
            if (Input.GetKeyDown(KeyCode.Alpha1)) RepairSender.Input(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) RepairSender.Input(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) RepairSender.Input(3);
            if (Input.GetKeyDown(KeyCode.Alpha4)) RepairSender.Input(4);
            if (Input.GetKeyDown(KeyCode.Alpha5)) RepairSender.Input(5);
            if (Input.GetKeyDown(KeyCode.Alpha6)) RepairSender.Input(6);
            if (Input.GetKeyDown(KeyCode.Alpha7)) RepairSender.Input(7);
            if (Input.GetKeyDown(KeyCode.Alpha8)) RepairSender.Input(8);
            if (Input.GetKeyDown(KeyCode.Alpha9)) RepairSender.Input(9);
            if (Input.GetKeyDown(KeyCode.Return)) RepairSender.InputEnter();
        }
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ToggleHighlight))]
class ToggleHighlightPatch
{
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] bool active, [HarmonyArgument(1)] RoleTeamTypes team)
    {
        var player = PlayerControl.LocalPlayer;
        if (!GameStates.IsInTask) return;

        if (player.CanUseKillButton())
        {
            __instance.cosmetics.currentBodySprite.BodySprite.material.SetColor("_OutlineColor", Utils.GetRoleColor(player.GetCustomRole()));
        }
    }
}
[HarmonyPatch(typeof(Vent), nameof(Vent.SetOutline))]
class SetVentOutlinePatch
{
    public static void Postfix(Vent __instance, [HarmonyArgument(1)] ref bool mainTarget)
    {
        var player = PlayerControl.LocalPlayer;
        Color color = PlayerControl.LocalPlayer.GetRoleColor();
        __instance.myRend.material.SetColor("_OutlineColor", color);
        __instance.myRend.material.SetColor("_AddColor", mainTarget ? color : Color.clear);
    }
}
[HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive), new Type[] { typeof(PlayerControl), typeof(RoleBehaviour), typeof(bool) })]
class SetHudActivePatch
{
    public static bool IsActive = false;
    public static void Prefix(HudManager __instance, [HarmonyArgument(2)] ref bool isActive)
    {
        isActive &= !GameStates.IsMeeting;
        return;
    }
    public static void Postfix(HudManager __instance, [HarmonyArgument(2)] bool isActive)
    {
        __instance.ReportButton.ToggleVisible(!GameStates.IsLobby && isActive);
        if (!GameStates.IsModHost) return;
        IsActive = isActive;
        if (GameStates.IsLobby) return;
        if (!isActive) return;

        var player = PlayerControl.LocalPlayer;
        __instance.KillButton.ToggleVisible(player.CanUseKillButton());
        __instance.ImpostorVentButton.ToggleVisible(player.CanUseImpostorVentButton());
        __instance.SabotageButton.ToggleVisible(player.CanUseSabotageButton());
    }
}
[HarmonyPatch(typeof(VentButton), nameof(VentButton.DoClick))]
class VentButtonDoClickPatch
{
    public static bool Prefix(VentButton __instance)
    {
        var pc = PlayerControl.LocalPlayer;
        if (pc == null || pc.inVent || __instance.currentTarget == null || !pc.CanMove || !__instance.isActiveAndEnabled) return true;
        if (pc.GetCustomRole() is CustomRoles.Swooper or CustomRoles.Arsonist or CustomRoles.Revolutionist or CustomRoles.Veteran or CustomRoles.Paranoia or CustomRoles.Mayor or CustomRoles.Grenadier or CustomRoles.DoveOfPeace)
        {
            pc?.MyPhysics?.RpcEnterVent(__instance.currentTarget.Id);
            return false;
        }
        return true;
    }
}
[HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.Show))]
class MapBehaviourShowPatch
{
    public static void Prefix(MapBehaviour __instance, ref MapOptions opts)
    {
        if (GameStates.IsMeeting) return;

        if (opts.Mode is MapOptions.Modes.Normal or MapOptions.Modes.Sabotage)
        {
            var player = PlayerControl.LocalPlayer;
            if (player.GetRoleClass() is IKiller killer && killer.CanUseSabotageButton())
                opts.Mode = MapOptions.Modes.Sabotage;
            else
                opts.Mode = MapOptions.Modes.Normal;
        }
    }
}
[HarmonyPatch(typeof(TaskPanelBehaviour), nameof(TaskPanelBehaviour.SetTaskText))]
class TaskPanelBehaviourPatch
{
    // タスク表示の文章が更新・適用された後に実行される
    public static void Postfix(TaskPanelBehaviour __instance)
    {
        if (GameStates.IsLobby) return;
        if (!GameStates.IsModHost) return;
        PlayerControl player = PlayerControl.LocalPlayer;

        var taskText = __instance.taskText.text;
        if (taskText == "None") return;

        // 役職説明表示
        if (!player.GetCustomRole().IsVanilla())
        {
            var RoleWithInfo = $"{player.GetTrueRoleName()}:\r\n";
            RoleWithInfo += player.GetRoleInfo();

            var AllText = Utils.ColorString(player.GetRoleColor(), RoleWithInfo);

            switch (Options.CurrentGameMode)
            {
                case CustomGameMode.Standard:

                    var lines = taskText.Split("\r\n</color>\n")[0].Split("\r\n\n")[0].Split("\r\n");
                    StringBuilder sb = new();
                    foreach (var eachLine in lines)
                    {
                        var line = eachLine.Trim();
                        if ((line.StartsWith("<color=#FF1919FF>") || line.StartsWith("<color=#FF0000FF>")) && sb.Length < 1 && !line.Contains('(')) continue;
                        sb.Append(line + "\r\n");
                    }
                    if (sb.Length > 1)
                    {
                        var text = sb.ToString().TrimEnd('\n').TrimEnd('\r');
                        if (!Utils.HasTasks(player.Data, false) && sb.ToString().Count(s => (s == '\n')) >= 2)
                            text = $"{Utils.ColorString(new Color32(255, 20, 147, byte.MaxValue), GetString("FakeTask"))}\r\n{text}";
                        AllText += $"\r\n\r\n<size=85%>{text}</size>";
                    }

                    if (MeetingStates.FirstMeeting)
                        AllText += $"\r\n\r\n</color><size=70%>{GetString("PressF1ShowRoleDescription")}</size>";

                    break;

                case CustomGameMode.SoloKombat:

                    var lpc = PlayerControl.LocalPlayer;

                    if (lpc.GetCustomRole() is CustomRoles.KB_Normal)
                    {
                        AllText += "\r\n";
                        AllText += $"\r\n{GetString("PVP.ATK")}: {(lpc.GetRoleClass() as KB_Normal)?.ATK}";
                        AllText += $"\r\n{GetString("PVP.DF")}: {(lpc.GetRoleClass() as KB_Normal)?.DF}";
                        AllText += $"\r\n{GetString("PVP.RCO")}: {(lpc.GetRoleClass() as KB_Normal)?.HPReco}";  
                    }
                    AllText += "\r\n";

                    Dictionary<byte, string> SummaryText = new();
                    foreach (var id in PlayerState.AllPlayerStates.Keys)
                    {
                        if (Utils.GetPlayerById(id).GetCustomRole() is CustomRoles.GM) continue;
                        string name = Main.AllPlayerNames[id].RemoveHtmlTags().Replace("\r\n", string.Empty);
                        string summary = $"{SoloKombatManager.GetDisplayScore(id)}  {Utils.ColorString(Main.PlayerColors[id], name)}";
                        if (SoloKombatManager.GetDisplayScore(id).ToString().Trim() == "") continue;
                        SummaryText[id] = summary;
                    }

                    List<(int, byte)> list = new();
                    foreach (var id in PlayerState.AllPlayerStates.Keys) list.Add((SoloKombatManager.GetRankOfScore(id), id));
                    list.Sort();
                    foreach (var id in list.Where(x => SummaryText.ContainsKey(x.Item2))) AllText += "\r\n" + SummaryText[id.Item2];

                    AllText = $"<size=80%>{AllText}</size>";

                    break;
            }

            __instance.taskText.text = AllText;
        }

        // RepairSenderの表示
        if (RepairSender.enabled && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
            __instance.taskText.text = RepairSender.GetText();
    }
}

class RepairSender
{
    public static bool enabled = false;
    public static bool TypingAmount = false;

    public static int SystemType;
    public static int amount;

    public static void Input(int num)
    {
        if (!TypingAmount)
        {
            //SystemType入力中
            SystemType *= 10;
            SystemType += num;
        }
        else
        {
            //Amount入力中
            amount *= 10;
            amount += num;
        }
    }
    public static void InputEnter()
    {
        if (!TypingAmount)
        {
            //SystemType入力中
            TypingAmount = true;
        }
        else
        {
            //Amount入力中
            Send();
        }
    }
    public static void Send()
    {
        ShipStatus.Instance.RpcUpdateSystem((SystemTypes)SystemType, (byte)amount);
        Reset();
    }
    public static void Reset()
    {
        TypingAmount = false;
        SystemType = 0;
        amount = 0;
    }
    public static string GetText()
    {
        return SystemType.ToString() + "(" + ((SystemTypes)SystemType).ToString() + ")\r\n" + amount;
    }
}