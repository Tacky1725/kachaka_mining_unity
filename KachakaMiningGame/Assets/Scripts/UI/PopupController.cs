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
        if (popupText == null)
        {
            return;
        }

        if (popupRoutine != null)
        {
            StopCoroutine(popupRoutine);
        }

        popupRoutine = StartCoroutine(ShowPopupRoutine(amount));
    }

    private IEnumerator ShowPopupRoutine(int amount)
    {
        popupText.text = $"+{amount}";
        popupText.enabled = true;
        yield return new WaitForSeconds(visibleSeconds);
        popupText.enabled = false;
    }
}
