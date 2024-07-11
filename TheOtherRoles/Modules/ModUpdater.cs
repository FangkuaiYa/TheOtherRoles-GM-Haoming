using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using AmongUs.Data;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Mono.Cecil;
using Newtonsoft.Json.Linq;
using TMPro;
using Twitch;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Action = System.Action;
using IntPtr = System.IntPtr;
using Version = SemanticVersioning.Version;

namespace TheOtherRoles.Modules
{
    public class ModUpdateBehaviour : MonoBehaviour
    {
        public static readonly bool CheckForSubmergedUpdates = true;
        public static bool showPopUp = true;
        public static bool updateInProgress = false;

        public static ModUpdateBehaviour Instance { get; private set; }
        public ModUpdateBehaviour(IntPtr ptr) : base(ptr) { }
        public class UpdateData
        {
            public string Content;
            public string Tag;
            public JObject Request;
            public Version Version => Version.Parse(Tag);

            public UpdateData(JObject data)
            {
                Tag = data["tag_name"]?.ToString().TrimStart('v');
                Content = data["body"]?.ToString();
                Request = data;
            }

            public bool IsNewer(Version version)
            {
                if (!Version.TryParse(Tag, out var myVersion)) return false;
                return myVersion.BaseVersion() > version.BaseVersion();
            }
        }

        public UpdateData TORUpdate;
        public UpdateData SubmergedUpdate;

        [HideFromIl2Cpp]
        public UpdateData RequiredUpdateData => TORUpdate ?? SubmergedUpdate;

        public void Awake()
        {
            if (Instance) Destroy(this);
            Instance = this;

            SceneManager.add_sceneLoaded((System.Action<Scene, LoadSceneMode>)(OnSceneLoaded));
            this.StartCoroutine(CoCheckUpdates());

            foreach (var file in Directory.GetFiles(Paths.PluginPath, "*.old"))
            {
                File.Delete(file);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (updateInProgress || scene.name != "MainMenu") return;
            if (RequiredUpdateData is null)
            {
                showPopUp = false;
                return;
            }

            if (TheOtherRolesPlugin.DebugMode.Value) FastDestroyableSingleton<EOSManager>.Instance.PlayOffline();
            AssetLoader.LoadAssets();
            CustomHatLoader.LaunchHatFetcher();
            var template = GameObject.Find("ExitGameButton");

            // Discrodボタン
            var buttonDiscord = UnityEngine.Object.Instantiate(template, null);
            buttonDiscord.transform.localPosition = new Vector3(buttonDiscord.transform.localPosition.x, buttonDiscord.transform.localPosition.y + 0.6f, buttonDiscord.transform.localPosition.z);

            var textDiscord = buttonDiscord.transform.GetChild(0).GetComponent<TMPro.TMP_Text>();
            Instance.StartCoroutine(Effects.Lerp(0.1f, new System.Action<float>((p) =>
            {
                textDiscord.SetText("Discord");
            })));

            PassiveButton passiveButtonDiscord = buttonDiscord.GetComponent<PassiveButton>();
            SpriteRenderer buttonSpriteDiscord = buttonDiscord.GetComponent<SpriteRenderer>();

            passiveButtonDiscord.OnClick = new Button.ButtonClickedEvent();
            passiveButtonDiscord.OnClick.AddListener((System.Action)(() => Application.OpenURL("https://discord.gg/sTt8EzEpHP")));

            Color discordColor = new Color32(88, 101, 242, byte.MaxValue);
            buttonSpriteDiscord.color = textDiscord.color = discordColor;
            passiveButtonDiscord.OnMouseOut.AddListener((System.Action)delegate
            {
                buttonSpriteDiscord.color = textDiscord.color = discordColor;
            });

            // Twitterボタン
            var buttonTwitter = UnityEngine.Object.Instantiate(template, null);
            buttonTwitter.transform.localPosition = new Vector3(buttonTwitter.transform.localPosition.x, buttonTwitter.transform.localPosition.y + 1.2f, buttonTwitter.transform.localPosition.z);

            var textTwitter = buttonTwitter.transform.GetChild(0).GetComponent<TMPro.TMP_Text>();
            Instance.StartCoroutine(Effects.Lerp(0.1f, new System.Action<float>((p) =>
            {
                textTwitter.SetText("Twitter");
            })));

            PassiveButton passiveButtonTwitter = buttonTwitter.GetComponent<PassiveButton>();
            SpriteRenderer buttonSpriteTwitter = buttonTwitter.GetComponent<SpriteRenderer>();

            passiveButtonTwitter.OnClick = new Button.ButtonClickedEvent();
            passiveButtonTwitter.OnClick.AddListener((System.Action)(() => Application.OpenURL("https://twitter.com/haoming_dev")));

            Color twitterColor = new Color32(29, 161, 242, byte.MaxValue);
            buttonSpriteTwitter.color = textTwitter.color = twitterColor;
            passiveButtonTwitter.OnMouseOut.AddListener((System.Action)delegate
            {
                buttonSpriteTwitter.color = textTwitter.color = twitterColor;
            });

            var button = Instantiate(template, null);
            var buttonTransform = button.transform;
            var pos = buttonTransform.localPosition;
            pos.y += 1.2f;
            buttonTransform.localPosition = pos;

            PassiveButton passiveButton = button.GetComponent<PassiveButton>();
            SpriteRenderer buttonSprite = button.GetComponent<SpriteRenderer>();
            passiveButton.OnClick = new Button.ButtonClickedEvent();
            passiveButton.OnClick.AddListener((Action)(() =>
            {
                this.StartCoroutine(CoUpdate());
                button.SetActive(false);
            }));

            var text = button.transform.GetChild(0).GetComponent<TMP_Text>();
            string t = "Update";
            if (TORUpdate is null && SubmergedUpdate is not null) t = SubmergedCompatibility.Loaded ? $"Update\nSubmerged" : $"Download\nSubmerged";

            StartCoroutine(Effects.Lerp(0.1f, (System.Action<float>)(p => text.SetText(t))));

            buttonSprite.color = text.color = Color.red;
            passiveButton.OnMouseOut.AddListener((Action)(() => buttonSprite.color = text.color = Color.red));

            var isSubmerged = TORUpdate == null;
            var announcement = $"<size=150%>A new <color=#FC0303>{(isSubmerged ? "Submerged" : "THE OTHER ROLES")}</color> update to {(isSubmerged ? SubmergedUpdate.Tag : TORUpdate.Tag)} is available</size>\n{(isSubmerged ? SubmergedUpdate.Content : TORUpdate.Content)}";
            var mgr = FindObjectOfType<MainMenuManager>(true);

            if (!isSubmerged)
            {
                try
                {
                    string updateVersion = TORUpdate.Content[^5..];
                    if (Version.Parse(TheOtherRolesPlugin.VersionString).BaseVersion() < Version.Parse(updateVersion).BaseVersion())
                    {
                        passiveButton.OnClick.RemoveAllListeners();
                        passiveButton.OnClick = new Button.ButtonClickedEvent();
                        passiveButton.OnClick.AddListener((Action)(() => {
                            mgr.StartCoroutine(CoShowAnnouncement($"<size=150%><color=#FC0303>A MANUAL UPDATE IS REQUIRED</color></size>"));
                        }));
                    }
                }
                catch
                {
                    TheOtherRolesPlugin.Logger.LogError("parsing version for auto updater failed :(");
                }

            }

            if (isSubmerged && !SubmergedCompatibility.Loaded) showPopUp = false;
            if (showPopUp) mgr.StartCoroutine(CoShowAnnouncement(announcement));
            showPopUp = false;
        }

        [HideFromIl2Cpp]
        public IEnumerator CoUpdate()
        {
            updateInProgress = true;
            var isSubmerged = TORUpdate is null;
            var updateName = (isSubmerged ? "Submerged" : "The Other Roles");

            var popup = Instantiate(TwitchManager.Instance.TwitchPopup);
            popup.TextAreaTMP.fontSize *= 0.7f;
            popup.TextAreaTMP.enableAutoSizing = false;

            popup.Show();

            var button = popup.transform.GetChild(2).gameObject;
            button.SetActive(false);
            popup.TextAreaTMP.text = $"Updating {updateName}\nPlease wait...";

            var download = Task.Run(DownloadUpdate);
            while (!download.IsCompleted) yield return null;

            button.SetActive(true);
            popup.TextAreaTMP.text = download.Result ? $"{updateName}\nupdated successfully\nPlease restart the game." : "Update wasn't successful\nTry again later,\nor update manually.";
        }

        [HideFromIl2Cpp]
        public IEnumerator CoShowAnnouncement(string announcement)
        {
            var popUp = Instantiate(FindObjectOfType<AnnouncementPopUp>(true));
            popUp.gameObject.SetActive(true);
            yield return popUp.Init();
            var last = DataManager.Announcements.LastViewedAnnouncement;
            last.Id = 1;
            last.Text = announcement;
            SelectableHyperLinkHelper.DestroyGOs(popUp.selectableHyperLinks, name);
            popUp.AnnounceTextMeshPro.text = announcement;
        }

        [HideFromIl2Cpp]
        public static IEnumerator CoCheckUpdates()
        {
            var torUpdateCheck = Task.Run(() => Instance.GetGithubUpdate("Eisbison", "TheOtherRoles"));
            while (!torUpdateCheck.IsCompleted) yield return null;
            Announcement.updateData = torUpdateCheck.Result;
            if (torUpdateCheck.Result != null && torUpdateCheck.Result.IsNewer(Version.Parse(TheOtherRolesPlugin.VersionString)))
            {
                Instance.TORUpdate = torUpdateCheck.Result;
            }

            if (CheckForSubmergedUpdates)
            {
                var submergedUpdateCheck = Task.Run(() => Instance.GetGithubUpdate("SubmergedAmongUs", "Submerged"));
                while (!submergedUpdateCheck.IsCompleted) yield return null;
                if (submergedUpdateCheck.Result != null && (!SubmergedCompatibility.Loaded || submergedUpdateCheck.Result.IsNewer(SubmergedCompatibility.Version)))
                {
                    Instance.SubmergedUpdate = submergedUpdateCheck.Result;
                }
            }

            Instance.OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        [HarmonyPatch(typeof(AnnouncementPopUp), nameof(AnnouncementPopUp.UpdateAnnounceText))]
        public static class Announcement
        {
            public static ModUpdateBehaviour.UpdateData updateData = null;
            public static bool Prefix(AnnouncementPopUp __instance)
            {
                if (ModUpdateBehaviour.showPopUp || updateData == null) return true;

                var text = __instance.AnnounceTextMeshPro;
                text.text = $"<size=150%><color=#FC0303>THE OTHER ROLES GM Haoming </color> {(updateData.Tag)}\n{(updateData.Content)}";

                return false;
            }
        }
        [HideFromIl2Cpp]
        public async Task<UpdateData> GetGithubUpdate(string owner, string repo)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "TheOtherRoles Updater");

            var req = await client.GetAsync($"https://api.github.com/repos/{owner}/{repo}/releases/latest", HttpCompletionOption.ResponseContentRead);
            if (!req.IsSuccessStatusCode) return null;

            var dataString = await req.Content.ReadAsStringAsync();
            JObject data = JObject.Parse(dataString);
            return new UpdateData(data);
        }

        private bool TryUpdateSubmergedInternally()
        {
            if (SubmergedUpdate == null) return false;
            try
            {
                if (!SubmergedCompatibility.LoadedExternally) return false;
                var thisAsm = Assembly.GetCallingAssembly();
                var resourceName = thisAsm.GetManifestResourceNames().FirstOrDefault(s => s.EndsWith("Submerged.dll"));
                if (resourceName == default) return false;

                using var submergedStream = thisAsm.GetManifestResourceStream(resourceName)!;
                var asmDef = AssemblyDefinition.ReadAssembly(submergedStream, TypeLoader.ReaderParameters);
                var pluginType = asmDef.MainModule.Types.FirstOrDefault(t => t.IsSubtypeOf(typeof(BasePlugin)));
                var info = IL2CPPChainloader.ToPluginInfo(pluginType, "");
                if (SubmergedUpdate.IsNewer(info.Metadata.Version)) return false;
                File.Delete(SubmergedCompatibility.Assembly.Location);

            }
            catch (Exception e)
            {
                TheOtherRolesPlugin.Logger.LogError(e);
                return false;
            }
            return true;
        }


        [HideFromIl2Cpp]
        public async Task<bool> DownloadUpdate()
        {
            var isSubmerged = TORUpdate is null;
            if (isSubmerged && TryUpdateSubmergedInternally()) return true;
            var data = isSubmerged ? SubmergedUpdate : TORUpdate;

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "TheOtherRoles Updater");

            JToken assets = data.Request["assets"];
            string downloadURI = "";
            for (JToken current = assets.First; current != null; current = current.Next)
            {
                string browser_download_url = current["browser_download_url"]?.ToString();
                if (browser_download_url != null && current["content_type"] != null)
                {
                    if (current["content_type"].ToString().Equals("application/x-msdownload") &&
                        browser_download_url.EndsWith(".dll"))
                    {
                        downloadURI = browser_download_url;
                        break;
                    }
                }
            }

            if (downloadURI.Length == 0) return false;

            var res = await client.GetAsync(downloadURI, HttpCompletionOption.ResponseContentRead);
            string filePath = Path.Combine(Paths.PluginPath, isSubmerged ? "Submerged.dll" : "TheOtherRoles.dll");
            if (File.Exists(filePath + ".old")) File.Delete(filePath + ".old");
            if (File.Exists(filePath)) File.Move(filePath, filePath + ".old");

            await using var responseStream = await res.Content.ReadAsStreamAsync();
            await using var fileStream = File.Create(filePath);
            await responseStream.CopyToAsync(fileStream);

            return true;
        }
    }
}
