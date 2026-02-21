using TONX.Attributes;
using UnityEngine;

namespace TONX;

public static class ServerAddManager
{
    private static ServerManager serverManager = DestroyableSingleton<ServerManager>.Instance;

    [PluginModuleInitializer]
    public static void Init()
    {
#if Windows
        // serverManager.AvailableRegions = ServerManager.DefaultRegions;
        List<IRegionInfo> regionInfos = new();

        regionInfos.Add(CreateHttp("au-as.duikbo.at", "Modded Asia (MAS)", 443, true));
        regionInfos.Add(CreateHttp("www.aumods.org", "Modded NA (MNA)", 443, true));
        regionInfos.Add(CreateHttp("au-eu.duikbo.at", "Modded EU (MEU)", 443, true));
        regionInfos.Add(CreateHttp("au-us.niko233.top", "Niko233(NA)", 443, true));
        regionInfos.Add(CreateHttp("au-as.niko233.top", "Niko233(AS)", 443, true));
        regionInfos.Add(CreateHttp("au-eu.niko233.top", "Niko233(EU)", 443, true));

        var defaultRegion = serverManager.CurrentRegion;
        regionInfos.Where(x => !serverManager.AvailableRegions.Contains(x)).Do(serverManager.AddOrUpdateRegion);
        serverManager.SetRegion(defaultRegion);
#endif
    }
    public static void SetServerName(IRegionInfo server = null)
    {
        server ??= ServerManager.Instance.CurrentRegion;
        string serverName = server.Name;
        var name = serverName switch
        {
            "Modded Asia (MAS)" => "MAS",
            "Modded NA (MNA)" => "MNA",
            "Modded EU (MEU)" => "MEU",
            "Niko233(NA)" => "Niko[NA]",
            "Niko233(AS)" => "Niko[AS]",
            "Niko233(EU)" => "Niko[EU]",
            _ => serverName,
        };

        Color32 color = serverName switch
        {
            "Asia" => new(58, 166, 117, 255),
            "Europe" => new(58, 166, 117, 255),
            "North America" => new(58, 166, 117, 255),
            "Niko233(NA)" => new(255, 224, 0, 255),
            "Niko233(AS)" => new(255, 224, 0, 255),
            "Niko233(EU)" => new(255, 224, 0, 255),
            "Modded Asia (MAS)" => new(255, 132, 0, 255),
            "Modded NA (MNA)" => new(255, 132, 0, 255),
            "Modded EU (MEU)" => new(255, 132, 0, 255),
            _ => new(255, 255, 255, 255),
        };

        if (server.TranslateName != StringNames.NoTranslation) name = GetString(server.TranslateName);
        PingTrackerUpdatePatch.ServerName = Utils.ColorString(color, $"{name} <size=60%>Server</size>");
    }

    public static IRegionInfo CreateHttp(string ip, string name, ushort port, bool ishttps)
    {
        string serverIp = (ishttps ? "https://" : "http://") + ip;
        ServerInfo serverInfo = new ServerInfo(name, serverIp, port, false);
        ServerInfo[] ServerInfo = new ServerInfo[] { serverInfo };
        return new StaticHttpRegionInfo(name, (StringNames)1003, ip, ServerInfo).Cast<IRegionInfo>();
    }
}