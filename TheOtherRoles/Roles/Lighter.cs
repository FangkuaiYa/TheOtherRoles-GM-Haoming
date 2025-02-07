using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TheOtherRoles.Objects;
using UnityEngine;

namespace TheOtherRoles;

[HarmonyPatch]
public class Lighter : RoleBase<Lighter>
{
    private static CustomButton lighterButton;

    public static Color color = new Color32(238, 229, 190, byte.MaxValue);

    private static Sprite buttonSprite;

    public bool lightActive;

    public Lighter()
    {
        RoleType = roleId = RoleType.Lighter;
        lightActive = false;
    }

    public static float lighterModeLightsOnVision => CustomOptionHolder.lighterModeLightsOnVision.getFloat();
    public static float lighterModeLightsOffVision => CustomOptionHolder.lighterModeLightsOffVision.getFloat();
    public static bool canSeeNinja => CustomOptionHolder.lighterCanSeeNinja.getBool();

    public static float cooldown => CustomOptionHolder.lighterCooldown.getFloat();
    public static float duration => CustomOptionHolder.lighterDuration.getFloat();

    public static bool isLightActive(PlayerControl player)
    {
        if (isRole(player) && player.isAlive())
        {
            Lighter r = players.First(x => x.player == player);
            return r.lightActive;
        }

        return false;
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
        // Lighter light
        lighterButton = new CustomButton(
            () => { local.lightActive = true; },
            () =>
            {
                return PlayerControl.LocalPlayer.isRole(RoleType.Lighter) &&
                       !PlayerControl.LocalPlayer.Data.IsDead;
            },
            () => { return PlayerControl.LocalPlayer.CanMove; },
            () =>
            {
                if (local != null) local.lightActive = false;
                lighterButton.Timer = lighterButton.MaxTimer;
                lighterButton.isEffectActive = false;
                lighterButton.actionButton.graphic.color = Palette.EnabledColor;
            },
            getButtonSprite(),
            CustomButton.ButtonPositions.lowerRowRight,
            hm,
            hm.UseButton,
            KeyCode.F,
            true,
            duration,
            () =>
            {
                local.lightActive = false;
                lighterButton.Timer = lighterButton.MaxTimer;
            }
        )
        {
            buttonText = ModTranslation.getString("LighterText")
        };
    }

    public static void SetButtonCooldowns()
    {
        lighterButton.MaxTimer = cooldown;
        lighterButton.EffectDuration = duration;
    }

    public static void Clear()
    {
        players = new List<Lighter>();
    }

    public static Sprite getButtonSprite()
    {
        if (buttonSprite) return buttonSprite;
        buttonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.LighterButton.png", 115f);
        return buttonSprite;
    }
}
