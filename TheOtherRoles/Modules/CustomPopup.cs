using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TheOtherRoles.Modules;

// https://github.com/XtremeWave/TownOfNewEpic_Xtreme/blob/TONEX/TONEX/Modules/CustomPopup.cs
public static class CustomPopup
{
    public static GameObject Fill;
    public static GameObject InfoScreen;

    public static TextMeshPro TitleTMP;
    public static TextMeshPro InfoTMP;

    public static PassiveButton ActionButtonPrefab;


    public static GameObject FillTemp;
    public static GameObject InfoScreenTemp;

    public static TextMeshPro TitleTMPTemp;
    public static TextMeshPro InfoTMPTemp;

    public static PassiveButton ActionButtonPrefabTemp;

    public static List<PassiveButton> ActionButtons;

    private static bool busy;
    private static (string title, string info, List<(string, Action)> buttons)? waitToShow;

    private static string waitToUpdateText = string.Empty;

    /// <summary>
    ///     显示一个全屏信息显示界面
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="info">内容</param>
    /// <param name="buttons">按钮（文字，点击事件）</param>
    public static void Show(string title, string info, List<(string, Action)> buttons)
    {
        if (busy || Fill == null || InfoScreen == null || ActionButtonPrefab == null || TitleTMP == null ||
            InfoTMP == null) Init();

        busy = true;

        TitleTMP.text = title;
        InfoTMP.text = info;

        ActionButtons?.Do(b => Object.Destroy(b.gameObject));
        ActionButtons = new List<PassiveButton>();

        if (buttons != null)
            foreach ((string, Action) buttonInfo in buttons.Where(b => b.Item1?.Trim() is not null and not ""))
            {
                (string text, Action action) = buttonInfo;
                PassiveButton button = Object.Instantiate(ActionButtonPrefab, InfoScreen.transform);
                TextMeshPro tmp = button.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>();
                tmp.text = text;
                button.OnClick = new Button.ButtonClickedEvent();
                button.OnClick.AddListener((Action)(() =>
                {
                    InfoScreen.SetActive(false);
                    Fill.SetActive(false);
                }));
                if (action != null) button.OnClick.AddListener(action);
                button.transform.SetLocalX(0);
                button.gameObject.SetActive(true);
                ActionButtons.Add(button);
            }

        if (ActionButtons.Count > 1)
        {
            float widthSum = ActionButtons.Count * ActionButtonPrefab.gameObject.GetComponent<BoxCollider2D>().size.x;
            widthSum += (ActionButtons.Count - 1) * 0.1f;
            float start = -Math.Abs(widthSum / 2);
            float each = widthSum / ActionButtons.Count;
            int index = 0;
            foreach (PassiveButton button in ActionButtons)
            {
                button.transform.SetLocalX(start + (each * (index + 0.5f)));
                index++;
            }
        }

        Fill.SetActive(true);
        InfoScreen.SetActive(true);

        busy = false;
    }

    public static void ShowLater(string title, string info, List<(string, Action)> buttons)
    {
        waitToShow = (title, info, buttons);
    }

    public static void UpdateTextLater(string info)
    {
        waitToUpdateText = info;
    }

    public static void Update()
    {
        if (waitToShow != null)
        {
            Show(waitToShow.Value.title, waitToShow.Value.info, waitToShow.Value.buttons);
            waitToShow = null;
        }

        if (!string.IsNullOrEmpty(waitToUpdateText))
        {
            InfoTMP?.SetText(waitToUpdateText);
            waitToUpdateText = string.Empty;
        }
    }

    public static void Init()
    {
        Transform DOBScreen = AccountManager.Instance.transform.FindChild("DOBEnterScreen");
        if (DOBScreen != null && (Fill == null || InfoScreen == null || ActionButtons == null))
        {
            if (Fill == null && FillTemp != null)
                Fill = FillTemp;
            else
            {
                Fill = Object.Instantiate(DOBScreen.FindChild("Fill").gameObject);
                FillTemp = Fill;
            }

            Fill.transform.SetLocalZ(-100f);
            Fill.name = "GMH Info Popup Fill";
            Fill.SetActive(false);

            InfoScreen = Object.Instantiate(DOBScreen.FindChild("InfoPage").gameObject);
            InfoScreen.transform.SetLocalZ(-110f);
            InfoScreen.name = "GMH Info Popup Page";
            InfoScreen.SetActive(false);

            TitleTMP = InfoScreen.transform.FindChild("Title Text").GetComponent<TextMeshPro>();
            TitleTMP.transform.localPosition = new Vector3(0f, 2.3f, 3f);
            TitleTMP.DestroyTranslator();
            TitleTMP.text = "";

            InfoTMP = InfoScreen.transform.FindChild("InfoText_TMP").GetComponent<TextMeshPro>();
            InfoTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(7f, 1.3f);
            InfoTMP.transform.localScale = new Vector3(1f, 1f, 1f);
            InfoTMP.DestroyTranslator();
            InfoTMP.text = "";

            ActionButtonPrefab = InfoScreen.transform.FindChild("BackButton").GetComponent<PassiveButton>();
            ActionButtonPrefab.gameObject.name = "ActionButtonPrefab";
            ActionButtonPrefab.transform.localScale = new Vector3(0.66f, 0.66f, 0.66f);
            ActionButtonPrefab.transform.localPosition = new Vector3(0f, -0.65f, 3f);
            ActionButtonPrefab.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>().DestroyTranslator();
            ActionButtonPrefab.gameObject.SetActive(false);
        }
    }
}
