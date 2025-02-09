using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace TheOtherRoles.Modules.CustomHats;

public static class CustomHatManager
{
    public const string ResourcesDirectory = "TheOtherHats";
    public const string InnerslothPackageName = "Innersloth Hats";
    public const string DeveloperPackageName = "Developer Hats";

    internal static readonly Tuple<string, string> Repository = new("TheOtherRolesAU", "TheOtherHats");

    internal static readonly string ManifestFileName = "CustomHats.json";

    internal static List<CustomHat> UnregisteredHats = new();
    internal static readonly Dictionary<string, HatViewData> ViewDataCache = new();
    internal static readonly Dictionary<string, HatExtension> ExtensionCache = new();

    private static readonly HatsLoader Loader;

    static CustomHatManager()
    {
        Loader = TheOtherRolesPlugin.Instance.AddComponent<HatsLoader>();
    }

    internal static string RepositoryUrl
    {
        get
        {
            (string owner, string repository) = Repository;
            return Helpers.isChinese()
                ? "https://dl.fangkuai.fun/ModFiles/TheOtherRoles-GM-Haoming/TheOtherHats"
                : $"https://raw.githubusercontent.com/{owner}/{repository}/master";
        }
    }

    internal static string CustomSkinsDirectory =>
        Path.Combine(Path.GetDirectoryName(Application.dataPath)!, ResourcesDirectory);

    internal static string HatsDirectory => CustomSkinsDirectory;

    internal static HatExtension TestExtension { get; private set; }

    internal static void LoadHats()
    {
        Loader.FetchHats();
    }

    internal static bool TryGetCached(this HatParent hatParent, out HatViewData asset)
    {
        if (hatParent && hatParent.Hat) return hatParent.Hat.TryGetCached(out asset);
        asset = null;
        return false;
    }

    internal static bool TryGetCached(this HatData hat, out HatViewData asset)
    {
        return ViewDataCache.TryGetValue(hat.name, out asset);
    }

    internal static bool IsCached(this HatData hat)
    {
        return ViewDataCache.ContainsKey(hat.name);
    }

    internal static bool IsCached(this HatParent hatParent)
    {
        return hatParent.Hat.IsCached();
    }

    internal static HatData CreateHatBehaviour(CustomHat ch, bool testOnly = false)
    {
        HatViewData viewData = ViewDataCache[ch.Name] = ScriptableObject.CreateInstance<HatViewData>();
        HatData hat = ScriptableObject.CreateInstance<HatData>();

        viewData.MainImage = CreateHatSprite(ch.Resource);
        if (viewData.MainImage == null) throw new FileNotFoundException("File not downloaded yet");
        viewData.FloorImage = viewData.MainImage;
        if (ch.BackResource != null)
        {
            viewData.BackImage = CreateHatSprite(ch.BackResource);
            ch.Behind = true;
        }

        if (ch.ClimbResource != null)
        {
            viewData.ClimbImage = CreateHatSprite(ch.ClimbResource);
            viewData.LeftClimbImage = viewData.ClimbImage;
        }

        hat.name = ch.Name;
        hat.displayOrder = 99;
        hat.ProductId = "hat_" + ch.Name.Replace(' ', '_');
        hat.InFront = !ch.Behind;
        hat.NoBounce = !ch.Bounce;
        hat.ChipOffset = new Vector2(0f, 0.2f);
        hat.Free = true;

        HatExtension extend = new()
        {
            Author = ch.Author ?? "Unknown",
            Package = ch.Package ?? "Misc.",
            Condition = ch.Condition ?? "none",
            Adaptive = ch.Adaptive
        };

        if (ch.FlipResource != null) extend.FlipImage = CreateHatSprite(ch.FlipResource);

        if (ch.BackFlipResource != null) extend.BackFlipImage = CreateHatSprite(ch.BackFlipResource);

        if (testOnly)
        {
            TestExtension = extend;
            TestExtension.Condition = hat.name;
        }
        else
            ExtensionCache[hat.name] = extend;

        hat.ViewDataRef = new AssetReference(ViewDataCache[hat.name].Pointer);
        hat.CreateAddressableAsset();
        return hat;
    }

    private static Sprite CreateHatSprite(string path)
    {
        Texture2D texture = Helpers.loadTextureFromDisk(Path.Combine(HatsDirectory, path));
        if (texture == null)
            texture = Helpers.loadTextureFromResources(path);
        if (texture == null) return null;
        Sprite sprite = Sprite.Create(texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.53f, 0.575f),
            texture.width * 0.375f);
        if (sprite == null) return null;
        texture.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
        sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;

        return sprite;
    }

    public static List<CustomHat> CreateHatDetailsFromFileNames(string[] fileNames, bool fromDisk = false)
    {
        Dictionary<string, CustomHat> fronts = new();
        Dictionary<string, string> backs = new();
        Dictionary<string, string> flips = new();
        Dictionary<string, string> backFlips = new();
        Dictionary<string, string> climbs = new();

        foreach (string fileName in fileNames)
        {
            int index = fileName.LastIndexOf("\\", StringComparison.InvariantCulture) + 1;
            string s = fromDisk ? fileName[index..].Split('.')[0] : fileName.Split('.')[3];
            string[] p = s.Split('_');
            HashSet<string> options = new(p);
            if (options.Contains("back") && options.Contains("flip"))
                backFlips[p[0]] = fileName;
            else if (options.Contains("climb"))
                climbs[p[0]] = fileName;
            else if (options.Contains("back"))
                backs[p[0]] = fileName;
            else if (options.Contains("flip"))
                flips[p[0]] = fileName;
            else
                fronts[p[0]] = new CustomHat
                {
                    Resource = fileName,
                    Name = p[0].Replace('-', ' '),
                    Bounce = options.Contains("bounce"),
                    Adaptive = options.Contains("adaptive"),
                    Behind = options.Contains("behind")
                };
        }

        List<CustomHat> hats = new();

        foreach (KeyValuePair<string, CustomHat> frontKvP in fronts)
        {
            string k = frontKvP.Key;
            CustomHat hat = frontKvP.Value;
            backs.TryGetValue(k, out string backResource);
            climbs.TryGetValue(k, out string climbResource);
            flips.TryGetValue(k, out string flipResource);
            backFlips.TryGetValue(k, out string backFlipResource);
            if (backResource != null) hat.BackResource = backResource;
            if (climbResource != null) hat.ClimbResource = climbResource;
            if (flipResource != null) hat.FlipResource = flipResource;
            if (backFlipResource != null) hat.BackFlipResource = backFlipResource;
            if (hat.BackResource != null) hat.Behind = true;
            hats.Add(hat);
        }

        return hats;
    }

    internal static List<CustomHat> SanitizeHats(SkinsConfigFile response)
    {
        foreach (CustomHat hat in response.Hats)
        {
            hat.Resource = SanitizeFileName(hat.Resource);
            hat.BackResource = SanitizeFileName(hat.BackResource);
            hat.ClimbResource = SanitizeFileName(hat.ClimbResource);
            hat.FlipResource = SanitizeFileName(hat.FlipResource);
            hat.BackFlipResource = SanitizeFileName(hat.BackFlipResource);
        }

        return response.Hats;
    }

    private static string SanitizeFileName(string path)
    {
        if (path == null || !path.EndsWith(".png")) return null;
        return path.Replace("\\", "")
            .Replace("/", "")
            .Replace("*", "")
            .Replace("..", "");
    }

    private static bool ResourceRequireDownload(string resFile, string resHash, HashAlgorithm algorithm)
    {
        string filePath = Path.Combine(HatsDirectory, resFile);
        if (resHash == null || !File.Exists(filePath)) return true;
        using FileStream stream = File.OpenRead(filePath);
        string hash = BitConverter.ToString(algorithm.ComputeHash(stream))
            .Replace("-", string.Empty)
            .ToLowerInvariant();
        return !resHash.Equals(hash);
    }

    internal static List<string> GenerateDownloadList(List<CustomHat> hats)
    {
        MD5 algorithm = MD5.Create();
        List<string> toDownload = new();

        foreach (CustomHat hat in hats)
        {
            List<Tuple<string, string>> files = new()
            {
                new Tuple<string, string>(hat.Resource, hat.ResHashA),
                new Tuple<string, string>(hat.BackResource, hat.ResHashB),
                new Tuple<string, string>(hat.ClimbResource, hat.ResHashC),
                new Tuple<string, string>(hat.FlipResource, hat.ResHashF),
                new Tuple<string, string>(hat.BackFlipResource, hat.ResHashBf)
            };
            foreach ((string fileName, string fileHash) in files)
                if (fileName != null && ResourceRequireDownload(fileName, fileHash, algorithm))
                    toDownload.Add(fileName);
        }

        return toDownload;
    }
}
