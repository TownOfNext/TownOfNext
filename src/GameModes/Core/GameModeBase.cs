using TMPro;
using System.Text;

namespace TONX.GameModes.Core;

public abstract class GameModeBase : IDisposable
{
    public GameModeBase(GameModeInfo modeInfo)
    {
        CustomGameModeManager.AllModesClass.Add(modeInfo.ModeName, this);
    }
    public void Dispose()
    {
        OnDestroy();
        CustomGameModeManager.AllModesClass.Remove(Options.CurrentGameMode);
    }

    // == 实例相关 ==
    /// <summary>
    /// 创建实例后立刻调用的函数
    /// </summary>
    public virtual void Add()
    { }
    /// <summary>
    /// 实例被销毁时调用的函数
    /// </summary>
    public virtual void OnDestroy()
    { }

    // == 职业分配相关 ==
    /// <summary>
    /// 用于特殊模式分配主职业
    /// </summary>
    /// <param name="RoleResult">职业分配字典</param>
    public virtual void SelectCustomRoles(ref Dictionary<PlayerControl, CustomRoles> RoleResult)
        => GameModeStandard.SelectCustomRoles(ref RoleResult);
    /// <summary>
    /// 用于判断特殊模式是否分配附加职业
    /// </summary>
    /// <returns>返回false不分配附加职业</returns>
    public virtual bool ShouldAssignAddons() => true;

    // == 游戏结束相关 ==
    /// <summary>
    /// 游戏结束的判断
    /// </summary>
    public virtual GameEndPredicate Predicate() => new GameModeStandard.NormalGameEndPredicate();
    /// <summary>
    /// 判断游戏是否结束时调用
    /// </summary>
    /// <param name="reason">游戏结束原因</param>
    /// <param name="predicate">游戏结束判断，可进行修改</param>
    public virtual void AfterCheckForGameEnd(GameOverReason reason, ref GameEndPredicate predicate)
        => GameModeStandard.AfterCheckForGameEnd(reason, ref predicate);

    // == 游戏过程相关 ==
    /// <summary>
    /// 帧 Task 处理函数<br/>
    /// 不需要验证您的身份，因为调用前已经验证<br/>
    /// 请注意：全部模组端都会调用<br/>
    /// </summary>
    /// <param name="player">目标玩家</param>
    public virtual void OnFixedUpdate(PlayerControl player)
    { }
    /// <summary>
    /// 秒 Task 处理函数<br/>
    /// 不需要验证您的身份，因为调用前已经验证<br/>
    /// 请注意：全部模组端都会调用<br/>
    /// </summary>
    /// <param name="player">目标玩家</param>
    /// <param name="now">当前10位时间戳</param>
    public virtual void OnSecondsUpdate(PlayerControl player, long now)
    { }
    /// <summary>
    /// 报告前检查调用的函数<br/>
    /// 与报告事件无关的玩家也会调用该函数<br/>
    /// </summary>
    /// <param name="reporter">报告者</param>
    /// <param name="target">被报告的玩家</param>
    /// <returns>false：取消报告</returns>
    public virtual bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target) => true;
    /// <summary>
    /// 当有人关门时调用
    /// </summary>
    /// <param name="door">关门的房间</param>
    /// <returns>返回false取消关门</returns>
    public virtual bool OnCloseDoors(SystemTypes door) => true;
    /// <summary>
    /// 是否能看到他人游戏进度<br/>
    /// 判断优先级高于职业相关代码<br/>
    /// </summary>
    /// <returns>返回true则能看到</returns>
    public virtual bool CanSeeOtherProgressText() => false;
    /// <summary>
    /// 是否能应随机出生<br/>
    /// 判断优先级高于设置的<br/>
    /// </summary>
    /// <returns>返回true则默认随机出生</returns>
    public virtual bool ShouldRandomSpawn() => false;

    // == 字符串相关 ==
    /// <summary>
    /// 对复盘信息进行排序
    /// </summary>
    /// <param name="clone">当前复盘信息包含的所有玩家id</param>
    /// <returns>返回排序后的玩家id</returns>
    public virtual List<byte> ArrangedSummaryText(List<byte> clone) => clone;
    /// <summary>
    /// 复盘信息显示的内容<br/>
    /// (是否显示击杀数量, 是否显示生命状态, 是否显示击杀者)<br/>
    /// </summary>
    public virtual (bool, bool, bool) GetSummaryTextContent() => (true, true, true);

    // == 编辑UI相关 ==
    /// <summary>
    /// 设置任务栏文字时调用<br/>
    /// 用于修改任务栏文字<br/>
    /// 请注意：全部模组端都会调用<br/>
    /// </summary>
    /// <param name="taskPanel">任务栏实例</param>
    /// <param name="AllText">需要修改的文字</param>
    public virtual void EditTaskText(TaskPanelBehaviour taskPanel, ref string AllText)
        => GameModeStandard.EditTaskText(taskPanel, ref AllText);
    /// <summary>
    /// 修改开始界面的样式<br/>
    /// 请注意：全部模组端都会调用<br/>
    /// </summary>
    /// <param name="intro">开始界面实例</param>
    public virtual void EditIntroFormat(ref IntroCutscene intro)
        => GameModeStandard.EditIntroFormat(ref intro);
    /// <summary>
    /// 修改结束界面的样式<br/>
    /// 请注意：全部模组端都会调用<br/>
    /// </summary>
    /// <param name="outro">结束界面实例</param>
    /// <param name="winnerText">胜利阵营文字实例</param>
    /// <param name="cwText">胜利阵营文字内容</param>
    /// <param name="awText">附加胜利文字内容</param>
    /// <param name="cwColor">胜利阵营文字颜色</param>
    public virtual void EditOutroFormat(ref EndGameManager outro, ref TextMeshPro winnerText, ref string cwText, ref StringBuilder awText, ref string cwColor)
        => GameModeStandard.EditOutroFormat(ref outro, ref winnerText, ref cwText, ref awText, ref cwColor);
}