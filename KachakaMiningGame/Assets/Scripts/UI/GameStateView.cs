using UnityEngine;
using UnityEngine.UI;

public class GameStateView : MonoBehaviour
{
    [SerializeField] private Text stateText;

    private void Awake()
    {
        if (stateText == null)
        {
            stateText = GetComponent<Text>();
        }
    }

    public void UpdateState(string stateLabel)
    {
        stateText.text = $"State: {stateLabel}";
    }
}
