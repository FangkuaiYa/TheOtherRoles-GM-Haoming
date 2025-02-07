using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.UI.Button;
using Object = UnityEngine.Object;

namespace TheOtherRoles.Patches;

[HarmonyPatch]
public static class ClientOptionsPatch
{
    private static List<SelectionBehaviour> AllOptions = new();

    private static GameObject popUp;
    private static TextMeshPro titleText;

    private static ToggleButtonBehaviour moreOptions;
    private static List<ToggleButtonBehaviour> modButtons = new();
    private static TextMeshPro titleTextTitle;

    private static ToggleButtonBehaviour buttonPrefab;
    private static int page = 1;

    private static void setAllOptions()
    {
        AllOptions = new List<SelectionBehaviour>
        {
            new("streamerModeButton",
                () => TheOtherRolesPlugin.StreamerMode.Value = !TheOtherRolesPlugin.StreamerMode.Value,
                TheOtherRolesPlugin.StreamerMode.Value),
            new("ghostsSeeTasksButton",
                () => TORMapOptions.ghostsSeeTasks =
                    TheOtherRolesPlugin.GhostsSeeTasks.Value = !TheOtherRolesPlugin.GhostsSeeTasks.Value,
                TheOtherRolesPlugin.GhostsSeeTasks.Value),
            new("ghostsSeeVotesButton",
                () => TORMapOptions.ghostsSeeVotes =
                    TheOtherRolesPlugin.GhostsSeeVotes.Value = !TheOtherRolesPlugin.GhostsSeeVotes.Value,
                TheOtherRolesPlugin.GhostsSeeVotes.Value),
            new("ghostsSeeRolesButton",
                () => TORMapOptions.ghostsSeeRoles =
                    TheOtherRolesPlugin.GhostsSeeRoles.Value = !TheOtherRolesPlugin.GhostsSeeRoles.Value,
                TheOtherRolesPlugin.GhostsSeeRoles.Value),
            new("showRoleSummaryButton",
                () => TORMapOptions.showRoleSummary = TheOtherRolesPlugin.ShowRoleSummary.Value =
                    !TheOtherRolesPlugin.ShowRoleSummary.Value, TheOtherRolesPlugin.ShowRoleSummary.Value),
            new("hideNameplates", () =>
            {
                TORMapOptions.hideNameplates =
                    TheOtherRolesPlugin.HideNameplates.Value = !TheOtherRolesPlugin.HideNameplates.Value;
                MeetingHudPatch.nameplatesChanged = true;
                return TORMapOptions.hideNameplates;
            }, TheOtherRolesPlugin.HideNameplates.Value),
            new("showLighterDarker",
                () => TORMapOptions.showLighterDarker = TheOtherRolesPlugin.ShowLighterDarker.Value =
                    !TheOtherRolesPlugin.ShowLighterDarker.Value, TheOtherRolesPlugin.ShowLighterDarker.Value),
            new("hideTaskArrows",
                () => TORMapOptions.hideTaskArrows =
                    TheOtherRolesPlugin.HideTaskArrows.Value = !TheOtherRolesPlugin.HideTaskArrows.Value,
                TheOtherRolesPlugin.HideTaskArrows.Value),
            new("offlineHats",
                () => TORMapOptions.offlineHats =
                    TheOtherRolesPlugin.OfflineHats.Value = !TheOtherRolesPlugin.OfflineHats.Value,
                TheOtherRolesPlugin.OfflineHats.Value),
            new("hideFakeTasks",
                () => TORMapOptions.hideFakeTasks =
                    TheOtherRolesPlugin.HideFakeTasks.Value = !TheOtherRolesPlugin.HideFakeTasks.Value,
                TheOtherRolesPlugin.HideFakeTasks.Value),
            new("betterSabotageMap",
                () => TORMapOptions.betterSabotageMap = TheOtherRolesPlugin.BetterSabotageMap.Value =
                    !TheOtherRolesPlugin.BetterSabotageMap.Value, TheOtherRolesPlugin.BetterSabotageMap.Value),
            new("forceNormalSabotageMap",
                () => TORMapOptions.forceNormalSabotageMap = TheOtherRolesPlugin.ForceNormalSabotageMap.Value =
                    !TheOtherRolesPlugin.ForceNormalSabotageMap.Value,
                TheOtherRolesPlugin.ForceNormalSabotageMap.Value),
            new("transparentMap",
                () => TORMapOptions.transparentMap =
                    TheOtherRolesPlugin.TransparentMap.Value = !TheOtherRolesPlugin.TransparentMap.Value,
                TheOtherRolesPlugin.TransparentMap.Value),
            new("showChatNotifications",
            () => TORMapOptions.ShowChatNotifications =
            TheOtherRolesPlugin.ShowChatNotifications.Value = !TheOtherRolesPlugin.ShowChatNotifications.Value,
            TheOtherRolesPlugin.ShowChatNotifications.Value)
        };
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public static void MainMenuManager_StartPostfix(MainMenuManager __instance)
    {
        // Prefab for the title
        var go = new GameObject("TitleTextTOR");
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.fontSize = 4;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.transform.localPosition += Vector3.left * 0.2f;
        titleText = Object.Instantiate(tmp);
        titleText.gameObject.SetActive(false);
        Object.DontDestroyOnLoad(titleText);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
    public static void OptionsMenuBehaviour_StartPostfix(OptionsMenuBehaviour __instance)
    {
        if (!__instance.CensorChatButton) return;

        if (!popUp) CreateCustom(__instance);

        if (!buttonPrefab)
        {
            buttonPrefab = Object.Instantiate(__instance.CensorChatButton);
            Object.DontDestroyOnLoad(buttonPrefab);
            buttonPrefab.name = "CensorChatPrefab";
            buttonPrefab.gameObject.SetActive(false);
        }

        SetUpOptions();
        InitializeMoreButton(__instance);
    }

    private static void CreateCustom(OptionsMenuBehaviour prefab)
    {
        popUp = Object.Instantiate(prefab.gameObject);
        Object.DontDestroyOnLoad(popUp);
        var transform = popUp.transform;
        var pos = transform.localPosition;
        pos.z = -810f;
        transform.localPosition = pos;

        Object.Destroy(popUp.GetComponent<OptionsMenuBehaviour>());
        foreach (GameObject gObj in popUp.gameObject.GetAllChilds())
            if (gObj.name is not "Background" and not "CloseButton")
                Object.Destroy(gObj);

        popUp.SetActive(false);
    }

    private static void InitializeMoreButton(OptionsMenuBehaviour __instance)
    {
        //__instance.BackButton.transform.localPosition += Vector3.right * 1.8f;
        moreOptions = Object.Instantiate(buttonPrefab, __instance.CensorChatButton.transform.parent);
        moreOptions.transform.localPosition =
            __instance.CensorChatButton.transform.localPosition + (Vector3.down * 1.0f);

        moreOptions.gameObject.SetActive(true);
        moreOptions.Text.text = ModTranslation.getString("modOptionsText");
        PassiveButton moreOptionsButton = moreOptions.GetComponent<PassiveButton>();
        moreOptionsButton.OnClick = new ButtonClickedEvent();
        moreOptionsButton.OnClick.AddListener((Action)(() =>
        {
            bool closeUnderlying = false;
            if (!popUp) return;

            if (__instance.transform.parent &&
                __instance.transform.parent == FastDestroyableSingleton<HudManager>.Instance.transform)
            {
                popUp.transform.SetParent(FastDestroyableSingleton<HudManager>.Instance.transform);
                popUp.transform.localPosition = new Vector3(0, 0, -800f);
                closeUnderlying = true;
            }
            else
            {
                popUp.transform.SetParent(null);
                Object.DontDestroyOnLoad(popUp);
            }

            CheckSetTitle();
            RefreshOpen();
            if (closeUnderlying)
                __instance.Close();
        }));

        GameObject leaveGameButton = GameObject.Find("LeaveGameButton");
        if (leaveGameButton != null) leaveGameButton.transform.localPosition += Vector3.right * 1.3f;
    }

    private static void RefreshOpen()
    {
        popUp.gameObject.SetActive(false);
        popUp.gameObject.SetActive(true);
        SetUpOptions();
    }

    private static void CheckSetTitle()
    {
        if (!popUp || popUp.GetComponentInChildren<TextMeshPro>() || !titleText) return;

        TextMeshPro title = titleTextTitle = Object.Instantiate(titleText, popUp.transform);
        title.GetComponent<RectTransform>().localPosition = Vector3.up * 2.3f;
        title.gameObject.SetActive(true);
        title.text = ModTranslation.getString("moreOptionsText");
        title.name = "TitleText";
    }

    private static void SetUpOptions()
    {
        // if (popUp.transform.GetComponentInChildren<ToggleButtonBehaviour>()) return;
        setAllOptions();

        foreach (ToggleButtonBehaviour button in modButtons)
            if (button != null)
                GameObject.Destroy(button.gameObject);

        modButtons = new List<ToggleButtonBehaviour>();
        int length = page * 10 < AllOptions.Count ? page * 10 : AllOptions.Count;

        for (int i = 0; i + ((page - 1) * 10) < length; i++)
        {
            SelectionBehaviour info = AllOptions[i + ((page - 1) * 10)];

            ToggleButtonBehaviour button = Object.Instantiate(buttonPrefab, popUp.transform);
            Vector3 pos = new(i % 2 == 0 ? -1.17f : 1.17f, 1.3f - (i / 2 * 0.8f), -.5f);

            Transform transform = button.transform;
            transform.localPosition = pos;

            button.onState = info.DefaultValue;
            button.Background.color = button.onState ? Color.green : Palette.ImpostorRed;

            button.Text.text = ModTranslation.getString(info.Title);
            button.Text.fontSizeMin = button.Text.fontSizeMax = 1.8f;
            button.Text.font = Object.Instantiate(titleText.font);
            button.Text.GetComponent<RectTransform>().sizeDelta = new Vector2(2, 2);

            button.name = info.Title.Replace(" ", "") + "Toggle";
            button.gameObject.SetActive(true);

            PassiveButton passiveButton = button.GetComponent<PassiveButton>();
            BoxCollider2D colliderButton = button.GetComponent<BoxCollider2D>();

            colliderButton.size = new Vector2(2.2f, .7f);

            passiveButton.OnClick = new ButtonClickedEvent();
            passiveButton.OnMouseOut = new UnityEvent();
            passiveButton.OnMouseOver = new UnityEvent();

            passiveButton.OnClick.AddListener((Action)(() =>
            {
                button.onState = info.OnClick();
                button.Background.color = button.onState ? Color.green : Palette.ImpostorRed;
            }));

            passiveButton.OnMouseOver.AddListener((Action)(() =>
                button.Background.color = new Color32(34, 139, 34, byte.MaxValue)));
            passiveButton.OnMouseOut.AddListener((Action)(() =>
                button.Background.color = button.onState ? Color.green : Palette.ImpostorRed));

            foreach (SpriteRenderer spr in button.gameObject.GetComponentsInChildren<SpriteRenderer>())
                spr.size = new Vector2(2.2f, .7f);

            modButtons.Add(button);
        }

        // ページ移動ボタンを追加
        if (page * 10 < AllOptions.Count)
        {
            ToggleButtonBehaviour button = Object.Instantiate(buttonPrefab, popUp.transform);
            Vector3 pos = new(1.2f, -2.5f, -0.5f);
            Transform transform = button.transform;
            transform.localPosition = pos;
            button.Text.text = ModTranslation.getString("next");
            button.Text.fontSizeMin = button.Text.fontSizeMax = 2.2f;
            button.Text.font = Object.Instantiate(titleText.font);
            button.Text.GetComponent<RectTransform>().sizeDelta = new Vector2(2, 2);
            button.gameObject.SetActive(true);
            PassiveButton passiveButton = button.GetComponent<PassiveButton>();
            BoxCollider2D colliderButton = button.GetComponent<BoxCollider2D>();
            colliderButton.size = new Vector2(2.2f, .7f);
            passiveButton.OnClick = new ButtonClickedEvent();
            passiveButton.OnMouseOut = new UnityEvent();
            passiveButton.OnMouseOver = new UnityEvent();
            passiveButton.OnClick.AddListener((Action)(() =>
            {
                page += 1;
                SetUpOptions();
            }));
            modButtons.Add(button);
        }

        if (page > 1)
        {
            ToggleButtonBehaviour button = Object.Instantiate(buttonPrefab, popUp.transform);
            Vector3 pos = new(-1.2f, -2.5f, -0.5f);
            Transform transform = button.transform;
            transform.localPosition = pos;
            button.Text.text = ModTranslation.getString("previous");
            button.Text.fontSizeMin = button.Text.fontSizeMax = 2.2f;
            button.Text.font = Object.Instantiate(titleText.font);
            button.Text.GetComponent<RectTransform>().sizeDelta = new Vector2(2, 2);
            button.gameObject.SetActive(true);
            PassiveButton passiveButton = button.GetComponent<PassiveButton>();
            BoxCollider2D colliderButton = button.GetComponent<BoxCollider2D>();
            colliderButton.size = new Vector2(2.2f, .7f);
            passiveButton.OnClick = new ButtonClickedEvent();
            passiveButton.OnMouseOut = new UnityEvent();
            passiveButton.OnMouseOver = new UnityEvent();
            passiveButton.OnClick.AddListener((Action)(() =>
            {
                page -= 1;
                SetUpOptions();
            }));
            modButtons.Add(button);
        }
    }

    private static IEnumerable<GameObject> GetAllChilds(this GameObject Go)
    {
        for (int i = 0; i < Go.transform.childCount; i++) yield return Go.transform.GetChild(i).gameObject;
    }

    public static void updateTranslations()
    {
        if (titleTextTitle)
            titleTextTitle.text = ModTranslation.getString("moreOptionsText");

        if (moreOptions)
            moreOptions.Text.text = ModTranslation.getString("modOptionsText");

        for (int i = 0; i < AllOptions.Count; i++)
        {
            if (i >= modButtons.Count) break;
            modButtons[i].Text.text = ModTranslation.getString(AllOptions[i].Title);
        }
    }

    private class SelectionBehaviour
    {
        public readonly bool DefaultValue;
        public readonly Func<bool> OnClick;
        public readonly string Title;

        public SelectionBehaviour(string title, Func<bool> onClick, bool defaultValue)
        {
            Title = title;
            OnClick = onClick;
            DefaultValue = defaultValue;
        }
    }
}

[HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.SetText))]
public static class HiddenTextPatch
{
    private static void Postfix(TextBoxTMP __instance)
    {
        bool flag = TheOtherRolesPlugin.StreamerMode.Value && (__instance.name == "GameIdText" ||
                                                               __instance.name == "IpTextBox" ||
                                                               __instance.name == "PortTextBox");
        if (flag) __instance.outputText.text = new string('*', __instance.text.Length);
    }
}
