using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using TONX.Modules;
using TONX.Roles.Core;
using TONX.Roles.Crewmate;
using TONX.Roles.Impostor;
using UnityEngine;
using static TONX.Translator;

namespace TONX;
public static class GuesserHelper
{
    public static string GetFormatString()
    {
        string text = GetString("PlayerIdList");
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            string id = pc.PlayerId.ToString();
            string name = pc.GetRealName();
            text += $"\n{id} → {name}";
        }
        return text;
    }
    public static bool MatchCommond(ref string msg, string command, bool exact = true)
    {
        var comList = command.Split('|');
        for (int i = 0; i < comList.Length; i++)
        {
            if (exact)
            {
                if (msg == "/" + comList[i]) return true;
            }
            else
            {
                if (msg.StartsWith("/" + comList[i]))
                {
                    msg = msg.Replace("/" + comList[i], string.Empty);
                    return true;
                }
            }
        }
        return false;
    }
    public static int GetColorFromMsg(string msg)
    {
        if (ComfirmIncludeMsg(msg, "红|紅|red")) return 0;
        if (ComfirmIncludeMsg(msg, "蓝|藍|深蓝|blue")) return 1;
        if (ComfirmIncludeMsg(msg, "绿|綠|深绿|green")) return 2;
        if (ComfirmIncludeMsg(msg, "粉红|粉紅|深粉|pink")) return 3;
        if (ComfirmIncludeMsg(msg, "橘|橘|orange")) return 4;
        if (ComfirmIncludeMsg(msg, "黄|黃|yellow")) return 5;
        if (ComfirmIncludeMsg(msg, "黑|黑|black")) return 6;
        if (ComfirmIncludeMsg(msg, "白|白|white")) return 7;
        if (ComfirmIncludeMsg(msg, "紫|紫|perple")) return 8;
        if (ComfirmIncludeMsg(msg, "棕|棕|brown")) return 9;
        if (ComfirmIncludeMsg(msg, "青|青|cyan")) return 10;
        if (ComfirmIncludeMsg(msg, "黄绿|黃綠|浅绿|淡绿|lime")) return 11;
        if (ComfirmIncludeMsg(msg, "红褐|紅褐|深红|maroon")) return 12;
        if (ComfirmIncludeMsg(msg, "玫红|玫紅|浅粉|淡粉|rose")) return 13;
        if (ComfirmIncludeMsg(msg, "焦黄|焦黃|浅黄|淡黄|banana")) return 14;
        if (ComfirmIncludeMsg(msg, "灰|灰|gray")) return 15;
        if (ComfirmIncludeMsg(msg, "茶|茶|tan")) return 16;
        if (ComfirmIncludeMsg(msg, "珊瑚|珊瑚|coral")) return 17;
        else return -1;
    }
    private static bool ComfirmIncludeMsg(string msg, string key) => key.Split('|').Any(msg.Contains);
    public static bool GuesserMsg(PlayerControl pc, string msg, out bool spam)
    {
        spam = false;

        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.NiceGuesser) && !pc.Is(CustomRoles.EvilGuesser)) return false;

        int operate; // 1:ID 2:猜测
        msg = msg.ToLower().Trim();
        if (MatchCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id")) operate = 1;
        else if (MatchCommond(ref msg, "shoot|guess|bet|st|gs|bt|猜|赌", false)) operate = 2;
        else return false;

        if (!pc.IsAlive())
        {
            Utils.SendMessage(GetString("GuessDead"), pc.PlayerId);
            return true;
        }

        if (operate == 1)
        {
            Utils.SendMessage(GetFormatString(), pc.PlayerId);
            return true;
        }
        else if (operate == 2)
        {
            spam = true;
            if (!AmongUsClient.Instance.AmHost) return true;

            if (!MsgToPlayerAndRole(msg, out byte targetId, out CustomRoles role, out string error))
            {
                Utils.SendMessage(error, pc.PlayerId);
                return true;
            }

            var target = Utils.GetPlayerById(targetId);
            if (!Guess(pc, target, role, out var reason))
                Utils.SendMessage(reason, pc.PlayerId);
        }
        return true;
    }
    public static bool Guess(PlayerControl guesser, PlayerControl target, CustomRoles role, out string reason, bool isUi = false)
    {
        reason = string.Empty;

        bool guesserSuicide = false;
        if (guesser.GetRoleClass() is NiceGuesser ngClass && ngClass.GuessLimit < 1)
        {
            reason = GetString("GGGuessMax");
            return false;
        }
        if (guesser.GetRoleClass() is EvilGuesser egClass && egClass.GuessLimit < 1)
        {
            reason = GetString("EGGuessMax");
            return false;
        }
        if (role == CustomRoles.SuperStar || target.Is(CustomRoles.SuperStar))
        {
            reason = GetString("GuessSuperStar");
            return false;
        }
        if (role == CustomRoles.GM || target.Is(CustomRoles.GM))
        {
            reason = GetString("GuessGM");
            return false;
        }
        if (target.Is(CustomRoles.Snitch) && target.AllTasksCompleted() && guesser.Is(CustomRoles.EvilGuesser) && !EvilGuesser.OptionCanGuessTaskDoneSnitch.GetBool())
        {
            reason = GetString("EGGuessSnitchTaskDone");
            return false;
        }
        if (role.IsAddon())
        {
            if (
                guesser.Is(CustomRoles.NiceGuesser) && !NiceGuesser.OptionCanGuessAddons.GetBool() ||
                guesser.Is(CustomRoles.EvilGuesser) && !EvilGuesser.OptionCanGuessAddons.GetBool()
                )
            {
                reason = GetString("GuessAdtRole");
                return false;
            }
        }
        if (role.IsVanilla())
        {
            if (
                guesser.Is(CustomRoles.NiceGuesser) && !NiceGuesser.OptionCanGuessVanilla.GetBool() ||
                guesser.Is(CustomRoles.EvilGuesser) && !EvilGuesser.OptionCanGuessVanilla.GetBool()
                )
            {
                reason = GetString("GuessVanillaRole");
                return false;
            }
        }
        if (guesser == target)
        {
            if (!isUi) Utils.SendMessage(GetString("LaughToWhoGuessSelf"), guesser.PlayerId, Utils.ColorString(Color.cyan, GetString("MessageFromKPD")));
            else guesser.ShowPopUp(Utils.ColorString(Color.cyan, GetString("MessageFromKPD")) + "\n" + GetString("LaughToWhoGuessSelf"));
            guesserSuicide = true;
        }
        else if (guesser.Is(CustomRoles.NiceGuesser) && role.IsCrewmate() && !NiceGuesser.OptionCanGuessCrew.GetBool() && !guesser.Is(CustomRoles.Madmate)) guesserSuicide = true;
        else if (guesser.Is(CustomRoles.EvilGuesser) && role.IsImpostor() && !EvilGuesser.OptionCanGuessImp.GetBool()) guesserSuicide = true;
        else if (!target.Is(role)) guesserSuicide = true;

        Logger.Info($"{guesser.GetNameWithRole()} 猜测了 {target.GetNameWithRole()}", "Guesser");

        var dp = guesserSuicide ? guesser : target;
        target = dp;

        Logger.Info($"赌场事件：{target.GetNameWithRole()} 死亡", "Guesser");

        string Name = dp.GetRealName();

        if (guesser.Is(CustomRoles.NiceGuesser)) (guesser.GetRoleClass() as NiceGuesser).GuessLimit--;
        if (guesser.Is(CustomRoles.EvilGuesser)) (guesser.GetRoleClass() as EvilGuesser).GuessLimit--;

        CustomSoundsManager.RPCPlayCustomSoundAll("Gunfire");

        _ = new LateTask(() =>
        {
            var state = PlayerState.GetByPlayerId(dp.PlayerId);
            state.DeathReason = CustomDeathReason.Gambled;
            dp.RpcSuicideWithAnime();
            dp.SetRealKiller(guesser);

            //死者检查
            Utils.NotifyRoles(isForMeeting: true, NoCache: true);

            _ = new LateTask(() => { Utils.SendMessage(string.Format(GetString("GuessKill"), Name), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceGuesser), GetString("GuessKillTitle"))); }, 0.6f, "Guess Msg");

        }, 0.2f, "Guesser Kill");

        return true;
    }
    public static TextMeshPro nameText(this PlayerControl p) => p.cosmetics.nameText;
    public static TextMeshPro NameText(this PoolablePlayer p) => p.cosmetics.nameText;
    private static bool MsgToPlayerAndRole(string msg, out byte id, out CustomRoles role, out string error)
    {
        id = byte.MaxValue;
        role = new();

        if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);

        Regex r = new("\\d+");
        MatchCollection mc = r.Matches(msg);
        string result = string.Empty;
        mc.Do(m => result += m);

        if (int.TryParse(result, out int num))
        {
            id = Convert.ToByte(num);
        }
        else
        {
            //FIXME: 指令中包含颜色后无法正确匹配职业名
            //并不是玩家编号，判断是否颜色
            int color = GetColorFromMsg(msg);
            List<PlayerControl> list = Main.AllAlivePlayerControls.Where(p => p.cosmetics.ColorId == color).ToList();
            if (list.Count < 1)
            {
                error = GetString("GuessNull");
                return false;
            }
            else if (list.Count != 1)
            {
                error = GetString("GuessMultipleColor");
                return false;
            }
            id = list.FirstOrDefault().PlayerId;
        }

        //判断选择的玩家是否合理
        PlayerControl target = Utils.GetPlayerById(id);
        if (target == null || target.Data.IsDead)
        {
            error = GetString("GuessNull");
            return false;
        }

        if (!ChatCommand.GetRoleByInputName(msg, out role, true))
        {
            error = GetString("GuessHelp");
            return false;
        }

        error = string.Empty;
        return true;
    }

    public const int MaxOneScreenRole = 40;
    public static int Page;
    public static PassiveButton ExitButton;
    public static GameObject guesserUI;
    private static Dictionary<CustomRoleTypes, List<Transform>> RoleButtons;
    private static Dictionary<CustomRoleTypes, SpriteRenderer> RoleSelectButtons;
    private static List<SpriteRenderer> PageButtons;
    public static CustomRoleTypes currentTeamType;
    static void GuesserSelectRole(CustomRoleTypes Role, bool SetPage = true)
    {
        currentTeamType = Role;
        if (SetPage) Page = 1;
        foreach (var RoleButton in RoleButtons)
        {
            int index = 0;
            foreach (var RoleBtn in RoleButton.Value)
            {
                if (RoleBtn == null) continue;
                index++;
                if (index <= (Page - 1) * 40) { RoleBtn.gameObject.SetActive(false); continue; }
                if (Page * 40 < index) { RoleBtn.gameObject.SetActive(false); continue; }
                RoleBtn.gameObject.SetActive(RoleButton.Key == Role);
            }
        }
        foreach (var RoleButton in RoleSelectButtons)
        {
            if (RoleButton.Value == null) continue;
            RoleButton.Value.color = new(0, 0, 0, RoleButton.Key == Role ? 1 : 0.25f);
        }
    }

    private static Color32 myColor = Color.white;
    public static TextMeshPro textTemplate;
    public static void ShowGuessPanel(byte playerId, MeetingHud __instance)
    {

        PlayerControl.LocalPlayer.RPCPlayCustomSound("Gunload");

        if (PlayerControl.LocalPlayer.cosmetics.ColorId >= 0 && PlayerControl.LocalPlayer.cosmetics.ColorId < Palette.PlayerColors.Count)
        {
            myColor = Palette.PlayerColors[PlayerControl.LocalPlayer.cosmetics.ColorId];
            myColor = Utils.ShadeColor(myColor, -2f);
        }

        var pc = Utils.GetPlayerById(playerId);
        if (pc == null || !pc.IsAlive() || guesserUI != null || !GameStates.IsVoting) return;

        try
        {
            Page = 1;
            RoleButtons = new();
            RoleSelectButtons = new();
            PageButtons = new();
            __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(false));

            Transform container = UnityEngine.Object.Instantiate(GameObject.Find("PhoneUI").transform, __instance.transform);
            container.gameObject.AddComponent<TransitionOpen>();
            container.transform.localPosition = new Vector3(0, 0, -200f);
            guesserUI = container.gameObject;

            List<int> i = new() { 0, 0, 0, 0 };
            var buttonTemplate = __instance.playerStates[0].transform.FindChild("votePlayerBase");
            var maskTemplate = __instance.playerStates[0].transform.FindChild("MaskArea");
            var smallButtonTemplate = __instance.playerStates[0].Buttons.transform.Find("CancelButton");
            textTemplate.enabled = true;
            if (textTemplate.transform.FindChild("RoleTextMeeting") != null) UnityEngine.Object.Destroy(textTemplate.transform.FindChild("RoleTextMeeting").gameObject);

            Transform exitButtonParent = new GameObject().transform;
            exitButtonParent.SetParent(container);
            Transform exitButton = UnityEngine.Object.Instantiate(buttonTemplate, exitButtonParent);
            exitButton.FindChild("ControllerHighlight").gameObject.SetActive(false);
            Transform exitButtonMask = UnityEngine.Object.Instantiate(maskTemplate, exitButtonParent);
            exitButtonMask.transform.localScale = new Vector3(2.88f, 0.8f, 1f);
            exitButtonMask.transform.localPosition = new Vector3(0f, 0f, 1f);
            exitButton.gameObject.GetComponent<SpriteRenderer>().sprite = smallButtonTemplate.GetComponent<SpriteRenderer>().sprite;
            exitButtonParent.transform.localPosition = new Vector3(3.88f, 2.12f, -200f);
            exitButtonParent.transform.localScale = new Vector3(0.22f, 0.9f, 1f);
            exitButtonParent.transform.SetAsFirstSibling();
            exitButton.GetComponent<PassiveButton>().OnClick = new();
            exitButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
            {
                __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
                UnityEngine.Object.Destroy(container.gameObject);
            }));
            ExitButton = exitButton.GetComponent<PassiveButton>();

            List<Transform> buttons = new();
            Transform selectedButton = null;

            int tabCount = 0;
            List<int> CustomRoleTypesIndex = new() { 1, 0, 2, 3 }; 
            for (int index = 0; index < 4; index++)
            {
                if (PlayerControl.LocalPlayer.Is(CustomRoles.EvilGuesser))
                {
                    if (!EvilGuesser.OptionCanGuessImp.GetBool() && index == 0) continue;
                    if (!EvilGuesser.OptionCanGuessAddons.GetBool() && index == 3) continue;
                }
                else
                {
                    if (!NiceGuesser.OptionCanGuessCrew.GetBool() && !PlayerControl.LocalPlayer.Is(CustomRoles.Madmate) && index == 1) continue;
                    if (!NiceGuesser.OptionCanGuessAddons.GetBool() && index == 3) continue;
                }
                Transform TeambuttonParent = new GameObject().transform;
                TeambuttonParent.SetParent(container);
                Transform Teambutton = UnityEngine.Object.Instantiate(buttonTemplate, TeambuttonParent);
                Teambutton.FindChild("ControllerHighlight").gameObject.SetActive(false);
                Transform TeambuttonMask = UnityEngine.Object.Instantiate(maskTemplate, TeambuttonParent);
                TextMeshPro Teamlabel = UnityEngine.Object.Instantiate(textTemplate, Teambutton);
                Teambutton.GetComponent<SpriteRenderer>().sprite = CustomButton.GetSprite("GuessPlateWithKPD");
                Teambutton.GetComponent<SpriteRenderer>().color = myColor;
                RoleSelectButtons.Add((CustomRoleTypes)CustomRoleTypesIndex[index], Teambutton.GetComponent<SpriteRenderer>());
                TeambuttonParent.localPosition = new(-2.75f + tabCount++ * 1.73f, 2.225f, -200);
                TeambuttonParent.localScale = new(0.53f, 0.53f, 1f);
                Teamlabel.color = Utils.GetCustomRoleTypeColor((CustomRoleTypes)CustomRoleTypesIndex[index]);
                Logger.Info(Teamlabel.color.ToString(), ((CustomRoleTypes)CustomRoleTypesIndex[index]).ToString());
                Teamlabel.text = GetString("Type" + ((CustomRoleTypes)CustomRoleTypesIndex[index]).ToString());
                Teamlabel.alignment = TextAlignmentOptions.Center;
                Teamlabel.transform.localPosition = new Vector3(0, 0, Teamlabel.transform.localPosition.z);
                Teamlabel.transform.localScale *= 1.6f;
                Teamlabel.autoSizeTextContainer = true;

                static void CreateTeamButton(Transform Teambutton, CustomRoleTypes type)
                {
                    Teambutton.GetComponent<PassiveButton>().OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                    {
                        if (type != currentTeamType)
                        {
                            GuesserSelectRole(type);
                            ReloadPage();
                        }
                    }));
                }
                if (PlayerControl.LocalPlayer.IsAlive()) CreateTeamButton(Teambutton, (CustomRoleTypes)CustomRoleTypesIndex[index]);
            }
            static void ReloadPage()
            {
                PageButtons[0].color = new(1, 1, 1, 1f);
                PageButtons[1].color = new(1, 1, 1, 1f);
                if (RoleButtons[currentTeamType].Count / MaxOneScreenRole + (RoleButtons[currentTeamType].Count % MaxOneScreenRole != 0 ? 1 : 0) < Page)
                {
                    Page -= 1;
                    PageButtons[1].color = new(1, 1, 1, 0.1f);
                }
                else if (RoleButtons[currentTeamType].Count / MaxOneScreenRole + (RoleButtons[currentTeamType].Count % MaxOneScreenRole != 0 ? 1 : 0) < Page + 1)
                {
                    PageButtons[1].color = new(1, 1, 1, 0.1f);
                }
                if (Page <= 1)
                {
                    Page = 1;
                    PageButtons[0].color = new(1, 1, 1, 0.1f);
                }
                GuesserSelectRole(currentTeamType, false);
            }
            static void CreatePage(bool IsNext, MeetingHud __instance, Transform container)
            {
                var buttonTemplate = __instance.playerStates[0].transform.FindChild("votePlayerBase");
                var maskTemplate = __instance.playerStates[0].transform.FindChild("MaskArea");
                var smallButtonTemplate = __instance.playerStates[0].Buttons.transform.Find("CancelButton");
                Transform PagebuttonParent = new GameObject().transform;
                PagebuttonParent.SetParent(container);
                Transform Pagebutton = UnityEngine.Object.Instantiate(buttonTemplate, PagebuttonParent);
                Pagebutton.FindChild("ControllerHighlight").gameObject.SetActive(false);
                Transform PagebuttonMask = UnityEngine.Object.Instantiate(maskTemplate, PagebuttonParent);
                TextMeshPro Pagelabel = UnityEngine.Object.Instantiate(textTemplate, Pagebutton);
                Pagebutton.GetComponent<SpriteRenderer>().sprite = CustomButton.GetSprite("GuessPlateWithKPD");
                PagebuttonParent.localPosition = IsNext ? new(3.535f, -2.2f, -200) : new(-3.475f, -2.2f, -200);
                PagebuttonParent.localScale = new(0.55f, 0.55f, 1f);
                Pagelabel.color = myColor;
                Pagelabel.text = GetString(IsNext ? "NextPage" : "PreviousPage");
                Pagelabel.alignment = TextAlignmentOptions.Center;
                Pagelabel.transform.localPosition = new Vector3(0, 0, Pagelabel.transform.localPosition.z);
                Pagelabel.transform.localScale *= 1.6f;
                Pagelabel.autoSizeTextContainer = true;
                if (!IsNext && Page <= 1) Pagebutton.GetComponent<SpriteRenderer>().color = new(1, 1, 1, 0.1f);
                Pagebutton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => ClickEvent()));
                void ClickEvent()
                {
                    if (IsNext) Page += 1;
                    else Page -= 1;
                    if (Page < 1) Page = 1;
                    ReloadPage();
                }
                PageButtons.Add(Pagebutton.GetComponent<SpriteRenderer>());
            }
            if (PlayerControl.LocalPlayer.IsAlive())
            {
                CreatePage(false, __instance, container);
                CreatePage(true, __instance, container);
            }
            int ind = 0;
            foreach (CustomRoles role in Enum.GetValues(typeof(CustomRoles)))
            {
                if (!EvilGuesser.OptionCanGuessVanilla.GetBool() && PlayerControl.LocalPlayer.Is(CustomRoles.EvilGuesser) && role.IsVanilla()) continue;
                if (!NiceGuesser.OptionCanGuessVanilla.GetBool() && PlayerControl.LocalPlayer.Is(CustomRoles.NiceGuesser) && role.IsVanilla()) continue;
                if (role is CustomRoles.GM or CustomRoles.NotAssigned or CustomRoles.SuperStar or CustomRoles.GuardianAngel) continue;
                CreateRole(role);
            }
            void CreateRole(CustomRoles role)
            {
                if (40 <= i[(int)role.GetCustomRoleTypes()]) i[(int)role.GetCustomRoleTypes()] = 0;
                Transform buttonParent = new GameObject().transform;
                buttonParent.SetParent(container);
                Transform button = UnityEngine.Object.Instantiate(buttonTemplate, buttonParent);
                button.FindChild("ControllerHighlight").gameObject.SetActive(false);
                Transform buttonMask = UnityEngine.Object.Instantiate(maskTemplate, buttonParent);
                TextMeshPro label = UnityEngine.Object.Instantiate(textTemplate, button);

                button.GetComponent<SpriteRenderer>().sprite = CustomButton.GetSprite("GuessPlate");
                button.GetComponent<SpriteRenderer>().color = myColor;
                if (!RoleButtons.ContainsKey(role.GetCustomRoleTypes()))
                {
                    RoleButtons.Add(role.GetCustomRoleTypes(), new());
                }
                RoleButtons[role.GetCustomRoleTypes()].Add(button);
                buttons.Add(button);
                int row = i[(int)role.GetCustomRoleTypes()] / 5;
                int col = i[(int)role.GetCustomRoleTypes()] % 5;
                buttonParent.localPosition = new Vector3(-3.47f + 1.75f * col, 1.5f - 0.45f * row, -200f);
                buttonParent.localScale = new Vector3(0.55f, 0.55f, 1f);
                label.text = GetString(role.ToString());
                label.color = Utils.GetRoleColor(role);
                label.alignment = TextAlignmentOptions.Center;
                label.transform.localPosition = new Vector3(0, 0, label.transform.localPosition.z);
                label.transform.localScale *= 1.6f;
                label.autoSizeTextContainer = true;
                int copiedIndex = i[(int)role.GetCustomRoleTypes()];

                button.GetComponent<PassiveButton>().OnClick = new();
                if (PlayerControl.LocalPlayer.IsAlive()) button.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
                {
                    if (selectedButton != button)
                    {
                        selectedButton = button;
                        buttons.ForEach(x => x.GetComponent<SpriteRenderer>().color = x == selectedButton ? Utils.GetRoleColor(PlayerControl.LocalPlayer.GetCustomRole()) : myColor);
                    }
                    else
                    {
                        if (!(__instance.state == MeetingHud.VoteStates.Voted || __instance.state == MeetingHud.VoteStates.NotVoted) || !PlayerControl.LocalPlayer.IsAlive()) return;

                        Logger.Msg($"Click: {pc.GetNameWithRole()} => {role}", "Guesser UI");

                        if (!PlayerControl.LocalPlayer.IsAlive())
                        {
                            PlayerControl.LocalPlayer.ShowPopUp(GetString("GuessDead"));
                        }
                        else
                        {
                            if (AmongUsClient.Instance.AmHost)
                            {
                                if (!Guess(PlayerControl.LocalPlayer, pc, role, out var reason, true))
                                    PlayerControl.LocalPlayer.ShowPopUp(reason);
                            }
                            else SendRPC(playerId, role);
                        }

                        // Reset the GUI
                        __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
                        UnityEngine.Object.Destroy(container.gameObject);
                        textTemplate.enabled = false;

                    }
                }));
                i[(int)role.GetCustomRoleTypes()]++;
                ind++;
            }
            container.transform.localScale *= 0.75f;
            GuesserSelectRole(PlayerControl.LocalPlayer.Is(CustomRoles.EvilGuesser) && !EvilGuesser.OptionCanGuessImp.GetBool() ?
                CustomRoleTypes.Crewmate : CustomRoleTypes.Impostor);
            ReloadPage();
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, "Guesser UI");
            return;
        }
    }

    private static void SendRPC(byte playerId, CustomRoles role)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.Guess, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write((byte)role);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        int PlayerId = reader.ReadByte();
        CustomRoles role = (CustomRoles)reader.ReadByte();
        if (!Guess(pc, Utils.GetPlayerById(PlayerId), role, out var reason, true))
            pc.ShowPopUp(reason);
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
    class MeetingHudOnDestroyGuesserUIClose
    {
        public static void Postfix()
        {
            if (textTemplate != null && textTemplate.gameObject != null)
                UnityEngine.Object.Destroy(textTemplate.gameObject);
        }
    }
}
