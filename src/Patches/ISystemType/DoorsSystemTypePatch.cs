using Hazel;
using TONX.Roles.Core.Interfaces;

namespace TONX.Patches.ISystemType;

[HarmonyPatch(typeof(DoorsSystemType), nameof(DoorsSystemType.UpdateSystem))]
public static class DoorsSystemTypeUpdateSystemPatch
{
    public static bool Prefix(DoorsSystemType __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
    {
        byte amount;
        {
            var newReader = MessageReader.Get(msgReader);
            amount = newReader.ReadByte();
            newReader.Recycle();
        }

        return player.MultipleBooleanFunc<ISystemTypeUpdateHook>(s => s.UpdateDoorsSystem(__instance, amount));
    }
}