using AmongUs.Data;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using static TONX.NameTagManager;
using Component = TONX.NameTagManager.Component;
using Object = UnityEngine.Object;

namespace TONX.Modules.NameTagInterface;

public static class NameTagEditMenu
{
    public static GameObject Menu { get; private set; }

    public static GameObject EditUpperButton { get; private set; }
    public static GameObject EditPrefixButton { get; private set; }
    public static GameObject EditSuffixButton { get; private set; }
    public static GameObject EditNameButton { get; private set; }

    public static GameObject Preview { get; private set; }

    public static GameObject Text_Info { get; private set; }
    public static GameObject Text_Enter { get; private set; }

    public static GameObject Size_Info { get; private set; }
    public static GameObject Size_Enter { get; private set; }

    public static GameObject Color_Info { get; private set; }
    public static GameObject Color1_Enter { get; private set; }
    public static GameObject Color2_Enter { get; private set; }
    public static GameObject Color3_Enter { get; private set; }

    public static GameObject PreviewButton { get; private set; }
    public static GameObject SaveAndExitButton { get; private set; }
    public static GameObject DeleteButton { get; private set; }

    private static string FriendCode;
    private static NameTag CacheTag;
    private static ComponentType CurrentComponent;
    private enum ComponentType { Upper, Prefix, Suffix, Name }

    public static void Hide()
    {
        if (Menu != null)
            Menu?.SetActive(false);
    }
#nullable enable
    public static void Toggle(string? friendCode, bool? on)
    {
        if (Menu == null) on ??= true;
        else on ??= !Menu.activeSelf;
        if (!GameStates.IsNotJoined || !on.Value)
        {
            if (Menu != null) Menu?.SetActive(false);
            return;
        }
        if (Menu == null) Init();
        if (Menu == null) return;
        Menu.SetActive(on.Value);
        FriendCode = friendCode;
        CacheTag = (friendCode != null && AllExternalNameTags.TryGetValue(friendCode, out var tag)) ? DeepClone(tag) : new NameTag();
        if (!Menu.activeSelf) return;
        LoadComponent(CacheTag?.UpperText);
        SetButtonHighlight(EditUpperButton);
        CurrentComponent = ComponentType.Upper;
        UpdatePreview();
    }
    private static void SetButtonHighlight(GameObject obj)
    {
        EditUpperButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>().color = Palette.DisabledGrey;
        EditPrefixButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>().color = Palette.DisabledGrey;
        EditSuffixButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>().color = Palette.DisabledGrey;
        EditNameButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>().color = Palette.DisabledGrey;
        obj.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>().color = new Color32(0, 164, 255, 255);
    }
    private static void LoadComponent(Component? com, bool name = false)
    {
        Text_Enter.GetComponent<TextBoxTMP>().enabled = !name;
        Text_Enter.GetComponent<TextBoxTMP>().SetText(!name ? (com?.Text ?? "") : GetString("CanNotEdit"));
        Size_Enter.GetComponent<TextBoxTMP>().SetText((com?.SizePercentage ?? 100).ToString());
        Color1_Enter.GetComponent<TextBoxTMP>().Clear();
        Color2_Enter.GetComponent<TextBoxTMP>().Clear();
        Color3_Enter.GetComponent<TextBoxTMP>().Clear();
        if (com?.Gradient?.IsValid ?? false)
        {
            int colorNum = 1;
            foreach (var color in com.Gradient.Colors)
            {
                (colorNum switch
                {
                    1 => Color1_Enter.transform,
                    2 => Color2_Enter.transform,
                    3 => Color3_Enter.transform,
                    _ => throw new NotImplementedException()
                }
                ).GetComponent<TextBoxTMP>().SetText(ColorUtility.ToHtmlStringRGBA(color)[..6]);
                colorNum++;
            }
        }
        else if (com?.TextColor != null)
        {
            Color1_Enter.GetComponent<TextBoxTMP>().SetText(ColorUtility.ToHtmlStringRGBA(com.TextColor.Value)[..6]);
        }
    }
#nullable disable
    private static void UpdatePreview()
    {
        if (!Menu.active || CacheTag == null || Preview == null) return;
        var name = CacheTag.Apply(DataManager.player.Customization.Name, false);
        Preview.GetComponent<TextMeshPro>().text = name;
    }
    private static void SaveToCache(ComponentType type)
    {
        var com = new Component();

        string text = Text_Enter.GetComponent<TextBoxTMP>().text.Trim();
        if (text != "" && type != ComponentType.Name) com.Text = text;
        string size = Size_Enter.GetComponent<TextBoxTMP>().text.Trim();
        if (size != "" && float.TryParse(size, out var sizef)) com.SizePercentage = sizef;
        string color1 = Color1_Enter.GetComponent<TextBoxTMP>().text.Trim();
        string color2 = Color2_Enter.GetComponent<TextBoxTMP>().text.Trim();
        string color3 = Color3_Enter.GetComponent<TextBoxTMP>().text.Trim();
        List<Color> colors = new();
        if (color1 != "" && ColorUtility.DoTryParseHtmlColor("#" + color1, out var c1)) colors.Add(c1);
        if (color2 != "" && ColorUtility.DoTryParseHtmlColor("#" + color2, out var c2)) colors.Add(c2);
        if (color3 != "" && ColorUtility.DoTryParseHtmlColor("#" + color3, out var c3)) colors.Add(c3);
        if (colors.Count > 1) com.Gradient = new(colors.ToArray());
        else if (colors.Count == 1) com.TextColor = colors[0];
        com.Spaced = default;

        switch (type)
        {
            case ComponentType.Upper:
                CacheTag.UpperText = com;
                break;
            case ComponentType.Prefix:
                CacheTag.Prefix = com;
                break;
            case ComponentType.Suffix:
                CacheTag.Suffix = com;
                break;
            case ComponentType.Name:
                CacheTag.Name = com;
                break;
        };
    }
#nullable enable
    private enum ComponentName
    {
        UpperText,
        Prefix,
        Suffix,
        Name
    }
    private static bool SaveToFile(string friendCode, NameTag tag)
    {
        if (FriendCode is null or "") return false;

        Il2CppSystem.IO.StringWriter sw = new();
        JsonWriter JsonWriter = new JsonTextWriter(sw);
        JsonWriter.WriteStartObject();

        foreach (ComponentName comName in Enum.GetValues(typeof(ComponentName)))
        {
            var com = comName switch
            {
                ComponentName.UpperText => tag.UpperText,
                ComponentName.Prefix => tag.Prefix,
                ComponentName.Suffix => tag.Suffix,
                ComponentName.Name => tag.Name,
                _ => null
            };

            if (com == null) continue;

            JsonWriter.WritePropertyName(Enum.GetName(typeof(ComponentName), comName));
            JsonWriter.WriteStartObject();

            if (com.Text != null && comName != ComponentName.Name)
            {
                JsonWriter.WritePropertyName("Text");
                JsonWriter.WriteValue(com.Text);
            }
            if (com.SizePercentage != null)
            {
                JsonWriter.WritePropertyName("SizePercentage");
                JsonWriter.WriteValue(com.SizePercentage.ToString());
            }
            if (com.Gradient != null && com.Gradient.IsValid)
            {
                string colors = "";
                com.Gradient.Colors.Do(c => colors += "#" + ColorUtility.ToHtmlStringRGBA(c)[..6] + ",");
                JsonWriter.WritePropertyName("Gradient");
                JsonWriter.WriteValue(colors.TrimEnd(','));
            }
            else if (com.TextColor != null)
            {
                JsonWriter.WritePropertyName("Color");
                JsonWriter.WriteValue("#" + ColorUtility.ToHtmlStringRGBA(com.TextColor.Value)[..6]);
            }
            if (comName is not ComponentName.UpperText and not ComponentName.Name)
            {
                JsonWriter.WritePropertyName("Spaced");
                JsonWriter.WriteValue(com.Spaced.ToString());
            }
            JsonWriter.WriteEndObject();
        }

        JsonWriter.WriteEndObject();
        sw.Flush();

        string fileName = TAGS_DIRECTORY_PATH + friendCode.Trim() + ".json";
        if (!File.Exists(fileName)) File.Create(fileName).Close();
        File.WriteAllText(fileName, sw.ToString());
        return true;
    }
#nullable disable
    public static void Init()
    {
        if (!GameStates.IsNotJoined) return;

        Menu = Object.Instantiate(AccountManager.Instance.transform.FindChild("InfoTextBox").gameObject, NameTagPanel.CustomBackground.transform.parent);
        Menu.name = "Name Tag Edit Menu";
        Menu.transform.SetLocalZ(-30f);
        Menu.transform.FindChild("Background").localScale *= 1.4f;

        Object.Destroy(Menu.transform.FindChild("Button2").gameObject);

        var closeButton = Object.Instantiate(Menu.transform.parent.FindChild("CloseButton"), Menu.transform);
        closeButton.transform.localPosition = new Vector3(4.9f, 2.5f, -1f);
        closeButton.transform.localScale = new Vector3(1f, 1f, 1f);
        closeButton.GetComponent<PassiveButton>().OnClick = new();
        closeButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
        {
            Toggle(null, false);
        }));

        var titlePrefab = Menu.transform.FindChild("TitleText_TMP").gameObject;
        titlePrefab.name = "Title Prefab";
        var infoPrefab = Menu.transform.FindChild("InfoText_TMP").gameObject;
        infoPrefab.name = "Info Prefab";
        var buttonPrefab = Menu.transform.FindChild("Button1").gameObject;
        buttonPrefab.name = "Button Prefab";
        buttonPrefab.GetComponent<PassiveButton>().OnClick = new();
        var enterPrefab = Object.Instantiate(AccountManager.Instance.transform.FindChild("PremissionRequestWindow/GuardianEmailConfirm").gameObject, Menu.transform);
        enterPrefab.name = "Enter Box Prefab";
        enterPrefab.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        enterPrefab.GetComponent<TextBoxTMP>().AllowPaste = true;
        Object.Destroy(enterPrefab.GetComponent<EmailTextBehaviour>());

        int editButtonNum = 0;

        PreviewButton = Object.Instantiate(buttonPrefab, Menu.transform);
        PreviewButton.name = "Refresh Preview Button";
        PreviewButton.transform.localPosition = new Vector3(1.2f, -2.5f, 0f);
        PreviewButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
        {
            SaveToCache(CurrentComponent);
            UpdatePreview();
        }));
        var previewButtonTmp = PreviewButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>();
        previewButtonTmp.text = GetString("RefreshPreview");

        SaveAndExitButton = Object.Instantiate(buttonPrefab, Menu.transform);
        SaveAndExitButton.name = "Save And Exit Button";
        SaveAndExitButton.transform.localPosition = new Vector3(3.5f, -2.5f, 0f);
        SaveAndExitButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
        {
            SaveToCache(CurrentComponent);
            SaveToFile(FriendCode, CacheTag);
            ReloadTag(FriendCode);
            NameTagPanel.RefreshTagList();
            Toggle(null, false);
        }));
        var saveButtonTmp = SaveAndExitButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>();
        saveButtonTmp.text = GetString("SaveAndClose");

        DeleteButton = Object.Instantiate(buttonPrefab, Menu.transform);
        DeleteButton.name = "Delete Name Tag Button";
        DeleteButton.transform.localPosition = new Vector3(-3.5f, -2.5f, 0f);
        DeleteButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
        {
            string fileName = TAGS_DIRECTORY_PATH + FriendCode.Trim() + ".json";
            if (File.Exists(fileName)) File.Delete(fileName);
            ReloadTag(FriendCode);
            NameTagPanel.RefreshTagList();
            Toggle(null, false);
        }));
        var deleteButtonTmp = DeleteButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>();
        deleteButtonTmp.color = Color.red;
        deleteButtonTmp.text = GetString("Delete");

        EditUpperButton = Object.Instantiate(buttonPrefab, Menu.transform);
        EditUpperButton.name = "Edit Upper Button";
        EditUpperButton.transform.localPosition = new Vector3(-3.6f + 2.2f * editButtonNum, 2f, 0f);
        EditUpperButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
        {
            SaveToCache(CurrentComponent);
            LoadComponent(CacheTag.UpperText);
            SetButtonHighlight(EditUpperButton);
            CurrentComponent = ComponentType.Upper;
        }));
        var upperButtonTmp = EditUpperButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>();
        upperButtonTmp.text = GetString("UpperText");

        editButtonNum++;

        EditPrefixButton = Object.Instantiate(buttonPrefab, Menu.transform);
        EditPrefixButton.name = "Edit Prefix Button";
        EditPrefixButton.transform.localPosition = new Vector3(-3.6f + 2.2f * editButtonNum, 2f, 0f);
        EditPrefixButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
        {
            SaveToCache(CurrentComponent);
            LoadComponent(CacheTag.Prefix);
            SetButtonHighlight(EditPrefixButton);
            CurrentComponent = ComponentType.Prefix;
        }));
        var prefixButtonTmp = EditPrefixButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>();
        prefixButtonTmp.text = GetString("PrefixText");

        editButtonNum++;

        EditSuffixButton = Object.Instantiate(buttonPrefab, Menu.transform);
        EditSuffixButton.name = "Edit Sufix Button";
        EditSuffixButton.transform.localPosition = new Vector3(-3.6f + 2.2f * editButtonNum, 2f, 0f);
        EditSuffixButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
        {
            SaveToCache(CurrentComponent);
            LoadComponent(CacheTag.Suffix);
            SetButtonHighlight(EditSuffixButton);
            CurrentComponent = ComponentType.Suffix;
        }));
        var suffixButtonTmp = EditSuffixButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>();
        suffixButtonTmp.text = GetString("SuffixText");

        editButtonNum++;

        EditNameButton = Object.Instantiate(buttonPrefab, Menu.transform);
        EditNameButton.name = "Edit Name Button";
        EditNameButton.transform.localPosition = new Vector3(-3.6f + 2.2f * editButtonNum, 2f, 0f);
        EditNameButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
        {
            SaveToCache(CurrentComponent);
            LoadComponent(CacheTag.Name, true);
            SetButtonHighlight(EditNameButton);
            CurrentComponent = ComponentType.Name;
        }));
        var nameButtonTmp = EditNameButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>();
        nameButtonTmp.text = GetString("PlayerName");

        Preview = Object.Instantiate(titlePrefab, Menu.transform);
        Preview.name = "Preview Text";
        var previewTmp = Preview.GetComponent<TextMeshPro>();
        previewTmp.text = DataManager.player.Customization.Name;
        previewTmp.fontSize = 0.6f;

        Text_Info = Object.Instantiate(infoPrefab, Menu.transform);
        Text_Info.name = "Edit Text Description";
        Text_Info.transform.localPosition = new Vector3(-2.95f, 0f, 0f);
        var textInfoTmp = Text_Info.GetComponent<TextMeshPro>();
        textInfoTmp.text = GetString("TextContent");

        Text_Enter = Object.Instantiate(enterPrefab, Menu.transform);
        Text_Enter.name = "Edit Text Enter Box";
        Text_Enter.transform.localPosition = new Vector3(-2.9f, 0f, 0f);
        var textEnterTBT = Text_Enter.GetComponent<TextBoxTMP>();
        textEnterTBT.allowAllCharacters =
        textEnterTBT.AllowEmail =
        textEnterTBT.AllowSymbols = true;

        Size_Info = Object.Instantiate(infoPrefab, Menu.transform);
        Size_Info.name = "Edit Size Description";
        Size_Info.transform.localPosition = new Vector3(-2.95f, -1.5f, 0f);
        var sizeInfoTmp = Size_Info.GetComponent<TextMeshPro>();
        sizeInfoTmp.text = GetString("TextContentDescription");

        Size_Enter = Object.Instantiate(enterPrefab, Menu.transform);
        Size_Enter.name = "Edit Size Enter Box";
        Size_Enter.transform.localPosition = new Vector3(-2.9f, -1.5f, 0f);
        var sizeEnterTBT = Size_Enter.GetComponent<TextBoxTMP>();
        sizeEnterTBT.allowAllCharacters =
        sizeEnterTBT.AllowEmail =
        sizeEnterTBT.AllowSymbols =
        sizeEnterTBT.AllowPaste = false;

        Color_Info = Object.Instantiate(infoPrefab, Menu.transform);
        Color_Info.name = "Edit Color Description";
        Color_Info.transform.localPosition = new Vector3(1.95f, 0f, 0f);
        var colorInfoTmp = Color_Info.GetComponent<TextMeshPro>();
        colorInfoTmp.text = GetString("TextColorDescription");

        Color1_Enter = Object.Instantiate(enterPrefab, Menu.transform);
        Color1_Enter.name = "Edit Color 1 Enter Box";
        Color1_Enter.transform.localPosition = new Vector3(1.95f, -0.3f, 0f);

        Color2_Enter = Object.Instantiate(enterPrefab, Menu.transform);
        Color2_Enter.name = "Edit Color 2 Enter Box";
        Color2_Enter.transform.localPosition = new Vector3(1.95f, -0.9f, 0f);

        Color3_Enter = Object.Instantiate(enterPrefab, Menu.transform);
        Color3_Enter.name = "Edit Color 3 Enter Box";
        Color3_Enter.transform.localPosition = new Vector3(1.95f, -1.5f, 0f);

        titlePrefab.SetActive(false);
        infoPrefab.SetActive(false);
        buttonPrefab.SetActive(false);
        enterPrefab.SetActive(false);

    }
}