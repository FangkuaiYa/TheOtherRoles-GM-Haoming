using System;
using System.Collections.Generic;
using AsmResolver.Collections;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TheOtherRoles.Modules
{
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    public class ModUpdaterButton
    {
        public static bool openFirst = true;
        private static void Prefix(MainMenuManager __instance)
        {
            var template = GameObject.Find("ExitGameButton");

            var menuobj = Object.Instantiate(template, null);
            Object.Destroy(menuobj.GetComponent<AspectPosition>());
            menuobj.transform.localPosition = new(4.4473f, -1.7764f, 0);

            var menubutton = menuobj.GetComponentInChildren<TextMeshPro>();
            menubutton.transform.localPosition = new(4.4473f, -1.7764f, 0);
            menubutton.alignment = TextAlignmentOptions.Right;
            __instance.StartCoroutine(Effects.Lerp(0.1f, new System.Action<float>((p) =>
            {
                menubutton.SetText(AmongUs.Data.DataManager.Settings.Language.CurrentLanguage == SupportedLangs.SChinese ? "联系与反馈" : "GITHUB");
            })));

            PassiveButton passiveButtonmenu = menuobj.GetComponent<PassiveButton>();
            SpriteRenderer buttonSpritemenu = menuobj.transform.FindChild("Inactive").GetComponent<SpriteRenderer>();

            passiveButtonmenu.OnClick = new Button.ButtonClickedEvent();
            passiveButtonmenu.OnClick.AddListener((Action)(() => Application.OpenURL(AmongUs.Data.DataManager.Settings.Language.CurrentLanguage == SupportedLangs.SChinese ? "https://qm.qq.com/q/CfsaQuYZBm" : "https://github.com/FangKuaiYa/TheOtherRoles-GM-Haoming")));
            Color menuColor = Color.cyan;
            buttonSpritemenu.color = menubutton.color = menuColor;
            passiveButtonmenu.OnMouseOut.AddListener((Action)delegate
            {
                buttonSpritemenu.color = menubutton.color = menuColor;
            });
        }
        public static string contributorsText = AmongUs.Data.DataManager.Settings.Language.CurrentLanguage == SupportedLangs.SChinese ? $@"
非常感谢您游玩TheOtherRoles-GM-Haoming最新适配版
非常感谢Haoming的开发团队和为TheOtherRoles-GM-Haoming做出贡献的人
适配过程中游戏崩溃数次，适配时长长达四天，看在我这么努力的份上不得去
哔哩哔哩搜索“方块geigei”点个关注
FangKuai - 主要适配人员
杰哥(这个名字竟然值6个硬币) - 场外指导和代码支持
Imp11 - 技术指导和代码支持
沫夏悠轩 - 技术指导
Slok7565 - 代码支持
" : $@"
Thanks to the team at Haoming and all those who have contributed to TheOtherRoles-GM-Haoming
FangKuai - Main update personnel
JieGeLoversDengDuaLang - Technical and code support
Imp11 - Technical and code support
mxyx-club - Technical support
Slok7565 - Code support
";
        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix]
        public static void Start_Postfix(MainMenuManager __instance)
        {
            if (openFirst)
            {
                CustomPopup.Show(ModTranslation.getString("contributorsText"), contributorsText, new List<(string, Action)> { (DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.Okay), null) });
                openFirst = false;
            }
        }
    }
}
