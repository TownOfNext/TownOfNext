using Hazel;
using TONX.Roles.Core.Interfaces;

namespace TONX.Patches.ISystemType;

[HarmonyPatch(typeof(HqHudSystemType), nameof(HqHudSystemType.UpdateSystem))]
public static class HqHudSystemTypeUpdateSystemPatch
{
    public static bool Prefix(HqHudSystemType __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
    {
        byte amount;
        {
            var newReader = MessageReader.Get(msgReader);
            amount = newReader.ReadByte();
            newReader.Recycle();
        }

        var tags = (HqHudSystemType.Tags)(amount & HqHudSystemType.TagMask);
        
        return player.MultipleBooleanFunc<ISystemTypeUpdateHook>(s => s.UpdateHqHudSystem(__instance, amount));
    }
    public static void Postfix()
    {
        Camouflage.CheckCamouflage();
        Utils.NotifyRoles();
    }
}