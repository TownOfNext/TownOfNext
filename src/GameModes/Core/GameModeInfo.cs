using UnityEngine;

namespace TONX.GameModes.Core;

public class GameModeInfo
{
    public Type ClassType;
    public CustomGameMode ModeName;
    public Func<GameModeBase> CreateInstance;
    public Color ModeColor;
    public string ModeColorCode;
    public int ConfigId;
    public OptionCreatorDelegate OptionCreator;
    public Func<string> HostTag;
    public (bool ShowModeDescription, bool ShowRoleDescription)? RolesHelp;

    private GameModeInfo(
        Type classType,
        Func<GameModeBase> createInstance,
        CustomGameMode modeName,
        int configId,
        OptionCreatorDelegate optionCreator,
        string colorCode,
        Func<string> hostTag,
        (bool, bool)? rolesHelp
    )
    {
        ClassType = classType;
        CreateInstance = createInstance;
        ModeName = modeName;
        ConfigId = configId;
        OptionCreator = optionCreator;
        RolesHelp = rolesHelp;

        if (colorCode == "") colorCode = "#ffffff";
        ModeColorCode = colorCode;

        _ = ColorUtility.TryParseHtmlString(colorCode, out ModeColor);

        hostTag ??= () => $"<color=#87cefa>{Main.PluginVersion}</color>";
        HostTag = hostTag;

        rolesHelp ??= (false, true);
        RolesHelp = rolesHelp;

        CustomGameModeManager.AllModesInfo.Add(modeName, this);
    }
    public static GameModeInfo Create(
        Type classType,
        Func<GameModeBase> createInstance,
        CustomGameMode modeName,
        int configId,
        OptionCreatorDelegate optionCreator,
        string colorCode = "",
        Func<string> hostTag = null,
        (bool, bool)? roleshelp = null
    )
    {
        var modeInfo = new GameModeInfo(
                classType,
                createInstance,
                modeName,
                configId,
                optionCreator,
                colorCode,
                hostTag,
                roleshelp
            );
        return modeInfo;
    }
    public delegate void OptionCreatorDelegate();
}