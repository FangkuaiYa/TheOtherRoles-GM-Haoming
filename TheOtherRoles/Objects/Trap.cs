using System;
using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using TheOtherRoles.Patches;
using UnityEngine;

namespace TheOtherRoles.Objects;

public class Trap
{
    public static Sprite trapSprite;
    public static Sprite trapActiveSprite;
    public static AudioClip place;
    public static AudioClip activate;
    public static AudioClip disable;
    public static AudioClip countdown;
    public static AudioClip kill;
    public static AudioRolloffMode rollOffMode = AudioRolloffMode.Linear;
    private static byte maxId;
    public static SortedDictionary<byte, Trap> traps = new();
    public AudioSource audioSource;
    public bool isActive;
    public DateTime placedTime;
    public PlayerControl target;
    public GameObject trap;

    public Trap(Vector3 pos)
    {
        // 最初の罠を消す
        if (traps.Count == Trapper.numTrap)
            foreach (byte key in traps.Keys)
            {
                Trap firstTrap = traps[key];
                if (firstTrap.trap != null)
                    GameObject.DestroyObject(firstTrap.trap);
                traps.Remove(key);
                break;
            }

        // 罠を設置
        trap = new GameObject("Trap");
        SpriteRenderer trapRenderer = trap.AddComponent<SpriteRenderer>();
        trap.AddSubmergedComponent(SubmergedCompatibility.Classes.ElevatorMover);
        trapRenderer.sprite = trapSprite;
        Vector3 position = new(pos.x, pos.y, (pos.y / 1000) + 0.001f);
        trap.transform.position = position;
        // this.trap.transform.localPosition = pos;
        trap.SetActive(true);

        // 音を鳴らす
        audioSource = trap.gameObject.AddComponent<AudioSource>();
        audioSource.priority = 0;
        audioSource.spatialBlend = 1;
        audioSource.clip = place;
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.maxDistance = 2 * Trapper.maxDistance / 3;
        audioSource.minDistance = Trapper.minDistance;
        audioSource.rolloffMode = rollOffMode;
        audioSource.PlayOneShot(place);

        // 設置時刻を設定
        placedTime = DateTime.UtcNow;

        traps.Add(getAvailableId(), this);
    }

    public static void loadSprite()
    {
        if (trapSprite == null)
            trapSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Trap.png", 300f);
        if (trapActiveSprite == null)
            trapActiveSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.TrapActive.png", 300f);
    }

    private static byte getAvailableId()
    {
        byte ret = maxId;
        maxId++;
        return ret;
    }

    public static void clearAllTraps()
    {
        loadSprite();
        foreach (Trap trap in traps.Values)
            if (trap.trap != null)
                GameObject.DestroyObject(trap.trap);
        traps = new SortedDictionary<byte, Trap>();
        maxId = 0;
    }

    public static void activateTrap(byte trapId, PlayerControl trapper, PlayerControl target)
    {
        Trap trap = traps[trapId];

        // 有効にする
        trap.isActive = true;
        trap.target = target;
        SpriteRenderer spriteRenderer = trap.trap.gameObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = trapActiveSprite;

        // 他のトラップを全て無効化する
        SortedDictionary<byte, Trap> newTraps = new()
        {
            { trapId, trap }
        };
        foreach (Trap t in traps.Values)
        {
            if (t.trap == null || t == trap) continue;
            t.trap.SetActive(false);
            GameObject.Destroy(t.trap);
        }

        traps = newTraps;


        // 音を鳴らす
        trap.audioSource.Stop();
        trap.audioSource.loop = true;
        trap.audioSource.priority = 0;
        trap.audioSource.spatialBlend = 1;
        trap.audioSource.maxDistance = Trapper.maxDistance;
        trap.audioSource.clip = countdown;
        trap.audioSource.Play();

        // ターゲットを動けなくする
        target.NetTransform.Halt();

        bool moveableFlag = false;
        FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(Trapper.killTimer,
            new Action<float>(p =>
            {
                try
                {
                    if (Trapper.meetingFlag) return;
                    if (trap == null || trap.trap == null || !trap.isActive) //　解除された場合の処理
                    {
                        if (!moveableFlag)
                        {
                            target.moveable = true;
                            moveableFlag = true;
                        }
                    }
                    else if (p == 1f && target.isAlive())
                    {
                        // 正常にキルが発生する場合の処理
                        target.moveable = true;
                        if (PlayerControl.LocalPlayer.isRole(RoleType.Trapper))
                        {
                            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                                PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.TrapperKill,
                                SendOption.Reliable);
                            writer.Write(trapId);
                            writer.Write(PlayerControl.LocalPlayer.PlayerId);
                            writer.Write(target.PlayerId);
                            AmongUsClient.Instance.FinishRpcImmediately(writer);
                            RPCProcedure.trapperKill(trapId, PlayerControl.LocalPlayer.PlayerId,
                                target.PlayerId);
                        }
                    }
                    else
                    {
                        // カウントダウン中の処理
                        target.moveable = false;
                        target.transform.position = trap.trap.transform.position + new Vector3(0, 0.3f, 0);
                    }
                }
                catch (Exception e)
                {
                    Helpers.log("カウントダウン中にエラー発生");
                    Helpers.log(e.Message);
                }
            })));
    }

    public static void disableTrap(byte trapId)
    {
        Trap trap = traps[trapId];
        trap.isActive = false;
        trap.audioSource.Stop();
        trap.audioSource.PlayOneShot(disable);
        FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(disable.length, new Action<float>(p =>
        {
            if (p == 1f)
            {
                if (trap.trap != null)
                    trap.trap.SetActive(false);
                GameObject.Destroy(trap.trap);
                traps.Remove(trapId);
            }
        })));

        if (PlayerControl.LocalPlayer.isRole(RoleType.Trapper))
        {
            PlayerControl.LocalPlayer.killTimer =
                GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown + Trapper.penaltyTime;
            Trapper.trapperSetTrapButton.Timer = Trapper.cooldown + Trapper.penaltyTime;
        }
    }

    public static void trapKill(byte trapId, PlayerControl trapper, PlayerControl target)
    {
        Trap trap = traps[trapId];
        AudioSource audioSource = trap.audioSource;
        audioSource.Stop();
        audioSource.maxDistance = Trapper.maxDistance;
        audioSource.PlayOneShot(kill);
        FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(kill.length, new Action<float>(p =>
        {
            if (p == 1f) clearAllTraps();
        })));
        Trapper.isTrapKill = true;
        KillAnimationCoPerformKillPatch.hideNextAnimation = true;
        trapper.MurderPlayer(target, MurderResultFlags.Succeeded);
    }

    public static void onMeeting()
    {
        Trapper.meetingFlag = true;
        foreach (KeyValuePair<byte, Trap> trap in traps)
        {
            trap.Value.audioSource.Stop();
            if (trap.Value.target != null)
                if (PlayerControl.LocalPlayer.isRole(RoleType.Trapper))
                    if (!trap.Value.target.isDead())
                    {
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                            PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.TrapperKill,
                            SendOption.Reliable);
                        writer.Write(trap.Key);
                        writer.Write(PlayerControl.LocalPlayer.PlayerId);
                        writer.Write(trap.Value.target.PlayerId);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                        RPCProcedure.trapperKill(trap.Key, PlayerControl.LocalPlayer.PlayerId,
                            trap.Value.target.PlayerId);
                    }
        }
    }

    public static bool isTrapped(PlayerControl p)
    {
        foreach (Trap trap in traps.Values)
            if (trap.target == p)
                return true;
        return false;
    }

    public static bool hasTrappedPlayer()
    {
        foreach (Trap trap in traps.Values)
            if (trap.target != null)
                return true;
        return false;
    }

    public static Trap getActiveTrap()
    {
        foreach (Trap trap in traps.Values)
            if (trap.target != null)
                return trap;
        return null;
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
    public static class PlayerPhysicsTrapPatch
    {
        public static void Postfix(PlayerPhysics __instance)
        {
            foreach (Trap trap in traps.Values)
            {
                bool canSee =
                    trap.isActive ||
                    PlayerControl.LocalPlayer.isImpostor() ||
                    PlayerControl.LocalPlayer.isDead() ||
                    (PlayerControl.LocalPlayer.isRole(RoleType.Lighter) &&
                     Lighter.isLightActive(PlayerControl.LocalPlayer)) ||
                    PlayerControl.LocalPlayer.isRole(RoleType.Fox);
                float opacity = canSee ? 1.0f : 0.0f;
                if (trap.trap != null)
                    trap.trap.GetComponent<SpriteRenderer>().material.color =
                        Color.Lerp(Palette.ClearWhite, Palette.White, opacity);
            }
        }
    }
}
