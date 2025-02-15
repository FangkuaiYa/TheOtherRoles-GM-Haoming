using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using TheOtherRoles.Objects;
using TheOtherRoles.Patches;
using UnityEngine;

namespace TheOtherRoles;

[HarmonyPatch]
public class SerialKiller : RoleBase<SerialKiller>
{
    private static CustomButton serialKillerButton;

    public static Color color = Palette.ImpostorRed;

    private static Sprite buttonSprite;

    public bool isCountDown;

    public SerialKiller()
    {
        RoleType = roleId = RoleType.SerialKiller;
        isCountDown = false;
    }

    public static float killCooldown => CustomOptionHolder.serialKillerKillCooldown.getFloat();

    public static float suicideTimer =>
        Mathf.Max(CustomOptionHolder.serialKillerSuicideTimer.getFloat(), killCooldown + 2.5f);

    public static bool resetTimer => CustomOptionHolder.serialKillerResetTimer.getBool();

    public override void OnMeetingStart()
    {
    }

    public override void OnMeetingEnd()
    {
        if (PlayerControl.LocalPlayer.isRole(RoleType.SerialKiller))
        {
            PlayerControl.LocalPlayer.SetKillTimerUnchecked(killCooldown);

            if (resetTimer)
                serialKillerButton.Timer = suicideTimer;
        }
    }

    public override void FixedUpdate()
    {
    }

    public override void HandleDisconnect(PlayerControl player, DisconnectReasons reason)
    {
    }

    public override void OnKill(PlayerControl target)
    {
        if (PlayerControl.LocalPlayer == player)
            player.SetKillTimerUnchecked(killCooldown);

        serialKillerButton.Timer = suicideTimer;
        isCountDown = true;
    }

    public override void OnDeath(PlayerControl killer)
    {
    }

    public override void OnFinishShipStatusBegin()
    {
    }

    public static Sprite getButtonSprite()
    {
        if (buttonSprite) return buttonSprite;
        buttonSprite = TheOtherRolesPlugin.getResources("SuicideButton.png");
        return buttonSprite;
    }

    public static void MakeButtons(HudManager hm)
    {
        // SerialKiller Suicide Countdown
        serialKillerButton = new CustomButton(
            () => { },
            () =>
            {
                return PlayerControl.LocalPlayer.isRole(RoleType.SerialKiller) &&
                       PlayerControl.LocalPlayer.isAlive() && local.isCountDown;
            },
            () => { return true; },
            () => { },
            getButtonSprite(),
            CustomButton.ButtonPositions.upperRowLeft,
            hm,
            hm.AbilityButton,
            KeyCode.F,
            true,
            suicideTimer,
            () => { local.suicide(); }
        )
        {
            buttonText = ModTranslation.getString("SerialKillerText"),
            isEffectActive = true
        };
    }

    public void suicide()
    {
        byte targetId = PlayerControl.LocalPlayer.PlayerId;
        MessageWriter killWriter = AmongUsClient.Instance.StartRpcImmediately(
            PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SerialKillerSuicide, SendOption.Reliable);
        killWriter.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(killWriter);
        RPCProcedure.serialKillerSuicide(targetId);
    }

    public static void SetButtonCooldowns()
    {
        serialKillerButton.MaxTimer = suicideTimer;
    }

    public static void Clear()
    {
        players = new List<SerialKiller>();
    }
}
