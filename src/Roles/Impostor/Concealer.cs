using AmongUs.GameOptions;
using TONX.Roles.Core.Interfaces;

namespace TONX.Roles.Impostor;
public sealed class Concealer : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Concealer),
            player => new Concealer(player),
            CustomRoles.Concealer,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            4500,
            SetupOptionItem,
            "co|隱蔽者|隐蔽|小黑人",
            experimental: true
        );
    public Concealer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionShapeshiftCooldown;
    static OptionItem OptionShapeshiftDuration;

    private static void SetupOptionItem()
    {
        OptionShapeshiftCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.ShapeshiftCooldown, new(2.5f, 180f, 2.5f), 25f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionShapeshiftDuration = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.ShapeshiftDuration, new(2.5f, 180f, 2.5f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = OptionShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = OptionShapeshiftDuration.GetFloat();
    }
    private bool Shapeshifting = false;
    public override void OnShapeshift(PlayerControl target)
    {
        Shapeshifting = !Is(target);

        if (!AmongUsClient.Instance.AmHost) return;

        Camouflage.CheckCamouflage();
    }
    public override bool GetAbilityButtonSprite(out string buttonName)
    {
        buttonName = "Camo";
        return !Shapeshifting;
    }
    public static bool IsHidding
        => Main.AllAlivePlayerControls.Any(x => (x.GetRoleClass() is Concealer roleClass) && roleClass.Shapeshifting) && GameStates.IsInTask;
}