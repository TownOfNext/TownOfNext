using TONX;

namespace TONEX.MoreGameModes;

public sealed class HotPotato : GameModeBase
{
    public static readonly GameModeInfo ModeInfo =
        GameModeInfo.Create(
            typeof(HotPotato),
            () => new HotPotato(),
            CustomGameMode.SoloKombat,
            40_000_000,
            SetupCustomOption,
            "#f55252",
            () => $"<color=#f55252><size=1.7>{GetString("ModeHotPotato")}</size></color>",
            (true, false)
        );
    
    public HotPotato() : base(ModeInfo)
    { }

    public static OptionItem HotPotatoMaxNum;
    public static OptionItem ExplosionTotalTime;

    public static void SetupCustomOption()
    {
        
    }
    
    public override void Add()
    {
    }
}