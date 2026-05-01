using System.Collections;
using UnityEngine;
using TMPro;

public class PopupController : MonoBehaviour
{
    [SerializeField] private float visibleSeconds = 0.8f;
    [SerializeField] private TMP_Text popupText;

    private Coroutine popupRoutine;

    private void Awake()
    {
        if (popupText == null)
        {
            popupText = GetComponentInChildren<TMP_Text>();
        }

        if (popupText != null)
        {
            popupText.enabled = false;
        }
    }

    public void ShowScorePopup(int amount)
    {
        ShowTextPopup($"+{amount}", visibleSeconds);
    }

    public void ShowTextPopup(string text, float displaySeconds)
    {
        if (popupText == null)
        {
            return;
        }

        if (popupRoutine != null)
        {
            StopCoroutine(popupRoutine);
        }

        popupRoutine = StartCoroutine(ShowPopupRoutine(text, displaySeconds));
    }

    public void HidePopup()
    {
        if (popupRoutine != null)
        {
            StopCoroutine(popupRoutine);
            popupRoutine = null;
        }

        if (popupText != null)
        {
            popupText.enabled = false;
        }
    }

    private IEnumerator ShowPopupRoutine(string text, float displaySeconds)
    {
        popupText.text = text;
        popupText.enabled = true;
        yield return new WaitForSeconds(Mathf.Max(0f, displaySeconds));
        popupText.enabled = false;
        popupRoutine = null;
    }
}
