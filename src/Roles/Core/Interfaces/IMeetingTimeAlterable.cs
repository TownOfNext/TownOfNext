namespace TONX.Roles.Core.Interfaces;

public interface IMeetingTimeAlterable
{
    /// <summary>
    /// 死亡后恢复会议时间<br/>
    /// 注意这是个 get-only 属性，写为 ｢=｣ 会不一样
    /// </summary>
    public bool RevertOnDie { get; }
    /// <summary>
    /// 增加或减少会议时间的函数
    /// </summary>
    /// <returns>时间增减值（秒）</returns>
    public int CalculateMeetingTimeDelta();
}
