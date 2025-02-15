using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using TheOtherRoles.Objects;
using TheOtherRoles.Patches;
using UnityEngine;
using static TheOtherRoles.Objects.CustomButton;

namespace TheOtherRoles;

[HarmonyPatch]
public class Pelican : RoleBase<Pelican>
{
    public static Color color = new Color32(240, 120, 200, byte.MaxValue);
    public static CustomButton pelicanKillButton;

    public PlayerControl currentTarget;
    public List<PlayerControl> eatenPlayers = new();
    public static float cooldown = 25f;
    public static float reduceCooldown = 25f;

    private static Sprite pelicanKillButtonSprite;
    public Pelican()
    {
        RoleType = roleId = RoleType.Pelican;

        currentTarget = null;
        eatenPlayers = new();
        cooldown = CustomOptionHolder.pelicanCooldown.getFloat();
        reduceCooldown = CustomOptionHolder.pelicanReduceCooldown.getFloat();
    }

    public override void OnMeetingStart()
    {
        if (eatenPlayers.Any(x => x == PlayerControl.LocalPlayer))
        {
            HudManager.Instance.PlayerCam.Target = PlayerControl.LocalPlayer;
            PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(player.transform.position);
        }
        eatenPlayers = new();
    }

    public override void OnMeetingEnd()
    {
    }
    public static void PelicanKill(byte targetId)
    {
        var target = Helpers.playerById(targetId);
        foreach (Pelican pelican in players)
        {
            if (pelican.player.Data.IsDead || target == null) return;
            target.Die(DeathReason.Kill, false);
            MurderPlayerPatch.Postfix(pelican.player, target);
            target.NetTransform.RpcSnapTo(new Vector3(-10f, 10f, 0f));
            pelican.eatenPlayers.Add(target);
            //if (PlayerControl.LocalPlayer == target)
            //    Camera.main.GetComponent<FollowerCamera>().Target = pelican.player;
        }
    }

    public static void PelicanDie(bool clear = false)
    {
        foreach (Pelican pelican in players)
        {
            if (clear || local.player?.Data.IsDead == true)
            {
                if (pelican.eatenPlayers.Any(x => x == PlayerControl.LocalPlayer))
                {
                    HudManager.Instance.PlayerCam.Target = PlayerControl.LocalPlayer;
                    PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(pelican.player.transform.position);
                }
                TheOtherRolesPlugin.Logger.LogMessage($"Pelican Player {pelican.player?.Data.PlayerName ?? "null"}");
                if (clear) Clear(true);
            }
        }
    }

    public override void FixedUpdate()
    {
        foreach (Pelican pelican in players)
        {
            if (player.isAlive() && eatenPlayers.Any(x => x == PlayerControl.LocalPlayer) && !MeetingHud.Instance)
            {
                HudManager.Instance.PlayerCam.Target = pelican.player;
                PlayerControl.LocalPlayer.transform.position = new(-10f, 10f, 0f);
                PlayerControl.LocalPlayer.moveable = false;
            }
        }
    }

    public override void OnKill(PlayerControl target)
    {
    }

    public override void OnDeath(PlayerControl killer = null)
    {
        if (killer == null && eatenPlayers?.Count > 0)
        {
            foreach (var player in eatenPlayers.Where(p => p != null && p.Data.IsDead))
            {
                if (PlayerControl.LocalPlayer == player)
                {
                    HudManager.Instance.PlayerCam.Target = PlayerControl.LocalPlayer;
                    PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(player.transform.position);
                }
                continue;
            }
            eatenPlayers = new();

            //PelicanDie();
        }
        else if (killer != null && eatenPlayers?.Count > 0)
        {
            foreach (var eatenPlayers in eatenPlayers.ToArray().Where(p => p != null && p.Data.IsDead))
            {
                eatenPlayers.NoDeath();
                PlayerControl.LocalPlayer.moveable = true;
                foreach (Pelican player in players)
                {
                    if (PlayerControl.LocalPlayer == eatenPlayers)
                    {
                        HudManager.Instance.PlayerCam.Target = PlayerControl.LocalPlayer;
                        PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(player.player.transform.position);
                    }
                }
                continue;
            }
            eatenPlayers = new();
        }
    }

    public override void OnFinishShipStatusBegin()
    {
    }

    public override void HandleDisconnect(PlayerControl player, DisconnectReasons reason)
    {
    }
    public static Sprite getpelicanKillButtonSprite()
    {
        if (pelicanKillButtonSprite) return pelicanKillButtonSprite;
        pelicanKillButtonSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.VultureButton.png", 115f);
        return pelicanKillButtonSprite;
    }

    public static void MakeButtons(HudManager hm)
    {
        pelicanKillButton = new CustomButton(
            () =>
            {
                var murderAttemptResult = Helpers.checkMuderAttempt(local.player, local.currentTarget);
                if (murderAttemptResult == MurderAttemptResult.SuppressKill) return;

                if (murderAttemptResult == MurderAttemptResult.PerformKill)
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                        PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PelicanKill, SendOption.Reliable);
                    writer.Write(local.currentTarget.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);

                    pelicanKillButton.Timer = reduceCooldown;
                    local.currentTarget = null;
                }
            },
            () =>
            {
                return PlayerControl.LocalPlayer.isRole(RoleType.Pelican) &&
                       !PlayerControl.LocalPlayer.Data.IsDead;
            },
            () =>
            {
                var untargetablePlayers = new List<PlayerControl>();
                foreach (Mini mini in Mini.players)
                {
                    if (mini != null && !Mini.isGrownUp(mini.player)) untargetablePlayers.Add(mini.player);
                }
                local.currentTarget = PlayerControlFixedUpdatePatch.setTarget(untargetablePlayers: untargetablePlayers);
                PlayerControlFixedUpdatePatch.setPlayerOutline(local.currentTarget, Palette.ImpostorRed);

                //showTargetNameOnButton(Pelican.currentTarget, pelicanKillButton, GetString("VultureText"));
                return local.currentTarget && PlayerControl.LocalPlayer.CanMove;
            },
            () => { pelicanKillButton.Timer = pelicanKillButton.MaxTimer; },
            getpelicanKillButtonSprite(),
            ButtonPositions.upperRowRight,
            hm,
            hm.AbilityButton,
            KeyCode.F
        );
        pelicanKillButton.buttonText = ModTranslation.getString("VultureText");
    }

    public static void SetButtonCooldowns()
    {
    }

    public static void Clear(bool clear = true)
    {
        players = new List<Pelican>();
    }
}
