using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TONX.Modules.NameTagInterface;

public static class NameTagNewWindow
{
    public static GameObject Window { get; private set; }
    public static GameObject Info { get; private set; }
    public static GameObject EnterBox { get; private set; }
    public static GameObject ConfirmButton { get; private set; }
    public static void Open()
    {
        if (Window == null) Init();
        if (Window == null) return;
        if (NameTagEditMenu.Menu?.active ?? false) return;
        Window.SetActive(true);
        EnterBox.GetComponent<TextBoxTMP>().Clear();
    }
    public static void Init()
    {
        if (!GameStates.IsNotJoined) return;

        Window = Object.Instantiate(AccountManager.Instance.transform.FindChild("InfoTextBox").gameObject, NameTagPanel.CustomBackground.transform.parent);
        Window.name = "New Name Tag Window";
        Window.transform.FindChild("Background").localScale *= 0.7f;

        Object.Destroy(Window.transform.FindChild("Button2").gameObject);

        var closeButton = Object.Instantiate(Window.transform.parent.FindChild("CloseButton"), Window.transform);
        closeButton.transform.localPosition = new Vector3(2.4f, 1.2f, -1f);
        closeButton.transform.localScale = new Vector3(1f, 1f, 1f);
        closeButton.GetComponent<PassiveButton>().OnClick = new();
        closeButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
        {
            Window.SetActive(false);
        }));

        var titlePrefab = Window.transform.FindChild("TitleText_TMP").gameObject;
        titlePrefab.name = "Title Prefab";
        var infoPrefab = Window.transform.FindChild("InfoText_TMP").gameObject;
        infoPrefab.name = "Info Prefab";
        var buttonPrefab = Window.transform.FindChild("Button1").gameObject;
        buttonPrefab.name = "Button Prefab";
        buttonPrefab.GetComponent<PassiveButton>().OnClick = new();
        var enterPrefab = Object.Instantiate(AccountManager.Instance.transform.FindChild("PremissionRequestWindow/GuardianEmailConfirm").gameObject, Window.transform);
        enterPrefab.name = "Enter Box Prefab";
        enterPrefab.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        Object.Destroy(enterPrefab.GetComponent<EmailTextBehaviour>());

        Info = Object.Instantiate(infoPrefab, Window.transform);
        Info.name = "Enter Friend Code Description";
        Info.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        var colorInfoTmp = Info.GetComponent<TextMeshPro>();
        colorInfoTmp.text = GetString("PleaseEnterFriendCode");

        EnterBox = Object.Instantiate(enterPrefab, Window.transform);
        EnterBox.name = "Enter Friend Code Box";
        EnterBox.transform.localPosition = new Vector3(0f, -0.04f, 0f);
        var enterBoxTBT = EnterBox.GetComponent<TextBoxTMP>();
        enterBoxTBT.AllowEmail = false;
        enterBoxTBT.AllowSymbols = true;
        enterBoxTBT.AllowPaste = true;

        ConfirmButton = Object.Instantiate(buttonPrefab, Window.transform);
        ConfirmButton.name = "Confirm Button";
        ConfirmButton.transform.localPosition = new Vector3(0, -0.8f, 0f);
        ConfirmButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
        {
            var code = EnterBox.GetComponent<TextBoxTMP>().text.ToLower().Trim().Replace("-", "#").Replace("—", "#").Replace(" ", string.Empty);
            var reg = new Regex(@"^[a-z]+#[0-9]{4}$");
            if (NameTagManager.AllNameTags.TryGetValue(code, out var tag) && !tag.Isinternal)
            {
                ConfirmButton.SetActive(false);
                colorInfoTmp.text = GetString("FriendCodeAlreadyExist");
                colorInfoTmp.color = Color.blue;
            }
            else if (!reg.Match(code).Success)
            {
                ConfirmButton.SetActive(false);
                colorInfoTmp.text = GetString("FriendCodeIncorrect");
                colorInfoTmp.color = Color.red;
            }
            else
            {
                Window.SetActive(false);
                NameTagEditMenu.Toggle(code, true);
                return;
            }
            new LateTask(() =>
            {
                colorInfoTmp.text = GetString("PleaseEnterFriendCode");
                colorInfoTmp.color = Color.white;
                ConfirmButton.SetActive(true);
            }, 1.2f, "Reactivate Enter Box");
        }));
        var upperButtonTmp = ConfirmButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>();
        upperButtonTmp.text = GetString(StringNames.Confirm);

        titlePrefab.SetActive(false);
        infoPrefab.SetActive(false);
        buttonPrefab.SetActive(false);
        enterPrefab.SetActive(false);
    }
}
