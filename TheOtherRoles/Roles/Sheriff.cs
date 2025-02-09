using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using TheOtherRoles.Objects;
using TMPro;
using UnityEngine;
using static TheOtherRoles.Patches.PlayerControlFixedUpdatePatch;
using static TheOtherRoles.TheOtherRoles;

namespace TheOtherRoles;

[HarmonyPatch]
public class Sheriff : RoleBase<Sheriff>
{
    private static CustomButton sheriffKillButton;
    public static TMP_Text sheriffNumShotsText;

    public static Color color = new Color32(248, 205, 70, byte.MaxValue);
    public bool canKill = sheriffCanKillNoDeadBody;
    public PlayerControl currentTarget;

    public int numShots = 2;

    public Sheriff()
    {
        RoleType = roleId = RoleType.Sheriff;
        numShots = maxShots;
        canKill = sheriffCanKillNoDeadBody;
    }

    public static float cooldown => CustomOptionHolder.sheriffCooldown.getFloat();
    public static int maxShots => Mathf.RoundToInt(CustomOptionHolder.sheriffNumShots.getFloat());
    public static bool canKillNeutrals => CustomOptionHolder.sheriffCanKillNeutrals.getBool();
    public static bool misfireKillsTarget => CustomOptionHolder.sheriffMisfireKillsTarget.getBool();
    public static bool spyCanDieToSheriff => CustomOptionHolder.spyCanDieToSheriff.getBool();
    public static bool madmateCanDieToSheriff => CustomOptionHolder.madmateCanDieToSheriff.getBool();
    public static bool createdMadmateCanDieToSheriff => CustomOptionHolder.createdMadmateCanDieToSheriff.getBool();
    public static bool sheriffCanKillNoDeadBody => CustomOptionHolder.sheriffCanKillNoDeadBody.getBool();
    public static bool honmeiCanDieToSheriff => CustomOptionHolder.akujoSheriffKillsHonmei.getBool();

    public override void OnMeetingStart()
    {
    }

    public override void OnMeetingEnd()
    {
        canKill = sheriffCanKillNoDeadBody ||
                  PlayerControl.AllPlayerControls.GetFastEnumerator().ToArray().Any(p => p.Data.IsDead);
    }

    public override void FixedUpdate()
    {
        if (player == PlayerControl.LocalPlayer && numShots > 0)
        {
            currentTarget = setTarget();
            setPlayerOutline(currentTarget, color);
        }
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
        // Sheriff Kill
        sheriffKillButton = new CustomButton(
            () =>
            {
                if (local.numShots <= 0) return;

                MurderAttemptResult murderAttemptResult =
                    Helpers.checkMuderAttempt(PlayerControl.LocalPlayer, local.currentTarget);
                if (murderAttemptResult == MurderAttemptResult.SuppressKill) return;

                if (murderAttemptResult == MurderAttemptResult.PerformKill)
                {
                    bool misfire = false;
                    byte targetId = local.currentTarget.PlayerId;
                    ;
                    if ((local.currentTarget.Data.Role.IsImpostor &&
                         (!local.currentTarget.hasModifier(ModifierType.Mini) ||
                          Mini.isGrownUp(local.currentTarget))) ||
                        (spyCanDieToSheriff && Spy.spy == local.currentTarget) ||
                        (madmateCanDieToSheriff && local.currentTarget.hasModifier(ModifierType.Madmate)) ||
                        (createdMadmateCanDieToSheriff &&
                         local.currentTarget.hasModifier(ModifierType.CreatedMadmate)) ||
                        (canKillNeutrals && local.currentTarget.isNeutral()) ||
                        (honmeiCanDieToSheriff && local.currentTarget.hasModifier(ModifierType.AkujoHonmei)) ||
                        Jackal.jackal == local.currentTarget || Sidekick.sidekick == local.currentTarget)
                        //targetId = Sheriff.currentTarget.PlayerId;
                        misfire = false;
                    else
                        //targetId = PlayerControl.LocalPlayer.PlayerId;
                        misfire = true;

                    // Mad sheriff always misfires.
                    if (local.player.hasModifier(ModifierType.Madmate)) misfire = true;
                    MessageWriter killWriter = AmongUsClient.Instance.StartRpcImmediately(
                        PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SheriffKill, SendOption.Reliable);
                    killWriter.Write(PlayerControl.LocalPlayer.Data.PlayerId);
                    killWriter.Write(targetId);
                    killWriter.Write(misfire);
                    AmongUsClient.Instance.FinishRpcImmediately(killWriter);
                    RPCProcedure.sheriffKill(PlayerControl.LocalPlayer.Data.PlayerId, targetId, misfire);
                }

                sheriffKillButton.Timer = sheriffKillButton.MaxTimer;
                local.currentTarget = null;
            },
            () =>
            {
                return PlayerControl.LocalPlayer.isRole(RoleType.Sheriff) && local.numShots > 0 &&
                       !PlayerControl.LocalPlayer.Data.IsDead && local.canKill;
            },
            () =>
            {
                if (sheriffNumShotsText != null)
                {
                    if (local.numShots > 0)
                        sheriffNumShotsText.text =
                            string.Format(ModTranslation.getString("sheriffShots"), local.numShots);
                    else
                        sheriffNumShotsText.text = "";
                }

                return local.currentTarget && PlayerControl.LocalPlayer.CanMove;
            },
            () => { sheriffKillButton.Timer = sheriffKillButton.MaxTimer; },
            hm.KillButton.graphic.sprite,
            CustomButton.ButtonPositions.upperRowRight,
            hm,
            hm.KillButton,
            KeyCode.Q
        );

        sheriffNumShotsText = GameObject.Instantiate(sheriffKillButton.actionButton.cooldownTimerText,
            sheriffKillButton.actionButton.cooldownTimerText.transform.parent);
        sheriffNumShotsText.text = "";
        sheriffNumShotsText.enableWordWrapping = false;
        sheriffNumShotsText.transform.localScale = Vector3.one * 0.5f;
        sheriffNumShotsText.transform.localPosition += new Vector3(-0.05f, 0.7f, 0);
    }

    public static void SetButtonCooldowns()
    {
        sheriffKillButton.MaxTimer = cooldown;
    }

    public static void Clear()
    {
        players = new List<Sheriff>();
    }
}
