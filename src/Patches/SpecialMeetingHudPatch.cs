using TONX.Modules;
using TONX.Roles.Crewmate;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TONX;

[HarmonyPatch]
public static class SpecialMeetingHudPatch
{
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
class JusticeMeetingHudPatch
{
    public static void Postfix(MeetingHud __instance)
    {
        HandleJusticeMeeting(__instance);
    }
    public static void HandleJusticeMeeting(MeetingHud __instance)
    {
        if (!Justice.IsJusticeMeeting()) return;

        var targets = Justice.GetHostingJustice()?.SelectedPlayers ?? new();
        var num = -1;
        foreach (var pva in __instance.playerStates)
        {
            if (!targets.Contains(pva.TargetPlayerId))
            {
                pva.gameObject.SetActive(false);
                continue;
            }
            pva.transform.localPosition = new Vector3(2f * num, 0f, pva.transform.localPosition.z);
            num *= -1;
        }
        __instance.SkipVoteButton.gameObject.SetActive(false);
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
class RoleDraftMeetingHudPatch
{
    public static void Postfix(MeetingHud __instance)
    {
        HandleRoleDraftMeeting(__instance);
    }
    public static void HandleRoleDraftMeeting(MeetingHud __instance)
    {
        if (!CustomGameMode.RoleDraft.IsEnable() || CustomRoleSelector.RoleAssigned) return;
        foreach (var pva in __instance.playerStates) pva.gameObject.SetActive(false);
    }
}