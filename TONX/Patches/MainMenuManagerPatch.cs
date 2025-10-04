using TMPro;
using TONX.Templates;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TONX;

[HarmonyPatch]
public class MainMenuManagerPatch
{
    public static MainMenuManager Instance { get; private set; }

    public static GameObject InviteButton;
    //public static GameObject WebsiteButton;
    public static GameObject UpdateButton;
    public static GameObject PlayButton;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.OpenGameModeMenu))]
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.OpenAccountMenu))]
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.OpenCredits))]
    [HarmonyPrefix, HarmonyPriority(Priority.Last)]
    public static void ShowRightPanel() => ShowingPanel = true;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Open))]
    [HarmonyPatch(typeof(AnnouncementPopUp), nameof(AnnouncementPopUp.Show))]
    [HarmonyPrefix, HarmonyPriority(Priority.Last)]
    public static void HideRightPanel()
    {
        ShowingPanel = false;
        AccountManager.Instance?.transform?.FindChild("AccountTab/AccountWindow")?.gameObject?.SetActive(false);
    }

    public static void ShowRightPanelImmediately()
    {
        ShowingPanel = true;
        TitleLogoPatch.RightPanel.transform.localPosition = TitleLogoPatch.RightPanelOp;
        Instance.OpenGameModeMenu();
    }

    private static bool isOnline = false;
    public static bool ShowedBak = false;
    private static bool ShowingPanel = false;
    [HarmonyPatch(typeof(SignInStatusComponent), nameof(SignInStatusComponent.SetOnline)), HarmonyPostfix]
    public static void SetOnline_Postfix() { _ = new LateTask(() => { isOnline = true; NameTagManager.Init(); }, 0.1f, "Set Online Status"); }
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.LateUpdate)), HarmonyPostfix]
    public static void MainMenuManager_LateUpdate()
    {
        var asa1 = InviteButton?.GetComponent<PassiveButton>()?.GetComponent<AspectScaledAsset>() ?? null;
        asa1?.ScaleObject(Utils.GetResolutionOffset(Screen.width, Screen.height));

        CustomPopup.Update();

        if (GameObject.Find("MainUI") == null) ShowingPanel = false;

        if (TitleLogoPatch.RightPanel != null)
        {
            var pos1 = TitleLogoPatch.RightPanel.transform.localPosition;
            var pos3 = new Vector3(TitleLogoPatch.RightPanelOp.x * Utils.GetResolutionOffset(Screen.width, Screen.height), TitleLogoPatch.RightPanelOp.y, TitleLogoPatch.RightPanelOp.z);
            Vector3 lerp1 = Vector3.Lerp(pos1, ShowingPanel ? pos3 : TitleLogoPatch.RightPanelOp + new Vector3(20f, 0f, 0f), Time.deltaTime * (ShowingPanel ? 3f : 2f));
            if (ShowingPanel
                ? TitleLogoPatch.RightPanel.transform.localPosition.x > pos3.x + 0.03f
                : TitleLogoPatch.RightPanel.transform.localPosition.x < TitleLogoPatch.RightPanelOp.x + 19f
                ) TitleLogoPatch.RightPanel.transform.localPosition = lerp1;
        }

        if (ShowedBak || !isOnline) return;
        var bak = GameObject.Find("BackgroundTexture");
        if (bak == null || !bak.active) return;
        var pos2 = bak.transform.position;
        Vector3 lerp2 = Vector3.Lerp(pos2, new Vector3(pos2.x, 7.1f, pos2.z), Time.deltaTime * 1.4f);
        bak.transform.position = lerp2;
        if (pos2.y > 7f) ShowedBak = true;
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix]
    public static void Start_Postfix(MainMenuManager __instance)
    {
        Instance = __instance;

        SimpleButton.SetBase(__instance.quitButton);

        int row = 1; int col = 0;
        void OpenUrl(string url)
        {
#if Android
            OpenURLAndroid(url);
#elif Windows
            Application.OpenURL(url);
#endif
        }

        GameObject CreatButton(string text, Action action)
        {
            col++; if (col > 2) { col = 1; row++; }
            var template = col == 1 ? __instance.creditsButton.gameObject : __instance.quitButton.gameObject;
            var button = Object.Instantiate(template, template.transform.parent);
            button.transform.transform.FindChild("FontPlacer").GetChild(0).gameObject.DestroyTranslator();
            var buttonText = button.transform.FindChild("FontPlacer").GetChild(0).GetComponent<TextMeshPro>();
            buttonText.text = text;
            PassiveButton passiveButton = button.GetComponent<PassiveButton>();
            passiveButton.OnClick = new();
            passiveButton.OnClick.AddListener(action);
            AspectPosition aspectPosition = button.GetComponent<AspectPosition>();
#if Android
            var yPosition = col == 1 ? 0.5f - 0.08f * row : 0.5f - 0.08f * (row - 1);
#else
            var yPosition = 0.5f - 0.08f * row;
#endif

            aspectPosition.anchorPoint = new Vector2(
                col == 1 ? 0.415f : 0.583f,
                yPosition
            );
            return button;
        }

        string extraLinkName = "Github";
        string extraLinkUrl = Main.GithubRepoUrl;
        bool extraLinkEnabled = Main.ShowGithubUrl;
        // if (IsChineseUser ? Main.ShowQQButton : Main.ShowDiscordButton)
        // {
        //     extraLinkName = IsChineseUser ? "QQ群" : "Discord";
        //     extraLinkUrl = IsChineseUser ? Main.QQInviteUrl : Main.DiscordInviteUrl;
        //     extraLinkEnabled = true;
        // }

        if (InviteButton == null) InviteButton = CreatButton(extraLinkName, () => { OpenUrl(extraLinkUrl); });
        InviteButton.gameObject.SetActive(extraLinkEnabled);
        InviteButton.name = "TONX Extra Link Button";

        // if (WebsiteButton == null) WebsiteButton = CreatButton(GetString("Website"), () => Application.OpenURL(Main.WebsiteUrl));
        // WebsiteButton.gameObject.SetActive(Main.ShowWebsiteButton);
        // WebsiteButton.name = "TONX Website Button";
#if Windows
        if (UpdateButton == null)
        {
            PlayButton = __instance.playButton.gameObject;
            UpdateButton = Object.Instantiate(PlayButton, PlayButton.transform.parent);
            UpdateButton.name = "TONX Update Button";
            UpdateButton.transform.localPosition = PlayButton.transform.localPosition - new Vector3(0f, 0f, 3f);
            var passiveButton = UpdateButton.GetComponent<PassiveButton>();
            passiveButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new Color(0.49f, 0.34f, 0.62f, 0.8f);
            passiveButton.activeSprites.GetComponent<SpriteRenderer>().color = new Color(0.49f, 0.34f, 0.62f, 1f);
            passiveButton.OnClick = new();
            passiveButton.OnClick.AddListener((Action)(() =>
            {
                PlayButton.SetActive(true);
                UpdateButton.SetActive(false);
                if (!DebugModeManager.AmDebugger || !Input.GetKey(KeyCode.LeftShift))
                    ModUpdater.StartUpdate();
            }));
            UpdateButton.transform.transform.FindChild("FontPlacer").GetChild(0).gameObject.DestroyTranslator();
        }
#endif

        Application.targetFrameRate = Main.UnlockFPS.Value ? 165 : 60;
    }
#if Android
    private static void OpenURLAndroid(string url)
    {
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
            AndroidJavaObject intentObject =
                new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW");
            AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
            AndroidJavaObject uriObject = uriClass.CallStatic<AndroidJavaObject>("parse", url);
            intentObject.Call<AndroidJavaObject>("setData", uriObject);
            var FLAG_ACTIVITY_NEW_TASK = 0x10000000;
            intentObject.Call<AndroidJavaObject>("setFlags", FLAG_ACTIVITY_NEW_TASK);
            currentActivity.Call("startActivity", intentObject);
            unityPlayer.Dispose();
            currentActivity.Dispose();
            intentClass.Dispose();
            intentObject.Dispose();
            uriClass.Dispose();
            uriObject.Dispose();
        }
        catch
        {
            Application.OpenURL(url);
        }
    }
#endif
}