using AmongUs.GameOptions;
using TMPro;
using UnityEngine;
using System.Text;
using TONX.Modules;
using TONX.Roles.Core.Interfaces;
using TONX.Roles.Neutral;

namespace TONX.GameModes.Core;

// ==== 游戏模式base默认调用的函数 ====
internal static class GameModeStandard
{
    // == 游戏开始相关 ==
    public static AvailableRolesData AddAvailableRoles()
    {
        var rd = IRandom.Instance;
        int playerCount = Main.AllAlivePlayerControls.Count();
        int optImpNum = Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors);
        if (Options.ImpRolesLimitEnabled.GetBool())
            switch (playerCount)
            {
                case < 4 when optImpNum > 0:
                    optImpNum = 0;
                    break;
                case > 3 and < 7 when optImpNum > 1:
                    optImpNum = 1;
                    break;
                case > 6 and < 9 when optImpNum > 2:
                    optImpNum = 2;
                    break;
            }

        int optNeutralNum = 0;
        if (Options.NeutralCountModes.GetInt() == 0)
            if (Options.NeutralRolesMaxPlayer.GetInt() > 0 && Options.NeutralRolesMaxPlayer.GetInt() >= Options.NeutralRolesMinPlayer.GetInt())
                optNeutralNum = rd.Next(Options.NeutralRolesMinPlayer.GetInt(), Options.NeutralRolesMaxPlayer.GetInt() + 1);
        else if (Options.NeutralCountModes.GetInt() == 1)
            if (Options.NeutralPassiveRolesMaxPlayer.GetInt() > 0 && Options.NeutralPassiveRolesMaxPlayer.GetInt() >= Options.NeutralPassiveRolesMinPlayer.GetInt())
                optNeutralNum = rd.Next(Options.NeutralPassiveRolesMinPlayer.GetInt(), Options.NeutralPassiveRolesMaxPlayer.GetInt() + 1);

        int optNeutralKillingNum = 0;
        if (Options.NeutralCountModes.GetInt() == 1)
            if (Options.NeutralKillingRolesMaxPlayer.GetInt() > 0 && Options.NeutralKillingRolesMaxPlayer.GetInt() >= Options.NeutralKillingRolesMinPlayer.GetInt())
            optNeutralKillingNum = rd.Next(Options.NeutralKillingRolesMinPlayer.GetInt(), Options.NeutralKillingRolesMaxPlayer.GetInt() + 1);

        List<CustomRoles> roleList = new();
        List<CustomRoles> roleOnList = new();
        List<CustomRoles> ImpOnList = new();
        List<CustomRoles> NeutralKillingOnList = new();
        List<CustomRoles> NeutralOnList = new();

        List<CustomRoles> roleRateList = new();
        List<CustomRoles> ImpRateList = new();
        List<CustomRoles> NeutralKillingRateList = new();
        List<CustomRoles> NeutralRateList = new();

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
            else if (role.IsNeutralKiller() && Options.NeutralCountModes.GetInt() == 1) NeutralKillingOnList.Add(role);
            else if (role.IsNeutral()) NeutralOnList.Add(role);
            else roleOnList.Add(role);
        }
        // 职业设置为：启用
        foreach (var role in roleList.Where(x => Options.GetRoleChance(x) == 1))
        {
            if (role.IsImpostor()) ImpRateList.Add(role);
            else if (role.IsNeutralKiller() && Options.NeutralCountModes.GetInt() == 1) NeutralKillingRateList.Add(role);
            else if (role.IsNeutral()) NeutralRateList.Add(role);
            else roleRateList.Add(role);
        }

        return new(
            optImpNum, optNeutralKillingNum, optNeutralNum,
            roleOnList, ImpOnList, NeutralKillingOnList, NeutralOnList,
            roleRateList, ImpRateList, NeutralKillingRateList, NeutralRateList
        );
    }
    public static void SelectCustomRoles(ref Dictionary<PlayerControl, CustomRoles> RoleResult, ref AvailableRolesData data)
    {
        var rd = IRandom.Instance;
        int playerCount = Main.AllAlivePlayerControls.Count();
        List<CustomRoles> rolesToAssign = new();
        int readyRoleNum = 0;

        void SelectRoles(string team, ref List<CustomRoles> currentRoleList, int optRoleNum, int lastReadyRoleNum, out int readyCurrentTeamRoleNum)
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

        SelectRoles("内鬼(优先)", ref data.ImpOnList, data.optImpNum, 0, out var readyImpNum); // 抽取优先职业（内鬼）
        SelectRoles("内鬼(启用)", ref data.ImpRateList, data.optImpNum, readyImpNum, out _); // 优先职业不足以分配，开始分配启用的职业（内鬼）
        SelectRoles("中立杀手(优先)", ref data.NeutralKillingOnList, data.optNeutralKillingNum, 0, out var readyNeutralKillingNum); // 抽取优先职业（中立杀手）
        SelectRoles("中立杀手(启用)", ref data.NeutralKillingRateList, data.optNeutralKillingNum, readyNeutralKillingNum, out _); // 优先职业不足以分配，开始分配启用的职业（中立杀手）
        SelectRoles("中立(优先)", ref data.NeutralOnList, data.optNeutralNum, 0, out var readyNeutralNum); // 抽取优先职业（中立）
        SelectRoles("中立(启用)", ref data.NeutralRateList, data.optNeutralNum, readyNeutralNum, out _); // 优先职业不足以分配，开始分配启用的职业（中立）
        SelectRoles("船员(优先)", ref data.roleOnList, playerCount, 0, out _); // 抽取优先职业（船员）
        SelectRoles("船员(启用)", ref data.roleRateList, playerCount, 0, out _); // 优先职业不足以分配，开始分配启用的职业（船员）

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

    // == 游戏结束相关 ==
    // ===== ゲーム終了条件 =====
    // 通常ゲーム用
    public class NormalGameEndPredicate : GameEndPredicate
    {
        public override bool CheckForEndGame(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorsByKill;
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;
            if (CheckGameEndByLivingPlayers(out reason)) return true;
            if (CheckGameEndByTask(out reason)) return true;
            if (CheckGameEndBySabotage(out reason)) return true;

            return false;
        }

        public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorsByKill;

            if (CustomRoles.Sunnyboy.IsExist() && Main.AllAlivePlayerControls.Count() > 1) return false;

            var counts = EnumHelper.GetAllValues<CountTypes>()
                .Where(x => x is not CountTypes.None and not CountTypes.OutOfGame)
                .ToDictionary(
                    type => type,
                    Utils.AlivePlayersCount
                );

            foreach (var dualPc in Main.AllAlivePlayerControls.Where(p => p.Is(CustomRoles.Schizophrenic) && p.GetCountTypes() is CountTypes.Impostor or CountTypes.Crew))
                counts[dualPc.GetCountTypes()]++;

            if (counts.Values.Sum() == 0)
            {
                reason = GameOverReason.ImpostorsByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                return true;
            }
            if (Main.AllAlivePlayerControls.All(p => p.Is(CustomRoles.Lovers))) // 恋人胜利
            {
                reason = GameOverReason.ImpostorsByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lovers);
                Main.AllPlayerControls.Where(p => p.Is(CustomRoles.Lovers) && !CustomWinnerHolder.WinnerIds.Contains(p.PlayerId))
                        .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                return true;
            }
            var crewCount = counts.First(kvp => kvp.Key is CountTypes.Crew).Value;
            var nonZeroEntries = counts.Where(kvp => kvp.Key is not CountTypes.Crew && kvp.Value > 0).ToList();
            switch (nonZeroEntries.Count)
            {
                case 1 when nonZeroEntries[0].Value >= crewCount && !CustomRoles.Sheriff.IsExist():
                    reason = GameOverReason.ImpostorsByKill;
                    var winnerTeam = (CustomWinner)nonZeroEntries.First().Key;
                    CustomWinnerHolder.ResetAndSetWinner(winnerTeam);
                    Main.AllPlayerControls
                        .Where(pc => (CustomWinner)pc.GetCountTypes() == winnerTeam && !pc.GetCustomSubRoles().Contains(CustomRoles.Madmate) && !pc.GetCustomSubRoles().Contains(CustomRoles.Charmed))
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    switch (winnerTeam)
                    {
                        case CustomWinner.Impostor:
                            Main.AllPlayerControls
                                .Where(pc => pc.GetCustomSubRoles().Contains(CustomRoles.Madmate))
                                .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                            break;
                        case CustomWinner.Succubus:
                            Main.AllPlayerControls
                                .Where(pc => pc.GetCustomSubRoles().Contains(CustomRoles.Charmed))
                                .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                            break;
                    }
                    break;
                case 0:
                    reason = GameOverReason.CrewmatesByVote;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
                    Main.AllPlayerControls
                        .Where(pc => (CustomWinner)pc.GetCountTypes() == CustomWinner.Crewmate && !pc.GetCustomSubRoles().Contains(CustomRoles.Madmate) && !pc.GetCustomSubRoles().Contains(CustomRoles.Charmed))
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                default:
                    return false; // 胜利条件未达成
            }
            return true;
        }
    }
    public static void AfterCheckForGameEnd(GameOverReason reason, ref GameEndPredicate predicate)
    {
        //ゲーム終了時
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default)
        {
            //カモフラージュ強制解除
            Main.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(pc, ForceRevert: true, RevertToDefault: true));

            if (reason == GameOverReason.ImpostorsBySabotage && CustomRoles.Jackal.IsExist() && Jackal.WinBySabotage && !Main.AllAlivePlayerControls.Any(x => x.GetCustomRole().IsImpostorTeam()))
            {
                reason = GameOverReason.ImpostorsByKill;
                CustomWinnerHolder.WinnerIds.Clear();
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jackal);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackal);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Sidekick);
            }

            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw and not CustomWinner.None and not CustomWinner.Error)
            {
                //抢夺胜利
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.GetRoleClass() is IOverrideWinner overrideWinner)
                    {
                        overrideWinner.CheckWin(ref CustomWinnerHolder.WinnerTeam, ref CustomWinnerHolder.WinnerIds);
                    }

                    if (pc.Is(CustomRoles.Egoist))
                    {
                        if ((CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate && pc.GetCustomRole().IsCrewmate())
                            || (CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor && pc.GetCustomRole().IsImpostor()))
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Egoist);
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                    }
                }

                //追加胜利
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.GetRoleClass() is IAdditionalWinner additionalWinner)
                    {
                        var winnerRole = pc.GetCustomRole();
                        if (additionalWinner.CheckWin(ref winnerRole))
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerRoles.Add(winnerRole);
                        }
                    }
                }

                // 中立共同胜利
                if (Options.NeutralWinTogether.GetBool() && Main.AllPlayerControls.Any(p => CustomWinnerHolder.WinnerIds.Contains(p.PlayerId) && p.IsNeutral()))
                {
                    Main.AllPlayerControls.Where(p => p.IsNeutral() && !CustomWinnerHolder.WinnerIds.Contains(p.PlayerId))
                        .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                }
                else if (Options.NeutralRoleWinTogether.GetBool())
                {
                    foreach (var pc in Main.AllPlayerControls.Where(p => CustomWinnerHolder.WinnerIds.Contains(p.PlayerId) && p.IsNeutral()))
                    {
                        Main.AllPlayerControls.Where(p => p.GetCustomRole() == pc.GetCustomRole() && !CustomWinnerHolder.WinnerIds.Contains(p.PlayerId))
                            .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                    }
                }

                // 恋人胜利
                if (Main.AllPlayerControls.Any(p => CustomWinnerHolder.WinnerIds.Contains(p.PlayerId) && p.Is(CustomRoles.Lovers)) && CustomWinnerHolder.WinnerTeam is not CustomWinner.Lovers)
                {
                    CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.Lovers);
                    Main.AllPlayerControls.Where(p => p.Is(CustomRoles.Lovers) && !CustomWinnerHolder.WinnerIds.Contains(p.PlayerId))
                        .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                }
            }
            ShipStatus.Instance.enabled = false;
            GameEndChecker.StartEndGame(reason);
            predicate = null;
        }
    }

    // == UI编辑相关 ==
    public static void EditTaskText(TaskPanelBehaviour taskPanel, ref string AllText)
    {
        var lines = taskPanel.taskText.text.Split("\r\n</color>\n")[0].Split("\r\n\n")[0].Split("\r\n");
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
            if (!Utils.HasTasks(PlayerControl.LocalPlayer.Data, false) && sb.ToString().Count(s => s == '\n') >= 2)
                text = $"{Utils.ColorString(new Color32(255, 20, 147, byte.MaxValue), GetString("FakeTask"))}\r\n{text}";
            AllText += $"\r\n\r\n<size=85%>{text}</size>";
        }

        if (MeetingStates.FirstMeeting)
            AllText += $"\r\n\r\n</color><size=70%>{GetString("PressF1ShowRoleDescription")}</size>";
    }
    public static void EditOutroFormat(ref EndGameManager outro, ref TextMeshPro winnerText, ref string cwText, ref StringBuilder awText, ref string cwColor)
    {
        var winnerRole = (CustomRoles)CustomWinnerHolder.WinnerTeam;
        if (winnerRole >= 0)
        {
            cwText = Utils.GetRoleName(winnerRole);
            cwColor = Utils.GetRoleColorCode(winnerRole);
            if (winnerRole.IsNeutral())
            {
                outro.BackgroundBar.material.color = Utils.GetRoleColor(winnerRole);
            }
        }
        if (AmongUsClient.Instance.AmHost && PlayerState.GetByPlayerId(0).MainRole == CustomRoles.GM)
        {
            outro.WinText.text = GetString("GameOver");
            outro.WinText.color = Utils.GetRoleColor(CustomRoles.GM);
            outro.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.GM);
        }

        switch (CustomWinnerHolder.WinnerTeam)
        {
            //通常勝利
            case CustomWinner.Crewmate:
                cwColor = Utils.GetRoleColorCode(CustomRoles.Engineer);
                break;
            //特殊勝利
            case CustomWinner.Terrorist:
                outro.Foreground.material.color = Color.red;
                break;
            case CustomWinner.Lovers:
                outro.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Lovers);
                break;
        }

        foreach (var role in CustomWinnerHolder.AdditionalWinnerRoles)
        {
            awText.Append('＆').Append(Utils.ColorString(Utils.GetRoleColor(role), Utils.GetRoleName(role)));
        }
        if (awText.Length < 1) winnerText.text = $"<color={cwColor}>{cwText}{GetString("Win")}</color>";
        else winnerText.text = $"<color={cwColor}>{cwText}</color>{awText}{GetString("Win")}";
    }
}