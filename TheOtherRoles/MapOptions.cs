using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TheOtherRoles;

internal static class TORMapOptions
{
    // Set values
    public static int maxNumberOfMeetings = 10;
    public static bool blockSkippingInEmergencyMeetings;
    public static bool noVoteIsSelfVote;
    public static bool hidePlayerNames;
    public static bool hideSettings;
    public static bool hideOutOfSightNametags;

    public static bool randomizeColors;
    //public static bool allowDupeNames;

    public static int restrictDevices;
    public static bool restrictAdmin = true;
    public static float restrictAdminTime = 600f;
    public static float restrictAdminTimeMax = 600f;
    public static bool restrictAdminText = true;
    public static bool restrictCameras = true;
    public static float restrictCamerasTime = 600f;
    public static float restrictCamerasTimeMax = 600f;
    public static bool restrictCamerasText = true;
    public static bool restrictVitals = true;
    public static float restrictVitalsTime = 600f;
    public static float restrictVitalsTimeMax = 600f;
    public static bool restrictVitalsText = true;
    public static bool disableVents;

    public static bool ghostsSeeRoles = true;
    public static bool ghostsSeeTasks = true;
    public static bool ghostsSeeVotes = true;
    public static bool showRoleSummary = true;
    public static bool hideNameplates;
    public static bool allowParallelMedBayScans;
    public static bool showLighterDarker;
    public static bool hideTaskArrows;
    public static bool offlineHats = false;
    public static bool hideFakeTasks = false;
    public static bool betterSabotageMap = false;
    public static bool ShowChatNotifications = true;
    public static bool forceNormalSabotageMap = false;
    public static bool transparentMap = false;

    // Updating values
    public static int meetingsCount;
    public static List<SurvCamera> camerasToAdd = new();
    public static List<Vent> ventsToSeal = new();
    public static Dictionary<byte, PoolablePlayer> playerIcons = new();
    public static TextMeshPro AdminTimerText;
    public static TextMeshPro CamerasTimerText;
    public static TextMeshPro VitalsTimerText;

    public static bool canUseAdmin => restrictDevices == 0 || restrictAdminTime > 0f;

    public static bool couldUseAdmin => restrictDevices == 0 || !restrictAdmin || restrictAdminTimeMax > 0f;

    public static bool canUseCameras => restrictDevices == 0 || !restrictCameras || restrictCamerasTime > 0f;

    public static bool couldUseCameras => restrictDevices == 0 || !restrictCameras || restrictCamerasTimeMax > 0f;

    public static bool canUseVitals => restrictDevices == 0 || !restrictVitals || restrictVitalsTime > 0f;

    public static bool couldUseVitals => restrictDevices == 0 || !restrictVitals || restrictVitalsTimeMax > 0f;

    public static void clearAndReloadTORMapOptions()
    {
        meetingsCount = 0;
        camerasToAdd = new List<SurvCamera>();
        ventsToSeal = new List<Vent>();
        playerIcons = new Dictionary<byte, PoolablePlayer>();

        maxNumberOfMeetings = Mathf.RoundToInt(CustomOptionHolder.maxNumberOfMeetings.getSelection());
        blockSkippingInEmergencyMeetings = CustomOptionHolder.blockSkippingInEmergencyMeetings.getBool();
        noVoteIsSelfVote = CustomOptionHolder.noVoteIsSelfVote.getBool();
        hidePlayerNames = CustomOptionHolder.hidePlayerNames.getBool();

        hideOutOfSightNametags = CustomOptionHolder.hideOutOfSightNametags.getBool();

        hideSettings = CustomOptionHolder.hideSettings.getBool();

        randomizeColors = CustomOptionHolder.uselessOptions.getBool() && CustomOptionHolder.playerColorRandom.getBool();
        //allowDupeNames = CustomOptionHolder.uselessOptions.getBool() && CustomOptionHolder.playerNameDupes.getBool();

        restrictDevices = CustomOptionHolder.restrictDevices.getSelection();
        restrictAdmin = CustomOptionHolder.restrictAdmin.getBool();
        restrictAdminTime = restrictAdminTimeMax = CustomOptionHolder.restrictAdminTime.getFloat();
        restrictAdminText = CustomOptionHolder.restrictAdminText.getBool();
        restrictCameras = CustomOptionHolder.restrictCameras.getBool();
        restrictCamerasTime = restrictCamerasTimeMax = CustomOptionHolder.restrictCamerasTime.getFloat();
        restrictCamerasText = CustomOptionHolder.restrictCamerasText.getBool();
        restrictVitalsText = CustomOptionHolder.restrictVitalsText.getBool();
        restrictVitals = CustomOptionHolder.restrictVitals.getBool();
        restrictVitalsTime = restrictVitalsTimeMax = CustomOptionHolder.restrictVitalsTime.getFloat();
        disableVents = CustomOptionHolder.disableVents.getBool();
        ClearTimerText();
        UpdateTimerText();

        allowParallelMedBayScans = CustomOptionHolder.allowParallelMedBayScans.getBool();
        ghostsSeeRoles = TheOtherRolesPlugin.GhostsSeeRoles.Value;
        ghostsSeeTasks = TheOtherRolesPlugin.GhostsSeeTasks.Value;
        ghostsSeeVotes = TheOtherRolesPlugin.GhostsSeeVotes.Value;
        showRoleSummary = TheOtherRolesPlugin.ShowRoleSummary.Value;
        hideNameplates = TheOtherRolesPlugin.HideNameplates.Value;
        showLighterDarker = TheOtherRolesPlugin.ShowLighterDarker.Value;
        hideTaskArrows = TheOtherRolesPlugin.HideTaskArrows.Value;
        ShowChatNotifications = TheOtherRolesPlugin.ShowChatNotifications.Value;
    }

    public static void resetDeviceTimes()
    {
        restrictAdminTime = restrictAdminTimeMax;
        restrictCamerasTime = restrictCamerasTimeMax;
        restrictVitalsTime = restrictVitalsTimeMax;
    }

    public static void MeetingEndedUpdate()
    {
        ClearTimerText();
        UpdateTimerText();
    }

    public static void UpdateTimerText()
    {
        if (restrictDevices == 0 || (!restrictAdminText && !restrictCamerasText && !restrictVitalsText))
            return;
        if (FastDestroyableSingleton<HudManager>.Instance == null)
            return;

        // Admin
        if (restrictAdminText)
        {
            AdminTimerText = Object.Instantiate(FastDestroyableSingleton<HudManager>.Instance.TaskPanel.taskText,
                FastDestroyableSingleton<HudManager>.Instance.transform);
            float y = -4.0f;
            if (restrictCamerasText)
                y += 0.2f;
            if (restrictVitalsText)
                y += 0.2f;
            AdminTimerText.transform.localPosition = new Vector3(-3.5f, y, 0);
            if (restrictAdminTime > 0)
                AdminTimerText.text = string.Format(ModTranslation.getString("adminText"),
                    restrictAdminTime.ToString("0.00"));
            else
                AdminTimerText.text = ModTranslation.getString("adminRanOut");
            AdminTimerText.gameObject.SetActive(true);
        }

        // Cameras
        if (restrictCamerasText)
        {
            CamerasTimerText = Object.Instantiate(FastDestroyableSingleton<HudManager>.Instance.TaskPanel.taskText,
                FastDestroyableSingleton<HudManager>.Instance.transform);
            float y = -4.0f;
            if (restrictVitalsText)
                y += 0.2f;
            CamerasTimerText.transform.localPosition = new Vector3(-3.5f, y, 0);
            if (restrictCamerasTime > 0)
                CamerasTimerText.text = string.Format(ModTranslation.getString("camerasText"),
                    restrictCamerasTime.ToString("0.00"));
            else
                CamerasTimerText.text = ModTranslation.getString("camerasRanOut");
            CamerasTimerText.gameObject.SetActive(true);
        }

        // Vitals
        if (restrictVitalsText)
        {
            VitalsTimerText = Object.Instantiate(FastDestroyableSingleton<HudManager>.Instance.TaskPanel.taskText,
                FastDestroyableSingleton<HudManager>.Instance.transform);
            VitalsTimerText.transform.localPosition = new Vector3(-3.5f, -4.0f, 0);
            if (restrictVitalsTime > 0)
                VitalsTimerText.text = string.Format(ModTranslation.getString("vitalsText"),
                    restrictVitalsTime.ToString("0.00"));
            else
                VitalsTimerText.text = ModTranslation.getString("vitalsRanOut");
            VitalsTimerText.gameObject.SetActive(true);
        }
    }

    private static void ClearTimerText()
    {
        if (AdminTimerText != null)
            Object.Destroy(AdminTimerText);
        AdminTimerText = null;
        if (CamerasTimerText != null)
            Object.Destroy(CamerasTimerText);
        CamerasTimerText = null;
        if (VitalsTimerText != null)
            Object.Destroy(VitalsTimerText);
        VitalsTimerText = null;
    }
}
