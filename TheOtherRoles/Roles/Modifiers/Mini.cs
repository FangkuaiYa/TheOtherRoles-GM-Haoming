using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace TheOtherRoles;

[HarmonyPatch]
public class Mini : ModifierBase<Mini>
{
    public const float defaultColliderRadius = 0.2233912f;
    public const float defaultColliderOffset = 0.3636057f;
    public static Color color = Color.yellow;

    public static float growingUpDuration = 400f;
    public static bool triggerMiniLose;
    public DateTime timeOfGrowthStart = DateTime.UtcNow;

    public Mini()
    {
        ModType = modId = ModifierType.Mini;
    }

    public static List<PlayerControl> candidates
    {
        get
        {
            List<PlayerControl> validPlayers = new();

            foreach (PlayerControl player in PlayerControl.AllPlayerControls.GetFastEnumerator())
                if (!player.hasModifier(ModifierType.Mini))
                    validPlayers.Add(player);

            return validPlayers;
        }
    }

    public static string postfix => ModTranslation.getString("miniPostfix");

    public static string fullName => ModTranslation.getString("mini");

    public float growingProgress()
    {
        float timeSinceStart = (float)(DateTime.UtcNow - timeOfGrowthStart).TotalMilliseconds;
        return Mathf.Clamp(timeSinceStart / (growingUpDuration * 1000), 0f, 1f);
    }

    public static bool isGrownUp(PlayerControl player)
    {
        Mini mini = players.First(x => x.player == player);
        if (mini == null) return true;
        return mini.growingProgress() == 1f;
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

    public static void Clear()
    {
        players = new List<Mini>();
        triggerMiniLose = false;
        growingUpDuration = CustomOptionHolder.miniGrowingUpDuration.getFloat();
    }
}
