using TONX.Modules.OptionItems.Interfaces;
using TONX.Roles.Core;
using UnityEngine;

namespace TONX.Modules.OptionItems;

public sealed class RoleSpawnChanceOptionItem : StringOptionItem, IRoleOptionItem
{
    public RoleSpawnChanceOptionItem(
        int id,
        string name,
        int defaultIndex,
        TabGroup tab,
        bool isSingleValue,
        string[] selections,
        CustomRoles roleId,
        Color roleColor) : base(id, name, defaultIndex, tab, isSingleValue, selections)
    {
        RoleId = roleId;
        RoleColor = roleColor;
    }
    public RoleSpawnChanceOptionItem(
        int id,
        string name,
        int defaultIndex,
        TabGroup tab,
        bool isSingleValue,
        string[] selections,
        SimpleRoleInfo roleInfo) : this(id, name, defaultIndex, tab, isSingleValue, selections, roleInfo.RoleName, roleInfo.RoleColor) { }

    public CustomRoles RoleId { get; }
    public Color RoleColor { get; }

    public override void Refresh()
    {
        base.Refresh();
        if (OptionBehaviour != null && OptionBehaviour.TitleText != null)
        {
            OptionBehaviour.TitleText.text = GetName(true);
        }
    }
}