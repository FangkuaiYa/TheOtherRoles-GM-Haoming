using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Assets.CoreScripts;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using static TheOtherRoles.GameHistory;
using static TheOtherRoles.TORMapOptions;
using static TheOtherRoles.TheOtherRoles;
using static TheOtherRoles.TheOtherRolesGM;
using Object = UnityEngine.Object;
using Random = System.Random;
using UnityEngine.UIElements.UIR;

namespace TheOtherRoles.Patches;

[HarmonyPatch]
internal class MeetingHudPatch
{
    private const float scale = 0.65f;
    private static bool[] selections;
    private static SpriteRenderer[] renderers;
    private static Sprite blankNameplate;
    public static bool nameplatesChanged = true;
    public static bool animateSwap;

    private static TextMeshPro meetingInfoText;

    private static GameObject guesserUI;

    public static void updateNameplate(PlayerVoteArea pva, byte playerId = byte.MaxValue)
    {
        blankNameplate ??=  ShipStatus.Instance.CosmeticsCache.GetNameplate("nameplate_NoPlate").Image;

        Sprite nameplate = blankNameplate;
        if (!hideNameplates)
        {
            PlayerControl p = Helpers.playerById(playerId != byte.MaxValue ? playerId : pva.TargetPlayerId);
            string nameplateId = p?.CurrentOutfit?.NamePlateId;
            nameplate = ShipStatus.Instance.CosmeticsCache.GetNameplate(nameplateId).Image;
        }

        pva.Background.sprite = nameplate;
    }

    private static void gmKillOnClick(int i, MeetingHud __instance)
    {
        if (__instance.state == MeetingHud.VoteStates.Results) return;
        SpriteRenderer renderer = renderers[i];
        PlayerVoteArea target = __instance.playerStates[i];

        if (target != null)
        {
            if (target.AmDead)
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                    PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.GMRevive, SendOption.Reliable, -1);
                writer.Write(target.TargetPlayerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.GMRevive(target.TargetPlayerId);

                renderer.sprite = Guesser.getTargetSprite();
                renderer.color = Color.red;
            }
            else
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                    PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.GMKill, SendOption.Reliable, -1);
                writer.Write(target.TargetPlayerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.GMKill(target.TargetPlayerId);

                renderer.sprite = Swapper.getCheckSprite();
                renderer.color = Color.green;
            }
        }
    }

    private static void swapperOnClick(int i, MeetingHud __instance)
    {
        if (Swapper.numSwaps <= 0) return;
        if (__instance.state == MeetingHud.VoteStates.Results) return;
        if (__instance.playerStates[i].AmDead) return;

        int selectedCount = selections.Where(b => b).Count();
        SpriteRenderer renderer = renderers[i];

        if (selectedCount == 0)
        {
            renderer.color = Color.green;
            selections[i] = true;
        }
        else if (selectedCount == 1)
        {
            if (selections[i])
            {
                renderer.color = Color.red;
                selections[i] = false;
            }
            else
            {
                selections[i] = true;
                renderer.color = Color.green;

                PlayerVoteArea firstPlayer = null;
                PlayerVoteArea secondPlayer = null;
                for (int A = 0; A < selections.Length; A++)
                    if (selections[A])
                    {
                        if (firstPlayer != null)
                        {
                            secondPlayer = __instance.playerStates[A];
                            break;
                        }

                        firstPlayer = __instance.playerStates[A];
                    }

                if (firstPlayer != null && secondPlayer != null)
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                        PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SwapperSwap, SendOption.Reliable, -1);
                    writer.Write(firstPlayer.TargetPlayerId);
                    writer.Write(secondPlayer.TargetPlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);

                    RPCProcedure.swapperSwap(firstPlayer.TargetPlayerId, secondPlayer.TargetPlayerId);
                }
            }
        }
    }

    public const int MaxOneScreenRole = 40;
    private static List<Transform> RoleButtons;
    private static List<SpriteRenderer> PageButtons;
    public static int Page;
    static void guesserSelectRole(bool SetPage = true)
    {
        if (SetPage) Page = 1;
        foreach (var RoleButton in RoleButtons)
        {
            int index = 0;
            foreach (var RoleBtn in RoleButtons)
            {
                if (RoleBtn == null) continue;
                index++;
                if (index <= (Page - 1) * MaxOneScreenRole) { RoleBtn.gameObject.SetActive(false); continue; }
                if ((Page * MaxOneScreenRole) < index) { RoleBtn.gameObject.SetActive(false); continue; }
                RoleBtn.gameObject.SetActive(true);
            }
        }
    }

    private static void guesserOnClick(int buttonTarget, MeetingHud __instance)
    {
        if (guesserUI != null || !(__instance.state == MeetingHud.VoteStates.Voted || __instance.state == MeetingHud.VoteStates.NotVoted)) return;
        __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(false));

        Page = 1;
        RoleButtons = new();
        PageButtons = new();

        Transform PhoneUI = UnityEngine.Object.FindObjectsOfType<Transform>().FirstOrDefault(x => x.name == "PhoneUI");
        Transform container = UnityEngine.Object.Instantiate(PhoneUI, __instance.transform);
        container.transform.localPosition = new Vector3(0, 0, -5f);
        guesserUI = container.gameObject;

        int i = 0;
        Transform buttonTemplate = __instance.playerStates[0].transform.FindChild("votePlayerBase");
        Transform maskTemplate = __instance.playerStates[0].transform.FindChild("MaskArea");
        Transform smallButtonTemplate = __instance.playerStates[0].Buttons.transform.Find("CancelButton");
        TextMeshPro textTemplate = __instance.playerStates[0].NameText;

        Transform exitButtonParent = new GameObject().transform;
        exitButtonParent.SetParent(container);
        Transform exitButton = Object.Instantiate(buttonTemplate.transform, exitButtonParent);
        Transform exitButtonMask = Object.Instantiate(maskTemplate, exitButtonParent);
        exitButton.gameObject.GetComponent<SpriteRenderer>().sprite =
            smallButtonTemplate.GetComponent<SpriteRenderer>().sprite;
        exitButtonParent.transform.localPosition = new Vector3(2.725f, 2.1f, -200f);
        exitButtonParent.transform.localScale = new Vector3(0.25f, 0.9f, 1f);
        exitButtonParent.transform.SetAsFirstSibling();
        exitButton.GetComponent<PassiveButton>().OnClick.RemoveAllListeners();
        exitButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
        {
            __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
            Object.Destroy(container.gameObject);
        }));

        static void ReloadPage()
        {
            PageButtons[0].gameObject.SetActive(true);
            PageButtons[1].gameObject.SetActive(true);
            if (((RoleButtons.Count / MaxOneScreenRole) +
                (RoleButtons.Count % MaxOneScreenRole != 0 ? 1 : 0)) < Page)
            {
                Page -= 1;
                PageButtons[1].gameObject.SetActive(false);
            }
            else if (((RoleButtons.Count / MaxOneScreenRole) +
                (RoleButtons.Count % MaxOneScreenRole != 0 ? 1 : 0)) < Page + 1)
            {
                PageButtons[1].gameObject.SetActive(false);
            }
            if (Page <= 1)
            {
                Page = 1;
                PageButtons[0].gameObject.SetActive(false);
            }
            guesserSelectRole(false);
        }
        void CreatePage(bool IsNext, MeetingHud __instance, Transform container)
        {
            var buttonTemplate = __instance.playerStates[0].transform.FindChild("votePlayerBase");
            var maskTemplate = __instance.playerStates[0].transform.FindChild("MaskArea");
            var smallButtonTemplate = __instance.playerStates[0].Buttons.transform.Find("CancelButton");
            var textTemplate = __instance.playerStates[0].NameText;
            Transform PagebuttonParent = new GameObject().transform;
            PagebuttonParent.SetParent(container);
            Transform Pagebutton = UnityEngine.Object.Instantiate(buttonTemplate, PagebuttonParent);
            Pagebutton.FindChild("ControllerHighlight").gameObject.SetActive(false);
            Transform PagebuttonMask = UnityEngine.Object.Instantiate(maskTemplate, PagebuttonParent);
            TextMeshPro Pagelabel = UnityEngine.Object.Instantiate(textTemplate, Pagebutton);
            Pagebutton.GetComponent<SpriteRenderer>().sprite = ShipStatus.Instance.CosmeticsCache.GetNameplate("nameplate_NoPlate").Image;
            PagebuttonParent.localPosition = IsNext ? new(3.535f, -2.2f, -200) : new(-3.475f, -2.2f, -200);
            PagebuttonParent.localScale = new(0.55f, 0.55f, 1f);
            Pagelabel.color = Color.white;
            Pagelabel.text = ModTranslation.getString(IsNext ? "next" : "previous");
            Pagelabel.alignment = TextAlignmentOptions.Center;
            Pagelabel.transform.localPosition = new Vector3(0, 0, Pagelabel.transform.localPosition.z);
            Pagelabel.transform.localScale *= 1.6f;
            Pagelabel.autoSizeTextContainer = true;
            Pagebutton.GetComponent<PassiveButton>().OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                if (IsNext) Page += 1;
                else Page -= 1;
                ReloadPage();
            }));
            PageButtons.Add(Pagebutton.GetComponent<SpriteRenderer>());
        }
        CreatePage(false, __instance, container);
        CreatePage(true, __instance, container);

        Transform selectedButton = null;

        foreach (RoleInfo roleInfo in RoleInfo.allRoleInfos)
        {
            RoleType guesserRole;
            if (PlayerControl.LocalPlayer.isRole(RoleType.NiceGuesser))
                guesserRole = RoleType.NiceGuesser;
            else if (PlayerControl.LocalPlayer.isRole(RoleType.EvilGuesser) ||
                     PlayerControl.LocalPlayer.hasModifier(ModifierType.LastImpostor))
                guesserRole = RoleType.EvilGuesser;
            else
                guesserRole = RoleType.NiceGuesser;

            if (roleInfo == null ||
                roleInfo.roleType == RoleType.Lovers ||
                roleInfo.roleType == guesserRole ||
                (!Guesser.evilGuesserCanGuessSpy && guesserRole == RoleType.EvilGuesser &&
                 roleInfo.roleType == RoleType.Spy) ||
                roleInfo == RoleInfo.gm ||
                (Guesser.onlyAvailableRoles && !roleInfo.enabled) ||
                roleInfo == RoleInfo.bomberB)
                continue; // Not guessable roles
            if (Guesser.guesserCantGuessSnitch && Snitch.snitch != null)
            {
                (int playerCompleted, int playerTotal) = TasksHandler.taskInfo(Snitch.snitch.Data);
                int numberOfLeftTasks = playerTotal - playerCompleted;
                if (numberOfLeftTasks <= 0 && roleInfo.roleType == RoleType.Snitch) continue;
            }
            CreateRole(roleInfo);
        }

        void CreateRole(RoleInfo roleInfo)
        {
            if (i >= MaxOneScreenRole) i = 0;

            Transform buttonParent = new GameObject().transform;
            buttonParent.SetParent(container);
            Transform button = Object.Instantiate(buttonTemplate, buttonParent);
            Transform buttonMask = Object.Instantiate(maskTemplate, buttonParent);
            TextMeshPro label = Object.Instantiate(textTemplate, button);
            button.GetComponent<SpriteRenderer>().sprite =  ShipStatus.Instance.CosmeticsCache.GetNameplate("nameplate_NoPlate").Image;
            RoleButtons.Add(button);
            int row = i / 5, col = i % 5;
            buttonParent.localPosition = new Vector3(-3.47f + 1.75f * col, 1.5f - 0.45f * row, -5);
            buttonParent.localScale = new Vector3(0.55f, 0.55f, 1f);
            label.text = Helpers.cs(roleInfo.color, roleInfo.name);
            label.alignment = TextAlignmentOptions.Center;
            label.transform.localPosition = new Vector3(0, 0, label.transform.localPosition.z);
            label.transform.localScale *= 1.6f;
            label.autoSizeTextContainer = true;
            int copiedIndex = i;

            button.GetComponent<PassiveButton>().OnClick.RemoveAllListeners();
            if (PlayerControl.LocalPlayer.isAlive())
                button.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
                {
                    if (selectedButton != button)
                    {
                        selectedButton = button;
                        RoleButtons.ForEach(x =>
                            x.GetComponent<SpriteRenderer>().color = x == selectedButton ? Color.red : Color.white);
                    }
                    else
                    {
                        PlayerControl focusedTarget =
                            Helpers.playerById(__instance.playerStates[buttonTarget].TargetPlayerId);
                        if (!(__instance.state == MeetingHud.VoteStates.Voted ||
                              __instance.state == MeetingHud.VoteStates.NotVoted) || focusedTarget == null) return;
                        if (Guesser.remainingShots(PlayerControl.LocalPlayer) <= 0) return;

                        if (!Guesser.killsThroughShield && focusedTarget == Medic.shielded)
                        {
                            // Depending on the options, shooting the shielded player will not allow the guess, notifiy everyone about the kill attempt and close the window
                            __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
                            Object.Destroy(container.gameObject);

                            MessageWriter murderAttemptWriter =
                                AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId,
                                    (byte)CustomRPC.ShieldedMurderAttempt, SendOption.Reliable, -1);
                            AmongUsClient.Instance.FinishRpcImmediately(murderAttemptWriter);
                            RPCProcedure.shieldedMurderAttempt();
                            return;
                        }

                        RoleInfo mainRoleInfo = RoleInfo.getRoleInfoForPlayer(focusedTarget).FirstOrDefault();
                        if (mainRoleInfo == null) return;

                        // BomberAとBomberBを同等に扱う
                        PlayerControl dyingTarget;
                        if (mainRoleInfo == roleInfo)
                            dyingTarget = focusedTarget;
                        else if (roleInfo == RoleInfo.bomberA && mainRoleInfo == RoleInfo.bomberB)
                            dyingTarget = focusedTarget;
                        else
                            dyingTarget = PlayerControl.LocalPlayer;


                        // Reset the GUI
                        __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
                        Object.Destroy(container.gameObject);
                        if (Guesser.hasMultipleShotsPerMeeting &&
                            Guesser.remainingShots(PlayerControl.LocalPlayer) > 1 &&
                            dyingTarget != PlayerControl.LocalPlayer)
                            __instance.playerStates.ToList().ForEach(x =>
                            {
                                if (x.TargetPlayerId == dyingTarget.PlayerId &&
                                    x.transform.FindChild("ShootButton") != null)
                                    Object.Destroy(x.transform.FindChild("ShootButton").gameObject);
                            });
                        else
                            __instance.playerStates.ToList().ForEach(x =>
                            {
                                if (x.transform.FindChild("ShootButton") != null)
                                    Object.Destroy(x.transform.FindChild("ShootButton").gameObject);
                            });

                        // Shoot player and send chat info if activated
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                            PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.GuesserShoot,
                            SendOption.Reliable, -1);
                        writer.Write(PlayerControl.LocalPlayer.PlayerId);
                        writer.Write(dyingTarget.PlayerId);
                        writer.Write(focusedTarget.PlayerId);
                        writer.Write((byte)roleInfo.roleType);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                        RPCProcedure.guesserShoot(PlayerControl.LocalPlayer.PlayerId, dyingTarget.PlayerId,
                            focusedTarget.PlayerId, (byte)roleInfo.roleType);
                    }
                }));

            i++;
        }
        guesserSelectRole();
        ReloadPage();
        container.transform.localScale *= 0.75f;
    }


    private static void populateButtonsPostfix(MeetingHud __instance)
    {
        nameplatesChanged = true;

        if (PlayerControl.LocalPlayer.isRole(RoleType.GM) && GM.canKill)
        {
            renderers = new SpriteRenderer[__instance.playerStates.Length];

            for (int i = 0; i < __instance.playerStates.Length; i++)
            {
                PlayerVoteArea playerVoteArea = __instance.playerStates[i];

                GameObject template = playerVoteArea.Buttons.transform.Find("CancelButton").gameObject;
                GameObject checkbox = Object.Instantiate(template);
                checkbox.transform.SetParent(playerVoteArea.transform);
                checkbox.transform.position = template.transform.position;
                checkbox.transform.localPosition = new Vector3(-0.95f, 0.03f, -20f);
                SpriteRenderer renderer = checkbox.GetComponent<SpriteRenderer>();
                renderer.sprite = playerVoteArea.AmDead ? Swapper.getCheckSprite() : Guesser.getTargetSprite();
                renderer.color = playerVoteArea.AmDead ? Color.green : Color.red;

                PassiveButton button = checkbox.GetComponent<PassiveButton>();
                button.OnClick.RemoveAllListeners();
                int copiedIndex = i;
                button.OnClick.AddListener((UnityAction)(() => gmKillOnClick(copiedIndex, __instance)));

                renderers[i] = renderer;
            }
        }

        // Add Swapper Buttons
        if (PlayerControl.LocalPlayer.isRole(RoleType.Swapper) && Swapper.numSwaps > 0 &&
            !Swapper.swapper.Data.IsDead)
        {
            selections = new bool[__instance.playerStates.Length];
            renderers = new SpriteRenderer[__instance.playerStates.Length];

            for (int i = 0; i < __instance.playerStates.Length; i++)
            {
                PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                if (playerVoteArea.AmDead ||
                    (playerVoteArea.TargetPlayerId == Swapper.swapper.PlayerId && Swapper.canOnlySwapOthers) ||
                    playerVoteArea.TargetPlayerId == GM.gm?.PlayerId) continue;

                GameObject template = playerVoteArea.Buttons.transform.Find("CancelButton").gameObject;
                GameObject checkbox = Object.Instantiate(template);
                checkbox.transform.SetParent(playerVoteArea.transform);
                checkbox.transform.position = template.transform.position;
                checkbox.transform.localPosition = new Vector3(-0.95f, 0.03f, -20f);
                SpriteRenderer renderer = checkbox.GetComponent<SpriteRenderer>();
                renderer.sprite = Swapper.getCheckSprite();
                renderer.color = Color.red;

                PassiveButton button = checkbox.GetComponent<PassiveButton>();
                button.OnClick.RemoveAllListeners();
                int copiedIndex = i;
                button.OnClick.AddListener((Action)(() => swapperOnClick(copiedIndex, __instance)));

                selections[i] = false;
                renderers[i] = renderer;
            }
        }

        // Add overlay for spelled players
        if (Witch.witch != null && Witch.futureSpelled != null)
            foreach (PlayerVoteArea pva in __instance.playerStates)
                if (Witch.futureSpelled.Any(x => x.PlayerId == pva.TargetPlayerId))
                {
                    SpriteRenderer rend = new GameObject().AddComponent<SpriteRenderer>();
                    rend.transform.SetParent(pva.transform);
                    rend.gameObject.layer = pva.Megaphone.gameObject.layer;
                    rend.transform.localPosition = new Vector3(-0.5f, -0.03f, -1f);
                    rend.sprite = Witch.getSpelledOverlaySprite();
                }

        // トラックボタン
        bool isTrackerButton = EvilTracker.canSetTargetOnMeeting && EvilTracker.target == null &&
                               PlayerControl.LocalPlayer.isRole(RoleType.EvilTracker) &&
                               PlayerControl.LocalPlayer.isAlive();
        if (isTrackerButton)
            for (int i = 0; i < __instance.playerStates.Length; i++)
            {
                PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                if (playerVoteArea.AmDead ||
                    playerVoteArea.TargetPlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                GameObject template = playerVoteArea.Buttons.transform.Find("CancelButton").gameObject;
                GameObject targetBox = Object.Instantiate(template, playerVoteArea.transform);
                targetBox.name = "EvilTrackerButton";
                targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1.3f);
                SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
                renderer.sprite = EvilTracker.getArrowSprite();
                renderer.color = Palette.CrewmateBlue;
                PassiveButton button = targetBox.GetComponent<PassiveButton>();
                button.OnClick.RemoveAllListeners();
                int copiedIndex = i;
                button.OnClick.AddListener((Action)(() =>
                {
                    PlayerControl focusedTarget =
                        Helpers.playerById(__instance.playerStates[copiedIndex].TargetPlayerId);
                    EvilTracker.target = focusedTarget;
                    // Reset the GUI
                    __instance.playerStates.ToList().ForEach(x =>
                    {
                        if (x.transform.FindChild("EvilTrackerButton") != null)
                            Object.Destroy(x.transform.FindChild("EvilTrackerButton").gameObject);
                    });
                    GameObject targetMark = Object.Instantiate(template, playerVoteArea.transform);
                    targetMark.name = "EvilTrackerMark";
                    PassiveButton button = targetMark.GetComponent<PassiveButton>();
                    targetMark.transform.localPosition = new Vector3(1.1f, 0.03f, -20f);
                    GameObject.Destroy(button);
                    SpriteRenderer renderer = targetMark.GetComponent<SpriteRenderer>();
                    renderer.sprite = EvilTracker.getArrowSprite();
                    renderer.color = Palette.CrewmateBlue;

                    bool isGuesserButton = Guesser.isGuesser(PlayerControl.LocalPlayer.PlayerId) &&
                                           PlayerControl.LocalPlayer.isAlive() &&
                                           Guesser.remainingShots(PlayerControl.LocalPlayer) > 0;
                    bool isLastImpostorButton =
                        PlayerControl.LocalPlayer.hasModifier(ModifierType.LastImpostor) &&
                        PlayerControl.LocalPlayer.isAlive() && LastImpostor.canGuess();
                    if (isGuesserButton || isLastImpostorButton) createGuesserButton(__instance);
                }));
            }

        // Add Guesser Buttons
        bool isGuesserButton = !isTrackerButton && Guesser.isGuesser(PlayerControl.LocalPlayer.PlayerId) &&
                               PlayerControl.LocalPlayer.isAlive() &&
                               Guesser.remainingShots(PlayerControl.LocalPlayer) > 0;
        bool isLastImpostorButton = !isTrackerButton &&
                                    PlayerControl.LocalPlayer.hasModifier(ModifierType.LastImpostor) &&
                                    PlayerControl.LocalPlayer.isAlive() && LastImpostor.canGuess();
        if (isGuesserButton || isLastImpostorButton) createGuesserButton(__instance);
    }

    public static void createGuesserButton(MeetingHud __instance)
    {
        for (int i = 0; i < __instance.playerStates.Length; i++)
        {
            PlayerVoteArea playerVoteArea = __instance.playerStates[i];
            if (playerVoteArea.AmDead ||
                playerVoteArea.TargetPlayerId == PlayerControl.LocalPlayer.PlayerId ||
                playerVoteArea.TargetPlayerId == GM.gm?.PlayerId) continue;

            GameObject template = playerVoteArea.Buttons.transform.Find("CancelButton").gameObject;
            GameObject targetBox = Object.Instantiate(template, playerVoteArea.transform);
            targetBox.name = "ShootButton";
            targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1.3f);
            SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
            renderer.sprite = Guesser.getTargetSprite();
            PassiveButton button = targetBox.GetComponent<PassiveButton>();
            button.OnClick.RemoveAllListeners();
            int copiedIndex = i;
            button.OnClick.AddListener((Action)(() => guesserOnClick(copiedIndex, __instance)));
        }
    }

    public static void updateMeetingText(MeetingHud __instance)
    {
        // Uses remaining text for guesser/swapper
        if (meetingInfoText == null)
        {
            meetingInfoText = Object.Instantiate(FastDestroyableSingleton<HudManager>.Instance.TaskPanel.taskText,
                __instance.transform);
            meetingInfoText.alignment = TextAlignmentOptions.BottomLeft;
            meetingInfoText.transform.position = Vector3.zero;
            meetingInfoText.transform.localPosition = new Vector3(-3.07f, 3.33f, -20f);
            meetingInfoText.transform.localScale *= 1.1f;
            meetingInfoText.color = Palette.White;
            meetingInfoText.gameObject.SetActive(false);
        }

        meetingInfoText.text = "";
        meetingInfoText.gameObject.SetActive(false);

        if (MeetingHud.Instance.state is not MeetingHud.VoteStates.Voted and
            not MeetingHud.VoteStates.NotVoted and
            not MeetingHud.VoteStates.Discussion)
            return;

        if (PlayerControl.LocalPlayer.isRole(RoleType.Swapper) && Swapper.numSwaps > 0 &&
            !Swapper.swapper.Data.IsDead)
        {
            meetingInfoText.text = string.Format(ModTranslation.getString("swapperSwapsLeft"), Swapper.numSwaps);
            meetingInfoText.gameObject.SetActive(true);
        }

        int numGuesses = Guesser.remainingShots(PlayerControl.LocalPlayer);
        if ((Guesser.isGuesser(PlayerControl.LocalPlayer.PlayerId) ||
             PlayerControl.LocalPlayer.hasModifier(ModifierType.LastImpostor)) &&
            PlayerControl.LocalPlayer.isAlive() && numGuesses > 0)
        {
            meetingInfoText.text = string.Format(ModTranslation.getString("guesserGuessesLeft"), numGuesses);
            meetingInfoText.gameObject.SetActive(true);
        }

        if (PlayerControl.LocalPlayer.isRole(RoleType.Shifter) && Shifter.futureShift != null)
        {
            meetingInfoText.text = string.Format(ModTranslation.getString("shifterTargetInfo"),
                Shifter.futureShift.Data.PlayerName);
            meetingInfoText.gameObject.SetActive(true);
        }
    }

    public static void startMeeting()
    {
        animateSwap = false;
        CustomOverlays.hideBlackBG();
        CustomOverlays.hideInfoOverlay();
        OnMeetingStart();
        MapBehaviorPatch.shareRealTasks();
    }

    public static void populateButtons(MeetingHud __instance, byte reporter)
    {
        // 投票画面に人形遣いのダミーを表示させない
        // 会議に参加しないPlayerControlを持つRoleが増えたらこのListに追加
        // 特殊なplayerInfo.Role.Roleを設定することで自動的に無視できないか？もしくはフラグをplayerInfoのどこかに追加
        List<PlayerControl> playerControlesToBeIgnored = new() { Puppeteer.dummy };
        playerControlesToBeIgnored.RemoveAll(x => x == null);
        IEnumerable<byte> playerIdsToBeIgnored = playerControlesToBeIgnored.Select(x => x.PlayerId);
        // Generate PlayerVoteAreas
        __instance.playerStates = new PlayerVoteArea[GameData.Instance.PlayerCount - playerIdsToBeIgnored.Count()];
        int playerStatesCounter = 0;
        for (int i = 0; i < __instance.playerStates.Length + playerIdsToBeIgnored.Count(); i++)
        {
            if (playerIdsToBeIgnored.Contains(GameData.Instance.AllPlayers[i].PlayerId)) continue;
            NetworkedPlayerInfo playerInfo = GameData.Instance.AllPlayers[i];
            PlayerVoteArea playerVoteArea =
                __instance.playerStates[playerStatesCounter] = __instance.CreateButton(playerInfo);
            playerVoteArea.Parent = __instance;
            playerVoteArea.SetTargetPlayerId(playerInfo.PlayerId);
            playerVoteArea.SetDead(reporter == playerInfo.PlayerId, playerInfo.Disconnected || playerInfo.IsDead,
                playerInfo.Role.Role == RoleTypes.GuardianAngel);
            playerVoteArea.UpdateOverlay();
            playerStatesCounter++;
        }

        foreach (PlayerVoteArea playerVoteArea2 in __instance.playerStates)
            ControllerManager.Instance.AddSelectableUiElement(playerVoteArea2.PlayerButton);
        __instance.SortButtons();
    }

    [HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetCosmetics))]
    private class PlayerVoteAreaCosmetics
    {
        private static void Postfix(PlayerVoteArea __instance, NetworkedPlayerInfo playerInfo)
        {
            updateNameplate(__instance, playerInfo.PlayerId);
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    private class MeetingHudUpdatePatch
    {
        private static void Postfix(MeetingHud __instance)
        {
            if (nameplatesChanged)
            {
                foreach (PlayerVoteArea pva in __instance.playerStates) updateNameplate(pva);
                nameplatesChanged = false;
            }

            if (__instance.state == MeetingHud.VoteStates.Animating)
                return;

            // Deactivate skip Button if skipping on emergency meetings is disabled
            if (blockSkippingInEmergencyMeetings)
                __instance.SkipVoteButton?.gameObject?.SetActive(false);

            updateMeetingText(__instance);

            // This fixes a bug with the original game where pressing the button and a kill happens simultaneously
            // results in bodies sometimes being created *after* the meeting starts, marking them as dead and
            // removing the corpses so there's no random corpses leftover afterwards
            foreach (DeadBody b in Object.FindObjectsOfType<DeadBody>())
            {
                if (b == null) continue;

                foreach (PlayerVoteArea pva in __instance.playerStates)
                {
                    if (pva == null) continue;

                    if (pva.TargetPlayerId == b?.ParentId && !pva.AmDead)
                    {
                        pva?.SetDead(pva.DidReport, true);
                        pva?.Overlay?.gameObject?.SetActive(true);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
    private class MeetingCalculateVotesPatch
    {
        private static Dictionary<byte, int> CalculateVotes(MeetingHud __instance)
        {
            Dictionary<byte, int> dictionary = new();
            for (int i = 0; i < __instance.playerStates.Length; i++)
            {
                PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                byte votedTarget = playerVoteArea.TargetPlayerId;
                byte votedFor = playerVoteArea.VotedFor;
                LogHelper.Info(
                    string.Format("{0,-2}{1}:{2,-3}{3}", votedTarget,
                        $"({Helpers.getVoteName(votedTarget)})".PadRightV2(40), votedFor,
                        Helpers.getVoteName(votedFor)), "Vote");
                if (votedFor is not 252 and not 255 and not 254)
                {
                    PlayerControl player = Helpers.playerById(playerVoteArea.TargetPlayerId);
                    if (player == null || player.Data == null || player.Data.IsDead || player.Data.Disconnected ||
                        player.isGM()) continue;

                    // don't try to vote for the GM
                    if (GM.gm != null && votedFor == GM.gm.PlayerId) continue;

                    if (player.isRole(RoleType.BomberB) && BomberA.hasOneVote && BomberA.isAlive()) continue;
                    if (player.isRole(RoleType.MimicA) && MimicK.hasOneVote && MimicK.isAlive()) continue;

                    int additionalVotes = Mayor.mayor != null && Mayor.mayor.PlayerId == playerVoteArea.TargetPlayerId
                        ? Mayor.numVotes
                        : 1; // Mayor vote
                    if (dictionary.TryGetValue(votedFor, out int currentVotes))
                        dictionary[votedFor] = currentVotes + additionalVotes;
                    else
                        dictionary[votedFor] = additionalVotes;
                }
            }

            // Swapper swap votes
            if (Swapper.swapper != null && !Swapper.swapper.Data.IsDead)
            {
                PlayerVoteArea swapped1 = null;
                PlayerVoteArea swapped2 = null;
                foreach (PlayerVoteArea playerVoteArea in __instance.playerStates)
                {
                    if (playerVoteArea.TargetPlayerId == Swapper.playerId1) swapped1 = playerVoteArea;
                    if (playerVoteArea.TargetPlayerId == Swapper.playerId2) swapped2 = playerVoteArea;
                }

                if (swapped1 != null && swapped2 != null)
                {
                    if (!dictionary.ContainsKey(swapped1.TargetPlayerId)) dictionary[swapped1.TargetPlayerId] = 0;
                    if (!dictionary.ContainsKey(swapped2.TargetPlayerId)) dictionary[swapped2.TargetPlayerId] = 0;
                    (dictionary[swapped2.TargetPlayerId], dictionary[swapped1.TargetPlayerId]) = (
                        dictionary[swapped1.TargetPlayerId], dictionary[swapped2.TargetPlayerId]);
                    if (AmongUsClient.Instance.AmHost)
                    {
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                            PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SwapperAnimate,
                            SendOption.Reliable, -1);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                        RPCProcedure.swapperAnimate();
                    }
                }
            }

            return dictionary;
        }


        private static bool Prefix(MeetingHud __instance)
        {
            if (__instance.playerStates.All(ps => ps.AmDead || ps.DidVote))
            {
                Dictionary<byte, int> self = CalculateVotes(__instance);
                KeyValuePair<byte, int> max = self.MaxPair(out bool tie);
                NetworkedPlayerInfo exiled = GameData.Instance.AllPlayers.ToArray()
                    .FirstOrDefault(v => !tie && v.PlayerId == max.Key && !v.IsDead);

                MeetingHud.VoterState[] array = new MeetingHud.VoterState[__instance.playerStates.Length];
                for (int i = 0; i < __instance.playerStates.Length; i++)
                {
                    PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                    array[i] = new MeetingHud.VoterState
                    {
                        VoterId = playerVoteArea.TargetPlayerId,
                        VotedForId = playerVoteArea.VotedFor
                    };
                }

                // RPCVotingComplete
                __instance.RpcVotingComplete(array, exiled, tie);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Select))]
    private class MeetingHudSelectPatch
    {
        public static bool Prefix(ref bool __result, MeetingHud __instance, [HarmonyArgument(0)] int suspectStateIdx)
        {
            __result = false;
            if (GM.gm != null && GM.gm.PlayerId == suspectStateIdx) return false;
            if (noVoteIsSelfVote && PlayerControl.LocalPlayer.PlayerId == suspectStateIdx) return false;
            if (blockSkippingInEmergencyMeetings && suspectStateIdx == -1) return false;

            return true;
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.BloopAVoteIcon))]
    private class MeetingHudBloopAVoteIconPatch
    {
        public static bool Prefix(MeetingHud __instance, NetworkedPlayerInfo voterPlayer, int index, Transform parent) {
            var spriteRenderer = UnityEngine.Object.Instantiate<SpriteRenderer>(__instance.PlayerVotePrefab);
            var showVoteColors = !GameManager.Instance.LogicOptions.GetAnonymousVotes() ||
                                 (PlayerControl.LocalPlayer.Data.IsDead && TORMapOptions.ghostsSeeVotes);
            if (showVoteColors)
            {
                PlayerMaterial.SetColors(voterPlayer.DefaultOutfit.ColorId, spriteRenderer);
            }
            else
            {
                PlayerMaterial.SetColors(Palette.DisabledGrey, spriteRenderer);
            }
            var transform = spriteRenderer.transform;
            transform.SetParent(parent);
            transform.localScale = Vector3.zero;
            var component = parent.GetComponent<PlayerVoteArea>();
            if (component != null)
            {
                spriteRenderer.material.SetInt(PlayerMaterial.MaskLayer, component.MaskLayer);
            }
            __instance.StartCoroutine(Effects.Bloop(index * 0.3f, transform));
            parent.GetComponent<VoteSpreader>().AddVote(spriteRenderer);
            return false;
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.PopulateResults))]
    private class MeetingHudPopulateVotesPatch
    {
        private static bool Prefix(MeetingHud __instance, Il2CppStructArray<MeetingHud.VoterState> states)
        {
            // Swapper swap
            PlayerVoteArea swapped1 = null;
            PlayerVoteArea swapped2 = null;

            foreach (PlayerVoteArea playerVoteArea in __instance.playerStates)
            {
                if (playerVoteArea.TargetPlayerId == Swapper.playerId1) swapped1 = playerVoteArea;
                if (playerVoteArea.TargetPlayerId == Swapper.playerId2) swapped2 = playerVoteArea;
            }

            bool doSwap = animateSwap && swapped1 != null && swapped2 != null && Swapper.swapper != null &&
                          !Swapper.swapper.Data.IsDead;
            if (doSwap)
            {
                __instance.StartCoroutine(Effects.Slide3D(swapped1.transform, swapped1.transform.localPosition,
                    swapped2.transform.localPosition, 1.5f));
                __instance.StartCoroutine(Effects.Slide3D(swapped2.transform, swapped2.transform.localPosition,
                    swapped1.transform.localPosition, 1.5f));

                Swapper.numSwaps--;
            }


            __instance.TitleText.text =
                FastDestroyableSingleton<TranslationController>.Instance.GetString(StringNames.MeetingVotingResults,
                    new Il2CppReferenceArray<Il2CppSystem.Object>(0));
            int num = 0;
            for (int i = 0; i < __instance.playerStates.Length; i++)
            {
                PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                byte targetPlayerId = playerVoteArea.TargetPlayerId;
                // Swapper change playerVoteArea that gets the votes
                if (doSwap && playerVoteArea.TargetPlayerId == swapped1.TargetPlayerId) playerVoteArea = swapped2;
                else if (doSwap && playerVoteArea.TargetPlayerId == swapped2.TargetPlayerId) playerVoteArea = swapped1;

                playerVoteArea.ClearForResults();
                int num2 = 0;
                //bool mayorFirstVoteDisplayed = false;
                Dictionary<int, int> votesApplied = new();
                for (int j = 0; j < states.Length; j++)
                {
                    MeetingHud.VoterState voterState = states[j];
                    PlayerControl voter = Helpers.playerById(voterState.VoterId);
                    if (voter == null) continue;

                    NetworkedPlayerInfo playerById = GameData.Instance.GetPlayerById(voterState.VoterId);
                    if (playerById == null)
                        Debug.LogError(string.Format("Couldn't find player info for voter: {0}", voterState.VoterId));
                    else if (GM.gm != null &&
                             (voterState.VoterId == GM.gm.PlayerId || voterState.VotedForId == GM.gm.PlayerId))
                        continue;
                    else if (i == 0 && voterState.SkippedVote && !playerById.IsDead)
                    {
                        __instance.BloopAVoteIcon(playerById, num, __instance.SkippedVoting.transform);
                        num++;
                    }
                    else if (voterState.VotedForId == targetPlayerId && !playerById.IsDead)
                    {
                        __instance.BloopAVoteIcon(playerById, num2, playerVoteArea.transform);
                        num2++;
                    }

                    if (!votesApplied.ContainsKey(voter.PlayerId))
                        votesApplied[voter.PlayerId] = 0;

                    votesApplied[voter.PlayerId]++;

                    // Major vote, redo this iteration to place a second vote
                    if (Mayor.mayor != null && voter.PlayerId == Mayor.mayor.PlayerId &&
                        votesApplied[voter.PlayerId] < Mayor.numVotes) j--;
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))]
    private class MeetingHudVotingCompletedPatch
    {
        private static void Postfix(MeetingHud __instance, [HarmonyArgument(0)] byte[] states,
            [HarmonyArgument(1)] NetworkedPlayerInfo exiled, [HarmonyArgument(2)] bool tie)
        {
            // Reset swapper values
            Swapper.playerId1 = byte.MaxValue;
            Swapper.playerId2 = byte.MaxValue;

            if (meetingInfoText != null)
                meetingInfoText.gameObject.SetActive(false);

            foreach (DeadBody b in Object.FindObjectsOfType<DeadBody>()) Object.Destroy(b.gameObject);

            if (exiled != null)
            {
                finalStatuses[exiled.PlayerId] = FinalStatus.Exiled;
                bool isLovers = exiled.Object.isLovers();

                if (isLovers)
                    finalStatuses[exiled.Object.getPartner().PlayerId] = FinalStatus.Suicide;
                LogHelper.Info($"Exiled: {exiled.PlayerId}({Helpers.getVoteName(exiled.PlayerId)})", "Vote");
            }
        }
    }

    [HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.Select))]
    private class PlayerVoteAreaSelectPatch
    {
        private static bool Prefix(MeetingHud __instance)
        {
            return !(PlayerControl.LocalPlayer != null &&
                     Guesser.isGuesser(PlayerControl.LocalPlayer.PlayerId) && guesserUI != null);
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.ServerStart))]
    private class MeetingServerStartPatch
    {
        private static void Postfix(MeetingHud __instance, byte reporter)
        {
            // Helpers.log("ServerStart Postfix");
            // Helpers.log($"StackTrace: '{System.Environment.StackTrace}'");
            // populateButtonsPostfix(__instance);
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Deserialize))]
    private class MeetingDeserializePatch
    {
        private static void Postfix(MeetingHud __instance, [HarmonyArgument(0)] MessageReader reader,
            [HarmonyArgument(1)] bool initialState)
        {
            // Helpers.log("Deserialize Postfix");
            // Helpers.log($"StackTrace: '{System.Environment.StackTrace}'");
            // Add swapper buttons
            // if (initialState) {
            //     populateButtonsPostfix(__instance);
            // }
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.OpenMeetingRoom))]
    private class OpenMeetingPatch
    {
        public static void Prefix(HudManager __instance)
        {
            startMeeting();
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.PopulateButtons))]
    private class MeetingHudPopulae
    {
        public static bool Prefix(MeetingHud __instance, byte reporter)
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.StartMeeting))]
    private class PlayerControlStartMeetingPatch
    {
        private static float delay => CustomOptionHolder.delayBeforeMeeting.getFloat();

        private static IEnumerator CoStartMeeting(PlayerControl reporter, NetworkedPlayerInfo target)
        {
            // 既存処理の移植
            {
                while (!MeetingHud.Instance) yield return null;
                MeetingRoomManager.Instance.RemoveSelf();
                for (int i = 0; i < PlayerControl.AllPlayerControls.Count; i++)
                {
                    PlayerControl playerControl = PlayerControl.AllPlayerControls[i];
                    if (playerControl != null) playerControl.ResetForMeeting();
                }

                if (MapBehaviour.Instance) MapBehaviour.Instance.Close();
                if (Minigame.Instance) Minigame.Instance.ForceClose();
                MapUtilities.CachedShipStatus.OnMeetingCalled();
                KillAnimation.SetMovement(reporter, true);
            }

            // 遅延処理追加そのままyield returnで待ちを入れるとロックしたのでHudManagerのコルーチンとして実行させる
            FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(CoStartMeeting2(reporter, target)
                .WrapToIl2Cpp());
            yield break;
        }

        private static IEnumerator CoStartMeeting2(PlayerControl reporter, NetworkedPlayerInfo target)
        {
            // Modで追加する遅延処理
            {
                // ボタンと同時に通報が入った場合のバグ対応、他のクライアントからキルイベントが飛んでくるのを待つ
                // 見えては行けないものが見えるので暗転させる
                MeetingHud.Instance.state =
                    MeetingHud.VoteStates.Animating; //ゲッサーのキル用meetingupdateが呼ばれないようにするおまじない（呼ばれるとバグる）
                HudManager hudManager = FastDestroyableSingleton<HudManager>.Instance;
                SpriteRenderer blackscreen = Object.Instantiate(hudManager.FullScreen, hudManager.transform);
                SpriteRenderer greyscreen = Object.Instantiate(hudManager.FullScreen, hudManager.transform);
                blackscreen.color = Palette.Black;
                blackscreen.transform.position = Vector3.zero;
                blackscreen.transform.localPosition = new Vector3(0f, 0f, -910f);
                blackscreen.transform.localScale = new Vector3(10f, 10f, 1f);
                blackscreen.gameObject.SetActive(true);
                blackscreen.enabled = true;
                greyscreen.color = Palette.Black;
                greyscreen.transform.position = Vector3.zero;
                greyscreen.transform.localPosition = new Vector3(0f, 0f, -920f);
                greyscreen.transform.localScale = new Vector3(10f, 10f, 1f);
                greyscreen.gameObject.SetActive(true);
                greyscreen.enabled = true;
                TMP_Text text;
                RoomTracker roomTracker = FastDestroyableSingleton<HudManager>.Instance?.roomTracker;
                GameObject gameObject = Object.Instantiate(roomTracker.gameObject);
                Object.DestroyImmediate(gameObject.GetComponent<RoomTracker>());
                gameObject.transform.SetParent(FastDestroyableSingleton<HudManager>.Instance.transform);
                gameObject.transform.localPosition = new Vector3(0, 0, -930f);
                gameObject.transform.localScale = Vector3.one * 5f;
                text = gameObject.GetComponent<TMP_Text>();
                yield return Effects.Lerp(delay, new Action<float>(p =>
                {
                    // Delayed action
                    greyscreen.color = new Color(1.0f, 1.0f, 1.0f, 0.5f - (p / 2));
                    string message = (delay - (p * delay)).ToString("0.00");
                    if (message == "0") return;
                    string prefix = "<color=#FFFFFFFF>";
                    text.text = prefix + message + "</color>";
                    if (text != null) text.color = Color.white;
                }));
                // yield return new WaitForSeconds(2f);
                Object.Destroy(text.gameObject);
                Object.Destroy(blackscreen);
                Object.Destroy(greyscreen);

                // ミーティング画面の並び替えを直す
                populateButtons(MeetingHud.Instance, reporter.Data.PlayerId);
                populateButtonsPostfix(MeetingHud.Instance);
            }

            // 既存処理の移植
            {
                DeadBody[] array = Object.FindObjectsOfType<DeadBody>();
                NetworkedPlayerInfo[] deadBodies = (from b in array
                    select GameData.Instance.GetPlayerById(b.ParentId)).ToArray();
                for (int j = 0; j < array.Length; j++)
                    if (array[j] != null && array[j].gameObject != null)
                        Object.Destroy(array[j].gameObject);
                    else
                        Debug.LogError("Encountered a null Dead Body while destroying.");

                ShapeshifterEvidence[] array2 = Object.FindObjectsOfType<ShapeshifterEvidence>();
                for (int k = 0; k < array2.Length; k++)
                    if (array2[k] != null && array2[k].gameObject != null)
                        Object.Destroy(array2[k].gameObject);
                    else
                        Debug.LogError("Encountered a null Evidence while destroying.");

                MeetingHud.Instance.StartCoroutine(MeetingHud.Instance.CoIntro(reporter.Data, target, deadBodies));
            }
        }

        private static void StartMeeting(PlayerControl reporter, NetworkedPlayerInfo target)
        {
            ShipStatus.Instance.StartCoroutine(CoStartMeeting(reporter, target).WrapToIl2Cpp());
        }

        public static bool Prefix(PlayerControl __instance, NetworkedPlayerInfo target)
        {
            // MOD追加処理
            {
                LogHelper.Info("ShipStatus.StartMeeting");
                startMeeting();
                // Safe AntiTeleport positions
                AntiTeleport.position = PlayerControl.LocalPlayer.transform.position;
                // Medium meeting start time
                Medium.meetingStartTime = DateTime.UtcNow;
                // Reset vampire bitten
                Vampire.bitten = null;
                // Count meetings
                if (target == null) meetingsCount++;
            }

            // 既存処理の移植
            {
                bool flag = target == null;
                UnityTelemetry.Instance.WriteMeetingStarted(flag);
                StartMeeting(__instance, target); // 変更部分
                if (__instance.AmOwner)
                {
                    if (flag)
                    {
                        __instance.RemainingEmergencies--;
                        StatsManager.Instance.IncrementStat(StringNames.StatsEmergenciesCalled);
                        return false;
                    }

                    StatsManager.Instance.IncrementStat(StringNames.StatsBodiesReported);
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
    private class MeetingHudClosePatch
    {
        private static void Postfix(MeetingHud __instance)
        {
            if (GameOptionsManager.Instance.currentNormalGameOptions.MapId == 2 && CustomOptionHolder.polusRandomSpawn.getBool())
                if (AmongUsClient.Instance.AmHost)
                    foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                    {
                        Random rand = new();
                        int randVal = rand.Next(0, 6);
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                            PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RandomSpawn,
                            SendOption.Reliable, -1);
                        writer.Write(player.Data.PlayerId);
                        writer.Write((byte)randVal);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                        RPCProcedure.randomSpawn(player.Data.PlayerId, (byte)randVal);
                    }
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    private class MeetingHudStartPatch
    {
        public static void Prefix(MeetingHud __instance)
        {
            LogHelper.Info("---------Meeting Start----------", "Phase");
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
    private class MeetingHudOnDestroyPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            LogHelper.Info("----------Meeting End-----------", "Phase");
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive), typeof(bool))]
    private class HudManagerSetHudActive
    {
        public static void Postfix(HudManager __instance)
        {
            FastDestroyableSingleton<HudManager>.Instance.transform.FindChild("TaskDisplay").FindChild("TaskPanel")
                .gameObject.SetActive(true);
        }
    }
}
