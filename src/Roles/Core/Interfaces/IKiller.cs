namespace TONX.Roles.Core.Interfaces;

/// <summary>
/// 带有击杀按钮的职业的接口
/// </summary>
public interface IKiller
{
    /// <summary>
    /// 可以使用击杀键击杀玩家
    /// </summary>
    public bool CanKill => true;
    /// <summary>
    /// 是按下击杀按键则击杀的职业吗<br/>
    /// 若设置为 false 则在您尝试击杀时不会请求目标进行任何检查
    /// 默认：返回 <see cref="CanKill"/> 的值
    /// </summary>
    public bool IsKiller => CanKill;

    /// <summary>
    /// 是否可以使用击杀按键
    /// 默认：返回 <see cref="CanKill"/> 的值
    /// </summary>
    /// <returns>true：可以使用击杀按键</returns>
    public bool CanUseKillButton() => CanKill;
    /// <summary>
    /// 计算击杀冷却时间<br/>
    /// 默认：<see cref="Options.DefaultKillCooldown"/>
    /// </summary>
    /// <returns>击杀冷却时间（秒）</returns>
    public float CalculateKillCooldown() => CanUseKillButton() ? Options.DefaultKillCooldown : 255f;
    /// <summary>
    /// サボタージュボタンを使えるかどうか
    /// </summary>
    /// <returns>trueを返した場合，サボタージュボタンを使える</returns>
    public bool CanUseSabotageButton();
    /// <summary>
    /// ベントボタンを使えるかどうか
    /// デフォルトでは使用可能
    /// </summary>
    /// <returns>trueを返した場合，ベントボタンを使える</returns>
    public bool CanUseImpostorVentButton() => true;

    /// <summary>
    /// CheckMurder 作为击杀者时的处理函数<br/>
    /// 设置 info.DoKill=false 为不击杀，但依然触发 CheckMurderAsTarget 函数<br/>
    /// 若您确定本次击杀不会发生，返回 false 立刻终止击杀事件，目标不会收到任何影响<br/>
    /// </summary>
    /// <param name="info">击杀事件的信息</param>
    /// <returns>false：不再触发 OnCheckMurderAsTarget 函数</returns>
    public bool OnCheckMurderAsKiller(MurderInfo info) => true;

    /// <summary>
    /// MurderPlayer 作为击杀者时的处理函数
    /// </summary>
    /// <param name="info">击杀事件的信息</param>
    public void OnMurderPlayerAsKiller(MurderInfo info) { }

    /// <summary>
    /// 本次击杀已经经过目标的检查，目标确定可以被击杀
    /// 这个函数相当于击杀者的二次确认，您还有机会取消本次击杀
    /// </summary>
    /// <param name="info">击杀事件的信息</param>
    public void BeforeMurderPlayerAsKiller(MurderInfo info) { }

    /// <summary>
    /// 更改击杀按钮的文本
    /// </summary>
    /// <param name="text">覆盖后的文本</param>
    /// <returns>true：确定要覆盖</returns>
    public bool OverrideKillButtonText(out string text)
    {
        text = default;
        return false;
    }

    /// <summary>
    /// 更改击杀按钮的图片
    /// </summary>
    /// <param name="buttonName">按钮图片名</param>
    /// <returns>true：确定要覆盖</returns>
    public bool OverrideKillButtonSprite(out string buttonName)
    {
        buttonName = default;
        return false;
    }

    /// <summary>
    /// 更改跳管按钮的图片
    /// </summary>
    /// <param name="buttonName">按钮图片名</param>
    /// <returns>true：确定要覆盖</returns>
    public bool OverrideVentButtonSprite(out string buttonName)
    {
        buttonName = default;
        return false;
    }
}
