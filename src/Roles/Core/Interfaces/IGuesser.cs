namespace TONX.Roles.Core.Interfaces;

/// <summary>
/// 持有赌怪能力的接口
/// </summary>
public interface IGuesser
{
    /// <summary>
    /// 猜测次数限制
    /// </summary>
    public int GuessLimit { get; set; }
    /// <summary>
    /// 无猜测次数时的提示消息
    /// </summary>
    public string GuessMaxMsg { get; }
    /// <summary>
    /// 是否可以猜测附加职业
    /// </summary>
    public bool CanGuessAddons => true;
    /// <summary>
    /// 是否可以猜测原版职业
    /// </summary>
    public bool CanGuessVanilla => true;
    /// <summary>
    /// 赌怪面板显示的阵营（按从左至右顺序）
    /// </summary>
    public List<CustomRoleTypes> GetCustomRoleTypesList()
        => new() { CustomRoleTypes.Impostor, CustomRoleTypes.Crewmate, CustomRoleTypes.Neutral, CustomRoleTypes.Addon };
    /// <summary>
    /// 猜测时调用
    /// </summary>
    /// <param name="guesser">赌怪</param>
    /// <param name="target">目标</param>
    /// <param name="role">猜测职业</param>
    /// <param name="reason">失败原因</param>
    /// <returns>返回false取消本次猜测</returns>
    public bool OnCheckGuessing(PlayerControl guesser, PlayerControl target, CustomRoles role, ref string reason) => true;
    /// <summary>
    /// 检查是否自杀时调用
    /// </summary>
    /// <param name="guesser">赌怪</param>
    /// <param name="target">目标</param>
    /// <param name="role">猜测职业</param>
    /// <returns>返回true则自杀，返回false则继续进行后续判断</returns>
    public bool OnCheckSuicide(PlayerControl guesser, PlayerControl target, CustomRoles role) => false;
    /// <summary>
    /// 检查完毕后调用<br/>
    /// 此时你还可以决定赌怪是否自杀<br/>
    /// </summary>
    /// <param name="guesser">赌怪</param>
    /// <param name="target">目标</param>
    /// <param name="role">猜测职业</param>
    /// <param name="guesserSuicide">是否自杀</param>
    /// <param name="reason">失败原因</param>
    /// <returns>返回false取消本次猜测</returns>
    public bool OnGuessing(PlayerControl guesser, PlayerControl target, CustomRoles role, bool guesserSuicide, ref string reason) => true;
    /// <summary>
    /// 猜测结束后调用
    /// </summary>
    /// <param name="guesser">赌怪</param>
    public void AfterGuessing(PlayerControl guesser) { }
}