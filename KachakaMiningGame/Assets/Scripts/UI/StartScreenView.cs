using System;
using UnityEngine;
using UnityEngine.UI;

public class StartScreenView : MonoBehaviour
{
    [SerializeField] private Text titleText;
    [SerializeField] private Button startButton;

    private Action startRequested;

    private void Awake()
    {
        if (titleText == null)
        {
            Transform titleTransform = transform.Find("TitleText");
            if (titleTransform != null)
            {
                titleText = titleTransform.GetComponent<Text>();
            }
        }

        if (startButton == null)
        {
            Transform buttonTransform = transform.Find("StartButton");
            if (buttonTransform != null)
            {
                startButton = buttonTransform.GetComponent<Button>();
            }
        }
    }

    public void Bind(Action onStartRequested)
    {
        startRequested = onStartRequested;

        if (startButton == null)
        {
            return;
        }

        startButton.onClick.RemoveListener(HandleStartButtonClicked);
        startButton.onClick.AddListener(HandleStartButtonClicked);
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    private void HandleStartButtonClicked()
    {
        startRequested?.Invoke();
    }
}
