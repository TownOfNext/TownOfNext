using AmongUs.GameOptions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TONX;

[HarmonyPatch(typeof(TaskAdderGame))]
class TaskAdderGamePatch
{
    private static TaskFolder CustomRolesFolder;
    private static TaskFolder ShipStylesFolder;
    [HarmonyPatch(nameof(TaskAdderGame.ShowFolder)), HarmonyPrefix]
    public static void ShowFolder_Prefix(TaskAdderGame __instance, [HarmonyArgument(0)] TaskFolder taskFolder)
    {
        if (__instance.Root != taskFolder) return;

        if (CustomRolesFolder == null && DestroyableSingleton<TutorialManager>.InstanceExists)
            CreateCustomFolder(__instance, ref CustomRolesFolder, Main.ModName);
        if (ShipStylesFolder == null)
            CreateCustomFolder(__instance, ref ShipStylesFolder, "ShipStyles");
    }
    private static void CreateCustomFolder(TaskAdderGame __instance, ref TaskFolder taskFolder, string name)
    {
        TaskFolder folder = Object.Instantiate<TaskFolder>(
            __instance.RootFolderPrefab,
            __instance.transform
        );
        folder.SetFolderColor(TaskFolder.FolderColor.Tan);
        folder.gameObject.SetActive(false);
        folder.FolderName = name;
        taskFolder = folder;
        __instance.Root.SubFolders.Add(taskFolder);
    }
    [HarmonyPatch(nameof(TaskAdderGame.ShowFolder)), HarmonyPostfix]
    public static void ShowFolder_Postfix(TaskAdderGame __instance, [HarmonyArgument(0)] TaskFolder taskFolder)
    {
        Logger.Info("Opened " + taskFolder.FolderName, "TaskFolder");
        float xCursor = 0f;
        float yCursor = 0f;
        float maxHeight = 0f;
        if (CustomRolesFolder != null && CustomRolesFolder.FolderName == taskFolder.FolderName)
        {
            var crewBehaviour = DestroyableSingleton<RoleManager>.Instance.AllRoles.ToArray().FirstOrDefault(role => role.Role == RoleTypes.Crewmate);
            foreach (var cRole in CustomRolesHelper.AllRoles)
            {
                var roleColor = Utils.GetRoleColor(cRole);
                CreateTaskAddButton(
                    __instance,
                    Utils.GetRoleName(cRole),
                    (int)cRole + 1000,
                    roleColor,
                    new Color(roleColor.r * 0.5f, roleColor.g * 0.5f, roleColor.b * 0.5f),
                    ref xCursor,
                    ref yCursor,
                    ref maxHeight
                );
            }
        }
        if (ShipStylesFolder != null && ShipStylesFolder.FolderName == taskFolder.FolderName)
        {
            var crewBehaviour = DestroyableSingleton<RoleManager>.Instance.AllRoles.ToArray().FirstOrDefault(role => role.Role == RoleTypes.Crewmate);
            for (var i = 0; i < SwitchShipStyleButtonPatch.ShipStyles.Count; i++)
            {
                var style = SwitchShipStyleButtonPatch.ShipStyles[i];
                if (!style) continue;
                CreateTaskAddButton(
                    __instance,
                    style.name,
                    i + 5000,
                    Color.white,
                    Main.ModColor32,
                    ref xCursor,
                    ref yCursor,
                    ref maxHeight
                );
            }
        }
    }
    private static void CreateTaskAddButton(TaskAdderGame __instance, string btnText, int id, Color fileColor, Color overColor, ref float xCursor, ref float yCursor, ref float maxHeight)
    {
        TaskAddButton button = Object.Instantiate<TaskAddButton>(__instance.RoleButton);
        button.Text.text = btnText;
        __instance.AddFileAsChild(ShipStylesFolder, button, ref xCursor, ref yCursor, ref maxHeight);
        var roleBehaviour = new RoleBehaviour
        {
            Role = (RoleTypes)id
        };
        button.Role = roleBehaviour;
        button.FileImage.color = fileColor;
        button.RolloverHandler.OutColor = fileColor;
        button.RolloverHandler.OverColor = overColor;
    }
    [HarmonyPatch(nameof(TaskAdderGame.PopulateRoot)), HarmonyPrefix]
    public static bool PopulateRoot_Prefix()
    {
        return DestroyableSingleton<TutorialManager>.InstanceExists;
    }
}

[HarmonyPatch(typeof(TaskAddButton))]
class TaskAddButtonPatch
{
    [HarmonyPatch(nameof(TaskAddButton.Start)), HarmonyPrefix]
    public static bool Start_Prefix(TaskAddButton __instance)
    {
        if (__instance.Role is null) return true;
        var role = (int)__instance.Role.Role;
        if (role >= 5000)
        {
            var style = role - 5000;
            __instance.Overlay.enabled = SwitchShipStyleButtonPatch.ShipStyles[style]?.active ?? false;
            __instance.Overlay.sprite = __instance.CheckImage;
            return false;
        }
        if (role >= 1000)
        {
            var PlayerCustomRole = PlayerControl.LocalPlayer.GetCustomRole();
            CustomRoles FileCustomRole = (CustomRoles)role - 1000;
            __instance.Overlay.enabled = PlayerCustomRole == FileCustomRole;
            __instance.Overlay.sprite = __instance.CheckImage;
            return false;
        }
        return true;
    }
    [HarmonyPatch(nameof(TaskAddButton.Update)), HarmonyPrefix]
    public static bool Update_Prefix(TaskAddButton __instance)
    {
        if (__instance.Role is null) return true;
        var role = (int)__instance.Role.Role;
        if (role >= 5000)
        {
            var style = role - 5000;
            __instance.Overlay.enabled = SwitchShipStyleButtonPatch.ShipStyles[style]?.active ?? false;
            return false;
        }
        if (role >= 1000)
        {
            var PlayerCustomRole = PlayerControl.LocalPlayer.GetCustomRole();
            CustomRoles FileCustomRole = (CustomRoles)role - 1000;
            __instance.Overlay.enabled = PlayerCustomRole == FileCustomRole;
            return false;
        }
        return true;
    }
    [HarmonyPatch(nameof(TaskAddButton.AddTask)), HarmonyPrefix]
    public static bool AddTask_Prefix(TaskAddButton __instance)
    {
        if (__instance.Role is null) return true;
        var role = (int)__instance.Role.Role;
        if (role >= 5000)
        {
            var style = role - 5000;
            var obj = SwitchShipStyleButtonPatch.ShipStyles[style];
            if (!obj) return false;
            var isActive = obj.active;
            foreach (var obj2 in SwitchShipStyleButtonPatch.ShipStyles) obj2?.SetActive(false);
            obj.SetActive(!isActive);
            return false;
        }
        if (role >= 1000)
        {
            CustomRoles FileCustomRole = (CustomRoles)role - 1000;
            PlayerControl.LocalPlayer.RpcSetCustomRole(FileCustomRole);
            PlayerControl.LocalPlayer.RpcSetRole(FileCustomRole.GetRoleTypes(), true);
            return false;
        }
        return true;
    }
}