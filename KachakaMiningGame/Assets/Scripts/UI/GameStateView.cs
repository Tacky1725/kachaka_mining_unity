using UnityEngine;
using UnityEngine.UI;

public class GameStateView : MonoBehaviour
{
    [SerializeField] private Text stateText;

    private void Awake()
    {
        EnsureStateText();
    }

    public void UpdateState(string stateLabel)
    {
        EnsureStateText();
        if (stateText == null)
        {
            return;
        }

        stateText.text = $"State: {stateLabel}";
    }

    private void EnsureStateText()
    {
        if (stateText != null)
        {
            return;
        }

        stateText = GetComponent<Text>();
        if (stateText == null)
        {
            stateText = GetComponentInChildren<Text>(true);
        }

        if (stateText == null)
        {
            stateText = gameObject.AddComponent<Text>();
            stateText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stateText.fontSize = 28;
            stateText.color = Color.white;
            stateText.alignment = TextAnchor.UpperLeft;
        }
    }
}
