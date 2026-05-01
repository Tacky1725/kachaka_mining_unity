using TMPro;
using UnityEngine;

public class ScoreView : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;

    private void Awake()
    {
        if (scoreText == null)
        {
            scoreText = GetComponent<TMP_Text>();
        }
    }

    public void UpdateScore(int score)
    {
        string pointsLabel = score == 1 ? "1 point" : $"{score} points";
        scoreText.text = $"Score: {pointsLabel}";
    }
}
