using UnityEngine;
using UnityEngine.UI;

public class TimerView : MonoBehaviour
{
    [SerializeField] private Text timerText;

    private void Awake()
    {
        if (timerText == null)
        {
            timerText = GetComponent<Text>();
        }
    }

    public void UpdateTime(float secondsRemaining)
    {
        timerText.text = $"Time: {Mathf.CeilToInt(secondsRemaining):00}s";
    }
}
