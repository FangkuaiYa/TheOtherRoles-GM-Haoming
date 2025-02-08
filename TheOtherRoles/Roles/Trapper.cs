using System;
using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using TheOtherRoles.Objects;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheOtherRoles;

[HarmonyPatch]
public class Trapper : RoleBase<Trapper>
{
    public enum Status
    {
        notPlaced,
        placed,
        active
    }

    public static Color color = Palette.ImpostorRed;
    public static Sprite trapButtonSprite;
    public static DateTime placedTime;
    public static float minDistance = 0f;
    public static bool isTrapKill;
    public static bool meetingFlag;

    public static CustomButton trapperSetTrapButton;

    private static Sprite trapeffectSprite;


    public Trapper()
    {
        RoleType = roleId = RoleType.NoRole;
    }

    public static int numTrap => (int)CustomOptionHolder.trapperNumTrap.getFloat();
    public static float extensionTime => CustomOptionHolder.trapperExtensionTime.getFloat();
    public static float killTimer => CustomOptionHolder.trapperKillTimer.getFloat();
    public static float cooldown => CustomOptionHolder.trapperCooldown.getFloat();
    public static float maxDistance => CustomOptionHolder.trapperMaxDistance.getFloat();
    public static float trapRange => CustomOptionHolder.trapperTrapRange.getFloat();
    public static float penaltyTime => CustomOptionHolder.trapperPenaltyTime.getFloat();
    public static float bonusTime => CustomOptionHolder.trapperBonusTime.getFloat();

    public override void OnMeetingStart()
    {
    }

    public override void OnMeetingEnd()
    {
        Trap.clearAllTraps();
        meetingFlag = false;
    }

    public override void FixedUpdate()
    {
        // 処理に自信がないので念の為tryで囲っておく
        try
        {
            if (PlayerControl.LocalPlayer.isRole(RoleType.Trapper) && Trap.traps.Count != 0 &&
                !Trap.hasTrappedPlayer() && !meetingFlag)
                // トラップを踏んだプレイヤーを動けなくする
                foreach (PlayerControl p in PlayerControl.AllPlayerControls.GetFastEnumerator())
                    foreach (KeyValuePair<byte, Trap> trap in Trap.traps)
                    {
                        if (DateTime.UtcNow.Subtract(trap.Value.placedTime).TotalSeconds < extensionTime) continue;
                        if (trap.Value.isActive || p.isDead() || p.inVent || meetingFlag) continue;
                        Vector3 p1 = p.transform.localPosition;
                        Dictionary<GameObject, byte> listActivate = new();
                        Vector3 p2 = trap.Value.trap.transform.localPosition;
                        float distance = Vector3.Distance(p1, p2);
                        if (distance < trapRange)
                        {
                            TMP_Text text;
                            RoomTracker roomTracker = FastDestroyableSingleton<HudManager>.Instance?.roomTracker;
                            GameObject gameObject = Object.Instantiate(roomTracker.gameObject);
                            Object.DestroyImmediate(gameObject.GetComponent<RoomTracker>());
                            gameObject.transform.SetParent(FastDestroyableSingleton<HudManager>.Instance.transform);
                            gameObject.transform.localPosition =
                                new Vector3(0, -1.8f, gameObject.transform.localPosition.z);
                            gameObject.transform.localScale = Vector3.one * 2f;
                            text = gameObject.GetComponent<TMP_Text>();
                            text.text = string.Format(ModTranslation.getString("trapperGetTrapped"), p.name);
                            FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(3f,
                                new Action<float>(p =>
                                {
                                    if (p == 1f && text != null && text.gameObject != null) Object.Destroy(text.gameObject);
                                })));
                            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                                PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ActivateTrap,
                                SendOption.Reliable, -1);
                            writer.Write(trap.Key);
                            writer.Write(PlayerControl.LocalPlayer.PlayerId);
                            writer.Write(p.PlayerId);
                            AmongUsClient.Instance.FinishRpcImmediately(writer);
                            RPCProcedure.activateTrap(trap.Key, PlayerControl.LocalPlayer.PlayerId,
                                p.PlayerId);
                            break;
                        }
                    }

            if (PlayerControl.LocalPlayer.isRole(RoleType.Trapper) && Trap.hasTrappedPlayer() &&
                !meetingFlag)
                // トラップにかかっているプレイヤーを救出する
                foreach (KeyValuePair<byte, Trap> trap in Trap.traps)
                {
                    if (trap.Value.trap == null || !trap.Value.isActive) return;
                    Vector3 p1 = trap.Value.trap.transform.position;
                    foreach (PlayerControl player in PlayerControl.AllPlayerControls.GetFastEnumerator())
                    {
                        if (player.PlayerId == trap.Value.target.PlayerId || player.isDead() || player.inVent ||
                            player.isRole(RoleType.Trapper)) continue;
                        Vector3 p2 = player.transform.position;
                        float distance = Vector3.Distance(p1, p2);
                        if (distance < 0.5)
                        {
                            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                                PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DisableTrap,
                                SendOption.Reliable, -1);
                            writer.Write(trap.Key);
                            AmongUsClient.Instance.FinishRpcImmediately(writer);
                            RPCProcedure.disableTrap(trap.Key);
                        }
                    }
                }
        }
        catch (NullReferenceException e)
        {
            Helpers.log(e.Message);
        }
    }

    public override void OnKill(PlayerControl target)
    {
        //　キルクールダウン設定
        if (PlayerControl.LocalPlayer.isRole(RoleType.Trapper))
        {
            if (Trap.isTrapped(target) && !isTrapKill) // トラップにかかっている対象をキルした場合のボーナス
            {
                Helpers.log("トラップにかかっている対象をキルした場合のボーナス");
                player.killTimer = GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown - bonusTime;
                trapperSetTrapButton.Timer = cooldown - bonusTime;
            }
            else if (Trap.isTrapped(target) && isTrapKill) // トラップキルした場合のペナルティ
            {
                Helpers.log("トラップキルした場合のクールダウン");
                player.killTimer = GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown;
                trapperSetTrapButton.Timer = cooldown;
            }
            else // トラップにかかっていない対象を通常キルした場合はペナルティーを受ける
            {
                Helpers.log("通常キル時のペナルティ");
                player.killTimer = GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown + penaltyTime;
                trapperSetTrapButton.Timer = cooldown + penaltyTime;
            }

            if (!isTrapKill)
            {
                MessageWriter writer;
                writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                    (byte)CustomRPC.ClearTrap, SendOption.Reliable, -1);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.clearTrap();
            }

            isTrapKill = false;
        }
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
        trapperSetTrapButton = new CustomButton(
            () =>
            {
                // ボタンが押された時に実行
                if (!PlayerControl.LocalPlayer.CanMove || Trap.hasTrappedPlayer()) return;
                setTrap();
                trapperSetTrapButton.Timer = trapperSetTrapButton.MaxTimer;
            },
            () =>
            {
                /*ボタン有効になる条件*/
                return PlayerControl.LocalPlayer.isRole(RoleType.Trapper) &&
                       !PlayerControl.LocalPlayer.Data.IsDead;
            },
            () =>
            {
                /*ボタンが使える条件*/
                return PlayerControl.LocalPlayer.CanMove && !Trap.hasTrappedPlayer();
            },
            () =>
            {
                /*ミーティング終了時*/
                trapperSetTrapButton.Timer = trapperSetTrapButton.MaxTimer;
            },
            getTrapButtonSprite(),
            // new Vector3(-2.6f, 0f, 0f),
            CustomButton.ButtonPositions.upperRowLeft,
            hm,
            hm.AbilityButton,
            KeyCode.F
        )
        {
            buttonText = ModTranslation.getString("trapperPlaceTrap")
        };
    }

    public static void SetButtonCooldowns()
    {
        trapperSetTrapButton.MaxTimer = cooldown;
    }

    public static void Clear()
    {
        players = new List<Trapper>();
        meetingFlag = false;
        Trap.clearAllTraps();
    }

    public static Sprite getTrapButtonSprite()
    {
        if (trapButtonSprite) return trapButtonSprite;
        trapButtonSprite = TheOtherRolesPlugin.getResources("TrapperButton.png");
        return trapButtonSprite;
    }

    public static void setTrap()
    {
        Vector3 pos = PlayerControl.LocalPlayer.transform.position;
        byte[] buff = new byte[sizeof(float) * 2];
        Buffer.BlockCopy(BitConverter.GetBytes(pos.x), 0, buff, 0 * sizeof(float), sizeof(float));
        Buffer.BlockCopy(BitConverter.GetBytes(pos.y), 0, buff, 1 * sizeof(float), sizeof(float));
        MessageWriter writer =
            AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlaceTrap);
        writer.WriteBytesAndSize(buff);
        writer.EndMessage();
        RPCProcedure.placeTrap(buff);
        placedTime = DateTime.UtcNow;
    }

    public static Sprite getTrapEffectSprite()
    {
        if (trapeffectSprite) return trapeffectSprite;
        trapeffectSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.TrapEffect.png", 300f);
        return trapeffectSprite;
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdReportDeadBody))]
    private class PlayerControlCmdReportDeadBodyPatch
    {
        public static void Prefix(PlayerControl __instance)
        {
            // トラップ中にミーティングが来たら直後に死亡する
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.TrapperMeetingFlag, SendOption.Reliable, -1);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.trapperMeetingFlag();
        }
    }
}
