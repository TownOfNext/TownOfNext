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

        if (IsChineseUser)
        {
            regionInfos.Add(CreateHttp("au-cn.niko233.me", "Niko233(CN)", 443, true));
            regionInfos.Add(CreateHttp("nb.8w.fan", "<color=#00FF00>新猫服</color><color=#ffff00>[宁波]</color>", 443, true));
            regionInfos.Add(CreateHttp("bj.8w.fan", "<color=#9900CC>新猫服</color><color=#ffff00>[北京]</color>", 443, true));
            regionInfos.Add(CreateHttp("player.fangkuai.fun", "<color=#00ffff>方块</color><color=#FF44FF>宿迁私服</color>", 443, true));
            regionInfos.Add(CreateHttp("auhk.fangkuai.fun", "<color=#00ffff>方块</color><color=#FFC0CB>香港私服</color>", 443, true));
        }
        regionInfos.Add(CreateHttp("au-as.duikbo.at", "Modded Asia (MAS)", 443, true));
        regionInfos.Add(CreateHttp("www.aumods.org", "Modded NA (MNA)", 443, true));
        regionInfos.Add(CreateHttp("au-eu.duikbo.at", "Modded EU (MEU)", 443, true));
        regionInfos.Add(CreateHttp("au-us.niko233.me", "Niko233(NA)", 443, true));
        regionInfos.Add(CreateHttp("au-as.niko233.me", "Niko233(AS)", 443, true));
        regionInfos.Add(CreateHttp("au-eu.niko233.me", "Niko233(EU)", 443, true));

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
            "<color=#00FF00>新猫服</color><color=#ffff00>[宁波]</color>" => "猫服[宁波]",
            "<color=#9900CC>新猫服</color><color=#ffff00>[北京]</color>" => "猫服[北京]",
            "<color=#00ffff>方块</color><color=#FF44FF>宿迁私服</color>" => "方块[宿迁]",
            "<color=#00ffff>方块</color><color=#FFC0CB>香港私服</color>" => "方块[香港]",
            "Niko233(NA)" => "Niko[NA]",
            "Niko233(AS)" => "Niko[AS]",
            "Niko233(EU)" => "Niko[EU]",
            "Niko233(CN)" => "Niko[CN]",
            _ => serverName,
        };

        Color32 color = serverName switch
        {
            "Asia" => new(58, 166, 117, 255),
            "Europe" => new(58, 166, 117, 255),
            "North America" => new(58, 166, 117, 255),
            "<color=#00FF00>新猫服</color><color=#ffff00>[宁波]</color>" => new(0, 255, 0, 255),
            "<color=#9900CC>新猫服</color><color=#ffff00>[北京]</color>" => new(153, 0, 204, 255),
            "<color=#00ffff>方块</color><color=#FF44FF>宿迁私服</color>" => new(0, 255, 255, 255),
            "<color=#00ffff>方块</color><color=#FFC0CB>香港私服</color>" => new(0, 255, 255, 255),
            "Niko233(NA)" => new(255, 224, 0, 255),
            "Niko233(AS)" => new(255, 224, 0, 255),
            "Niko233(EU)" => new(255, 224, 0, 255),
            "Niko233(CN)" => new(255, 224, 0, 255),
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