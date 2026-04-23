using UnityEngine;

public class DummyGameDataProvider : MonoBehaviour
{
    [SerializeField] private Vector2 artifactPosition = new Vector2(1.2f, 0.7f);
    [SerializeField] private Vector2 robotPathCenter = Vector2.zero;
    [SerializeField] private Vector2 robotPathRadius = new Vector2(1.1f, 0.7f);
    [SerializeField] private float robotPathSpeed = 0.6f;

    public Vector2 GetRobotPosition(float elapsedSeconds)
    {
        float angle = elapsedSeconds * robotPathSpeed;
        return robotPathCenter + new Vector2(Mathf.Cos(angle) * robotPathRadius.x, Mathf.Sin(angle) * robotPathRadius.y);
    }

    public float GetRobotHeadingDegrees(float elapsedSeconds)
    {
        return elapsedSeconds * robotPathSpeed * Mathf.Rad2Deg + 90f;
    }

    public Vector2 GetArtifactPosition()
    {
        return artifactPosition;
    }
}
