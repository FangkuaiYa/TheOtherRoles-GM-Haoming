using System.IO;
using System.Reflection;
using Il2CppInterop.Runtime;
using TheOtherRoles.Objects;
using UnityEngine;

namespace TheOtherRoles;

public static class AssetLoader
{
    private static readonly Assembly dll = Assembly.GetExecutingAssembly();
    private static bool flag;
    public static GameObject foxTask;

    public static void LoadAsset()
    {
        if (flag) return;
        flag = true;
        LoadAudioAssets();
        LoadHaomingAssets();
    }

    private static void LoadAudioAssets()
    {
        Stream resourceAudioAssetBundleStream =
            dll.GetManifestResourceStream("TheOtherRoles.Resources.AssetBundle.audiobundle");
        AssetBundle assetBundleBundle = AssetBundle.LoadFromMemory(resourceAudioAssetBundleStream.ReadFully());
        Trap.activate = assetBundleBundle.LoadAsset<AudioClip>("TrapperActivate.mp3").DontUnload();
        Trap.countdown = assetBundleBundle.LoadAsset<AudioClip>("TrapperCountdown.mp3").DontUnload();
        Trap.disable = assetBundleBundle.LoadAsset<AudioClip>("TrapperDisable.mp3").DontUnload();
        Trap.kill = assetBundleBundle.LoadAsset<AudioClip>("TrapperKill.mp3").DontUnload();
        Trap.place = assetBundleBundle.LoadAsset<AudioClip>("TrapperPlace.mp3").DontUnload();
        Puppeteer.laugh = assetBundleBundle.LoadAsset<AudioClip>("PuppeteerLaugh.mp3").DontUnload();
    }

    private static void LoadHaomingAssets()
    {
        Stream resourceTestAssetBundleStream =
            dll.GetManifestResourceStream("TheOtherRoles.Resources.AssetBundle.haomingassets");
        AssetBundle assetBundleBundle = AssetBundle.LoadFromMemory(resourceTestAssetBundleStream.ReadFully());
        FoxTask.prefab = assetBundleBundle.LoadAsset<GameObject>("FoxTask.prefab").DontUnload();
        Shrine.sprite = assetBundleBundle.LoadAsset<Sprite>("shrine2.png").DontUnload();
        //HaomingMenu.menuPrefab = assetBundleBundle.LoadAsset<GameObject>("HaomingMenu.prefab").DontUnload();
        //HaomingMenu.loadSettingsPrefab =
        //    assetBundleBundle.LoadAsset<GameObject>("LoadSettingsMenu.prefab").DontUnload();
    }

    public static byte[] ReadFully(this Stream input)
    {
        using MemoryStream ms = new();
        input.CopyTo(ms);
        return ms.ToArray();
    }

    public static T LoadAsset<T>(this AssetBundle assetBundle, string name) where T : Object
    {
        return assetBundle.LoadAsset(name, Il2CppType.Of<T>())?.Cast<T>();
    }

    public static T DontUnload<T>(this T obj) where T : Object
    {
        obj.hideFlags |= HideFlags.DontUnloadUnusedAsset;

        return obj;
    }
}
