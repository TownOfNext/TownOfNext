namespace TONX.GameModes;

public sealed class Standard : GameModeBase
{
    public static readonly GameModeInfo ModeInfo =
        GameModeInfo.Create(
            typeof(Standard),
            () => new Standard(),
            CustomGameMode.Standard,
            10_000_000,
            null,
            "#ffffff"
        );
    public Standard() : base(ModeInfo)
    { }
}