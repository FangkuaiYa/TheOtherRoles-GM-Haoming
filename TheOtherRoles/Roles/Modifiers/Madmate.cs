using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace TheOtherRoles;

[HarmonyPatch]
public class Madmate : ModifierBase<Madmate>
{
    public enum MadmateAbility
    {
        None = 0,
        Fanatic = 1
    }

    public enum MadmateType
    {
        Simple = 0,
        WithRole = 1,
        Random = 2
    }

    public static Color color = Palette.ImpostorRed;

    public static List<RoleType> validRoles = new()
    {
        RoleType.NoRole, // NoRole = off
        RoleType.Shifter,
        RoleType.Mayor,
        RoleType.Engineer,
        RoleType.Sheriff,
        RoleType.Lighter,
        RoleType.Detective,
        RoleType.TimeMaster,
        RoleType.Medic,
        RoleType.Swapper,
        RoleType.Seer,
        RoleType.Hacker,
        RoleType.Tracker,
        RoleType.SecurityGuard,
        RoleType.Bait,
        RoleType.Medium,
        RoleType.NiceGuesser,
        RoleType.Watcher
    };

    public Madmate()
    {
        ModType = modId = ModifierType.Madmate;
    }

    public static bool canEnterVents => CustomOptionHolder.madmateCanEnterVents.getBool();
    public static bool hasImpostorVision => CustomOptionHolder.madmateHasImpostorVision.getBool();
    public static bool canSabotage => CustomOptionHolder.madmateCanSabotage.getBool();
    public static bool canFixComm => CustomOptionHolder.madmateCanFixComm.getBool();

    public static MadmateType madmateType => (MadmateType)CustomOptionHolder.madmateType.getSelection();
    public static MadmateAbility madmateAbility => (MadmateAbility)CustomOptionHolder.madmateAbility.getSelection();
    public static RoleType fixedRole => validRoles[CustomOptionHolder.madmateFixedRole.getSelection()];

    public static int numCommonTasks => CustomOptionHolder.madmateTasks.commonTasks;
    public static int numLongTasks => CustomOptionHolder.madmateTasks.longTasks;
    public static int numShortTasks => CustomOptionHolder.madmateTasks.shortTasks;

    public static bool hasTasks => madmateAbility == MadmateAbility.Fanatic;
    public static bool exileCrewmate => CustomOptionHolder.madmateExilePlayer.getBool();

    public static string prefix => ModTranslation.getString("madmatePrefix");

    public static string fullName => ModTranslation.getString("madmate");

    public static List<PlayerControl> candidates
    {
        get
        {
            List<PlayerControl> crewHasRole = new();
            List<PlayerControl> crewNoRole = new();
            List<PlayerControl> validCrewmates = new();

            foreach (PlayerControl player in PlayerControl.AllPlayerControls.GetFastEnumerator().ToArray()
                         .Where(x => x.isCrew() && !hasModifier(x)).ToList())
            {
                List<RoleInfo> info = RoleInfo.getRoleInfoForPlayer(player);
                if (info.Contains(RoleInfo.crewmate) && !player.hasModifier(ModifierType.Munou) &&
                    !player.isRole(RoleType.FortuneTeller))
                {
                    crewNoRole.Add(player);
                    validCrewmates.Add(player);
                }
                else if (info.Any(x => validRoles.Contains(x.roleType)))
                {
                    if (fixedRole == RoleType.NoRole || info.Any(x => x.roleType == fixedRole))
                        crewHasRole.Add(player);

                    validCrewmates.Add(player);
                }
            }

            if (madmateType == MadmateType.Simple) return crewNoRole;
            if (madmateType == MadmateType.WithRole && crewHasRole.Count > 0) return crewHasRole;
            if (madmateType == MadmateType.Random) return validCrewmates;
            return validCrewmates;
        }
    }

    public override void OnMeetingStart()
    {
    }

    public override void OnMeetingEnd()
    {
    }

    public override void FixedUpdate()
    {
    }

    public override void OnKill(PlayerControl target)
    {
    }

    public override void OnDeath(PlayerControl killer = null)
    {
        player.clearAllTasks();
    }

    public override void OnFinishShipStatusBegin()
    {
        PlayerControl.LocalPlayer.clearAllTasks();
        local.assignTasks();
    }

    public override void HandleDisconnect(PlayerControl player, DisconnectReasons reason)
    {
    }

    public static void MakeButtons(HudManager hm)
    {
    }

    public static void SetButtonCooldowns()
    {
    }

    public void assignTasks()
    {
        player.generateAndAssignTasks(numCommonTasks, numShortTasks, numLongTasks);
    }

    public static bool knowsImpostors(PlayerControl player)
    {
        return hasTasks && hasModifier(player) && tasksComplete(player);
    }

    public static bool tasksComplete(PlayerControl player)
    {
        if (!hasTasks) return false;

        int counter = 0;
        int totalTasks = numCommonTasks + numLongTasks + numShortTasks;
        if (totalTasks == 0) return true;
        foreach (NetworkedPlayerInfo.TaskInfo task in player.Data.Tasks)
            if (task.Complete)
                counter++;

        return counter == totalTasks;
    }

    public static void Clear()
    {
        players = new List<Madmate>();
    }
}
