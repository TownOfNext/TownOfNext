using UnityEngine;
using Object = UnityEngine.Object;

namespace TONX;

[HarmonyPatch(typeof(ServerDropdown))]
public static class ServerDropdownPatch
{
    public static int CurrentPage = 1;
    public static int MaxPage = 1;
    public static int ButtonsPerPage = 4;
    public static ServerListButton PreviousPageButton;
    public static ServerListButton NextPageButton;
    [HarmonyPatch(nameof(ServerDropdown.FillServerOptions)), HarmonyPostfix]
    public static void FillServerOptions_Postfix(ServerDropdown __instance)
    {
        List<ServerListButton> serverListButton = __instance.ButtonPool.GetComponentsInChildren<ServerListButton>()
            .Where(x => x.name != "PreviousPageButton" && x.name != "NextPageButton").OrderByDescending(x => x.transform.localPosition.y).ToList();
        // 调整背景大小和位置
        __instance.background.size = new Vector2(__instance.background.size.x, __instance.background.size.y / serverListButton.Count * (ButtonsPerPage + 2f));
        __instance.background.transform.localPosition = new Vector3(0f, (1f - ButtonsPerPage * 0.5f) / 2, 0f);
        // 调整服务器选项按钮位置
        MaxPage = serverListButton.Count / ButtonsPerPage + 1;
        if (CurrentPage > MaxPage) CurrentPage = MaxPage;
        List<ServerListButton> currentPageButton = new();
        var max = CurrentPage * ButtonsPerPage > serverListButton.Count ? serverListButton.Count : CurrentPage * ButtonsPerPage;
        for (var i = (CurrentPage - 1) * ButtonsPerPage; i < max; i++) currentPageButton.Add(serverListButton[i]);
        foreach (ServerListButton button in serverListButton) if (!currentPageButton.Contains(button)) button.gameObject.SetActive(false);
        for (var i = 0; i < currentPageButton.Count; i++)
        {
            var button = currentPageButton[i];
            button.transform.localPosition = new Vector3(0f, -1f + i * -0.5f, -1f);
        }
        // 创建翻页按钮
        var template = serverListButton[0];
        if (PreviousPageButton == null || PreviousPageButton.gameObject == null) PreviousPageButton = CreateServerListButton(template, "PreviousPageButton", GetString("PreviousPage"),
            new Vector3(0f, -0.5f, -1f), () => { if (CurrentPage > 1) { CurrentPage--; RefreshServerOptions(__instance); } });
        PreviousPageButton.gameObject.SetActive(true);
        if (NextPageButton == null || NextPageButton.gameObject == null) NextPageButton = CreateServerListButton(template, "NextPageButton", GetString("NextPage"),
            new Vector3(0f, -1f + ButtonsPerPage * -0.5f, -1f), () => { if (CurrentPage < MaxPage) { CurrentPage++; RefreshServerOptions(__instance); } });
        NextPageButton.gameObject.SetActive(true);
    }
    public static ServerListButton CreateServerListButton(ServerListButton template, string name, string text, Vector3 position, Action onclickaction)
    {
        var button = Object.Instantiate(template, template.transform.parent);
        button.name = name;
        button.Text.text = text;
        button.transform.localPosition = position;
        button.Button.OnClick = new();
        button.Button.OnClick.AddListener(onclickaction);
        return button;
    }
    public static void RefreshServerOptions(ServerDropdown __instance)
    {
        foreach (ServerListButton button in __instance.ButtonPool.GetComponentsInChildren<ServerListButton>()) button.gameObject.SetActive(false);
        __instance.FillServerOptions();
    }
}

[HarmonyPatch(typeof(EnterCodeManager))]
public static class EnterCodeManagerPatch
{
    public static IRegionInfo CurrentFindGameByCodeClientRegion;
    public static int CurrentFindGameByCodeClientGameId;
    [HarmonyPatch(nameof(EnterCodeManager.FindGameResult)), HarmonyPostfix]
    public static void FindGameResult_Postfix(HttpMatchmakerManager.FindGameByCodeResponse response)
    {
        if (response == null) return;
        CurrentFindGameByCodeClientRegion = response.Region == StringNames.NoTranslation ?
            ServerManager.DefaultRegions.FirstOrDefault(x => x.TranslateName == response.Region) :
            ServerManager.Instance.AvailableRegions.FirstOrDefault(x => x.Name == response.UntranslatedRegion);
        if (CurrentFindGameByCodeClientRegion == null) return;
        CurrentFindGameByCodeClientGameId = response.Game.GameId;
    }
}