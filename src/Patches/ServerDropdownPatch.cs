using UnityEngine;

namespace TONX;

[HarmonyPatch(typeof(ServerDropdown))]
public static class ServerDropdownPatch
{
    private static int CurrentPage = 1;
    private static int MaxPage = 1;
    private const int ButtonsPerPage = 4;

    [HarmonyPatch(nameof(ServerDropdown.FillServerOptions)), HarmonyPostfix]
    public static void FillServerOptions_Postfix(ServerDropdown __instance)
    {
        List<ServerListButton> serverListButtons = __instance.ButtonPool.GetComponentsInChildren<ServerListButton>().OrderByDescending(x => x.transform.localPosition.y).ToList();
        MaxPage = Mathf.Max(1, Mathf.CeilToInt((float)serverListButtons.Count / ButtonsPerPage));
        if (CurrentPage > MaxPage) CurrentPage = MaxPage;

        // 调整服务器选项按钮位置
        int num = 0;
        int count = 1;
        foreach (ServerListButton button in serverListButtons)
        {
            if (num < (CurrentPage - 1) * ButtonsPerPage || num >= CurrentPage * ButtonsPerPage)
            {
                button.gameObject.SetActive(false);
                num++;
                continue;
            }
            button.transform.localPosition = new Vector3(0f, __instance.y_posButton + -0.55f * count, -1f);
            num++;
            count++;
        }

        // 调整背景大小和位置
        __instance.background.transform.localPosition = new Vector3(0f, __instance.initialYPos + -0.3f * (ButtonsPerPage + 1), 0f);
        __instance.background.size = new Vector2(__instance.background.size.x, 1.2f + 0.6f * (ButtonsPerPage + 1));

        // 创建翻页按钮
        CreateServerListButton(__instance, "PreviousPageButton", GetString("PreviousPage"), new Vector3(0f, __instance.y_posButton, -1f), () =>
        {
            CurrentPage = CurrentPage > 1 ? CurrentPage - 1 : MaxPage;
            RefreshServerOptions(__instance);
        });
        CreateServerListButton(__instance, "NextPageButton", GetString("NextPage"), new Vector3(0f, __instance.y_posButton + -0.55f * (ButtonsPerPage + 1), -1f), () =>
        {
            CurrentPage = CurrentPage < MaxPage ? CurrentPage + 1 : 1;
            RefreshServerOptions(__instance);
        });
    }
    private static void CreateServerListButton(ServerDropdown __instance, string name, string text, Vector3 position, Action onclickaction)
    {
        ServerListButton button = __instance.ButtonPool.Get<ServerListButton>();
        button.name = name;
        button.transform.localPosition = position;
        button.transform.localScale = Vector3.one;
        button.Text.text = text;
        button.Text.ForceMeshUpdate();
        button.Button.OnClick.RemoveAllListeners();
        button.Button.OnClick.AddListener(onclickaction);
        button.gameObject.SetActive(true);
    }
    private static void RefreshServerOptions(ServerDropdown __instance)
    {
        __instance.ButtonPool.ReclaimAll();
        __instance.controllerSelectable = new Il2CppSystem.Collections.Generic.List<UiElement>();
        __instance.FillServerOptions();
    }
}