using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AmongUs.Data;
using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using InnerNet;
using TheOtherRoles.Modules;
using TheOtherRoles.Modules.CustomHats;
using TheOtherRoles.Objects;
using TheOtherRoles.Patches;
using UnityEngine;
using Random = System.Random;

namespace TheOtherRoles;

[BepInPlugin(Id, "The Other Roles GM", VersionString)]
[BepInDependency(SubmergedCompatibility.SUBMERGED_GUID, BepInDependency.DependencyFlags.SoftDependency)]
//[BepInProcess("Among Us.exe")]
public class TheOtherRolesPlugin : BasePlugin
{
    public const string Id = "me.eisbison.theotherroles";

    public const string VersionString = "2.3.139";

    public static Version Version = Version.Parse(VersionString);
    internal static ManualLogSource Logger;
    public static TheOtherRolesPlugin Instance;

    public static int optionsPage = 0;

    public static Assembly JsonNet;

    public static Sprite ModStamp;

    public static IRegionInfo[] defaultRegions;

    private static Dictionary<string, Sprite> gmhResources = new();

    private static Dictionary<string, AudioClip> gmhAudio = new();

    public Harmony Harmony { get; } = new(Id);

    public static ConfigEntry<bool> DebugMode { get; private set; }
    public static ConfigEntry<bool> StreamerMode { get; set; }
    public static ConfigEntry<bool> GhostsSeeTasks { get; set; }
    public static ConfigEntry<bool> GhostsSeeRoles { get; set; }
    public static ConfigEntry<bool> GhostsSeeVotes { get; set; }
    public static ConfigEntry<bool> ShowRoleSummary { get; set; }
    public static ConfigEntry<bool> HideNameplates { get; set; }
    public static ConfigEntry<bool> ShowLighterDarker { get; set; }
    public static ConfigEntry<bool> HideTaskArrows { get; set; }
    public static ConfigEntry<bool> OfflineHats { get; set; }
    public static ConfigEntry<bool> HideFakeTasks { get; set; }
    public static ConfigEntry<bool> BetterSabotageMap { get; set; }
    public static ConfigEntry<bool> ShowChatNotifications { get; set; }
    public static ConfigEntry<bool> ForceNormalSabotageMap { get; set; }
    public static ConfigEntry<string> StreamerModeReplacementText { get; set; }
    public static ConfigEntry<string> StreamerModeReplacementColor { get; set; }
    public static ConfigEntry<string> Ip { get; set; }
    public static ConfigEntry<ushort> Port { get; set; }
    public static ConfigEntry<string> DebugRepo { get; private set; }
    public static ConfigEntry<string> ShowPopUpVersion { get; set; }
    public static ConfigEntry<string> WebhookUrl { get; set; }
    public static ConfigEntry<bool> TransparentMap { get; set; }

    public static void LoadREAssets()
    {
        gmhResources = new Dictionary<string, Sprite>();
        Assembly assembly = Assembly.GetExecutingAssembly();
        string[] resourceNames = assembly.GetManifestResourceNames();

        Stream resourceBundle =
            assembly.GetManifestResourceStream("TheOtherRoles.Resources.AssetBundle.fangkuaiassets");
        AssetBundle assetBundle = AssetBundle.LoadFromMemory(resourceBundle.ReadFully());
        foreach (string f in assetBundle.GetAllAssetNames())
            gmhResources.Add(f, assetBundle.LoadAsset<Sprite>(f).DontUnload());
        assetBundle.Unload(false);
    }
    public static void LoadAudioAssets()
    {
        gmhAudio = new Dictionary<string, AudioClip>();
        Assembly assembly = Assembly.GetExecutingAssembly();
        string[] resourceNames = assembly.GetManifestResourceNames();

        Stream resourceBundle =
            assembly.GetManifestResourceStream("TheOtherRoles.Resources.AssetBundle.fangkuaiaudioassets");
        AssetBundle assetBundle = AssetBundle.LoadFromMemory(resourceBundle.ReadFully());
        foreach (string f in assetBundle.GetAllAssetNames())
            gmhAudio.Add(f, assetBundle.LoadAsset<AudioClip>(f).DontUnload());
        assetBundle.Unload(false);
    }
    public static Sprite getResources(string path)
    {
        path = "assets/resources/" + path.ToLower();
        Sprite returnValue;
        return gmhResources.TryGetValue(path, out returnValue) ? returnValue : null;
    }
    public static AudioClip getAudio(string path)
    {
        path = "assets/audio/" + path.ToLower();
        AudioClip returnValue;
        return gmhAudio.TryGetValue(path, out returnValue) ? returnValue : null;
    }

    public static void UpdateRegions()
    {
        ServerManager serverManager = FastDestroyableSingleton<ServerManager>.Instance;
        IRegionInfo[] regions = new[]
        {
            new StaticHttpRegionInfo("Custom", StringNames.NoTranslation, Ip.Value,
                    new Il2CppReferenceArray<ServerInfo>(new ServerInfo[1]
                        { new("Custom", Ip.Value, Port.Value, false) }))
                .CastFast<IRegionInfo>(),
                        new StaticHttpRegionInfo("fangkuai-server", StringNames.NoTranslation, "https://player.fangkuai.fun",
                    new Il2CppReferenceArray<ServerInfo>(new ServerInfo[1]
                        { new("fangkuai-server", "https://player.fangkuai.fun", 443, false) }))
                .CastFast<IRegionInfo>()

        };
        IRegionInfo currentRegion = serverManager.CurrentRegion;
        foreach (IRegionInfo region in regions)
            if (region == null)
                Logger.LogError("Could not add region");
            else
            {
                if (currentRegion != null && region.Name.Equals(currentRegion.Name, StringComparison.OrdinalIgnoreCase))
                    currentRegion = region;
                serverManager.AddOrUpdateRegion(region);
            }

        // AU remembers the previous region that was set, so we need to restore it
        if (currentRegion != null)
        {
            Logger.LogDebug("Resetting previous region");
            serverManager.SetRegion(currentRegion);
        }
    }

    public override void Load()
    {
        ModTranslation.Load();
        AssetLoader.LoadAsset();
        LoadREAssets();
        LoadAudioAssets();
        Instance = this;
        Logger = Log;
        LogHelper.SetLogSource(Log);
        DebugMode = Config.Bind("Custom", "Enable Debug Mode", false);
        StreamerMode = Config.Bind("Custom", "Enable Streamer Mode", false);
        GhostsSeeTasks = Config.Bind("Custom", "Ghosts See Remaining Tasks", true);
        GhostsSeeRoles = Config.Bind("Custom", "Ghosts See Roles", true);
        GhostsSeeVotes = Config.Bind("Custom", "Ghosts See Votes", true);
        ShowRoleSummary = Config.Bind("Custom", "Show Role Summary", true);
        HideNameplates = Config.Bind("Custom", "Hide Nameplates", false);
        ShowLighterDarker = Config.Bind("Custom", "Show Lighter / Darker", false);
        HideTaskArrows = Config.Bind("Custom", "Hide Task Arrows", false);
        OfflineHats = Config.Bind("Custom", "Offline Hats", false);
        HideFakeTasks = Config.Bind("Custom", "Hide Fake Tasks", false);
        ShowChatNotifications = Config.Bind("Custom", "Show Chat Notifications", true);
        BetterSabotageMap = Config.Bind("Custom", "BetterSabotageMap", false);
        ForceNormalSabotageMap = Config.Bind("Custom", "ForceNormalSabotageMap", false);
        ShowPopUpVersion = Config.Bind("Custom", "Show PopUp", "0");
        StreamerModeReplacementText = Config.Bind("Custom", "Streamer Mode Replacement Text", "\n\nThe Other Roles GM");
        StreamerModeReplacementColor = Config.Bind("Custom", "Streamer Mode Replacement Text Hex Color", "#87AAF5FF");
        DebugRepo = Config.Bind("Custom", "Debug Hat Repo", "");
        WebhookUrl = Config.Bind("Custom", "WebhookUrl", "");
        TransparentMap = Config.Bind("Custom", "TransparentMap", false);

        Ip = Config.Bind("Custom", "Custom Server IP", "127.0.0.1");
        Port = Config.Bind("Custom", "Custom Server Port", (ushort)22023);
        defaultRegions = ServerManager.DefaultRegions;
        // Removes vanilla Servers
        ServerManager.DefaultRegions = new Il2CppReferenceArray<IRegionInfo>(new IRegionInfo[0]);
        UpdateRegions();

        GameOptionsData.RecommendedImpostors = Enumerable.Repeat(3, 16).ToArray();
        GameOptionsData.MaxImpostors = Enumerable.Repeat(15, 16).ToArray(); // Max Imp = Recommended Imp = 3
        GameOptionsData.MinPlayers = Enumerable.Repeat(4, 15).ToArray(); // Min Players = 4

        Trap.place = getAudio("TrapperPlace.ogg");
        Trap.disable = getAudio("TrapperDisable.ogg");
        Trap.countdown = getAudio("TrapperCountdown.ogg");
        Trap.kill = getAudio("TrapperKill.ogg");
        Puppeteer.laugh = getAudio("PuppeteerLaugh.ogg");

        DebugMode = Config.Bind("Custom", "Enable Debug Mode", false);
        Harmony.PatchAll();
        CustomOptionHolder.Load();
        CustomHatManager.LoadHats();
        //MainMenuPatch.addSceneChangeCallbacks();
        AddComponent<ModUpdater>();
        RoleInfo.Load();
        CustomColors.Load();
        SubmergedPatch.Patch();
        SubmergedCompatibility.Initialize();

        //Newtonsoft.Jsonを読み込み
        Stream stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("TheOtherRoles.Resources.Newtonsoft.Json.dll");
        byte[] buffer = new byte[stream.Length];
        stream.Read(buffer, 0, buffer.Length);
        JsonNet = Assembly.Load(buffer);

        // オレオレオブジェクト有効化
        ClassInjector.RegisterTypeInIl2Cpp(typeof(FoxTask));
    }
}

// Deactivate bans, since I always leave my local testing game and ban myself
[HarmonyPatch(typeof(StatsManager), nameof(StatsManager.AmBanned), MethodType.Getter)]
public static class AmBannedPatch
{
    public static void Postfix(out bool __result)
    {
        __result = false;
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.Awake))]
public static class ChatControllerAwakePatch
{
    private static void Prefix()
    {
        if (!EOSManager.Instance.isKWSMinor) DataManager.Settings.Multiplayer.chatMode = (QuickChatModes)1;
        // SaveManager.isGuest = false;
    }
}

// Debugging tools
[HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
public static class DebugManager
{
    private static readonly Random random = new((int)DateTime.Now.Ticks);
    private static readonly List<PlayerControl> bots = new();

    public static void Postfix(KeyboardJoystick __instance)
    {
#if DEBUG
        // Spawn dummys
        if (Input.GetKeyDown(KeyCode.F))
        {
            PlayerControl playerControl = UnityEngine.Object.Instantiate(AmongUsClient.Instance.PlayerPrefab);
            var playerId = playerControl.PlayerId = (byte)GameData.Instance.GetAvailableId();

            bots.Add(playerControl);
            var data = GameData.Instance.AddDummy(playerControl);
            AmongUsClient.Instance.Spawn(data);
            AmongUsClient.Instance.Spawn(playerControl);
            playerControl.isDummy = true;

            playerControl.transform.position = PlayerControl.LocalPlayer.transform.position;
            playerControl.GetComponent<DummyBehaviour>().enabled = true;
            playerControl.NetTransform.enabled = false;

            playerControl.SetName(RandomString(10));
            playerControl.SetColor((byte)random.Next(Palette.PlayerColors.Length));
            playerControl.SetHat(HatManager.Instance.allHats[random.Next(HatManager.Instance.allHats.Count)].ProdId, playerControl.Data.DefaultOutfit.ColorId);
            playerControl.SetPet(HatManager.Instance.allPets[random.Next(HatManager.Instance.allPets.Count)].ProdId);
            playerControl.SetSkin(HatManager.Instance.allSkins[random.Next(HatManager.Instance.allSkins.Count)].ProdId, playerControl.Data.DefaultOutfit.ColorId);
            playerControl.SetVisor(HatManager.Instance.allVisors[random.Next(HatManager.Instance.allVisors.Count)].ProdId, playerControl.Data.DefaultOutfit.ColorId);
            playerControl.SetNamePlate(HatManager.Instance.allNamePlates[random.Next(HatManager.Instance.allNamePlates.Count)].ProdId);
            data.PlayerLevel = playerId;

            data.RpcSetTasks(new Il2CppStructArray<byte>(0));
        }
#endif

        if (AmongUsClient.Instance.AmHost && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started)
            //ゲーム強制終了
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.F5))
                GameManager.Instance.RpcEndGame((GameOverReason)CustomGameOverReason.ForceEnd, false);

        if (!TheOtherRolesPlugin.DebugMode.Value) return;
    }

    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

[HarmonyPatch(typeof(SplashManager), nameof(SplashManager.Update))]
internal class SplashLogoAnimatorPatch
{
    public static void Prefix(SplashManager __instance)
    {
        if (TheOtherRolesPlugin.DebugMode.Value)
        {
            __instance.sceneChanger.AllowFinishLoadingScene();
            __instance.startedSceneLoad = true;
        }
    }
}
// [HarmonyPatch(typeof(SignInGuestOfflineChoice), nameof(SignInGuestOfflineChoice.Open))]
// public class SignInGuestOfflineChoiceOpenPatch
// {
//     private static void Postfix(SignInGuestOfflineChoice __instance)
//     {
//         if (TheOtherRolesPlugin.DebugMode.Value) __instance?.continueOfflineButton?.OnClick?.Invoke();
//     }
// }
