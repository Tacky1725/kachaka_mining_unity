using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PopupController : MonoBehaviour
{
    [SerializeField] private float visibleSeconds = 0.8f;

    private Text popupText;
    private Coroutine popupRoutine;

    private void Awake()
    {
        popupText = GetComponentInChildren<Text>();
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
