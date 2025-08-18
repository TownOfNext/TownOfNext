using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hazel;
using System.Collections;
using TONX.GameModes;
using TONX.Roles.Core.Interfaces;
using TONX.Roles.Neutral;
using UnityEngine;

namespace TONX;

[HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
class GameEndChecker
{
    private static GameEndPredicate predicate;
    public static bool Prefix()
    {
        if (!AmongUsClient.Instance.AmHost) return true;

        //ゲーム終了判定済みなら中断
        if (predicate == null) return false;

        //ゲーム終了しないモードで廃村以外の場合は中断
        if (Options.NoGameEnd.GetBool() && CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw and not CustomWinner.Error) return false;

        //廃村用に初期値を設定
        GameOverReason reason;

        //ゲーム終了判定
        predicate.CheckForEndGame(out reason);

        // SoloKombat
        if (Options.CurrentGameMode == CustomGameMode.SoloKombat)
        {
            if (CustomWinnerHolder.WinnerIds.Count > 0 || CustomWinnerHolder.WinnerTeam != CustomWinner.Default)
            {
                ShipStatus.Instance.enabled = false;
                StartEndGame(reason);
                predicate = null;
            }
            return false;
        }

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
                if (Main.AllPlayerControls.Any(p => CustomWinnerHolder.WinnerIds.Contains(p.PlayerId) && p.Is(CustomRoles.Lovers)))
                {
                    CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.Lovers);
                    Main.AllPlayerControls.Where(p => p.Is(CustomRoles.Lovers))
                        .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                }
            }
            ShipStatus.Instance.enabled = false;
            StartEndGame(reason);
            predicate = null;
        }
        return false;
    }
    public static void StartEndGame(GameOverReason reason)
    {
        AmongUsClient.Instance.StartCoroutine(CoEndGame(AmongUsClient.Instance, reason).WrapToIl2Cpp());
    }
    private static IEnumerator CoEndGame(AmongUsClient self, GameOverReason reason)
    {
        // サーバー側のパケットサイズ制限によりCustomRpcSenderが利用できないため，遅延を挟むことで順番の整合性を保つ．

        // バニラ画面でのアウトロを正しくするためのゴーストロール化
        List<byte> ReviveRequiredPlayerIds = new();
        var winner = CustomWinnerHolder.WinnerTeam;
        foreach (var pc in Main.AllPlayerControls)
        {
            if (winner == CustomWinner.Draw)
            {
                SetGhostRole(ToGhostImpostor: true);
                continue;
            }
            bool canWin = CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId) || CustomWinnerHolder.WinnerRoles.Contains(pc.GetCustomRole());
            bool isCrewmateWin = reason.Equals(GameOverReason.CrewmatesByVote) || reason.Equals(GameOverReason.CrewmatesByTask);
            SetGhostRole(ToGhostImpostor: canWin ^ isCrewmateWin);

            void SetGhostRole(bool ToGhostImpostor)
            {
                var isDead = pc.Data.IsDead;
                if (!isDead) ReviveRequiredPlayerIds.Add(pc.PlayerId); 
                if (ToGhostImpostor)
                {
                    Logger.Info($"{pc.GetNameWithRole()}: ImpostorGhostに変更", "ResetRoleAndEndGame");
                    pc.RpcSetRole(RoleTypes.ImpostorGhost);
                }
                else
                {
                    Logger.Info($"{pc.GetNameWithRole()}: CrewmateGhostに変更", "ResetRoleAndEndGame");
                    pc.RpcSetRole(RoleTypes.CrewmateGhost);
                }
                pc.Data.IsDead = isDead;
            }
            SetEverythingUpPatch.LastWinsReason = winner is CustomWinner.Crewmate or CustomWinner.Impostor ? GetString($"GameOverReason.{reason}") : "";
        }

        // CustomWinnerHolderの情報の同期
        var winnerWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, SendOption.Reliable);
        CustomWinnerHolder.WriteTo(winnerWriter);
        AmongUsClient.Instance.FinishRpcImmediately(winnerWriter);

        // 蘇生を確実にゴーストロール設定の後に届けるための遅延
        yield return new WaitForSeconds(EndGameDelay);

        if (ReviveRequiredPlayerIds.Count > 0)
        {
            // 蘇生 パケットが膨れ上がって死ぬのを防ぐため，1送信につき1人ずつ蘇生する
            for (int i = 0; i < ReviveRequiredPlayerIds.Count; i++)
            {
                var playerId = ReviveRequiredPlayerIds[i];
                var playerInfo = GameData.Instance.GetPlayerById(playerId);
                // 蘇生
                playerInfo.IsDead = false;
                // 送信
                playerInfo.MarkDirty();
                AmongUsClient.Instance.SendAllStreamedObjects();
            }
            // ゲーム終了を確実に最後に届けるための遅延
            yield return new WaitForSeconds(EndGameDelay);
        }

        // ゲーム終了
        GameManager.Instance.RpcEndGame(reason, false);
    }
    private const float EndGameDelay = 0.2f;

    public static void SetPredicateToNormal() => predicate = new NormalGameEndPredicate();
    public static void SetPredicateToSoloKombat() => predicate = new SoloKombatGameEndPredicate();

    // ===== ゲーム終了条件 =====
    // 通常ゲーム用
    class NormalGameEndPredicate : GameEndPredicate
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

    // 个人竞技模式用
    class SoloKombatGameEndPredicate : GameEndPredicate
    {
        public override bool CheckForEndGame(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorsByKill;
            if (CustomWinnerHolder.WinnerIds.Count > 0) return false;
            if (CheckGameEndByLivingPlayers(out reason)) return true;
            return false;
        }

        public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorsByKill;

            if (SoloKombatManager.RoundTime > 0) return false;

            var list = Main.AllPlayerControls.Where(x => !x.Is(CustomRoles.GM) && SoloKombatManager.GetRankOfScore(x.PlayerId) == 1);
            var winner = list.FirstOrDefault();
            if (winner != null) CustomWinnerHolder.WinnerIds = new() { winner.PlayerId };
            else CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
            Main.DoBlockNameChange = true;

            return true;
        }
    }
}

public abstract class GameEndPredicate
{
    /// <summary>ゲームの終了条件をチェックし、CustomWinnerHolderに値を格納します。</summary>
    /// <params name="reason">バニラのゲーム終了処理に使用するGameOverReason</params>
    /// <returns>ゲーム終了の条件を満たしているかどうか</returns>
    public abstract bool CheckForEndGame(out GameOverReason reason);

    /// <summary>GameData.TotalTasksとCompletedTasksをもとにタスク勝利が可能かを判定します。</summary>
    public virtual bool CheckGameEndByTask(out GameOverReason reason)
    {
        reason = GameOverReason.ImpostorsByKill;
        if (Options.DisableTaskWin.GetBool() || TaskState.InitialTotalTasks == 0) return false;

        if (GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks)
        {
            reason = GameOverReason.CrewmatesByTask;
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
            return true;
        }
        return false;
    }
    /// <summary>ShipStatus.Systems内の要素をもとにサボタージュ勝利が可能かを判定します。</summary>
    public virtual bool CheckGameEndBySabotage(out GameOverReason reason)
    {
        reason = GameOverReason.ImpostorsByKill;
        if (ShipStatus.Instance.Systems == null) return false;

        // TryGetValueは使用不可
        var systems = ShipStatus.Instance.Systems;
        LifeSuppSystemType LifeSupp;
        if (systems.ContainsKey(SystemTypes.LifeSupp) && // サボタージュ存在確認
            (LifeSupp = systems[SystemTypes.LifeSupp].TryCast<LifeSuppSystemType>()) != null && // キャスト可能確認
            LifeSupp.Countdown < 0f) // タイムアップ確認
        {
            // 酸素サボタージュ
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
            reason = GameOverReason.ImpostorsBySabotage;
            LifeSupp.Countdown = 10000f;
            return true;
        }

        ISystemType sys = null;
        if (systems.ContainsKey(SystemTypes.Reactor)) sys = systems[SystemTypes.Reactor];
        else if (systems.ContainsKey(SystemTypes.Laboratory)) sys = systems[SystemTypes.Laboratory];
        else if (systems.ContainsKey(SystemTypes.HeliSabotage)) sys = systems[SystemTypes.HeliSabotage];

        ICriticalSabotage critical;
        if (sys != null && // サボタージュ存在確認
            (critical = sys.TryCast<ICriticalSabotage>()) != null && // キャスト可能確認
            critical.Countdown < 0f) // タイムアップ確認
        {
            // リアクターサボタージュ
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
            reason = GameOverReason.ImpostorsBySabotage;
            critical.ClearSabotage();
            return true;
        }

        return false;
    }
}