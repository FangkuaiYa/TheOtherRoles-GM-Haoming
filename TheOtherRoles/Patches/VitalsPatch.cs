using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using TMPro;
using UnityEngine;
using static TheOtherRoles.GameHistory;
using static TheOtherRoles.TheOtherRoles;
using Object = UnityEngine.Object;

namespace TheOtherRoles.Patches;

[Harmony]
public class VitalsPatch
{
    private static float vitalsTimer;
    private static TextMeshPro TimeRemaining;
    private static List<TextMeshPro> hackerTexts = new();

    public static void ResetData()
    {
        vitalsTimer = 0f;
        if (TimeRemaining != null)
        {
            Object.Destroy(TimeRemaining);
            TimeRemaining = null;
        }
    }

    private static void UseVitalsTime()
    {
        // Don't waste network traffic if we're out of time.
        if (TORMapOptions.restrictDevices > 0 && TORMapOptions.restrictVitals && TORMapOptions.restrictVitalsTime > 0f &&
            PlayerControl.LocalPlayer.isAlive())
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.UseVitalsTime, SendOption.Reliable, -1);
            writer.Write(vitalsTimer);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            RPCProcedure.useVitalsTime(vitalsTimer);
        }

        vitalsTimer = 0f;
    }

    [HarmonyPatch(typeof(VitalsMinigame), nameof(VitalsMinigame.Begin))]
    private class VitalsMinigameStartPatch
    {
        private static void Postfix(VitalsMinigame __instance)
        {
            vitalsTimer = 0f;

            if (Hacker.hacker != null && PlayerControl.LocalPlayer == Hacker.hacker)
            {
                hackerTexts = new List<TextMeshPro>();
                foreach (VitalsPanel panel in __instance.vitals)
                {
                    TextMeshPro text = Object.Instantiate(__instance.SabText, panel.transform);
                    hackerTexts.Add(text);
                    Object.DestroyImmediate(text.GetComponent<AlphaBlink>());
                    text.gameObject.SetActive(false);
                    text.transform.localScale = Vector3.one * 0.75f;
                    text.transform.localPosition = new Vector3(-0.75f, -0.23f, 0f);
                }
            }
        }
    }

    [HarmonyPatch(typeof(VitalsMinigame), nameof(VitalsMinigame.Update))]
    private class VitalsMinigameUpdatePatch
    {
        private static bool Prefix(VitalsMinigame __instance)
        {
            vitalsTimer += Time.deltaTime;
            if (vitalsTimer > 0.05f)
                UseVitalsTime();

            if (TORMapOptions.restrictDevices > 0 && TORMapOptions.restrictVitals)
            {
                if (TimeRemaining == null)
                {
                    TimeRemaining = Object.Instantiate(FastDestroyableSingleton<HudManager>.Instance.TaskPanel.taskText,
                        __instance.transform);
                    TimeRemaining.alignment = TextAlignmentOptions.BottomRight;
                    TimeRemaining.transform.position = Vector3.zero;
                    TimeRemaining.transform.localPosition = new Vector3(1.7f, 4.45f);
                    TimeRemaining.transform.localScale *= 1.8f;
                    TimeRemaining.color = Palette.White;
                }

                if (TORMapOptions.restrictVitalsTime <= 0f)
                {
                    __instance.Close();
                    return false;
                }

                string timeString = TimeSpan.FromSeconds(TORMapOptions.restrictVitalsTime).ToString(@"mm\:ss\.ff");
                TimeRemaining.text = string.Format(ModTranslation.getString("timeRemaining"), timeString);
                TimeRemaining.gameObject.SetActive(true);
            }

            return true;
        }

        private static void Postfix(VitalsMinigame __instance)
        {
            // Hacker show time since death
            if (Hacker.hacker != null && Hacker.hacker == PlayerControl.LocalPlayer &&
                Hacker.hackerTimer > 0)
                for (int k = 0; k < __instance.vitals.Length; k++)
                {
                    VitalsPanel vitalsPanel = __instance.vitals[k];
                    NetworkedPlayerInfo player = GameData.Instance.AllPlayers[k];

                    // Hacker update
                    if (vitalsPanel.IsDead)
                    {
                        DeadPlayer deadPlayer = deadPlayers?.Where(x => x.player?.PlayerId == player?.PlayerId)
                            ?.FirstOrDefault();
                        if (deadPlayer != null && k < hackerTexts.Count && hackerTexts[k] != null)
                        {
                            float timeSinceDeath = (float)(DateTime.UtcNow - deadPlayer.timeOfDeath).TotalMilliseconds;
                            hackerTexts[k].gameObject.SetActive(true);
                            hackerTexts[k].text = Math.Round(timeSinceDeath / 1000) + "s";
                        }
                    }
                }
            else
                foreach (TextMeshPro text in hackerTexts)
                    if (text != null && text.gameObject != null)
                        text.gameObject.SetActive(false);
        }
    }

    // [HarmonyPatch]
    // class VitalsMinigameClosePatch
    // {
    //     private static IEnumerable<MethodBase> TargetMethods()
    //     {
    //         return typeof(Minigame).GetMethods().Where(x => x.Name == "Close");
    //     }

    //     static void Prefix(Minigame __instance)
    //     {
    //         if (__instance is VitalsMinigame)
    //             UseVitalsTime();
    //     }
    // }
}
