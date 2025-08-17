using UnityEngine;
using Object = UnityEngine.Object;

namespace TONX;

public static class ObjectHelper
{
    public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> collection)
    {
        return collection.SelectMany(x => x);
    }

    /// <summary>
    /// オブジェクトの<see cref="TextTranslatorTMP"/>コンポーネントを破棄します
    /// </summary>
    public static void DestroyTranslator(this GameObject obj)
    {
        if (obj == null) return;
        obj.ForEachChild((Il2CppSystem.Action<GameObject>)DestroyTranslator);
        TextTranslatorTMP[] translator = obj.GetComponentsInChildren<TextTranslatorTMP>(true);
        translator?.Do(Object.Destroy);
    }
    /// <summary>
    /// オブジェクトの<see cref="TextTranslatorTMP"/>コンポーネントを破棄します
    /// </summary>
    public static void DestroyTranslator(this MonoBehaviour obj) => obj?.gameObject?.DestroyTranslator();
}
