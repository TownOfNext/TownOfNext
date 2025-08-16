using Hazel;
using TONX.Roles.AddOns.Common;
using TONX.Roles.Core;
using TONX.Roles.Crewmate;
using TONX.Roles.Impostor;
using TONX.Roles.Neutral;

namespace TONX;

public static class NameColorManager
{
    public static string ApplyNameColorData(this string name, PlayerControl seer, PlayerControl target, bool isMeeting)
    {
        if (!AmongUsClient.Instance.IsGameStarted) return name;

        if (!TryGetData(seer, target, out var colorCode))
        {
            if (KnowTargetRoleColor(seer, target, isMeeting, out var color))
                colorCode = color == "" ? target.GetRoleColorCode() : color;
        }
        string openTag = "", closeTag = "";
        if (colorCode != "")
        {
            if (!colorCode.StartsWith('#'))
                colorCode = "#" + colorCode;
            openTag = $"<color={colorCode}>";
            closeTag = "</color>";
        }
        return openTag + name + closeTag;
    }
    private static bool KnowTargetRoleColor(PlayerControl seer, PlayerControl target, bool isMeeting, out string color)
    {
        color = "";

        // 内鬼阵营
        if (seer.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoleTypes.Impostor)) color = (target.Is(CustomRoles.Egoist) && Egoist.OptionImpEgoVisibalToAllies.GetBool() && seer != target) ? Utils.GetRoleColorCode(CustomRoles.Egoist) : Utils.GetRoleColorCode(CustomRoles.Impostor);
        if (seer.Is(CustomRoles.Madmate) && target.Is(CustomRoleTypes.Impostor) && Options.MadmateKnowWhosImp.GetBool()) color = Main.roleColors[CustomRoles.Impostor];
        if (seer.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoles.Madmate) && Options.ImpKnowWhosMadmate.GetBool()) color = Main.roleColors[CustomRoles.Madmate];
        if (seer.Is(CustomRoles.Madmate) && target.Is(CustomRoles.Madmate) && Options.MadmateKnowWhosMadmate.GetBool()) color = Main.roleColors[CustomRoles.Madmate];
        if (seer.Is(CustomRoles.Gangster) && target.Is(CustomRoles.Madmate)) color = Main.roleColors[CustomRoles.Madmate];

        // 豺狼阵营
        if (seer.Is(CustomRoles.Sidekick) && target.Is(CustomRoles.Jackal)) color = CustomRoles.Jackal.GetRoleInfo().RoleColorCode;
        if (seer.Is(CustomRoles.Jackal) && target.Is(CustomRoles.Sidekick)) color = CustomRoles.Jackal.GetRoleInfo().RoleColorCode;
        if (seer.Is(CustomRoles.Sidekick) && target.Is(CustomRoles.Sidekick)) color = CustomRoles.Jackal.GetRoleInfo().RoleColorCode;
        if (seer.Is(CustomRoles.Jackal) && target.Is(CustomRoles.Jackal)) color = CustomRoles.Jackal.GetRoleInfo().RoleColorCode;

        // 魅魔阵营
        if (seer.Is(CustomRoles.Charmed) && target.Is(CustomRoles.Succubus)) color = Main.roleColors[CustomRoles.Succubus];
        if (seer.Is(CustomRoles.Succubus) && target.Is(CustomRoles.Charmed)) color = Main.roleColors[CustomRoles.Charmed];
        if (seer.Is(CustomRoles.Charmed) && target.Is(CustomRoles.Charmed) && Succubus.OptionTargetKnowOtherTarget.GetBool()) color = Main.roleColors[CustomRoles.Charmed];

        if (!string.IsNullOrEmpty(color)) return true;
        return seer == target 
           || (Main.GodMode.Value && seer.AmOwner) 
           || seer.Is(CustomRoles.GM)
           || target.Is(CustomRoles.GM)
           || seer.Is(CustomRoles.God)
               
           || SuperStar.KnowTargetRoleColor(target, isMeeting)
           || Workaholic.KnowTargetRoleColor(target, isMeeting)
           || Mare.KnowTargetRoleColor(target, isMeeting);
    }
    public static bool TryGetData(PlayerControl seer, PlayerControl target, out string colorCode)
    {
        colorCode = "";
        var state = PlayerState.GetByPlayerId(seer.PlayerId);
        if (!state.TargetColorData.TryGetValue(target.PlayerId, out var value)) return false;
        colorCode = value;
        return true;
    }

    public static void Add(byte seerId, byte targetId, string colorCode = "")
    {
        if (colorCode == "")
        {
            var target = Utils.GetPlayerById(targetId);
            if (target == null) return;
            colorCode = target.GetRoleColorCode();
        }

        var state = PlayerState.GetByPlayerId(seerId);
        if (state.TargetColorData.TryGetValue(targetId, out var value) && colorCode == value) return;
        state.TargetColorData.Add(targetId, colorCode);

        SendRPC(seerId, targetId, colorCode);
    }
    public static void Remove(byte seerId, byte targetId)
    {
        var state = PlayerState.GetByPlayerId(seerId);
        if (!state.TargetColorData.ContainsKey(targetId)) return;
        state.TargetColorData.Remove(targetId);

        SendRPC(seerId, targetId);
    }
    public static void RemoveAll(byte seerId)
    {
        PlayerState.GetByPlayerId(seerId).TargetColorData.Clear();

        SendRPC(seerId);
    }
    private static void SendRPC(byte seerId, byte targetId = byte.MaxValue, string colorCode = "")
    {
        if (!AmongUsClient.Instance.AmHost) return;

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetNameColorData, SendOption.Reliable, -1);
        writer.Write(seerId);
        writer.Write(targetId);
        writer.Write(colorCode);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte seerId = reader.ReadByte();
        byte targetId = reader.ReadByte();
        string colorCode = reader.ReadString();

        if (targetId == byte.MaxValue)
            RemoveAll(seerId);
        else if (colorCode == "")
            Remove(seerId, targetId);
        else
            Add(seerId, targetId, colorCode);
    }
}