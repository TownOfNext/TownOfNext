using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static TONX.NameTagManager;
using static TONX.Translator;
using Object = UnityEngine.Object;

namespace TONX.Modules.NameTagInterface;

public static class NameTagPanel
{
    public static ToggleButtonBehaviour TagOptionsButton { get; private set; }
    public static SpriteRenderer CustomBackground { get; private set; }
    public static GameObject Slider { get; private set; }
    public static Dictionary<string, GameObject> Items { get; private set; }

    private static int numItems = 0;
    public static void Hide()
    {
        if (CustomBackground != null)
            CustomBackground?.gameObject?.SetActive(false);
    }
    public static void Init(OptionsMenuBehaviour optionsMenuBehaviour)
    {
        var mouseMoveToggle = optionsMenuBehaviour.DisableMouseMovement;

        UiElement[] selectableButtons = optionsMenuBehaviour.ControllerSelectable.ToArray();
        PassiveButton leaveButton = null;
        PassiveButton returnButton = null;
        for (int i = 0; i < selectableButtons.Length; i++)
        {
            var button = selectableButtons[i];
            if (button == null) continue;
            if (button.name == "LeaveGameButton") leaveButton = button.GetComponent<PassiveButton>();
            else if (button.name == "ReturnToGameButton") returnButton = button.GetComponent<PassiveButton>();
        }
        var generalTab = mouseMoveToggle.transform.parent.parent.parent;

        if (TagOptionsButton == null)
        {
            TagOptionsButton = Object.Instantiate(mouseMoveToggle, generalTab);
            var pos = leaveButton?.transform?.localPosition;
            TagOptionsButton.transform.localPosition = pos != null ? pos.Value - new Vector3(1.3f, 0f, 0f) : new(-1.3f, -2.4f, 1f);
            TagOptionsButton.name = "Name Tag Options";
            TagOptionsButton.Background.color = Main.ModColor32;
            var tagOptionsPassiveButton = TagOptionsButton.GetComponent<PassiveButton>();
            tagOptionsPassiveButton.OnClick = new();
            tagOptionsPassiveButton.OnClick.AddListener(new Action(() =>
            {
                CustomBackground.gameObject.SetActive(true);
            }));
        }

        if (!GameStates.IsNotJoined)
        {
            TagOptionsButton.Text.text = GetString("OnlyAvailableInMainMenu");
            TagOptionsButton.GetComponent<PassiveButton>().enabled = false;
            TagOptionsButton.Background.color = Palette.DisabledGrey;
            return;
        }
        TagOptionsButton.Text.text = GetString("NameTagOptions");
        TagOptionsButton.GetComponent<PassiveButton>().enabled = true;
        TagOptionsButton.Background.color = Main.ModColor32;

        if (CustomBackground == null)
        {
            numItems = 0;
            CustomBackground = Object.Instantiate(optionsMenuBehaviour.Background, optionsMenuBehaviour.transform);
            CustomBackground.name = "Name Tag Panel Background";
            CustomBackground.transform.localScale = new(0.9f, 0.9f, 1f);
            CustomBackground.transform.localPosition += Vector3.back * 8;
            CustomBackground.gameObject.SetActive(false);

            var closeButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
            closeButton.transform.localPosition = new(1.3f, -2.43f, -6f);
            closeButton.name = "Close";
            closeButton.Text.text = GetString("Close");
            closeButton.Background.color = Palette.DisabledGrey;
            var closePassiveButton = closeButton.GetComponent<PassiveButton>();
            closePassiveButton.OnClick = new();
            closePassiveButton.OnClick.AddListener(new Action(() =>
            {
                CustomBackground.gameObject.SetActive(false);
            }));

            var newButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
            newButton.transform.localPosition = new(1.3f, -1.88f, -6f);
            newButton.name = "New Tag";
            newButton.Text.text = GetString("NewNameTag");
            newButton.Background.color = Palette.White;
            var newPassiveButton = newButton.GetComponent<PassiveButton>();
            newPassiveButton.OnClick = new();
            newPassiveButton.OnClick.AddListener(new Action(NameTagNewWindow.Open));

            var helpText = Object.Instantiate(CustomPopup.InfoTMP.gameObject, CustomBackground.transform);
            helpText.name = "Help Text";
            helpText.transform.localPosition = new(-1.25f, -2.15f, -5f);
            helpText.transform.localScale = new(1f, 1f, 1f);
            var helpTextTMP = helpText.GetComponent<TextMeshPro>();
            helpTextTMP.text = GetString("CustomNameTagHelp");
            helpText.gameObject.GetComponent<RectTransform>().sizeDelta = new(2.45f, 1f);

            var sliderTemplate = AccountManager.Instance.transform.FindChild("MainSignInWindow/SignIn/AccountsMenu/Accounts/Slider").gameObject;
            if (sliderTemplate != null && Slider == null)
            {
                Slider = Object.Instantiate(sliderTemplate, CustomBackground.transform);
                Slider.name = "Name Tags Slider";
                Slider.transform.localPosition = new Vector3(0f, 0.5f, -1f);
                Slider.transform.localScale = new Vector3(1f, 1f, 1f);
                Slider.GetComponent<SpriteRenderer>().size = new(5f, 4f);
                var scroller = Slider.GetComponent<Scroller>();
                scroller.ScrollWheelSpeed = 0.3f;
                var mask = Slider.transform.FindChild("Mask");
                mask.transform.localScale = new Vector3(4.9f, 3.92f, 1f);
            }
        }

        ReloadTag(null);
        RefreshTagList();
    }
    public static void RefreshTagList()
    {
        var scroller = Slider.GetComponent<Scroller>();
        scroller.Inner.gameObject.ForEachChild((Action<GameObject>)(DestroyObj));
        static void DestroyObj(GameObject obj)
        {
            if (obj.name.StartsWith("AccountButton")) Object.Destroy(obj);
        }

        var numberSetter = AccountManager.Instance.transform.FindChild("DOBEnterScreen/EnterAgePage/MonthMenu/Months").GetComponent<NumberSetter>();
        var buttonPrefab = numberSetter.ButtonPrefab.gameObject;

        Items?.Values?.Do(Object.Destroy);
        Items = new();

        foreach (var nameTag in AllNameTags.Where(t => !t.Value.Isinternal))
        {
            numItems++;
            var button = Object.Instantiate(buttonPrefab, scroller.Inner);
            button.transform.localPosition = new(-1f, 1.6f - 0.6f * numItems, -0.5f);
            button.transform.localScale = new(1.2f, 1.2f, 1.2f);
            button.name = "Name Tag Item For " + nameTag.Key;
            Object.Destroy(button.GetComponent<UIScrollbarHelper>());
            Object.Destroy(button.GetComponent<NumberButton>());
            button.transform.GetChild(0).GetComponent<TextMeshPro>().text = nameTag.Key;
            var renderer = button.GetComponent<SpriteRenderer>();
            renderer.color = Palette.DisabledGrey;
            var rollover = button.GetComponent<ButtonRolloverHandler>();
            rollover.OutColor = Palette.DisabledGrey;
            var passiveButton = button.GetComponent<PassiveButton>();
            passiveButton.OnClick = new();
            passiveButton.OnClick.AddListener(new Action(() =>
            {
                NameTagEditMenu.Toggle(nameTag.Key, null);
            }));
            var previewText = Object.Instantiate(button.transform.GetChild(0).GetComponent<TextMeshPro>(), button.transform);
            previewText.transform.SetLocalX(1.9f);
            previewText.fontSize = 1f;
            string preview = GetString("PreviewNotAvailable");
            if (nameTag.Value.UpperText?.Text != null)
                preview = nameTag.Value.UpperText.Generate();
            previewText.text = preview;
            Items.Add(nameTag.Key, button);
        }

        scroller.SetYBoundsMin(0f);
        scroller.SetYBoundsMax(0.6f * numItems);
    }
}
