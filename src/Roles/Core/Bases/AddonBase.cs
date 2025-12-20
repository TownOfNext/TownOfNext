using AmongUs.GameOptions;
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
}
