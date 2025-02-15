using System;
using System.Collections.Generic;
using System.Reflection;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using InnerNet;
using UnityEngine;
using Object = Il2CppSystem.Object;

namespace TheOtherRoles.Patches;

public class GameStartManagerPatch
{
    public static Dictionary<int, PlayerVersion> playerVersions = new();
    private static float timer = 600f;
    private static float kickingTimer;
    private static bool versionSent;
    private static string lobbyCodeText = "";

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnBecomeHost))]
    public class AmongUsClientOnBecomeHostPatch
    {
        public static void Postfix(AmongUsClient __instance)
        {
            LogHelper.Info($"My Player ID:{__instance.ClientId} Now Become Host", "Session");
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
    public class AmongUsClientOnGameJoinedPatch
    {
        public static void Postfix(AmongUsClient __instance)
        {
            LogHelper.Info($"My Player ID:{__instance.ClientId} Joined", "Session");
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.ExitGame))]
    public class AmongUsClientOnDisconnectedPatch
    {
        public static void Prefix(AmongUsClient __instance)
        {
            LogHelper.Info($"My Player ID:{__instance.ClientId} Exit", "Session");
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    public class AmongUsClientOnPlayerJoinedPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
        {
            if (PlayerControl.LocalPlayer != null) Helpers.shareGameVersion();
            LogHelper.Info($"Player \"{client.PlayerName}(ID:{client.Id})\" Joined", "Session");
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    public class AmongUsClientOnPlayerLeftPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client,
            [HarmonyArgument(1)] DisconnectReasons reason)
        {
            LogHelper.Info($"Player \"{client.PlayerName}(ID:{client.Id})\" Left (Reason: {reason})", "Session");
        }
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
    public class GameStartManagerStartPatch
    {
        public static void Postfix(GameStartManager __instance)
        {
            // Trigger version refresh
            versionSent = false;
            // Reset lobby countdown timer
            timer = 600f;
            // Reset kicking timer
            kickingTimer = 0f;
            // Copy lobby code
            string code = GameCode.IntToGameName(AmongUsClient.Instance.GameId);
            GUIUtility.systemCopyBuffer = code;
            lobbyCodeText =
                FastDestroyableSingleton<TranslationController>.Instance.GetString(StringNames.RoomCode,
                    new Il2CppReferenceArray<Object>(0)) + "\r\n" + code;
        }
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
    public class GameStartManagerUpdatePatch
    {
        private static bool update;
        private static string currentText = "";
        private static GameObject copiedStartButton;
        public static float startingTimer = 0;

        public static void Prefix(GameStartManager __instance)
        {
            if (!AmongUsClient.Instance.AmHost || !GameData.Instance) return; // Not host or no instance
#if DEBUG
            __instance.MinPlayers = 1;
#endif
            update = GameData.Instance.PlayerCount != __instance.LastPlayerCount;
            //カウントダウンキャンセル
            if (Input.GetKeyDown(KeyCode.C) &&
                GameStartManager.Instance.startState == GameStartManager.StartingStates.Countdown)
                GameStartManager.Instance.ResetStartState();
            //即スタート
            if (Input.GetKeyDown(KeyCode.LeftShift) &&
                GameStartManager.Instance.startState == GameStartManager.StartingStates.Countdown)
                GameStartManager.Instance.countDownTimer = 0;
        }

        public static void Postfix(GameStartManager __instance)
        {
            // Send version as soon as PlayerControl.LocalPlayer exists
            if (PlayerControl.LocalPlayer != null && !versionSent)
            {
                versionSent = true;
                Helpers.shareGameVersion();
            }

            // Host update with version handshake infos
            if (AmongUsClient.Instance.AmHost)
            {
                bool blockStart = false;
                string message = "";
                foreach (ClientData client in AmongUsClient.Instance.allClients.ToArray())
                {
                    if (client.Character == null) continue;
                    if (!playerVersions.ContainsKey(client.Id))
                    {
                        blockStart = true;
                        message +=
                            $"<color=#FF0000FF>{client.Character.Data.PlayerName}:  {ModTranslation.getString("errorNotInstalled")}\n</color>";
                    }
                    else
                    {
                        PlayerVersion PV = playerVersions[client.Id];
                        int diff = TheOtherRolesPlugin.Version.CompareTo(PV.version);
                        if (diff > 0)
                        {
                            message +=
                                $"<color=#FF0000FF>{client.Character.Data.PlayerName}:  {ModTranslation.getString("errorOlderVersion")} (v{playerVersions[client.Id].version})\n</color>";
                            blockStart = true;
                        }
                        else if (diff < 0)
                        {
                            message +=
                                $"<color=#FF0000FF>{client.Character.Data.PlayerName}:  {ModTranslation.getString("errorNewerVersion")} (v{playerVersions[client.Id].version})\n</color>";
                            blockStart = true;
                        }
                        else if (!PV.GuidMatches())
                        {
                            // version presumably matches, check if Guid matches
                            message +=
                                $"<color=#FF0000FF>{client.Character.Data.PlayerName}:  {ModTranslation.getString("errorWrongVersion")} v{playerVersions[client.Id].version} <size=30%>({PV.guid})</size>\n</color>";
                            blockStart = true;
                        }
                    }
                }

                if (blockStart)
                {
                    __instance.GameStartText.text = message;
                    __instance.GameStartText.transform.localPosition = __instance.StartButton.transform.localPosition + Vector3.up * 5;
                    __instance.GameStartText.transform.localScale = new Vector3(2f, 2f, 1f);
                    __instance.GameStartTextParent.SetActive(true);
                }
                else
                {
                    __instance.GameStartText.transform.localPosition = Vector3.zero;
                    __instance.GameStartText.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
                    if (!__instance.GameStartText.text.Contains(FastDestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameStarting).Replace("{0}", "")))
                    {
                        __instance.GameStartText.text = String.Empty;
                        __instance.GameStartTextParent.SetActive(false);
                    }
                }
                if (__instance.startState != GameStartManager.StartingStates.Countdown)
                    copiedStartButton?.Destroy();

                // Make starting info available to clients:
                if (startingTimer <= 0 && __instance.startState == GameStartManager.StartingStates.Countdown)
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetGameStarting, Hazel.SendOption.Reliable, -1);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.setGameStarting();

                    // Activate Stop-Button
                    copiedStartButton = GameObject.Instantiate(__instance.StartButton.gameObject, __instance.StartButton.gameObject.transform.parent);
                    copiedStartButton.transform.localPosition = __instance.StartButton.transform.localPosition;
                    copiedStartButton.SetActive(true);
                    var startButtonText = copiedStartButton.GetComponentInChildren<TMPro.TextMeshPro>();
                    startButtonText.text = "";
                    startButtonText.fontSize *= 0.8f;
                    startButtonText.fontSizeMax = startButtonText.fontSize;
                    startButtonText.gameObject.transform.localPosition = Vector3.zero;
                    PassiveButton startButtonPassiveButton = copiedStartButton.GetComponent<PassiveButton>();

                    void StopStartFunc()
                    {
                        __instance.ResetStartState();
                        copiedStartButton.Destroy();
                        startingTimer = 0;
                    }
                    startButtonPassiveButton.OnClick.AddListener((Action)(() => StopStartFunc()));
                    __instance.StartCoroutine(Effects.Lerp(.1f, new System.Action<float>((p) => {
                        startButtonText.text = "";
                    })));
                }
            }

            // Client update with handshake infos
            if (!AmongUsClient.Instance.AmHost)
            {
                if (!playerVersions.ContainsKey(AmongUsClient.Instance.HostId) ||
                    TheOtherRolesPlugin.Version.CompareTo(playerVersions[AmongUsClient.Instance.HostId].version) != 0)
                {
                    kickingTimer += Time.deltaTime;
                    if (kickingTimer > 10)
                    {
                        kickingTimer = 0;
                        AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
                        SceneChanger.ChangeScene("MainMenu");
                    }

                    __instance.GameStartText.text = string.Format(ModTranslation.getString("errorHostNoVersion"),
                        Math.Round(10 - kickingTimer));
                    __instance.GameStartText.transform.localPosition =
                        __instance.StartButton.transform.localPosition + (Vector3.up * 2);
                }
                else
                {
                    __instance.GameStartText.transform.localPosition = __instance.StartButton.transform.localPosition;
                    if (__instance.startState != GameStartManager.StartingStates.Countdown)
                        __instance.GameStartText.text = string.Empty;
                }
                if (!__instance.GameStartText.text.Contains(FastDestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameStarting).Replace("{0}", "")) || !CustomOptionHolder.anyPlayerCanStopStart.getBool())
                    copiedStartButton?.Destroy();
                if (CustomOptionHolder.anyPlayerCanStopStart.getBool() && copiedStartButton == null && __instance.GameStartText.text.Contains(FastDestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameStarting).Replace("{0}", "")))
                {

                    // Activate Stop-Button
                    copiedStartButton = GameObject.Instantiate(__instance.StartButton.gameObject, __instance.StartButton.gameObject.transform.parent);
                    copiedStartButton.transform.localPosition = __instance.StartButton.transform.localPosition;
                    copiedStartButton.SetActive(true);
                    var startButtonText = copiedStartButton.GetComponentInChildren<TMPro.TextMeshPro>();
                    startButtonText.text = "";
                    startButtonText.fontSize *= 0.8f;
                    startButtonText.fontSizeMax = startButtonText.fontSize;
                    startButtonText.gameObject.transform.localPosition = Vector3.zero;
                    PassiveButton startButtonPassiveButton = copiedStartButton.GetComponent<PassiveButton>();

                    void StopStartFunc()
                    {
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.StopStart, Hazel.SendOption.Reliable, AmongUsClient.Instance.HostId);
                        writer.Write(PlayerControl.LocalPlayer.PlayerId);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                        copiedStartButton.Destroy();
                        __instance.GameStartText.text = String.Empty;
                        startingTimer = 0;
                    }
                    startButtonPassiveButton.OnClick.AddListener((Action)(() => StopStartFunc()));
                    __instance.StartCoroutine(Effects.Lerp(.1f, new System.Action<float>((p) => {
                        startButtonText.text = "";
                    })));

                }
            }

            // Start Timer
            if (startingTimer > 0)
            {
                startingTimer -= Time.deltaTime;
            }
            // Lobby code replacement
            //__instance.GameRoomName.text = TheOtherRolesPlugin.StreamerMode.Value ? $"<color={TheOtherRolesPlugin.StreamerModeReplacementColor.Value}>{TheOtherRolesPlugin.StreamerModeReplacementText.Value}</color>" : lobbyCodeText;

            // Lobby timer
            if (!AmongUsClient.Instance.AmHost || !GameData.Instance || !__instance.PlayerCounter)
                return; // Not host or no instance

            if (update) currentText = __instance.PlayerCounter.text;

            timer = Mathf.Max(0f, timer -= Time.deltaTime);
            int minutes = (int)timer / 60;
            int seconds = (int)timer % 60;
            string suffix = $" ({minutes:00}:{seconds:00})";

            if (!AmongUsClient.Instance) return;
        }
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.BeginGame))]
    public class GameStartManagerBeginGame
    {
        public static bool Prefix(GameStartManager __instance)
        {
            // Block game start if not everyone has the same mod version
            bool continueStart = true;

            if (AmongUsClient.Instance.AmHost)
            {
                foreach (ClientData client in AmongUsClient.Instance.allClients)
                {
                    if (client.Character == null) continue;
                    DummyBehaviour dummyComponent = client.Character.GetComponent<DummyBehaviour>();
                    if (dummyComponent != null && dummyComponent.enabled)
                        continue;

                    if (!playerVersions.ContainsKey(client.Id))
                    {
                        continueStart = false;
                        break;
                    }

                    PlayerVersion PV = playerVersions[client.Id];
                    int diff = TheOtherRolesPlugin.Version.CompareTo(PV.version);
                    if (diff != 0 || !PV.GuidMatches())
                    {
                        continueStart = false;
                        break;
                    }
                }

                if (CustomOptionHolder.uselessOptions.getBool() && CustomOptionHolder.dynamicMap.getBool() &&
                    continueStart)
                {
                    // 0 = Skeld
                    // 1 = Mira HQ
                    // 2 = Polus
                    // 3 = Dleks - deactivated
                    // 4 = Airship
                    List<byte> possibleMaps = new();
                    if (CustomOptionHolder.dynamicMapEnableSkeld.getBool())
                        possibleMaps.Add(0);
                    if (CustomOptionHolder.dynamicMapEnableMira.getBool())
                        possibleMaps.Add(1);
                    if (CustomOptionHolder.dynamicMapEnablePolus.getBool())
                        possibleMaps.Add(2);
                    // if (CustomOptionHolder.dynamicMapEnableDleks.getBool())
                    //     possibleMaps.Add(3);
                    if (CustomOptionHolder.dynamicMapEnableAirShip.getBool())
                        possibleMaps.Add(4);
                    if (CustomOptionHolder.dynamicMapEnableFungle.getBool())
                        possibleMaps.Add(5);
                    if (CustomOptionHolder.dynamicMapEnableSubmerged.getBool())
                        possibleMaps.Add(6);
                    byte chosenMapId = possibleMaps[TheOtherRoles.rnd.Next(possibleMaps.Count)];

                    // Translate chosen map to presets page and use that maps random map preset page
                    if (CustomOptionHolder.dynamicMapSeparateSettings.getBool())
                        CustomOptionHolder.presetSelection.updateSelection(chosenMapId + 2);

                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
                        PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DynamicMapOption,
                        SendOption.Reliable);
                    writer.Write(chosenMapId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.dynamicMapOption(chosenMapId);
                }
            }

            return continueStart;
        }
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.SetStartCounter))]
    public static class SetStartCounterPatch
    {
        public static void Postfix(GameStartManager __instance, sbyte sec)
        {
            if (sec > 0) __instance.startState = GameStartManager.StartingStates.Countdown;

            if (sec <= 0) __instance.startState = GameStartManager.StartingStates.NotStarting;
        }
    }

    public class PlayerVersion
    {
        public readonly Guid guid;
        public readonly Version version;

        public PlayerVersion(Version version, Guid guid)
        {
            this.version = version;
            this.guid = guid;
        }

        public bool GuidMatches()
        {
            return Assembly.GetExecutingAssembly().ManifestModule.ModuleVersionId.Equals(guid);
        }

        // Moves the haunt menu a bit further down
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HauntMenuMinigame), nameof(HauntMenuMinigame.FixedUpdate))]
        public static void UpdatePostfix(HauntMenuMinigame __instance)
        {
            if (GameOptionsManager.Instance.currentGameOptions.GameMode != GameModes.Normal) return;
            if (PlayerControl.LocalPlayer.Data.Role.IsImpostor &&
                TheOtherRoles.Vampire.vampire != PlayerControl.LocalPlayer)
                __instance.gameObject.transform.localPosition =
                    new Vector3(-6f, -1.1f, __instance.gameObject.transform.localPosition.z);
        }
    }
}
