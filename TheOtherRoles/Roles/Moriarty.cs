using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using TheOtherRoles.Objects;
using TMPro;
using UnityEngine;
using static TheOtherRoles.Patches.PlayerControlFixedUpdatePatch;
using Object = UnityEngine.Object;

namespace TheOtherRoles;

[HarmonyPatch]
public class Moriarty : RoleBase<Moriarty>
{
    public static Color color = Color.green;
    public static PlayerControl tmpTarget;
    public static PlayerControl target;
    public static PlayerControl currentTarget;
    public static PlayerControl killTarget;
    public static List<PlayerControl> brainwashed;
    public static CustomButton killButton;
    public static int KillDistance;
    public static CustomButton brainwashButton;
    public static float killCooldown = 0;
    public static TMP_Text text;
    public static int counter;
    public static Sprite brainwashIcon;
    public static Sprite brainwashGyudonIcon;
    public static Sprite brainwashNikukyuuIcon;
    public static List<Arrow> arrows = new();
    public static float updateTimer;
    public static float arrowUpdateInterval = 0.5f;
    public static TMP_Text targetPositionText;
    public static Sprite arrowSprite;

    public Moriarty()
    {
        RoleType = roleId = RoleType.NoRole;
    }

    public static int brainwashDistance => CustomOptionHolder.moriartyBrainwashDistance.getSelection();
    public static int killDistance => CustomOptionHolder.moriartyKillDistance.getSelection();
    public static float brainwashTime => CustomOptionHolder.moriartyBrainwashTime.getFloat();
    public static float brainwashCooldown => CustomOptionHolder.moriartyBrainwashCooldown.getFloat();
    public static int numberToWin => (int)CustomOptionHolder.moriartyNumberToWin.getFloat();

    public override void OnMeetingStart()
    {
    }

    public override void OnMeetingEnd()
    {
        brainwashed.Clear();
        target = null;
    }

    public override void FixedUpdate()
    {
        if (player == PlayerControl.LocalPlayer)
        {
            currentTarget = setTarget(killDistance: brainwashDistance);
            if (target != null)
                killTarget = setTarget(targetingPlayer: target, killDistance: killDistance);
            else
                killTarget = null;
            setPlayerOutline(currentTarget, color);
            arrowUpdate();
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
        // kill button
        killButton = new CustomButton(
            // OnClick
            () =>
            {
                MurderAttemptResult murder =
                    Helpers.checkMuderAttemptAndKill(PlayerControl.LocalPlayer, killTarget, showAnimation: false);
                if (murder != MurderAttemptResult.BlankKill)
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                        (byte)CustomRPC.MoriartyKill, SendOption.Reliable, -1);
                    writer.Write(killTarget.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.moriartyKill(killTarget.PlayerId);
                    target = null;
                    brainwashButton.Timer = brainwashButton.MaxTimer;
                }
            },
            // HasButton
            () =>
            {
                return PlayerControl.LocalPlayer.isRole(RoleType.Moriarty) && PlayerControl.LocalPlayer.isAlive();
            },
            // CouldUse
            () =>
            {
                if (text != null) text.text = $"{counter}/{numberToWin}";
                killButton.buttonText = killTarget ? killTarget.name : "None";
                return killTarget != null && PlayerControl.LocalPlayer.CanMove;
            },
            // OnMeetingEnds
            () => { killButton.Timer = killButton.MaxTimer = killCooldown; },
            hm.KillButton.graphic.sprite,
            CustomButton.ButtonPositions.upperRowRight,
            hm,
            hm.KillButton,
            KeyCode.Q
        )
        {
            MaxTimer = killCooldown
        };
        killButton.Timer = killButton.MaxTimer;
        text = GameObject.Instantiate(killButton.actionButton.cooldownTimerText,
            killButton.actionButton.cooldownTimerText.transform.parent);
        text.text = "";
        text.enableWordWrapping = false;
        text.transform.localScale = Vector3.one * 0.5f;
        text.transform.localPosition += new Vector3(-0.05f, 0.7f, 0);

        brainwashButton = new CustomButton(
            // OnClick
            () =>
            {
                if (currentTarget != null)
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                        (byte)CustomRPC.SetBrainwash, SendOption.Reliable, -1);
                    writer.Write(currentTarget.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.setBrainwash(currentTarget.PlayerId);

                    // 洗脳終了までのカウントダウン
                    TMP_Text text;
                    RoomTracker roomTracker = HudManager.Instance?.roomTracker;
                    GameObject gameObject = Object.Instantiate(roomTracker.gameObject);
                    Object.DestroyImmediate(gameObject.GetComponent<RoomTracker>());
                    gameObject.transform.SetParent(HudManager.Instance.transform);
                    gameObject.transform.localPosition = new Vector3(0, -1.3f, gameObject.transform.localPosition.z);
                    gameObject.transform.localScale = Vector3.one * 3f;
                    text = gameObject.GetComponent<TMP_Text>();
                    PlayerControl tmpP = target;
                    bool done = false;
                    HudManager.Instance.StartCoroutine(Effects.Lerp(brainwashTime, new Action<float>(p =>
                    {
                        if (done) return;
                        if (target == null || MeetingHud.Instance != null || p == 1f)
                        {
                            if (text != null && text.gameObject) Object.Destroy(text.gameObject);
                            if (target == tmpP) target = null;
                            done = true;
                        }
                        else
                        {
                            string message = (brainwashTime - (p * brainwashTime)).ToString("0");
                            bool even = (int)(p * brainwashTime / 0.25f) % 2 == 0; // Bool flips every 0.25 seconds
                            // string prefix = even ? "<color=#555555FF>" : "<color=#FFFFFFFF>";
                            string prefix = "<color=#555555FF>";
                            text.text = prefix + message + "</color>";
                            if (text != null) text.color = even ? Color.yellow : Color.red;
                        }
                    })));
                }

                tmpTarget = null;
                brainwashButton.Timer = brainwashButton.MaxTimer;
            },
            // HasButton
            () =>
            {
                return PlayerControl.LocalPlayer.isRole(RoleType.Moriarty) && PlayerControl.LocalPlayer.isAlive() &&
                       target == null;
            },
            // CouldUse
            () =>
            {
                brainwashButton.buttonText = currentTarget ? currentTarget.name : "None";
                if (currentTarget != null && currentTarget.name == "牛丼")
                    brainwashButton.Sprite = getBrainwashGyudonIcon();
                else if (currentTarget != null && currentTarget.name == "にくきゅう")
                    brainwashButton.Sprite = getBrainwashNikukyuuIcon();
                else
                    brainwashButton.Sprite = getBrainwashIcon();
                return PlayerControl.LocalPlayer.CanMove && currentTarget != null;
            },
            // OnMeetingEnds
            () => { brainwashButton.Timer = brainwashButton.MaxTimer; },
            getBrainwashIcon(),
            new Vector3(-0.9f, 1f, 0),
            hm,
            hm.AbilityButton,
            KeyCode.F
        )
        {
            buttonText = ModTranslation.getString("moriartyBrainwashButton"),
            MaxTimer = brainwashCooldown
        };
    }

    public static void SetButtonCooldowns()
    {
        brainwashButton.MaxTimer = brainwashCooldown;
        killButton.MaxTimer = killCooldown;
    }

    public static void Clear()
    {
        players = new List<Moriarty>();
        brainwashed = new List<PlayerControl>();
        counter = 0;
        arrows = new List<Arrow>();
    }

    public static Sprite getBrainwashIcon()
    {
        if (brainwashIcon) return brainwashIcon;
        brainwashIcon = TheOtherRolesPlugin.getResources("Brainwash.png");
        return brainwashIcon;
    }

    public static Sprite getBrainwashGyudonIcon()
    {
        if (brainwashGyudonIcon) return brainwashGyudonIcon;
        brainwashGyudonIcon = TheOtherRolesPlugin.getResources("BrainwashGyudon.png");
        return brainwashGyudonIcon;
    }

    public static Sprite getBrainwashNikukyuuIcon()
    {
        if (brainwashNikukyuuIcon) return brainwashNikukyuuIcon;
        brainwashNikukyuuIcon = TheOtherRolesPlugin.getResources("BrainwashNikukyuu.png");
        return brainwashNikukyuuIcon;
    }

    public static int countLovers()
    {
        int counter = 0;
        foreach (PlayerControl player in allPlayers)
            if (player.isLovers())
                counter += 1;
        return counter;
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
            // ターゲットの位置を示すArrowを描画
            if (target != null && !target.isDead())
            {
                Arrow arrow = new(Palette.CrewmateBlue);
                arrow.arrow.SetActive(true);
                arrow.Update(target.transform.position);
                arrows.Add(arrow);
                if (targetPositionText == null)
                {
                    RoomTracker roomTracker = HudManager.Instance?.roomTracker;
                    if (roomTracker == null) return;
                    GameObject gameObject = Object.Instantiate(roomTracker.gameObject);
                    Object.DestroyImmediate(gameObject.GetComponent<RoomTracker>());
                    gameObject.transform.SetParent(HudManager.Instance.transform);
                    gameObject.transform.localPosition = new Vector3(0, -2.0f, gameObject.transform.localPosition.z);
                    gameObject.transform.localScale = Vector3.one * 1.0f;
                    targetPositionText = gameObject.GetComponent<TMP_Text>();
                    targetPositionText.alpha = 1.0f;
                }

                PlainShipRoom room = Helpers.getPlainShipRoom(target);
                targetPositionText.gameObject.SetActive(true);
                int nearestPlayer = 0;
                foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                    if (p != target)
                    {
                        float dist = Vector2.Distance(p.transform.position, target.transform.position);
                        if (dist < 7f) nearestPlayer += 1;
                    }

                if (room != null)
                    targetPositionText.text = "<color=#8CFFFFFF>" + $"{target.name}({nearestPlayer})(" +
                                              DestroyableSingleton<TranslationController>.Instance.GetString(
                                                  room.RoomId) + ")</color>";
                else
                    targetPositionText.text = "<color=#8CFFFFFF>" + $"{target.name}({nearestPlayer})</color>";
            }
            else
            {
                if (targetPositionText != null) targetPositionText.text = "";
            }

            // タイマーに時間をセット
            updateTimer = arrowUpdateInterval;
        }
    }

    public static Sprite getArrowSprite()
    {
        if (!arrowSprite)
            arrowSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.Arrow.png", 300f);
        return arrowSprite;
    }

    public static bool isAlive()
    {
        return allPlayers.Count(x => x.isAlive()) != 0;
    }
}
