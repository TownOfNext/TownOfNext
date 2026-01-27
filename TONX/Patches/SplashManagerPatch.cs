using System.Collections;
using UnityEngine;
using BepInEx.Unity.IL2CPP.Utils;
using TMPro;
using TONX.Modules;

namespace TONX.Patches;

[HarmonyPatch(typeof(SplashManager))]
public class SplashManagerPatch
{
    [HarmonyPatch(nameof(SplashManager.Start)), HarmonyPrefix]
    public static bool Start(SplashManager __instance)
    {
        __instance.startTime = Time.time;
        __instance.StartCoroutine(InitializeRefData(__instance));
        return false;
    }
    
    private static IEnumerator InitializeRefData(SplashManager instance)
    {
        var logoAnimator = GameObject.Find("LogoAnimator");
        logoAnimator.SetActive(false);
        CreateTextObj();
        yield return StartLogoAnima();
        yield return DestroyableSingleton<ReferenceDataManager>.Instance.Initialize();
        try
        {
            DestroyableSingleton<TranslationController>.Instance.Initialize();
        }
        catch
        {
            /* Ignored */
        }
        instance.sceneChanger.BeginLoadingScene();
        instance.doneLoadingRefdata = true;
    }

    private static IEnumerator StartLogoAnima()
    {
        var logoObj = new GameObject("Logo")
        {
            layer = 5
        };
        logoObj.transform.localPosition+=Vector3.back*10;
        var logoRenderer = logoObj.AddComponent<SpriteRenderer>();
        logoRenderer.sprite = Utils.LoadSprite("TONX.Resources.Images.TONX-Logo.png", 100f);

        if (logoRenderer == null) yield break;
        var animControllerObj = new GameObject("TONX_LogoAnimationController_Instance");
        var controller = animControllerObj.AddComponent<LogoAnimationController>();
        controller.Initialize(logoRenderer);

        yield return controller.PlayAnimationSequence();
    }
    
    private static void CreateTextObj()
    {
        var versionTextObj = new GameObject("VersionText")
        {
            layer = 5
        };
        versionTextObj.transform.localPosition+=Vector3.back*10;
        
        var versionTMP = versionTextObj.AddComponent<TextMeshPro>();
        versionTMP.text = $"Town Of Next - v{Main.PluginVersion}";
        versionTMP.alignment = TextAlignmentOptions.Right;
        versionTMP.color = ((Color)Main.ModColor32).SetAlpha(0.7f).ShadeColor(0.9f);
        versionTMP.fontSize = 2;
        versionTextObj.GetComponent<RectTransform>().sizeDelta = new Vector2(2, 2);
        var versionAP = versionTextObj.AddComponent<AspectPosition>();
        versionAP.Alignment = AspectPosition.EdgeAlignments.RightBottom;
        versionAP.DistanceFromEdge = new Vector3(1.1f, 0.1f,-10f);
        versionAP.updateAlways = true;
    }
    
}