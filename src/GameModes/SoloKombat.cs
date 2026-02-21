using UnityEngine;
using TONX.Roles.GameMode;
using TMPro;
using System.Text;

namespace TONX.GameModes;

public sealed class SoloKombat : GameModeBase
{
    public static readonly GameModeInfo ModeInfo =
        GameModeInfo.Create(
            typeof(SoloKombat),
            () => new SoloKombat(),
            CustomGameMode.SoloKombat,
            20_000_000,
            SetupCustomOption,
            "#f55252",
            () => $"<color=#f55252><size=1.7>{GetString("ModeSoloKombat")}</size></color>",
            (true, false)
        );
    public SoloKombat() : base(ModeInfo)
    { }
    public static int RoundTime;

    private static OptionItem KB_GameTime;
    public static OptionItem KB_ATKCooldown;
    public static OptionItem KB_HPMax;
    public static OptionItem KB_ATK;
    public static OptionItem KB_RecoverAfterSecond;
    public static OptionItem KB_RecoverPerSecond;
    public static OptionItem KB_ResurrectionWaitingTime;
    public static OptionItem KB_KillBonusMultiplier;

    public static void SetupCustomOption()
    {
        TextOptionItem.Create(ModeInfo, 100_001, "MenuTitle.GameMode");
        KB_GameTime = IntegerOptionItem.Create(ModeInfo, 1, "KB_GameTime", new(30, 300, 5), 180, false)
            .SetValueFormat(OptionFormat.Seconds)
            .SetHeader(true);
        KB_ATKCooldown = FloatOptionItem.Create(ModeInfo, 2, "KB_ATKCooldown", new(1f, 10f, 0.1f), 1f, false)
            .SetValueFormat(OptionFormat.Seconds);
        KB_HPMax = FloatOptionItem.Create(ModeInfo, 3, "KB_HPMax", new(10f, 990f, 5f), 100f, false)
            .SetValueFormat(OptionFormat.Health);
        KB_ATK = FloatOptionItem.Create(ModeInfo, 4, "KB_ATK", new(1f, 100f, 1f), 8f, false)
            .SetValueFormat(OptionFormat.Health);
        KB_RecoverPerSecond = FloatOptionItem.Create(ModeInfo, 5, "KB_RecoverPerSecond", new(1f, 180f, 1f), 2f, false)
            .SetValueFormat(OptionFormat.Health);
        KB_RecoverAfterSecond = IntegerOptionItem.Create(ModeInfo, 6, "KB_RecoverAfterSecond", new(0, 60, 1), 8, false)
            .SetValueFormat(OptionFormat.Seconds);
        KB_ResurrectionWaitingTime = IntegerOptionItem.Create(ModeInfo, 7, "KB_ResurrectionWaitingTime", new(5, 990, 1), 15, false)
            .SetValueFormat(OptionFormat.Seconds);
        KB_KillBonusMultiplier = FloatOptionItem.Create(ModeInfo, 8, "KB_KillBonusMultiplier", new(0.25f, 5f, 0.25f), 1.25f, false)
            .SetValueFormat(OptionFormat.Multiplier);
    }

    public override void Add()
    {
        RoundTime = KB_GameTime.GetInt() + 8;
    }
    public override bool ShouldAssignAddons() => false;
    public override AvailableRolesData AddAvailableRoles() => default;
    public override void SelectCustomRoles(ref Dictionary<PlayerControl, CustomRoles> RoleResult, ref AvailableRolesData data)
    {
        foreach (var pc in Main.AllAlivePlayerControls)
            RoleResult.Add(pc, pc.PlayerId == 0 && Options.EnableGM.GetBool() ? CustomRoles.GM : CustomRoles.KB_Normal);
    }

    public override List<byte> ArrangedSummaryText(List<byte> clone) => clone.OrderBy(GetRankOfScore).ToList();
    public override (bool, bool, bool) GetSummaryTextContent() => (false, true, false);

    public override bool CanSeeOtherProgressText() => true;
    public override bool ShouldRandomSpawn() => true;
    public override bool OnCloseDoors(SystemTypes door) => false;
    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target) => false;
    public override void OnSecondsUpdate(PlayerControl player, long now)
    {
        if (!GameStates.IsInTask || player != PlayerControl.LocalPlayer) return;
        // 减少全局倒计时
        RoundTime--;
    }

    public override void EditTaskText(TaskPanelBehaviour taskPanel, ref string AllText)
    {
        var lpc = PlayerControl.LocalPlayer;
        var kb_Normal = lpc.GetRoleClass() as KB_Normal;

        if (lpc.GetCustomRole() is CustomRoles.KB_Normal)
        {
            AllText += "\r\n";
            AllText += $"\r\n{GetString("PVP.ATK")}: {kb_Normal?.ATK}";
            AllText += $"\r\n{GetString("PVP.DF")}: {kb_Normal?.DF}";
            AllText += $"\r\n{GetString("PVP.RCO")}: {kb_Normal?.HPReco}";
        }
        AllText += "\r\n";

        Dictionary<byte, string> SummaryText = new();
        List<byte> AllPlayerIds = PlayerState.AllPlayerStates.Keys.Where(k => (Utils.GetPlayerById(k)?.Data ?? null) != null).ToList();
        foreach (var id in AllPlayerIds)
        {
            if (Utils.GetPlayerById(id).GetCustomRole() is CustomRoles.GM) continue;
            string name = Main.AllPlayerNames[id].RemoveHtmlTags().Replace("\r\n", string.Empty);
            string summary = $"{GetDisplayScore(id)}  {Utils.ColorString(Main.PlayerColors[id], name)}";
            if (GetDisplayScore(id).ToString().Trim() == "") continue;
            SummaryText[id] = summary;
        }

        List<(int, byte)> list = new();
        foreach (var id in AllPlayerIds) list.Add((GetRankOfScore(id), id));
        list.Sort();
        foreach (var id in list.Where(x => SummaryText.ContainsKey(x.Item2))) AllText += "\r\n" + SummaryText[id.Item2];

        AllText = $"<size=80%>{AllText}</size>";
    }
    public override void EditIntroFormat(ref IntroCutscene intro)
    {
        CustomRoles role = PlayerControl.LocalPlayer.GetCustomRole();
        var color = ColorUtility.TryParseHtmlString("#f55252", out var c) ? c : new(255, 255, 255, 255);
        intro.TeamTitle.text = Utils.GetRoleName(role);
        intro.TeamTitle.color = Utils.GetRoleColor(role);
        intro.ImpostorText.gameObject.SetActive(true);
        intro.ImpostorText.text = GetString("ModeSoloKombat");
        intro.BackgroundBar.material.color = color;
        PlayerControl.LocalPlayer.Data.Role.IntroSound = DestroyableSingleton<HnSImpostorScreamSfx>.Instance.HnSOtherImpostorTransformSfx;
    }
    public override void EditOutroFormat(ref EndGameManager outro, ref TextMeshPro winnerText, ref string cwt, ref StringBuilder awt, ref string cwc)
    {
        var winnerId = CustomWinnerHolder.WinnerIds.FirstOrDefault();
        outro.WinText.text = Main.AllPlayerNames[winnerId] + GetString("Win");
        outro.WinText.fontSize -= 5f;
        outro.WinText.color = Main.PlayerColors[winnerId];
        outro.BackgroundBar.material.color = new Color32(245, 82, 82, 255);
        winnerText.text = $"<color=#f55252>{GetString("ModeSoloKombat")}</color>";
        winnerText.color = Color.red;
    }

    public override void AfterCheckForGameEnd(GameOverReason reason, ref GameEndPredicate predicate)
    {
        if (CustomWinnerHolder.WinnerIds.Count > 0 || CustomWinnerHolder.WinnerTeam != CustomWinner.Default)
        {
            ShipStatus.Instance.enabled = false;
            GameEndChecker.StartEndGame(reason);
            predicate = null;
        }
    }
    public override GameEndPredicate Predicate() => new SoloKombatGameEndPredicate();
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

            if (RoundTime > 0) return false;

            var list = Main.AllPlayerControls.Where(x => !x.Is(CustomRoles.GM) && GetRankOfScore(x.PlayerId) == 1);
            var winner = list.FirstOrDefault();
            if (winner != null) CustomWinnerHolder.WinnerIds = new() { winner.PlayerId };
            else CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
            Main.DoBlockNameChange = true;

            return true;
        }
    }

    private static Dictionary<byte, int> KBScore = new();
    public static string GetDisplayScore(byte playerId)
    {
        int rank = GetRankOfScore(playerId);
        string score = KBScore.TryGetValue(playerId, out var s) ? $"{s}" : "Invalid";
        string text = string.Format(GetString("KBDisplayScore"), rank.ToString(), score);
        Color color = Utils.GetRoleColor(CustomRoles.KB_Normal);
        return Utils.ColorString(color, text);
    }
    public static int GetRankOfScore(byte playerId)
    {
        if (!GameStates.IsLobby)
        {
            foreach (var player in Main.AllPlayerControls)
            {
                var role = player.GetRoleClass() as KB_Normal;
                KBScore.TryAdd(player.PlayerId, role?.Score ?? -255);
                KBScore[player.PlayerId] = role?.Score ?? -255;
            }
        }
        try
        {
            int ms = KBScore[playerId];
            int rank = 1 + KBScore.Values.Count(x => x > ms);
            rank += KBScore.Where(x => x.Value == ms).ToList().IndexOf(new(playerId, ms));
            return rank;
        }
        catch
        {
            return Main.AllPlayerControls.Count();
        }
    }
}