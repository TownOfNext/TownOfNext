namespace TONX.Roles.Core.Interfaces;

public interface IOverrideWinner
{
    public void CheckWin(ref CustomWinner WinnerTeam, ref HashSet<byte> WinnerIds);
}
