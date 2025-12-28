using UnityEngine;

namespace TONX.Modules.OptionItems.Interfaces;

public interface IRoleOptionItem
{
    public CustomRoles RoleId { get; }
    public Color RoleColor { get; }
}