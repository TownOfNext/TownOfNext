using Hazel;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace TONX.Modules;

public static class CustomSoundsManager
{
    public static void RPCPlayCustomSound(this PlayerControl pc, string sound, bool force = false)
    {
        if (pc == null || pc.AmOwner)
        {
            Play(sound);
            return;
        }
        if (!force) if (!AmongUsClient.Instance.AmHost || !pc.IsModClient()) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlayCustomSound, SendOption.Reliable, pc.GetClientId());
        writer.Write(sound);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RPCPlayCustomSoundAll(string sound)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlayCustomSound, SendOption.Reliable, -1);
        writer.Write(sound);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
        Play(sound);
    }
    public static void ReceiveRPC(MessageReader reader) => Play(reader.ReadString());

#if Windows
    private static readonly string SOUNDS_PATH = @$"{Environment.CurrentDirectory.Replace(@"\", "/")}/BepInEx/resources/";
#elif Android || iOS
    private static readonly string SOUNDS_PATH = @$"{Application.persistentDataPath}/TONX_DATA/resources/";
#endif
    
    
    public static void Play(string sound)
    {
        if (!Constants.ShouldPlaySfx() || !Main.EnableCustomSoundEffect.Value || !OperatingSystem.IsWindows()) return;
        var path = SOUNDS_PATH + sound + ".wav";
        if (!Directory.Exists(SOUNDS_PATH)) Directory.CreateDirectory(SOUNDS_PATH);
        DirectoryInfo folder = new(SOUNDS_PATH);
        if ((folder.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
            folder.Attributes = FileAttributes.Hidden;
        if (!File.Exists(path))
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TONX.Resources.Sounds." + sound + ".wav");
            if (stream == null)
            {
                Logger.Warn($"声音文件缺失：{sound}", "CustomSounds");
                return;
            }
            var fs = File.Create(path);
            stream.CopyTo(fs);
            fs.Close();
        }
        StartPlay(path);
        Logger.Msg($"播放声音：{sound}", "CustomSounds");
    }

#if Windows
    [DllImport("winmm.dll")]
    private static extern bool PlaySound(string Filename, int Mod, int Flags);
#endif
    public static void StartPlay(string path)
    {
#if Windows
        PlaySound(@$"{path}", 0, 1); // 安卓暂不做处理，会导致声音缺失
#endif
    }

}
