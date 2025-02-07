using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TheOtherRoles.Objects;

public sealed class FoxTask : Minigame
{
    public static GameObject prefab;
    public static Sprite shrine;
    private Button closeButton;
    private bool completed;
    private GameObject obj;
    private TextMeshProUGUI RemainingTime;
    private TextMeshProUGUI TaskText;
    private float timer;

    public void Awake()
    {
        if (obj != null) Destroy(obj);
        obj = Instantiate(prefab, transform);
        List<TextMeshProUGUI> texts = obj.GetComponentsInChildren<TextMeshProUGUI>().ToList();
        RemainingTime = texts.FirstOrDefault(x => x.name == "RemainingTime");
        TaskText = texts.FirstOrDefault(x => x.name == "TaskText");
        TaskText.text = ModTranslation.getString("foxTaskPray");
        closeButton = obj.GetComponentsInChildren<Button>().ToList().FirstOrDefault(x => x.name == "CloseButton");
        closeButton.onClick = new Button.ButtonClickedEvent();
        closeButton.onClick.AddListener((UnityAction)onClick);
        obj.SetActive(true);
        enabled = true;
    }

    private void FixedUpdate()
    {
        if (timer > 0)
        {
            RemainingTime.text = $"{timer:n}秒";
            timer -= Time.fixedDeltaTime;
        }
        else if (!completed)
        {
            completed = true;
            obj.SetActive(false);
            MyNormTask.NextStep();
            CustomNormalPlayerTask.completedConsoles.Add(ConsoleId);
            if (MyNormTask.taskStep < MyNormTask.MaxStep)
            {
                Console console =
                    ShipStatus.Instance.AllConsoles.FirstOrDefault(x =>
                        x.ConsoleId == MyNormTask.Data[MyNormTask.taskStep]);
                MyNormTask.StartAt = console.Room;
            }

            StartCoroutine(CoStartClose(0.5f));
        }
    }

    public void OnEnable()
    {
        enabled = true;
        completed = false;
        timer = Fox.stayTime;
    }

    public void OnDisable()
    {
        obj.SetActive(false);
    }

    public void OnDestroy()
    {
    }

    private void onClick()
    {
        Close();
    }
}
