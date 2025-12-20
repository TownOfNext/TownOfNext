using AmongUs.GameOptions;
using UnityEngine;

namespace TONX.Roles.Core;

public abstract class RoleBase : BaseCore
{
    /// <summary>
    /// 玩家状态
    /// </summary>
    public readonly PlayerState MyState;
    /// <summary>
    /// 玩家任务状态
    /// </summary>
    public readonly TaskState MyTaskState;
    /// <summary>
    /// 是否拥有任务
    /// 默认只有在您是船员的时候有任务
    /// </summary>
    protected Func<HasTask> hasTasks;
    /// <summary>
    /// 是否拥有任务
    /// </summary>
    public HasTask HasTasks => hasTasks.Invoke();
    /// <summary>
    /// 任务是否完成
    /// </summary>
    public bool IsTaskFinished => MyTaskState.IsTaskFinished;
    /// <summary>
    /// 可以成为叛徒
    /// </summary>
    public bool CanBeMadmate { get; private set; }
    /// <summary>
    /// 是否拥有技能按钮
    /// </summary>
    public bool HasAbility { get; private set; }
    public RoleBase(
        SimpleRoleInfo roleInfo,
        PlayerControl player,
        Func<HasTask> hasTasks = null,
        bool? hasAbility = null,
        bool? canBeMadmate = null
    ) : base(player)
    {
        this.hasTasks = hasTasks ?? (roleInfo.CustomRoleType == CustomRoleTypes.Crewmate ? () => HasTask.True : () => HasTask.False);
        CanBeMadmate = canBeMadmate ?? Player.Is(CustomRoleTypes.Crewmate);
        HasAbility = hasAbility ?? roleInfo.BaseRoleType.Invoke() is
            RoleTypes.Scientist or
            RoleTypes.GuardianAngel or
            RoleTypes.Engineer or
            RoleTypes.Tracker or
            RoleTypes.Detective or
            RoleTypes.CrewmateGhost or
            RoleTypes.Shapeshifter or
            RoleTypes.Phantom or
            RoleTypes.ImpostorGhost;

        MyState = PlayerState.GetByPlayerId(player.PlayerId);
        MyTaskState = MyState.GetTaskState();

        CustomRoleManager.AllActiveRoles.Add(Player.PlayerId, this);
    }
    public override void OnDispose()
    {
        CustomRoleManager.AllActiveRoles.Remove(Player.PlayerId);
    }

    /// <summary>
    /// 可以使用技能按钮
    /// </summary>
    /// <returns>true：可以使用能力按钮</returns>
    public virtual bool CanUseAbilityButton() => true;

    /// <summary>
    /// 变形时调用的函数
    /// 不需要验证您的身份，因为调用前已经验证
    /// 请注意：全部模组端都会调用
    /// </summary>
    /// <param name="shapeshifter">变形目标</param>
    ///  /// <summary>
    /// 自視点のみ変身する
    /// 抜け殻を自視点のみに残すことが可能
    /// </summary>
    public virtual bool CanDesyncShapeshift => false;

    // NameSystem
    // 显示的名字结构如下
    // [Role][Progress]
    // [Name][Mark]
    // [Lower][suffix]
    // Progress：任务进度、剩余子弹等信息
    // Mark：通过位置能力等进行目标标记
    // Lower：附加文本信息，模组端则会显示在屏幕下方
    // Suffix：其他信息，例如箭头

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
    /// 显示在职业旁边的文本
    /// </summary>
    /// <param name="comms">目前是否为通讯破坏状态</param>
    public virtual string GetProgressText(bool comms = false) => "";
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
    /// 修改技能按钮的剩余次数
    /// </summary>
    public virtual int OverrideAbilityButtonUsesRemaining() => -1;
    /// <summary>
    /// 更改变形/跳管/生命面板按钮的文本
    /// </summary>
    public virtual bool GetAbilityButtonText(out string text)
    {
        text = default;
        return false;
    }
    /// <summary>
    /// 更改变形/跳管/生命面板按钮的图片
    /// </summary>
    /// <param name="buttonName">按钮图片名</param>
    /// <returns>true：确定要覆盖</returns>
    public virtual bool GetAbilityButtonSprite(out string buttonName)
    {
        buttonName = default;
        return false;
    }
    /// <summary>
    /// 更改报告按钮的文本
    /// </summary>
    /// <param name="text">覆盖后的文本</param>
    /// <returns>true：确定要覆盖</returns>
    public virtual string GetReportButtonText() => GetString(StringNames.ReportLabel);
    /// <summary>
    /// 更改报告按钮的图片
    /// </summary>
    /// <param name="buttonName">按钮图片名</param>
    /// <returns>true：确定要覆盖</returns>
    public virtual bool GetReportButtonSprite(out string buttonName)
    {
        buttonName = default;
        return false;
    }
}
