namespace TONX.Roles.Core.Interfaces;

public interface IDeathReasonSeeable
{
    /// <summary>
    /// 可以知道死因的接口
    /// </summary>
    /// <param name="seen">死亡的玩家</param>
    /// <returns>true：可以看到死因</returns>
    public bool CheckSeeDeathReason(PlayerControl seen) => true;
}
