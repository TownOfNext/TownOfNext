using AmongUs.GameOptions;

namespace TONX.Roles.Crewmate;
public sealed class Celebrity : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Celebrity),
            player => new Celebrity(player),
            CustomRoles.Celebrity,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20400,
            SetupOptionItem,
            "se|網紅",
            "#ee4a55"
        );
    public Celebrity(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        MsgToSend = new();
    }

    public static OptionItem OptionImpKnow;
    public static OptionItem OptionNeutralKillerKnow;
    public static OptionItem OptionNeutralNonKillerKnow;
    enum OptionName
    {
        ImpKnowCelebrityDead,
        NeutralKillerKnowCelebrityDead,
        NeutralNonKillerKnowCelebrityDead,
    }

    private List<(string, byte, string)> MsgToSend;
    private static void SetupOptionItem()
    {
        OptionImpKnow = BooleanOptionItem.Create(RoleInfo, 10, OptionName.ImpKnowCelebrityDead, false, false);
        OptionNeutralKillerKnow = BooleanOptionItem.Create(RoleInfo, 11, OptionName.NeutralKillerKnowCelebrityDead, false, false);
        OptionNeutralNonKillerKnow = BooleanOptionItem.Create(RoleInfo, 12, OptionName.NeutralNonKillerKnowCelebrityDead, false, false);
    }
    public static bool CanSeeKillFlash(PlayerControl player)
    {
        return !player.IsAlive()
            || player.IsCrew()
            || (player.IsImp() && OptionImpKnow.GetBool())
            || (player.IsNeutralKiller() && OptionNeutralKillerKnow.GetBool())
            || (player.IsNeutralNonKiller() && OptionNeutralNonKillerKnow.GetBool());
    }
    public override void OnPlayerDeath(PlayerControl player, CustomDeathReason deathReason, bool isOnMeeting = false)
    {
        if (!Is(player)) return;

        foreach (var pc in Main.AllPlayerControls.Where(x => CanSeeKillFlash(x) || player.Is(CustomRoles.Madmate)))
        {
            if (isOnMeeting)
            {
                Utils.SendMessage(string.Format(GetString("CelebrityDead"), pc.GetRealName()), pc.PlayerId, Utils.ColorString(RoleInfo.RoleColor, GetString("CelebrityNewsTitle"))); ;
            }
            else
            {
                MsgToSend.Add((string.Format(GetString("CelebrityDead"), pc.GetRealName()), pc.PlayerId, Utils.ColorString(RoleInfo.RoleColor, GetString("CelebrityNewsTitle"))));
                pc.Notify(Utils.ColorString(RoleInfo.RoleColor, GetString("OnCelebrityDead")));
            }
        }
    }
    public override void NotifyOnMeetingStart(ref List<(string, byte, string)> msgToSend)
    {
        if (MsgToSend?.Any() ?? false)
            msgToSend.AddRange(MsgToSend.ToArray());
        MsgToSend = new();
    }
}