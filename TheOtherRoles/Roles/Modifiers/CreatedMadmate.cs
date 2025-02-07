using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace TheOtherRoles;

[HarmonyPatch]
public class CreatedMadmate : ModifierBase<CreatedMadmate>
{
    public enum CreatedMadmateAbility
    {
        None = 0,
        Fanatic = 1
    }

    public enum CreatedMadmateType
    {
        Simple = 0,
        WithRole = 1,
        Random = 2
    }

    public static Color color = Palette.ImpostorRed;

    public CreatedMadmate()
    {
        ModType = modId = ModifierType.CreatedMadmate;
    }

    public static bool canEnterVents => CustomOptionHolder.createdMadmateCanEnterVents.getBool();
    public static bool hasImpostorVision => CustomOptionHolder.createdMadmateHasImpostorVision.getBool();
    public static bool canSabotage => CustomOptionHolder.createdMadmateCanSabotage.getBool();
    public static bool canFixComm => CustomOptionHolder.createdMadmateCanFixComm.getBool();

    public static CreatedMadmateType madmateType => CreatedMadmateType.Simple;

    public static CreatedMadmateAbility madmateAbility =>
        (CreatedMadmateAbility)CustomOptionHolder.createdMadmateAbility.getSelection();

    public static int numTasks => (int)CustomOptionHolder.createdMadmateNumTasks.getFloat();

    public static bool hasTasks => madmateAbility == CreatedMadmateAbility.Fanatic;
    public static bool exileCrewmate => CustomOptionHolder.createdMadmateExileCrewmate.getBool();

    public static string prefix => ModTranslation.getString("madmatePrefix");

    public static string fullName => ModTranslation.getString("madmate");

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
        player.generateAndAssignTasks(0, numTasks, 0);
    }

    public static bool knowsImpostors(PlayerControl player)
    {
        return hasTasks && hasModifier(player) && tasksComplete(player);
    }

    public static bool tasksComplete(PlayerControl player)
    {
        if (!hasTasks) return false;

        int counter = 0;
        int totalTasks = numTasks;
        if (totalTasks == 0) return true;
        foreach (NetworkedPlayerInfo.TaskInfo task in player.Data.Tasks)
            if (task.Complete)
                counter++;

        return counter >= totalTasks;
    }

    public static void Clear()
    {
        players = new List<CreatedMadmate>();
    }
}
