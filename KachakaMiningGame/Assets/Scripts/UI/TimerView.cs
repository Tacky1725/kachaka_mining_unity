using UnityEngine;
using TMPro;

public class TimerView : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;

    private void Awake()
    {
        if (timerText == null)
        {
            timerText = GetComponent<TMP_Text>();
        }
    }

    public void UpdateTime(float secondsRemaining)
    {
        timerText.text = $"Time: {Mathf.CeilToInt(secondsRemaining):00}s";
    }
}
