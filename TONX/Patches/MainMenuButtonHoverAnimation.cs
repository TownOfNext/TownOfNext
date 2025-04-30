using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TONX;

[HarmonyPatch]
public class MainMenuButtonHoverAnimation
{
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void Start_Postfix(MainMenuManager __instance)
    {
        var mainButtons = GameObject.Find("Main Buttons");
        mainButtons.ForEachChild((Il2CppSystem.Action<GameObject>)Init);
    }
    public static void RefreshButtons(GameObject obj)
    {
        AllButtons = new();
        obj.ForEachChild((Il2CppSystem.Action<GameObject>)Init);
    }
    static void Init(GameObject obj)
    {
        if (obj.name is "BottomButtonBounds" or "Divider") return;
        if (AllButtons.ContainsKey(obj)) return;
        SetButtonStatus(obj, false);
        var pb = obj.GetComponent<PassiveButton>();
        pb.OnMouseOver = new();
        pb.OnMouseOver.AddListener((Action)(() => SetButtonStatus(obj, true)));
        pb.OnMouseOut = new();
        pb.OnMouseOut.AddListener((Action)(() => SetButtonStatus(obj, false)));
    }

    private static Dictionary<GameObject, (Vector3, bool)> AllButtons = new();
    private static void SetButtonStatus(GameObject obj, bool active)
    {
        AllButtons.TryAdd(obj, (obj.transform.position, active));
        AllButtons[obj] = (AllButtons[obj].Item1, active);
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.LateUpdate)), HarmonyPostfix]
    private static void Update_Postfix(MainMenuManager __instance)
    {
        if (GameObject.Find("MainUI") == null) return;

        foreach (var kvp in AllButtons.Where(x => x.Key != null && x.Key.active))
        {
            var button = kvp.Key;
            var pos = button.transform.position;
            var targetPos = kvp.Value.Item1 + new Vector3(kvp.Value.Item2 ? 0.35f : 0f, 0f, 0f);
            if (kvp.Value.Item2 && pos.x > (kvp.Value.Item1.x + 0.2f)) continue;
            button.transform.position = kvp.Value.Item2
                ? Vector3.Lerp(pos, targetPos, Time.deltaTime * 2f)
                : Vector3.MoveTowards(pos, targetPos, Time.deltaTime * 2f);
        }
    }
}
