using UnityEngine;

public class DummyGameDataProvider : MonoBehaviour
{
    [Header("Artifact")]
    [SerializeField] private Vector2[] artifactSpawnCandidates =
    {
        new Vector2(1.2f, 0.7f),
        new Vector2(-1.1f, 0.9f),
        new Vector2(0.8f, -0.8f),
        new Vector2(-0.9f, -0.6f)
    };
    [SerializeField] private bool avoidImmediateArtifactRespawnRepeat = true;

    [Header("Robot Dummy Motion")]
    [SerializeField] private Vector2 robotPathCenter = Vector2.zero;
    [SerializeField] private Vector2 robotPathRadius = new Vector2(1.1f, 0.7f);
    [SerializeField] private float robotPathSpeed = 0.6f;

    private Vector2 currentArtifactPosition;
    private int currentArtifactIndex = -1;

    private void Awake()
    {
        if (artifactSpawnCandidates == null || artifactSpawnCandidates.Length == 0)
        {
            artifactSpawnCandidates = new[] { Vector2.zero };
        }

        ResetArtifactSpawn();
    }

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
        return currentArtifactPosition;
    }

    public Vector2 ResetArtifactSpawn()
    {
        return SelectArtifactSpawn(forceDifferentFromCurrent: false);
    }

    public Vector2 RespawnArtifact()
    {
        return SelectArtifactSpawn(forceDifferentFromCurrent: avoidImmediateArtifactRespawnRepeat);
    }

    private Vector2 SelectArtifactSpawn(bool forceDifferentFromCurrent)
    {
        if (artifactSpawnCandidates == null || artifactSpawnCandidates.Length == 0)
        {
            currentArtifactIndex = -1;
            currentArtifactPosition = Vector2.zero;
            return currentArtifactPosition;
        }

        int nextIndex = 0;
        if (artifactSpawnCandidates.Length == 1)
        {
            nextIndex = 0;
        }
        else
        {
            nextIndex = Random.Range(0, artifactSpawnCandidates.Length);
            if (forceDifferentFromCurrent && currentArtifactIndex >= 0 && nextIndex == currentArtifactIndex)
            {
                nextIndex = (nextIndex + 1 + Random.Range(0, artifactSpawnCandidates.Length - 1)) % artifactSpawnCandidates.Length;
            }
        }

        currentArtifactIndex = nextIndex;
        currentArtifactPosition = artifactSpawnCandidates[currentArtifactIndex];
        return currentArtifactPosition;
    }
}
