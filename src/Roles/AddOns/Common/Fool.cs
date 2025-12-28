using TONX.Roles.Core.Interfaces;

namespace TONX.Roles.AddOns.Common;
public sealed class Fool : AddonBase, ISystemTypeUpdateHook
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForAddon(
            typeof(Fool),
            player => new Fool(player),
            CustomRoles.Fool,
            81300,
            SetupCustomOption,
            "fo|蠢蛋|笨蛋|蠢狗|傻逼",
            "#e6e7ff",
            conflicts: Conflicts
        );
    public Fool(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public static OptionItem OptionImpFoolCanNotSabotage;
    public static OptionItem OptionImpFoolCanNotOpenDoor;

    enum OptionName
    {
        ImpFoolCanNotSabotage,
        FoolCanNotOpenDoor
    }

    private static List<CustomRoles> Conflicts = new() { CustomRoles.Repairman };
    private static void SetupCustomOption()
    {
        OptionImpFoolCanNotSabotage = BooleanOptionItem.Create(RoleInfo, 20, OptionName.ImpFoolCanNotSabotage, true, false);
        OptionImpFoolCanNotOpenDoor = BooleanOptionItem.Create(RoleInfo, 21, OptionName.FoolCanNotOpenDoor, false, false);
    }

    public override bool OnSabotage(PlayerControl player, SystemTypes systemType)
    {
        return !(OptionImpFoolCanNotSabotage.GetBool() && player.IsImp());
    }
    bool ISystemTypeUpdateHook.UpdateReactorSystem(ReactorSystemType reactorSystem, byte amount) => false;
    bool ISystemTypeUpdateHook.UpdateHeliSabotageSystem(HeliSabotageSystem heliSabotageSystem, byte amount) => false;
    bool ISystemTypeUpdateHook.UpdateLifeSuppSystem(LifeSuppSystemType lifeSuppSystem, byte amount) => false;
    bool ISystemTypeUpdateHook.UpdateHqHudSystem(HqHudSystemType hqHudSystemType, byte amount) => false;
    bool ISystemTypeUpdateHook.UpdateSwitchSystem(SwitchSystem switchSystem, byte amount) => false;
    bool ISystemTypeUpdateHook.UpdateDoorsSystem(DoorsSystemType doorsSystem, byte amount)
        => !OptionImpFoolCanNotOpenDoor.GetBool();
}