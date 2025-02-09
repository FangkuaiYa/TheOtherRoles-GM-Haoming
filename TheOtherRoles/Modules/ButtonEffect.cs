using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using Rewired;
using UnityEngine;

namespace TheOtherRoles.Modules;

public static class ButtonEffect
{
    private static readonly Image keyBindBackgroundSprite =
        SpriteLoader.FromResource("TheOtherRoles.Resources.KeyBindBackground.png", 100f);

    private static Image mouseDisableActionSprite =
        SpriteLoader.FromResource("TheOtherRoles.Resources.MouseActionDisableIcon.png", 100f);

    public static GameObject AddKeyGuide(GameObject button, KeyCode key, Vector2 pos, bool removeExistingGuide,
        string action = null)
    {
        if (removeExistingGuide)
            button.gameObject.ForEachChild((Action<GameObject>)(obj =>
            {
                if (obj.name == "HotKeyGuide") GameObject.Destroy(obj);
            }));

        Sprite numSprite = null;
        if (KeyCodeInfo.AllKeyInfo.ContainsKey(key)) numSprite = KeyCodeInfo.AllKeyInfo[key].Sprite;
        if (numSprite == null) return null;

        GameObject obj = new();
        obj.name = "HotKeyGuide";
        obj.transform.SetParent(button.transform);
        obj.layer = button.layer;
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.transform.localPosition = (Vector3)pos + new Vector3(0f, 0f, -10f);
        renderer.sprite = keyBindBackgroundSprite.GetSprite();

        GameObject numObj = new();
        numObj.name = "HotKeyText";
        numObj.transform.SetParent(obj.transform);
        numObj.layer = button.layer;
        renderer = numObj.AddComponent<SpriteRenderer>();
        renderer.transform.localPosition = new Vector3(0, 0, -1f);
        renderer.sprite = numSprite;

        return obj;
    }

    public static GameObject SetKeyGuide(GameObject button, KeyCode key, bool removeExistingGuide = true,
        string action = null)
    {
        return AddKeyGuide(button, key, new Vector2(0.48f, 0.48f), removeExistingGuide, action);
    }

    public static void ShowVanillaKeyGuide(this HudManager manager)
    {
        //ボタンのガイドを表示
        KeyboardMap keyboardMap = ReInput.mapping.GetKeyboardMapInstanceSavedOrDefault(0, 0, 0);
        Il2CppReferenceArray<ActionElementMap> actionArray;
        ActionElementMap actionMap;

        //マップ
        actionArray = keyboardMap.GetButtonMapsWithAction(4);
        if (actionArray.Count > 0)
        {
            actionMap = actionArray[0];
            SetKeyGuide(HudManager.Instance.SabotageButton.gameObject, actionMap.keyCode,
                action: TranslationController.Instance.GetString(StringNames.SabotageLabel).camelString());
        }

        //使用
        actionArray = keyboardMap.GetButtonMapsWithAction(6);
        if (actionArray.Count > 0)
        {
            actionMap = actionArray[0];
            SetKeyGuide(HudManager.Instance.UseButton.gameObject, actionMap.keyCode,
                action: TranslationController.Instance.GetString(StringNames.UseLabel).camelString());
            SetKeyGuide(HudManager.Instance.PetButton.gameObject, actionMap.keyCode,
                action: TranslationController.Instance.GetString(StringNames.PetLabel).camelString());
        }

        //レポート
        actionArray = keyboardMap.GetButtonMapsWithAction(7);
        if (actionArray.Count > 0)
        {
            actionMap = actionArray[0];
            SetKeyGuide(HudManager.Instance.ReportButton.gameObject, actionMap.keyCode,
                action: TranslationController.Instance.GetString(StringNames.ReportLabel).camelString());
        }

        //キル
        actionArray = keyboardMap.GetButtonMapsWithAction(8);
        if (actionArray.Count > 0)
        {
            actionMap = actionArray[0];
            SetKeyGuide(HudManager.Instance.KillButton.gameObject, actionMap.keyCode,
                action: TranslationController.Instance.GetString(StringNames.KillLabel).camelString());
        }

        //ベント
        actionArray = keyboardMap.GetButtonMapsWithAction(50);
        if (actionArray.Count > 0)
        {
            actionMap = actionArray[0];
            SetKeyGuide(HudManager.Instance.ImpostorVentButton.gameObject, actionMap.keyCode,
                action: TranslationController.Instance.GetString(StringNames.VentLabel).camelString());
        }
    }

    public class KeyCodeInfo
    {
        public static Dictionary<KeyCode, KeyCodeInfo> AllKeyInfo = new();

        static KeyCodeInfo()
        {
            DividedSpriteLoader spriteLoader;
            spriteLoader =
                DividedSpriteLoader.FromResource("TheOtherRoles.Resources.KeyBindCharacters0.png", 100f, 18, 19, true);
            new KeyCodeInfo(KeyCode.Tab, "Tab", spriteLoader, 0);
            new KeyCodeInfo(KeyCode.Space, "Space", spriteLoader, 1);
            new KeyCodeInfo(KeyCode.Comma, "<", spriteLoader, 2);
            new KeyCodeInfo(KeyCode.Period, ">", spriteLoader, 3);
            spriteLoader =
                DividedSpriteLoader.FromResource("TheOtherRoles.Resources.KeyBindCharacters1.png", 100f, 18, 19, true);
            for (KeyCode key = KeyCode.A; key <= KeyCode.Z; key++)
                new KeyCodeInfo(key, ((char)('A' + key - KeyCode.A)).ToString(), spriteLoader, key - KeyCode.A);
            spriteLoader =
                DividedSpriteLoader.FromResource("TheOtherRoles.Resources.KeyBindCharacters2.png", 100f, 18, 19, true);
            for (int i = 0; i < 15; i++)
                new KeyCodeInfo(KeyCode.F1 + i, "F" + (i + 1), spriteLoader, i);
            spriteLoader =
                DividedSpriteLoader.FromResource("TheOtherRoles.Resources.KeyBindCharacters3.png", 100f, 18, 19, true);
            new KeyCodeInfo(KeyCode.RightShift, "RShift", spriteLoader, 0);
            new KeyCodeInfo(KeyCode.LeftShift, "LShift", spriteLoader, 1);
            new KeyCodeInfo(KeyCode.RightControl, "RControl", spriteLoader, 2);
            new KeyCodeInfo(KeyCode.LeftControl, "LControl", spriteLoader, 3);
            new KeyCodeInfo(KeyCode.RightAlt, "RAlt", spriteLoader, 4);
            new KeyCodeInfo(KeyCode.LeftAlt, "LAlt", spriteLoader, 5);
            spriteLoader =
                DividedSpriteLoader.FromResource("TheOtherRoles.Resources.KeyBindCharacters4.png", 100f, 18, 19, true);
            for (int i = 0; i < 6; i++)
                new KeyCodeInfo(KeyCode.Mouse1 + i,
                    "Mouse " + (i == 0 ? "Right" : i == 1 ? "Middle" : (i + 1).ToString()), spriteLoader, i);
            spriteLoader =
                DividedSpriteLoader.FromResource("TheOtherRoles.Resources.KeyBindCharacters5.png", 100f, 18, 19, true);
            for (int i = 0; i < 10; i++)
                new KeyCodeInfo(KeyCode.Alpha0 + i, "0" + (i + 1), spriteLoader, i);
        }

        public KeyCodeInfo(KeyCode keyCode, string translationKey, DividedSpriteLoader spriteLoader, int num)
        {
            this.keyCode = keyCode;
            TranslationKey = translationKey;
            textureHolder = spriteLoader;
            this.num = num;

            AllKeyInfo.Add(keyCode, this);
        }

        public KeyCode keyCode { get; private set; }
        public DividedSpriteLoader textureHolder { get; }
        public int num { get; }
        public string TranslationKey { get; }

        public Sprite Sprite => textureHolder.GetSprite(num);

        public static string GetKeyDisplayName(KeyCode keyCode)
        {
            if (keyCode == KeyCode.Return)
                return "Return";
            if (AllKeyInfo.TryGetValue(keyCode, out KeyCodeInfo val)) return val.TranslationKey;
            return null;
        }
    }
}
