using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using TheOtherRoles.Objects;
using TMPro;
using UnityEngine;

namespace TheOtherRoles;

[HarmonyPatch]
public class SoulPlayer
{
    public static Color color = Palette.CrewmateBlue;
    private static CustomButton senriganButton;
    public static bool toggle;
    public static Sprite senriganIcon;
    public static Dictionary<byte, float> killTimers;
    public static float updateInterval = 2f;
    public static float timer;
    public static TMP_Text statusText;
    private static bool enableSenrigan => CustomOptionHolder.enableSenrigan.getBool();

    public static void senrigan()
    {
        HudManager hm = FastDestroyableSingleton<HudManager>.Instance;
        if (toggle)
        {
            Camera.main.orthographicSize /= 6f;
            hm.UICamera.orthographicSize /= 6f;
            hm.transform.localScale /= 6f;
        }
        else
        {
            Camera.main.orthographicSize *= 6f;
            hm.UICamera.orthographicSize *= 6f;
            hm.transform.localScale *= 6f;
        }

        toggle = !toggle;
    }

    public static void FixedUpdate()
    {
        if (CustomOptionHolder.deadImpostorCanSeeKillColdown.getBool() &&
            PlayerControl.LocalPlayer.isImpostor() && PlayerControl.LocalPlayer.isAlive())
        {
            timer += Time.fixedDeltaTime;
            if (timer >= updateInterval)
            {
                timer = 0;
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                    PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncKillTimer, SendOption.Reliable);
                writer.Write(PlayerControl.LocalPlayer.PlayerId);
                writer.Write(PlayerControl.LocalPlayer.killTimer);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.syncKillTimer(PlayerControl.LocalPlayer.PlayerId,
                    PlayerControl.LocalPlayer.killTimer);
            }
        }

        UpdateStatusText();
    }

    public static void MakeButtons(HudManager hm)
    {
        senriganButton = new CustomButton(
            () =>
            {
                /*ボタンが押されたとき*/
                senrigan();
            },
            () =>
            {
                /*ボタンが有効になる条件*/
                return enableSenrigan && PlayerControl.LocalPlayer.isDead() &&
                       !PlayerControl.LocalPlayer.isRole(RoleType.Puppeteer);
            },
            () =>
            {
                /*ボタンが使える条件*/
                return PlayerControl.LocalPlayer.isDead();
            },
            () =>
            {
                /*ミーティング終了時*/
            },
            getSenriganIcon(),
            CustomButton.ButtonPositions.upperRowFarLeft,
            hm,
            hm.AbilityButton,
            KeyCode.G
        )
        {
            MaxTimer = 0f,
            Timer = 0f,
            buttonText = ModTranslation.getString("")
        };
    }

    public static Sprite getSenriganIcon()
    {
        if (senriganIcon) return senriganIcon;
        senriganIcon = TheOtherRolesPlugin.getResources("Senrigan.png");
        return senriganIcon;
    }

    public static void SetButtonCooldowns()
    {
        senriganButton.Timer = senriganButton.MaxTimer = 0f;
    }


    public static void Clear()
    {
        toggle = false;
        if (statusText) GameObject.Destroy(statusText);
        statusText = null;
        killTimers = new Dictionary<byte, float>();
    }

    public static void UpdateStatusText()
    {
        if (CustomOptionHolder.deadImpostorCanSeeKillColdown.getBool())
        {
            if (MeetingHud.Instance != null)
            {
                if (statusText != null) statusText.gameObject.SetActive(false);
                return;
            }

            if (PlayerControl.LocalPlayer.isDead() && PlayerControl.LocalPlayer.isImpostor())
            {
                if (statusText == null)
                {
                    GameObject gameObject =
                        Object.Instantiate(FastDestroyableSingleton<HudManager>.Instance?.roomTracker.gameObject);
                    gameObject.transform.SetParent(FastDestroyableSingleton<HudManager>.Instance.transform);
                    gameObject.SetActive(true);
                    Object.DestroyImmediate(gameObject.GetComponent<RoomTracker>());
                    statusText = gameObject.GetComponent<TMP_Text>();
                    gameObject.transform.localPosition =
                        new Vector3(-2.7f, -0.1f, gameObject.transform.localPosition.z);

                    statusText.transform.localScale = new Vector3(1f, 1f, 1f);
                    statusText.fontSize = 1.5f;
                    statusText.fontSizeMin = 1.5f;
                    statusText.fontSizeMax = 1.5f;
                    statusText.alignment = TextAlignmentOptions.BottomLeft;
                }

                statusText.gameObject.SetActive(true);
                string text = "KillTimer\n";
                killTimers.Keys.ToList().ForEach(key =>
                {
                    if (key == PlayerControl.LocalPlayer.PlayerId) return;
                    PlayerControl p = Helpers.playerById(key);
                    if (p.isDead()) return;
                    if (killTimers[key] > 0)
                        killTimers[key] -= Time.fixedDeltaTime;
                    else
                        killTimers[key] = 0;
                    text += $"{p.name}: {killTimers[key]:F2}s";
                    text += "\n";
                });
                statusText.text = text;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.StartMeeting))]
    private class StartMeetingPatch
    {
        public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] NetworkedPlayerInfo meetingTarget)
        {
            if (PlayerControl.LocalPlayer.Data.IsDead)
                if (toggle)
                    senrigan();
        }
    }

    [HarmonyPatch(typeof(Minigame), nameof(Minigame.Begin))]
    private class MinigameBeginPatch
    {
        private static void Prefix(Minigame __instance)
        {
            if (PlayerControl.LocalPlayer.isDead())
                if (toggle)
                    senrigan();
        }
    }
}
