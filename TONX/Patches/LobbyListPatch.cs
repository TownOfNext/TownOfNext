using InnerNet;

namespace TONX;

class LobbyListPath
{
    [HarmonyPatch(typeof(GameContainer), nameof(GameContainer.SetupGameInfo)), HarmonyPostfix]
    public static void GameContainerSetupGameInfo_Postfix(GameContainer __instance)
    {
        __instance.capacity.text += GetMoreLobbyInfo(__instance.gameListing);
    }
    [HarmonyPatch(typeof(FindGameMoreInfoPopup), nameof(FindGameMoreInfoPopup.SetupInfo)), HarmonyPostfix]
    public static void FindGameMoreInfoPopupSetupInfo_Postfix(FindGameMoreInfoPopup __instance)
    {
        __instance.capacity.text += GetMoreLobbyInfo(__instance.gameListing);
    }
    public static string GetMoreLobbyInfo(GameListing game)
    {
        var (color, name) = game.Platform switch
        {
            Platforms.StandaloneItch => ("#00a4ff", "Itch.io"),
            Platforms.StandaloneWin10 => ("#00a4ff", "Microsoft Store"),
            Platforms.StandaloneEpicPC => ("#00a4ff", "Epic"),
            Platforms.StandaloneSteamPC => ("#00a4ff", "Steam"),
            Platforms.StandaloneMac => ("#00a4ff", "Mac"),

            Platforms.Xbox => ("#dd001b", "Xbox"),
            Platforms.Switch => ("#dd001b", "Switch"),
            Platforms.Playstation => ("#dd001b", "PlayStation"),

            Platforms.IPhone => ("#68bc71", "IPhone"),
            Platforms.Android => ("#68bc71", "Android"),

            _ => ("#ffffff", "Unknown")
        };
        return $"\n<size=60%><color={color}>{name}</color></size><size=30%> ({Math.Max(0, 100 - game.Age / 100)}%)</size>";
    }
}