using AmongUs.GameOptions;

namespace TONX.Modules.Extensions;

public static class IGameManagerEx
{
    public static void Set(this BoolOptionNames name, bool value, IGameOptions opt) => opt.SetBool(name, value);
    public static void Set(this BoolOptionNames name, bool value, NormalGameOptionsV11 opt) => opt.SetBool(name, value);
    public static void Set(this BoolOptionNames name, bool value, HideNSeekGameOptionsV11 opt) => opt.SetBool(name, value);

    public static void Set(this Int32OptionNames name, int value, IGameOptions opt) => opt.SetInt(name, value);
    public static void Set(this Int32OptionNames name, int value, NormalGameOptionsV11 opt) => opt.SetInt(name, value);
    public static void Set(this Int32OptionNames name, int value, HideNSeekGameOptionsV11 opt) => opt.SetInt(name, value);

    public static void Set(this FloatOptionNames name, float value, IGameOptions opt) => opt.SetFloat(name, value);
    public static void Set(this FloatOptionNames name, float value, NormalGameOptionsV11 opt) => opt.SetFloat(name, value);
    public static void Set(this FloatOptionNames name, float value, HideNSeekGameOptionsV11 opt) => opt.SetFloat(name, value);

    public static void Set(this ByteOptionNames name, byte value, IGameOptions opt) => opt.SetByte(name, value);
    public static void Set(this ByteOptionNames name, byte value, NormalGameOptionsV11 opt) => opt.SetByte(name, value);
    public static void Set(this ByteOptionNames name, byte value, HideNSeekGameOptionsV11 opt) => opt.SetByte(name, value);

    public static void Set(this UInt32OptionNames name, uint value, IGameOptions opt) => opt.SetUInt(name, value);
    public static void Set(this UInt32OptionNames name, uint value, NormalGameOptionsV11 opt) => opt.SetUInt(name, value);
    public static void Set(this UInt32OptionNames name, uint value, HideNSeekGameOptionsV11 opt) => opt.SetUInt(name, value);
}