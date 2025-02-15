using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using TheOtherRoles.Objects;
using TheOtherRoles.Patches;
using UnityEngine;

namespace TheOtherRoles;

[HarmonyPatch]
public class Ninja : RoleBase<Ninja>
{
    private static CustomButton ninjaButton;

    public static Color color = Palette.ImpostorRed;

    private static Sprite buttonSprite;

    public bool penalized;
    public bool stealthed;
    public DateTime stealthedAt = DateTime.UtcNow;

    public Ninja()
    {
        RoleType = roleId = RoleType.Ninja;
        penalized = false;
        stealthed = false;
        stealthedAt = DateTime.UtcNow;
    }

    public static float stealthCooldown => CustomOptionHolder.ninjaStealthCooldown.getFloat();
    public static float stealthDuration => CustomOptionHolder.ninjaStealthDuration.getFloat();
    public static float killPenalty => CustomOptionHolder.ninjaKillPenalty.getFloat();
    public static float speedBonus => CustomOptionHolder.ninjaSpeedBonus.getFloat() / 100f;
    public static float fadeTime => CustomOptionHolder.ninjaFadeTime.getFloat();
    public static bool canUseVents => CustomOptionHolder.ninjaCanVent.getBool();
    public static bool canBeTargeted => CustomOptionHolder.ninjaCanBeTargeted.getBool();

    public override void OnMeetingStart()
    {
        stealthed = false;
        ninjaButton.isEffectActive = false;
        ninjaButton.Timer = ninjaButton.MaxTimer = stealthCooldown;
    }

    public override void OnMeetingEnd()
    {
        if (player == PlayerControl.LocalPlayer)
        {
            if (penalized)
                player.SetKillTimerUnchecked(GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown +
                                             killPenalty);
            else
                player.SetKillTimer(GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown);
        }
    }

    public override void ResetRole()
    {
        penalized = false;
        stealthed = false;
        setOpacity(player, 1.0f);
        ninjaButton.isEffectActive = false;
        ninjaButton.Timer = ninjaButton.MaxTimer = stealthCooldown;
    }

    public override void FixedUpdate()
    {
    }

    public override void HandleDisconnect(PlayerControl player, DisconnectReasons reason)
    {
    }

    public static bool isStealthed(PlayerControl player)
    {
        if (isRole(player) && player.isAlive())
        {
            Ninja n = players.First(x => x.player == player);
            return n.stealthed;
        }

        return false;
    }

    public static float stealthFade(PlayerControl player)
    {
        if (isRole(player) && fadeTime > 0f && player.isAlive())
        {
            Ninja n = players.First(x => x.player == player);
            return Mathf.Min(1.0f, (float)(DateTime.UtcNow - n.stealthedAt).TotalSeconds / fadeTime);
        }

        return 1.0f;
    }

    public static bool isPenalized(PlayerControl player)
    {
        if (isRole(player) && player.isAlive())
        {
            Ninja n = players.First(x => x.player == player);
            return n.penalized;
        }

        return false;
    }

    public static void setStealthed(PlayerControl player, bool stealthed = true)
    {
        if (isRole(player))
        {
            Ninja n = players.First(x => x.player == player);
            n.stealthed = stealthed;
            n.stealthedAt = DateTime.UtcNow;
        }
    }

    public override void OnKill(PlayerControl target)
    {
        penalized = stealthed;
        float penalty = penalized ? killPenalty : 0f;
        if (PlayerControl.LocalPlayer == player)
            player.SetKillTimerUnchecked(GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown + penalty);
    }

    public override void OnDeath(PlayerControl killer)
    {
        stealthed = false;
        ninjaButton.isEffectActive = false;
    }

    public override void OnFinishShipStatusBegin()
    {
    }

    public static Sprite getButtonSprite()
    {
        if (buttonSprite) return buttonSprite;
        buttonSprite = TheOtherRolesPlugin.getResources("NinjaButton.png");
        return buttonSprite;
    }

    public static void MakeButtons(HudManager hm)
    {
        // Ninja stealth
        ninjaButton = new CustomButton(
            () =>
            {
                if (ninjaButton.isEffectActive)
                {
                    ninjaButton.Timer = 0;
                    return;
                }

                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                    PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.NinjaStealth, SendOption.Reliable);
                writer.Write(PlayerControl.LocalPlayer.PlayerId);
                writer.Write(true);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.ninjaStealth(PlayerControl.LocalPlayer.PlayerId, true);
            },
            () =>
            {
                return PlayerControl.LocalPlayer.isRole(RoleType.Ninja) &&
                       !PlayerControl.LocalPlayer.Data.IsDead;
            },
            () =>
            {
                if (ninjaButton.isEffectActive)
                    ninjaButton.buttonText = ModTranslation.getString("NinjaUnstealthText");
                else
                    ninjaButton.buttonText = ModTranslation.getString("NinjaText");
                return PlayerControl.LocalPlayer.CanMove;
            },
            () => { ninjaButton.Timer = ninjaButton.MaxTimer = stealthCooldown; },
            getButtonSprite(),
            CustomButton.ButtonPositions.upperRowLeft,
            hm,
            hm.KillButton,
            KeyCode.F,
            true,
            stealthDuration,
            () =>
            {
                ninjaButton.Timer = ninjaButton.MaxTimer = stealthCooldown;

                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                    PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.NinjaStealth, SendOption.Reliable);
                writer.Write(PlayerControl.LocalPlayer.PlayerId);
                writer.Write(false);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.ninjaStealth(PlayerControl.LocalPlayer.PlayerId, false);

                PlayerControl.LocalPlayer.SetKillTimerUnchecked(
                    Math.Max(PlayerControl.LocalPlayer.killTimer, killPenalty));
            }
        )
        {
            buttonText = ModTranslation.getString("NinjaText"),
            effectCancellable = true
        };
    }

    public static void SetButtonCooldowns()
    {
        ninjaButton.MaxTimer = stealthCooldown;
    }

    public static void Clear()
    {
        players = new List<Ninja>();
    }

    public static void setOpacity(PlayerControl player, float opacity)
    {
        Color color = Color.Lerp(Palette.ClearWhite, Palette.White, opacity);
        try
        {
            Helpers.setInvisible(player, color, opacity);
        }
        catch
        {
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
    public static class PlayerPhysicsNinjaPatch
    {
        public static void Postfix(PlayerPhysics __instance)
        {
            if (__instance.AmOwner && __instance.myPlayer.CanMove && GameData.Instance &&
                isStealthed(__instance.myPlayer)) __instance.body.velocity *= speedBonus;

            if (isRole(__instance.myPlayer))
            {
                PlayerControl ninja = __instance.myPlayer;
                if (ninja == null || ninja.isDead()) return;

                bool canSee =
                    PlayerControl.LocalPlayer.isImpostor() ||
                    PlayerControl.LocalPlayer.isDead() ||
                    (Lighter.canSeeNinja && PlayerControl.LocalPlayer.isRole(RoleType.Lighter) &&
                     Lighter.isLightActive(PlayerControl.LocalPlayer));

                float opacity = canSee ? 0.1f : 0.0f;

                if (isStealthed(ninja))
                {
                    opacity = Math.Max(opacity, 1.0f - stealthFade(ninja));
                    ninja.cosmetics.currentBodySprite.BodySprite.material.SetFloat("_Outline", 0f);
                }
                else
                    opacity = Math.Max(opacity, stealthFade(ninja));

                setOpacity(ninja, opacity);
            }
        }
    }
}
