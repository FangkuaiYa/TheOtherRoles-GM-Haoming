using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using AmongUs.Data;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using TheOtherRoles.Patches;

namespace TheOtherRoles;

public class ModTranslation
{
    private const string blankText = "[BLANK]";
    public static int defaultLanguage = (int)SupportedLangs.English;
    public static Dictionary<string, Dictionary<int, string>> stringData;

    public static void Load()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        Stream stream = assembly.GetManifestResourceStream("TheOtherRoles.Resources.stringData.json");
        byte[] byteArray = new byte[stream.Length];
        int read = stream.Read(byteArray, 0, (int)stream.Length);
        string json = Encoding.UTF8.GetString(byteArray);

        stringData = new Dictionary<string, Dictionary<int, string>>();
        JObject parsed = JObject.Parse(json);

        for (int i = 0; i < parsed.Count; i++)
        {
            JProperty token = parsed.ChildrenTokens[i].TryCast<JProperty>();
            if (token == null) continue;

            string stringName = token.Name;
            JObject val = token.Value.TryCast<JObject>();

            if (token.HasValues)
            {
                Dictionary<int, string> strings = new();

                for (int j = 0; j < (int)SupportedLangs.Irish + 1; j++)
                {
                    string key = j.ToString();
                    string text = val[key]?.TryCast<JValue>().Value.ToString();

                    if (text != null && text.Length > 0)
                    {
                        if (text == blankText) strings[j] = "";
                        else strings[j] = text;
                    }
                }

                stringData[stringName] = strings;
            }
        }

        //TheOtherRolesPlugin.Instance.Log.LogInfo($"Language: {stringData.Keys}");
    }

    public static string getString(string key, string def = null)
    {
        // Strip out color tags.
        string keyClean = Regex.Replace(key, "<.*?>", "");
        keyClean = Regex.Replace(keyClean, "^-\\s*", "");
        keyClean = keyClean.Trim();

        def ??= key;
        if (!stringData.ContainsKey(keyClean)) return def;

        Dictionary<int, string> data = stringData[keyClean];
        int lang = (int)DataManager.Settings.Language.CurrentLanguage;

        if (data.ContainsKey(lang)) return key.Replace(keyClean, data[lang]);

        if (data.ContainsKey(defaultLanguage)) return key.Replace(keyClean, data[defaultLanguage]);

        return key;
    }
}

[HarmonyPatch(typeof(LanguageSetter), nameof(LanguageSetter.SetLanguage))]
internal class SetLanguagePatch
{
    private static void Postfix()
    {
        ClientOptionsPatch.updateTranslations();
    }
}
