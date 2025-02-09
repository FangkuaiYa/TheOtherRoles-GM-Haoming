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
public class PlagueDoctor : RoleBase<PlagueDoctor>
{
    private static CustomButton plagueDoctorButton;
    public static Color color = new Color32(255, 192, 0, byte.MaxValue);

    public static Dictionary<int, PlayerControl> infected;
    public static Dictionary<int, float> progress;
    public static Dictionary<int, bool> dead;
    public static TMP_Text statusText;
    public static TMP_Text numInfectionsText;
    public static bool triggerPlagueDoctorWin;

    public static Sprite plagueDoctorIcon;

    public PlayerControl currentTarget;
    public bool meetingFlag;
    public int numInfections;

    public PlagueDoctor()
    {
        RoleType = roleId = RoleType.PlagueDoctor;

        numInfections = maxInfectable;
        meetingFlag = false;

        updateDead();
    }

    public static float infectCooldown => CustomOptionHolder.plagueDoctorInfectCooldown.getFloat();
    public static int maxInfectable => Mathf.RoundToInt(CustomOptionHolder.plagueDoctorNumInfections.getFloat());
    public static float infectDistance => CustomOptionHolder.plagueDoctorDistance.getFloat();
    public static float infectDuration => CustomOptionHolder.plagueDoctorDuration.getFloat();
    public static float immunityTime => CustomOptionHolder.plagueDoctorImmunityTime.getFloat();

    public static bool infectKiller => CustomOptionHolder.plagueDoctorInfectKiller.getBool();

    public static bool resetAfterMeeting =>
        //return CustomOptionHolder.plagueDoctorResetMeeting.getBool();
        false;

    public static bool canWinDead => CustomOptionHolder.plagueDoctorWinDead.getBool();

    public override void OnMeetingStart()
    {
        meetingFlag = true;
    }

    public override void OnMeetingEnd()
    {
        if (resetAfterMeeting) progress.Clear();

        updateDead();

        FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(immunityTime, new Action<float>(p =>
        {
            // 5秒後から感染開始
            if (p == 1f) meetingFlag = false;
        })));
    }

    public override void OnKill(PlayerControl target)
    {
    }

    public override void HandleDisconnect(PlayerControl player, DisconnectReasons reason)
    {
    }

    public override void OnDeath(PlayerControl killer = null)
    {
        if (killer != null && infectKiller)
        {
            byte targetId = killer.PlayerId;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlagueDoctorSetInfected,
                SendOption.Reliable);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.plagueDoctorInfected(targetId);
        }
    }

    public override void OnFinishShipStatusBegin()
    {
    }

    public override void FixedUpdate()
    {
        if (player == PlayerControl.LocalPlayer)
        {
            if (numInfections > 0 && player.isAlive())
            {
                currentTarget = setTarget(untargetablePlayers: infected.Values.ToList());
                setPlayerOutline(currentTarget, color);
            }

            if (!meetingFlag && (canWinDead || player.isAlive()))
            {
                List<PlayerControl> newInfected = new();
                foreach (PlayerControl target in PlayerControl.AllPlayerControls)
                {
                    // 非感染プレイヤーのループ
                    if (target == player || target.isDead() || infected.ContainsKey(target.PlayerId) ||
                        target.inVent) continue;

                    // データが無い場合は作成する
                    if (!progress.ContainsKey(target.PlayerId)) progress[target.PlayerId] = 0f;

                    foreach (PlayerControl source in infected.Values.ToList())
                    {
                        // 感染プレイヤーのループ
                        if (source.isDead()) continue;
                        float distance = Vector3.Distance(source.transform.position, target.transform.position);
                        // 障害物判定
                        bool anythingBetween = PhysicsHelpers.AnythingBetween(source.GetTruePosition(),
                            target.GetTruePosition(), Constants.ShipAndObjectsMask, false);

                        if (distance <= infectDistance && !anythingBetween)
                        {
                            progress[target.PlayerId] += Time.fixedDeltaTime;

                            // 他のクライアントに進行状況を通知する
                            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                                PlayerControl.LocalPlayer.NetId,
                                (byte)CustomRPC.PlagueDoctorUpdateProgress, SendOption.Reliable);
                            writer.Write(target.PlayerId);
                            writer.Write(progress[target.PlayerId]);
                            AmongUsClient.Instance.FinishRpcImmediately(writer);

                            // Only update a player's infection once per FixedUpdate
                            break;
                        }
                    }

                    // 既定値を超えたら感染扱いにする
                    if (progress[target.PlayerId] >= infectDuration) newInfected.Add(target);
                }

                // 感染者に追加する
                foreach (PlayerControl p in newInfected)
                {
                    byte targetId = p.PlayerId;
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                        PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlagueDoctorSetInfected,
                        SendOption.Reliable);
                    writer.Write(targetId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.plagueDoctorInfected(targetId);
                }

                // 勝利条件を満たしたか確認する
                bool winFlag = true;
                foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                {
                    if (p.isDead()) continue;
                    if (p == player) continue;
                    if (!infected.ContainsKey(p.PlayerId))
                    {
                        winFlag = false;
                        break;
                    }
                }

                if (winFlag)
                {
                    MessageWriter winWriter = AmongUsClient.Instance.StartRpcImmediately(
                        PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlagueDoctorWin,
                        SendOption.Reliable);
                    AmongUsClient.Instance.FinishRpcImmediately(winWriter);
                    RPCProcedure.plagueDoctorWin();
                }
            }
        }

        UpdateStatusText();
    }

    private bool hasInfected()
    {
        bool flag = false;
        foreach (KeyValuePair<int, float> item in progress)
            if (item.Value != 0f)
            {
                flag = true;
                break;
            }

        return flag;
    }

    public void UpdateStatusText()
    {
        // ロード画面でstatusTextを生成すると上手く表示されないのでゲームが開始してから最初に感染させた時点から表示する
        if (!hasInfected()) return;
        if (MeetingHud.Instance != null)
        {
            if (statusText != null) statusText.gameObject.SetActive(false);
            return;
        }

        if ((player != null && PlayerControl.LocalPlayer == player) ||
            PlayerControl.LocalPlayer.isDead())
        {
            if (statusText == null)
            {
                GameObject gameObject =
                    Object.Instantiate(FastDestroyableSingleton<HudManager>.Instance?.roomTracker.gameObject);
                gameObject.transform.SetParent(FastDestroyableSingleton<HudManager>.Instance.transform);
                gameObject.SetActive(true);
                Object.DestroyImmediate(gameObject.GetComponent<RoomTracker>());
                statusText = gameObject.GetComponent<TMP_Text>();
                gameObject.transform.localPosition = new Vector3(-2.7f, -0.1f, gameObject.transform.localPosition.z);

                statusText.transform.localScale = new Vector3(1f, 1f, 1f);
                statusText.fontSize = 1.5f;
                statusText.fontSizeMin = 1.5f;
                statusText.fontSizeMax = 1.5f;
                statusText.alignment = TextAlignmentOptions.BottomLeft;
            }

            statusText.gameObject.SetActive(true);
            string text = $"[{ModTranslation.getString("plagueDoctorProgress")}]\n";
            foreach (PlayerControl p in PlayerControl.AllPlayerControls)
            {
                if (p == player) continue;
                if (dead.ContainsKey(p.PlayerId) && dead[p.PlayerId]) continue;
                text += $"{p.name}: ";
                if (infected.ContainsKey(p.PlayerId))
                    text += Helpers.cs(Color.red, ModTranslation.getString("plagueDoctorInfectedText"));
                else
                {
                    // データが無い場合は作成する
                    if (!progress.ContainsKey(p.PlayerId)) progress[p.PlayerId] = 0f;
                    text += getProgressString(progress[p.PlayerId]);
                }

                text += "\n";
            }

            statusText.text = text;
        }
    }

    public static void MakeButtons(HudManager hm)
    {
        plagueDoctorButton = new CustomButton(
            () =>
            {
                /*ボタンが押されたとき*/
                byte targetId = local.currentTarget.PlayerId;

                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                    PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlagueDoctorSetInfected,
                    SendOption.Reliable);
                writer.Write(targetId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.plagueDoctorInfected(targetId);
                local.numInfections--;

                plagueDoctorButton.Timer = plagueDoctorButton.MaxTimer;
                local.currentTarget = null;
            },
            () =>
            {
                /*ボタンが有効になる条件*/
                return PlayerControl.LocalPlayer.isRole(RoleType.PlagueDoctor) &&
                       local.numInfections > 0 && !PlayerControl.LocalPlayer.isDead();
            },
            () =>
            {
                /*ボタンが使える条件*/
                if (numInfectionsText != null)
                {
                    if (local.numInfections > 0)
                        numInfectionsText.text = string.Format(ModTranslation.getString("plagueDoctorInfectionsLeft"),
                            local.numInfections);
                    else
                        numInfectionsText.text = "";
                }

                return local.currentTarget != null && local.numInfections > 0 &&
                       PlayerControl.LocalPlayer.CanMove;
            },
            () =>
            {
                /*ミーティング終了時*/
                plagueDoctorButton.Timer = plagueDoctorButton.MaxTimer;
            },
            getSyringeIcon(),
            CustomButton.ButtonPositions.lowerRowRight,
            hm,
            hm.UseButton,
            KeyCode.F
        )
        {
            buttonText = ModTranslation.getString("plagueDoctorInfectButton")
        };

        numInfectionsText = GameObject.Instantiate(plagueDoctorButton.actionButton.cooldownTimerText,
            plagueDoctorButton.actionButton.cooldownTimerText.transform.parent);
        numInfectionsText.text = "";
        numInfectionsText.enableWordWrapping = false;
        numInfectionsText.transform.localScale = Vector3.one * 0.5f;
        numInfectionsText.transform.localPosition += new Vector3(-0.05f, 0.7f, 0);
    }

    public static Sprite getSyringeIcon()
    {
        if (plagueDoctorIcon) return plagueDoctorIcon;
        plagueDoctorIcon = TheOtherRolesPlugin.getResources("InfectButton.png");
        return plagueDoctorIcon;
    }

    public static void SetButtonCooldowns()
    {
        plagueDoctorButton.MaxTimer = infectCooldown;
    }

    public static void updateDead()
    {
        foreach (PlayerControl pc in PlayerControl.AllPlayerControls.GetFastEnumerator())
            dead[pc.PlayerId] = pc.isDead();
    }

    public static string getProgressString(float progress)
    {
        // Go from green -> yellow -> red based on infection progress
        Color color;
        float prog = progress / infectDuration;
        if (prog < 0.5f)
            color = Color.Lerp(Color.green, Color.yellow, prog * 2);
        else
            color = Color.Lerp(Color.yellow, Color.red, (prog * 2) - 1);

        float progPercent = prog * 100;
        return Helpers.cs(color, $"{progPercent:F1}%");
    }

    public static void Clear()
    {
        players = new List<PlagueDoctor>();
        triggerPlagueDoctorWin = false;
        infected = new Dictionary<int, PlayerControl>();
        progress = new Dictionary<int, float>();
        dead = new Dictionary<int, bool>();
    }
}
