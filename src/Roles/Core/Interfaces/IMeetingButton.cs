namespace TONX.Roles.Core.Interfaces;

/// <summary>
/// 创建会议期间技能按钮的接口
/// </summary>
public interface IMeetingButton
{
    /// <summary>
    /// 是否在该目标上显示技能按钮
    /// </summary>
    /// <param name="target">将要显示按钮的目标</param>
    public bool ShouldShowButtonFor(PlayerControl target) => false;

    /// <summary>
    /// 是否应该显示会议技能按钮
    /// </summary>
    public bool ShouldShowButton() => false;

    /// <summary>
    /// 按钮图片文件的名字
    /// </summary>
    public string ButtonName => "";

    /// <summary>
    /// 玩家按下按钮的事件
    /// 该事件已经自动完成了RPC同步，因此该事件只会在 Host 调用
    /// </summary>
    /// <param name="target">目标玩家</param>
    public void OnClickButton(PlayerControl target) { }

    /// <summary>
    /// 玩家按下按钮的事件
    /// 该事件只会在按下按钮的客户端调用
    /// </summary>
    /// <param name="target">目标玩家</param>
    /// <returns>false：不继续调用 Host 的 OnClickButton 函数</returns>
    public bool OnClickButtonLocal(PlayerControl target) => true;

    /// <summary>
    /// 帧 Task 处理函数<br/>
    /// 请注意：全部模组端都会调用<br/>
    /// </summary>
    /// /// <param name="meetingHud">会议界面实例</param>
    public void OnUpdateButton(MeetingHud meetingHud) { }
}