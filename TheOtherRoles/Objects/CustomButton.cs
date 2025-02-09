using System;
using System.Collections.Generic;
using TheOtherRoles.Modules;
using TheOtherRoles.Patches;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TheOtherRoles.Objects;

public class CustomButton
{
    public static List<CustomButton> buttons = new();

    public static bool stopCountdown = true;
    private readonly Func<bool> CouldUse;
    private readonly Func<bool> HasButton;
    private readonly KeyCode? hotkey;
    private readonly HudManager hudManager;
    private readonly bool mirror;
    private readonly Action OnClick;
    private readonly Action OnEffectEnds;
    private readonly Action OnMeetingEnds;
    public ActionButton actionButton;
    public string buttonText;
    public bool effectCancellable = false;
    public float EffectDuration;
    public bool HasEffect;
    public bool isEffectActive;
    public Vector3 LocalScale = Vector3.one;
    public float MaxTimer = float.MaxValue;
    public Vector3 PositionOffset;
    public bool showButtonText = true;
    public Sprite Sprite;
    public float Timer;

    public CustomButton(Action OnClick, Func<bool> HasButton, Func<bool> CouldUse, Action OnMeetingEnds, Sprite Sprite,
        Vector3 PositionOffset, HudManager hudManager, ActionButton textTemplate, KeyCode? hotkey, bool HasEffect,
        float EffectDuration, Action OnEffectEnds, bool mirror = false, string buttonText = null)
    {
        this.hudManager = hudManager;
        this.OnClick = OnClick;
        this.HasButton = HasButton;
        this.CouldUse = CouldUse;
        this.PositionOffset = PositionOffset;
        this.OnMeetingEnds = OnMeetingEnds;
        this.HasEffect = HasEffect;
        this.EffectDuration = EffectDuration;
        this.OnEffectEnds = OnEffectEnds;
        this.Sprite = Sprite;
        this.mirror = mirror;
        this.hotkey = hotkey;
        this.buttonText = buttonText;
        Timer = 16.2f;
        buttons.Add(this);
        actionButton = Object.Instantiate(hudManager.KillButton, hudManager.KillButton.transform.parent);
        PassiveButton button = actionButton.GetComponent<PassiveButton>();
        button.OnClick = new Button.ButtonClickedEvent();
        button.OnClick.AddListener((UnityAction)onClickEvent);

        LocalScale = actionButton.transform.localScale;
        if (textTemplate)
        {
            Object.Destroy(actionButton.buttonLabelText);
            actionButton.buttonLabelText = Object.Instantiate(textTemplate.buttonLabelText, actionButton.transform);
        }

        setKeyBind();
        setActive(false);
    }
#nullable enable
    public CustomButton(Action OnClick, Func<bool> HasButton, Func<bool> CouldUse, Action OnMeetingEnds, Sprite Sprite,
        Vector3 PositionOffset, HudManager hudManager, ActionButton? textTemplate, KeyCode? hotkey, bool mirror = false,
        string? buttonText = null)
        : this(OnClick, HasButton, CouldUse, OnMeetingEnds, Sprite, PositionOffset, hudManager, textTemplate, hotkey,
            false, 0f, () => { }, mirror, buttonText)
    {
    }
#nullable disable

    private void onClickEvent()
    {
        if ((HasEffect && isEffectActive && effectCancellable) || (Timer < 0f && HasButton() && CouldUse()))
        {
            actionButton.graphic.color = new Color(1f, 1f, 1f, 0.3f);
            LogHelper.Info(
                $"Click \"{(buttonText is null or "" && hotkey is not null ? Enum.GetName(typeof(KeyCode), hotkey) : buttonText)}\"",
                "Button");
            OnClick();

            if (HasEffect && !isEffectActive)
            {
                Timer = EffectDuration;
                actionButton.cooldownTimerText.color = new Color(0F, 0.8F, 0F);
                isEffectActive = true;
            }
        }
    }

    public static void HudUpdate()
    {
        buttons.RemoveAll(item => item.actionButton == null);

        for (int i = 0; i < buttons.Count; i++)
            try
            {
                buttons[i].Update();
            }
            catch (NullReferenceException)
            {
                System.Console.WriteLine(
                    "[WARNING] NullReferenceException from HudUpdate().HasButton(), if theres only one warning its fine");
            }
    }

    public static void MeetingEndedUpdate()
    {
        buttons.RemoveAll(item => item.actionButton == null);
        for (int i = 0; i < buttons.Count; i++)
            try
            {
                buttons[i].OnMeetingEnds();
                buttons[i].Update();
            }
            catch (NullReferenceException)
            {
                System.Console.WriteLine(
                    "[WARNING] NullReferenceException from MeetingEndedUpdate().HasButton(), if theres only one warning its fine");
            }
    }

    public static void ResetAllCooldowns()
    {
        for (int i = 0; i < buttons.Count; i++)
            try
            {
                buttons[i].Timer = buttons[i].MaxTimer;
                buttons[i].Update();
            }
            catch (NullReferenceException)
            {
                System.Console.WriteLine(
                    "[WARNING] NullReferenceException from MeetingEndedUpdate().HasButton(), if theres only one warning its fine");
            }
    }

    public void setActive(bool isActive)
    {
        actionButton.gameObject.SetActive(isActive);
        actionButton.graphic.enabled = isActive;
    }

    private void Update()
    {
        if (PlayerControl.LocalPlayer.Data == null || MeetingHud.Instance || ExileController.Instance ||
            !HasButton())
        {
            setActive(false);
            return;
        }

        setActive(hudManager.UseButton.isActiveAndEnabled || hudManager.PetButton.isActiveAndEnabled);

        actionButton.graphic.sprite = Sprite;
        if (showButtonText && buttonText != null) actionButton.OverrideText(buttonText);
        actionButton.buttonLabelText.enabled = showButtonText; // Only show the text if it's a kill button

        if (hudManager.UseButton != null)
        {
            Vector3 pos = hudManager.UseButton.transform.localPosition;
            if (mirror)
            {
                float aspect = Camera.main.aspect;
                float safeOrthographicSize = CameraSafeArea.GetSafeOrthographicSize(Camera.main);
                float xpos = 0.05f - (safeOrthographicSize * aspect * 1.70f);
                pos = new Vector3(xpos, pos.y, pos.z);
            }

            actionButton.transform.localPosition = pos + PositionOffset;
            actionButton.transform.localScale = LocalScale;
        }

        if (CouldUse())
        {
            actionButton.graphic.color = actionButton.buttonLabelText.color = Palette.EnabledColor;
            actionButton.graphic.material.SetFloat("_Desat", 0f);
        }
        else
        {
            actionButton.graphic.color = actionButton.buttonLabelText.color = Palette.DisabledClear;
            actionButton.graphic.material.SetFloat("_Desat", 1f);
        }

        if (Timer >= 0 && !stopCountdown)
        {
            if (HasEffect && isEffectActive)
                Timer -= Time.deltaTime;
            else if (!PlayerControl.LocalPlayer.inVent)
                if (!(CustomOptionHolder.exceptOnTask.getBool() && ElectricPatch.isOntask()))
                    Timer -= Time.deltaTime;
        }

        if (Timer <= 0 && HasEffect && isEffectActive)
        {
            isEffectActive = false;
            actionButton.cooldownTimerText.color = Palette.EnabledColor;
            OnEffectEnds();
        }

        actionButton.SetCoolDown(Timer, HasEffect && isEffectActive ? EffectDuration : MaxTimer);

        // Trigger OnClickEvent if the hotkey is being pressed down
        if (hotkey.HasValue && Input.GetKeyDown(hotkey.Value)) onClickEvent();
    }

    public void setKeyBind()
    {
        if (hotkey != null && hotkey != KeyCode.None && hotkey != KeyCode.KeypadPlus)
        {
            actionButton.gameObject.ForEachChild((Il2CppSystem.Action<GameObject>)(c =>
            {
                if (c.name.Equals("HotKeyGuide")) GameObject.Destroy(c);
            }));
            ButtonEffect.SetKeyGuide(actionButton.gameObject, (KeyCode)hotkey,
                action: showButtonText && buttonText != "" ? buttonText : "Action");
        }
    }

    public static class ButtonPositions
    {
        public static readonly Vector3
            lowerRowRight = new(-2f, -0.06f, 0); // Not usable for imps beacuse of new button positions!

        public static readonly Vector3 lowerRowCenter = new(-3f, -0.06f, 0);
        public static readonly Vector3 lowerRowLeft = new(-4f, -0.06f, 0);

        public static readonly Vector3
            upperRowRight = new(0f, 1f, 0f); // Not usable for imps beacuse of new button positions!

        public static readonly Vector3
            upperRowCenter = new(-1f, 1f, 0f); // Not usable for imps beacuse of new button positions!

        public static readonly Vector3 upperRowLeft = new(-2f, 1f, 0f);
        public static readonly Vector3 upperRowFarLeft = new(-3f, 1f, 0f);
    }

    /*public void resetKeyBind()
    {
        bool isVampire = Sprite == Vampire.getButtonSprite();
        if (buttonText == "" && !isVampire) return; // English or something that doesn't require an update, return
                                                    // Specify vampire as not to override things with English language
        if ((buttonText != "" || isVampire)) return;
        actionButton.gameObject.ForEachChild((Il2CppSystem.Action<GameObject>)((c) => { if (c.name.Equals("HotKeyGuide")) GameObject.Destroy(c); }));
        setKeyBind();
    }*/
}
