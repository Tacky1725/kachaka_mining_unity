using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenuView : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button backButton;
    [SerializeField] private Button quitGameButton;

    private Action backRequested;
    private Action quitGameRequested;

    private void Awake()
    {
        if (messageText == null)
        {
            Transform messageTransform = FindDescendant("PauseMessageText");
            if (messageTransform != null)
            {
                messageText = messageTransform.GetComponent<TMP_Text>();
            }
        }

        if (backButton == null)
        {
            Transform buttonTransform = FindDescendant("BackButton");
            if (buttonTransform != null)
            {
                backButton = buttonTransform.GetComponent<Button>();
            }
        }

        if (quitGameButton == null)
        {
            Transform buttonTransform = FindDescendant("QuitGameButton");
            if (buttonTransform != null)
            {
                quitGameButton = buttonTransform.GetComponent<Button>();
            }
        }
    }

    public void Bind(Action onBackRequested, Action onQuitGameRequested)
    {
        backRequested = onBackRequested;
        quitGameRequested = onQuitGameRequested;

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(HandleBackButtonClicked);
            backButton.onClick.AddListener(HandleBackButtonClicked);
        }

        if (quitGameButton != null)
        {
            quitGameButton.onClick.RemoveListener(HandleQuitGameButtonClicked);
            quitGameButton.onClick.AddListener(HandleQuitGameButtonClicked);
        }
    }

    public void SetMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    private void HandleBackButtonClicked()
    {
        backRequested?.Invoke();
    }

    private void HandleQuitGameButtonClicked()
    {
        quitGameRequested?.Invoke();
    }

    private Transform FindDescendant(string objectName)
    {
        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in transforms)
        {
            if (child != null && child.gameObject.name == objectName)
            {
                return child;
            }
        }

        return null;
    }
}
