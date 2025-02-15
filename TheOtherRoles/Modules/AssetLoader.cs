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
        LoadHaomingAssets();
    }
    private static void LoadHaomingAssets()
    {
        Stream resourceTestAssetBundleStream =
            dll.GetManifestResourceStream("TheOtherRoles.Resources.AssetBundle.haomingassets");
        AssetBundle assetBundleBundle = AssetBundle.LoadFromMemory(resourceTestAssetBundleStream.ReadFully());
        FoxTask.prefab = assetBundleBundle.LoadAsset<GameObject>("FoxTask.prefab").DontUnload();
        FoxTask.shrine = assetBundleBundle.LoadAsset<Sprite>("shrine2.png").DontUnload();
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
