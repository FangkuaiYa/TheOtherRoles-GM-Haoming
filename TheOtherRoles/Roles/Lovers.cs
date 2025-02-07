using System.Collections.Generic;
using HarmonyLib;
using TheOtherRoles.Patches;
using UnityEngine;
using static TheOtherRoles.GameHistory;
using static TheOtherRoles.TheOtherRoles;

namespace TheOtherRoles;

public class Couple
{
    public Color color;
    public PlayerControl lover1;
    public PlayerControl lover2;

    public Couple(PlayerControl lover1, PlayerControl lover2, Color color)
    {
        this.lover1 = lover1;
        this.lover2 = lover2;
        this.color = color;
    }

    public string icon => Helpers.cs(color, " ♥");

    public bool existing => lover1 != null && lover2 != null && !lover1.Data.Disconnected && !lover2.Data.Disconnected;

    public bool alive => lover1 != null && lover2 != null && lover1.isAlive() && lover2.isAlive();

    public bool existingAndAlive => existing && alive;

    public bool existingWithKiller =>
        existing && (lover1 == Jackal.jackal || lover2 == Jackal.jackal
                                             || lover1 == Sidekick.sidekick || lover2 == Sidekick.sidekick
                                             || lover1.Data.Role.IsImpostor || lover2.Data.Role.IsImpostor);

    public bool hasAliveKillingLover => existingAndAlive && existingWithKiller;
}

[HarmonyPatch]
public static class Lovers
{
    public static List<Couple> couples = new();
    public static Color color = new Color32(232, 57, 185, byte.MaxValue);

    public static List<Color> loverIconColors = new()
    {
        color, // pink
        new Color32(255, 165, 0, 255), // orange
        new Color32(255, 255, 0, 255), // yellow
        new Color32(0, 255, 0, 255), // green
        new Color32(0, 0, 255, 255), // blue
        new Color32(0, 255, 255, 255), // light blue
        new Color32(255, 0, 0, 255) // red
    };

    public static bool bothDie => CustomOptionHolder.loversBothDie.getBool();

    // Making this closer to the au.libhalt.net version of Lovers
    public static bool separateTeam => CustomOptionHolder.loversSeparateTeam.getBool();
    public static bool tasksCount => CustomOptionHolder.loversTasksCount.getBool();
    public static bool enableChat => CustomOptionHolder.loversEnableChat.getBool();

    public static bool hasTasks => tasksCount;

    public static string getIcon(PlayerControl player)
    {
        if (isLovers(player))
        {
            Couple couple = couples.Find(x => x.lover1 == player || x.lover2 == player);
            return couple.icon;
        }

        return "";
    }

    public static void addCouple(PlayerControl player1, PlayerControl player2)
    {
        List<Color> availableColors = new(loverIconColors);
        foreach (Couple couple in couples) availableColors.RemoveAll(x => x == couple.color);
        couples.Add(new Couple(player1, player2, availableColors[0]));
    }

    public static void eraseCouple(PlayerControl player)
    {
        couples.RemoveAll(x => x.lover1 == player || x.lover2 == player);
    }

    public static void swapLovers(PlayerControl player1, PlayerControl player2)
    {
        int couple1 = couples.FindIndex(x => x.lover1 == player1 || x.lover2 == player1);
        int couple2 = couples.FindIndex(x => x.lover1 == player2 || x.lover2 == player2);

        // trying to swap within the same couple, just ignore
        if (couple1 == couple2) return;

        if (couple1 >= 0)
        {
            if (couples[couple1].lover1 == player1) couples[couple1].lover1 = player2;
            if (couples[couple1].lover2 == player1) couples[couple1].lover2 = player2;
        }

        if (couple2 >= 0)
        {
            if (couples[couple2].lover1 == player2) couples[couple2].lover1 = player1;
            if (couples[couple2].lover2 == player2) couples[couple2].lover2 = player1;
        }
    }

    public static void killLovers(PlayerControl player, PlayerControl killer = null)
    {
        if (!player.isLovers()) return;

        if (separateTeam && tasksCount)
            player.clearAllTasks();

        if (!bothDie) return;

        PlayerControl partner = getPartner(player);
        if (partner != null)
        {
            if (!partner.Data.IsDead)
            {
                if (killer != null)
                    partner.MurderPlayer(partner, MurderResultFlags.Succeeded);
                else
                    partner.Exiled();

                finalStatuses[partner.PlayerId] = FinalStatus.Suicide;
            }

            if (separateTeam && tasksCount)
                partner.clearAllTasks();
        }
    }

    public static PlayerControl getPartner(PlayerControl player)
    {
        Couple couple = getCouple(player);
        if (couple != null) return player?.PlayerId == couple.lover1?.PlayerId ? couple.lover2 : couple.lover1;
        return null;
    }

    public static bool isLovers(PlayerControl player)
    {
        return getCouple(player) != null;
    }

    public static Couple getCouple(PlayerControl player)
    {
        foreach (Couple pair in couples)
            if (pair.lover1?.PlayerId == player?.PlayerId || pair.lover2?.PlayerId == player?.PlayerId)
                return pair;
        return null;
    }

    public static bool existing(PlayerControl player)
    {
        return getCouple(player)?.existing == true;
    }

    public static bool anyAlive()
    {
        foreach (Couple couple in couples)
            if (couple.alive)
                return true;
        return false;
    }

    public static bool anyNonKillingCouples()
    {
        foreach (Couple couple in couples)
            if (!couple.hasAliveKillingLover)
                return true;
        return false;
    }

    public static bool existingAndAlive(PlayerControl player)
    {
        return getCouple(player)?.existingAndAlive == true;
    }

    public static bool existingWithKiller(PlayerControl player)
    {
        return getCouple(player)?.existingWithKiller == true;
    }

    public static void HandleDisconnect(PlayerControl player, DisconnectReasons reason)
    {
        eraseCouple(player);
    }

    public static void Clear()
    {
        couples = new List<Couple>();
    }
}
