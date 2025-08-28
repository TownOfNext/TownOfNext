using AmongUs.GameOptions;
using TONX.Roles.Neutral;

namespace TONX.Roles.Core.Interfaces;

/// <summary>
/// 内鬼的接口<br/>
/// <see cref="IKiller"/>的继承
/// </summary>
public interface IImpostor : IKiller
{
    /// インポスターは基本サボタージュボタンを使える
    bool IKiller.CanUseSabotageButton() => true;
    /// <summary>
    /// 是否可以成为绝境者
    /// </summary>
    public bool CanBeLastImpostor => true;
}
