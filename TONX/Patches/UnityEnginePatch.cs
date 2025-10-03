using Object = UnityEngine.Object;

namespace TONX.Patches;

[HarmonyPatch(typeof(Object), nameof(Object.Destroy), typeof(Object))]
public class UnityEnginePatch
{
    public static void Prefix([HarmonyArgument(0)] Object obj)
    {
        try
        {
#if Android
            if (obj.name is "IntroCutscene")
                IntroCutscenePatch.OnDestroy_Postfix();
#endif
        }
        catch
        {
        }
        
    }
}