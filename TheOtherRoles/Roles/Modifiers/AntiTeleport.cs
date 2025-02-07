using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace TheOtherRoles;

[HarmonyPatch]
public class AntiTeleport : ModifierBase<AntiTeleport>
{
    public static Color color = Palette.Orange;
    public static Vector3 position;

    public AntiTeleport()
    {
        ModType = modId = ModifierType.AntiTeleport;
    }

    public static List<PlayerControl> candidates
    {
        get
        {
            List<PlayerControl> validPlayers = new();

            foreach (PlayerControl player in PlayerControl.AllPlayerControls.GetFastEnumerator())
                if (!player.hasModifier(ModifierType.AntiTeleport))
                    validPlayers.Add(player);

            return validPlayers;
        }
    }

    public static string postfix => ModTranslation.getString("antiTeleportPostfix");

    public static string fullName => ModTranslation.getString("antiTeleport");

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
        players = new List<AntiTeleport>();
        position = new Vector3();
    }
}
