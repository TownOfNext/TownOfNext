using Microsoft.Win32;
#if Android
using UnityEngine;
#endif

namespace TONX;

# pragma warning disable CA1416
public static class RegistryManager
{
#if Windows
    public static RegistryKey SoftwareKeys => Registry.CurrentUser.OpenSubKey("Software", true);
    public static RegistryKey Keys = SoftwareKeys?.OpenSubKey("AU-TONX", true);
#endif

    public static Version LastVersion;

    public static void Init()
    {
#if Windows
        if (Keys == null)
        {
            Logger.Info("Create TONX Registry Key", "Registry Manager");
            Keys = SoftwareKeys?.CreateSubKey("AU-TONX", true);
        }
        if (Keys == null)
        {
            Logger.Error("Create Registry Failed", "Registry Manager");
            return;
        }

        if (Keys.GetValue("Last launched version") is not string regLastVersion)
            LastVersion = new Version(0, 0, 0);
        else LastVersion = Version.Parse(regLastVersion);

        Keys.SetValue("Last launched version", Main.version.ToString());
        Keys.SetValue("Path", Path.GetFullPath("./"));

#elif Android
        string regLastVersion = PlayerPrefs.GetString("Last launched version", "");
        if (string.IsNullOrEmpty(regLastVersion))
            LastVersion = new Version(0, 0, 0);
        else LastVersion = Version.Parse(regLastVersion);

        PlayerPrefs.SetString("Last launched version", Main.version.ToString());
        PlayerPrefs.SetString("Path", Path.GetFullPath("./"));
        PlayerPrefs.Save();
#endif

        List<string> FoldersNFileToDel =
            [
#if Windows
                @"./TOH_DATA",
                @"./TOHE_DATA",
#endif
            ];

        Logger.Warn("上次启动的TONX版本：" + LastVersion, "Registry Manager");

#if Windows
        if (LastVersion < new Version(3, 0, 0))
        {
            Logger.Warn("v3.0.0 New Version Operation Needed", "Registry Manager");
            FoldersNFileToDel.Add(@"./BepInEx/config");
        }

        if (LastVersion <= new Version(3, 0, 0))
        {
            Logger.Warn("v3.0.1 New Version Operation Needed", "Registry Manager");
            FoldersNFileToDel.Add(@"./TONX/Data/template.txt");
        }
#endif

        FoldersNFileToDel.DoIf(Directory.Exists, p =>
        {
            Logger.Warn("Delete Useless Directory:" + p, "Registry Manager");
            Directory.Delete(p, true);
        });
        FoldersNFileToDel.DoIf(File.Exists, p =>
        {
            Logger.Warn("Delete Useless File:" + p, "Registry Manager");
            File.Delete(p);
        });
    }
}
