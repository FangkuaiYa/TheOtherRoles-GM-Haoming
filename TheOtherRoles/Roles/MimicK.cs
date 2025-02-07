using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TheOtherRoles.Objects;
using TheOtherRoles.Patches;
using UnityEngine;
using static TheOtherRoles.GameHistory;
using Object = UnityEngine.Object;

namespace TheOtherRoles;

[HarmonyPatch]
public class MimicK : RoleBase<MimicK>
{
    public static Color color = Palette.ImpostorRed;
    public static List<Arrow> arrows = new();
    public static float updateTimer;
    public static float arrowUpdateInterval = 0.5f;

    public MimicK()
    {
        RoleType = roleId = RoleType.MimicK;
    }

    public static bool ifOneDiesBothDie => CustomOptionHolder.mimicIfOneDiesBothDie.getBool();
    public static bool hasOneVote => CustomOptionHolder.mimicHasOneVote.getBool();
    public static bool countAsOne => CustomOptionHolder.mimicCountAsOne.getBool();

    public override void OnMeetingStart()
    {
        FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(3f, new Action<float>(p =>
        {
            // Delayed action
            if (p == 1f) player.resetMorph();
        })));
    }

    public override void OnMeetingEnd()
    {
    }

    public override void FixedUpdate()
    {
        if (PlayerControl.LocalPlayer == player)
            arrowUpdate();
    }

    public override void OnKill(PlayerControl target)
    {
        // 死体を消す
        DeadBody[] array = Object.FindObjectsOfType<DeadBody>();
        for (int i = 0; i < array.Length; i++)
            if (GameData.Instance.GetPlayerById(array[i].ParentId).PlayerId == target.PlayerId)
                array[i].gameObject.active = false;

        player.morphToPlayer(target);
    }

    public override void OnDeath(PlayerControl killer = null)
    {
        if (ifOneDiesBothDie)
        {
            PlayerControl partner = MimicA.players.FirstOrDefault().player;
            if (!partner.Data.IsDead)
            {
                if (killer != null)
                    partner.MurderPlayer(partner, MurderResultFlags.Succeeded);
                else
                    partner.Exiled();
                finalStatuses[partner.PlayerId] = FinalStatus.Suicide;
            }
        }
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
        players = new List<MimicK>();
    }

    public static bool isAlive()
    {
        foreach (MimicK p in players)
            if (!(p.player.Data.IsDead || p.player.Data.Disconnected))
                return true;
        return false;
    }

    private static void arrowUpdate()
    {
        // 前フレームからの経過時間をマイナスする
        updateTimer -= Time.fixedDeltaTime;

        // 1秒経過したらArrowを更新
        if (updateTimer <= 0.0f)
        {
            // 前回のArrowをすべて破棄する
            foreach (Arrow arrow in arrows)
                if (arrow != null && arrow.arrow != null)
                {
                    arrow.arrow.SetActive(false);
                    Object.Destroy(arrow.arrow);
                }

            // Arrows一覧
            arrows = new List<Arrow>();

            // インポスターの位置を示すArrowsを描画
            foreach (PlayerControl p in PlayerControl.AllPlayerControls)
            {
                if (p.Data.IsDead) continue;
                Arrow arrow;
                if (p.isRole(RoleType.MimicA))
                {
                    arrow = MimicA.isMorph ? new Arrow(Palette.White) : new Arrow(Palette.ImpostorRed);
                    arrow.arrow.SetActive(true);
                    arrow.Update(p.transform.position);
                    arrows.Add(arrow);
                }
            }

            // タイマーに時間をセット
            updateTimer = arrowUpdateInterval;
        }
    }
}
