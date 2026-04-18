using UnityEngine;

namespace TONX.Roles.Core;

public abstract class AddonBase : BaseCore
{
    public AddonBase(
        SimpleRoleInfo roleInfo,
        PlayerControl player
    ) : base(player)
    {
        if (!CustomRoleManager.AllActiveAddons.TryAdd(Player.PlayerId, new() { this }))
            CustomRoleManager.AllActiveAddons[player.PlayerId].Add(this);
    }
    public override void OnDispose()
    {
        CustomRoleManager.AllActiveAddons[Player.PlayerId].Remove(this);
        if (CustomRoleManager.AllActiveAddons[Player.PlayerId].Count == 0)
            CustomRoleManager.AllActiveAddons.Remove(Player.PlayerId);
    }

    /// <summary>
    /// 显示在职业旁边的文本
    /// </summary>
    /// <param name="comms">目前是否为通讯破坏状态</param>
    public virtual string GetProgressText(bool comms = false) => "";
    /// <summary>
    /// 作为 seen 重写显示上的 RoleName
    /// </summary>
    /// <param name="seer">将要看到您的 RoleName 的玩家</param>
    /// <param name="enabled">是否显示 RoleName</param>
    /// <param name="roleColor">RoleName 的颜色</param>
    /// <param name="roleText">RoleName 的文本</param>
    public virtual void OverrideDisplayRoleNameAsSeen(PlayerControl seer, ref bool enabled, ref Color roleColor, ref string roleText)
    { }
    /// <summary>
    /// 作为 seer 重写显示上的 RoleName
    /// </summary>
    /// <param name="seen">您将要看到其 RoleName 的玩家</param>
    /// <param name="enabled">是否显示 RoleName</param>
    /// <param name="roleColor">RoleName 的颜色</param>
    /// <param name="roleText">RoleName 的文本</param>
    public virtual void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText)
    { }
    /// <summary>
    /// 重写原来的职业名
    /// </summary>
    /// <param name="roleColor">RoleName 的颜色</param>
    /// <param name="roleText">RoleName 的文本</param>
    public virtual void OverrideTrueRoleName(ref Color roleColor, ref string roleText)
    { }
    /// <summary>
    /// 作为 seer 重写 ProgressText
    /// </summary>
    /// <param name="seen">您将要看到其 ProgressText 的玩家</param>
    /// <param name="enabled">是否显示 ProgressText</param>
    /// <param name="text">ProgressText 的文本</param>
    public virtual void OverrideProgressTextAsSeer(PlayerControl seen, ref bool enabled, ref string text)
    { }
    /// <summary>
    /// 作为 seer 时获取 Mark 的函数
    /// 如果您想在 seer,seen 都不是您时进行处理，请使用相同的参数将其实现为静态
    /// 并注册为 CustomRoleManager.MarkOthers
    /// </summary>
    /// <param name="seer">看到的人</param>
    /// <param name="seen">被看到的人</param>
    /// <param name="isForMeeting">是否正在会议中</param>
    /// <returns>構築したMark</returns>
    public virtual string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false) => "";
    /// <summary>
    /// 作为 seer 时获取 LowerTex 的函数
    /// 如果您想在 seer,seen 都不是您时进行处理，请使用相同的参数将其实现为静态
    /// 并注册为 CustomRoleManager.LowerOthers
    /// </summary>
    /// <param name="seer">看到的人</param>
    /// <param name="seen">被看到的人</param>
    /// <param name="isForMeeting">是否正在会议中</param>
    /// <param name="isForHud">是否显示在模组端的HUD</param>
    /// <returns>组合后的全部 LowerText</returns>
    public virtual string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false) => "";
    /// <summary>
    /// 作为 seer 时获取 LowerTex 的函数
    /// 如果您想在 seer,seen 都不是您时进行处理，请使用相同的参数将其实现为静态
    /// 并注册为 CustomRoleManager.SuffixOthers
    /// </summary>
    /// <param name="seer">看到的人</param>
    /// <param name="seen">被看到的人</param>
    /// <param name="isForMeeting">是否正在会议中</param>
    /// <returns>组合后的全部 Suffix</returns>
    public virtual string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false) => "";
    /// <summary>
    /// 作为 seen 重写 Name
    /// </summary>
    /// <param name="seer">将要看到您的 Name 的玩家</param>
    /// <param name="nameText">Name 的文本</param>
    /// <param name="isForMeeting">是否用于显示在会议上</param>
    public virtual void OverrideNameAsSeen(PlayerControl seer, ref string nameText, bool isForMeeting = false)
    { }
    /// <summary>
    /// 作为 seer 重写 Name
    /// </summary>
    /// <param name="seen">您将要看到其 Name 的玩家</param>
    /// <param name="roleColor">Name 的颜色</param>
    /// <param name="nameText">Name 的文本</param>
    /// <param name="isForMeeting">是否用于显示在会议上</param>
    public virtual void OverrideNameAsSeer(PlayerControl seen, ref string nameText, bool isForMeeting = false)
    { }
}
