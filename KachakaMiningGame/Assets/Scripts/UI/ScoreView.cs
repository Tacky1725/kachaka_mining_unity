using UnityEngine;
using UnityEngine.UI;

public class ScoreView : MonoBehaviour
{
    [SerializeField] private Text scoreText;

    private void Awake()
    {
        if (scoreText == null)
        {
            scoreText = GetComponent<Text>();
        }
    }

    public void UpdateScore(int score)
    {
        scoreText.text = $"Score: {score}";
    }
}
