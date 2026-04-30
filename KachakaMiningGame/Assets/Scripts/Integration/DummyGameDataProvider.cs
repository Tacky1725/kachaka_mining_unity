using System.Collections.Generic;
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
    [SerializeField] private ArtifactKind[] artifactKinds =
    {
        ArtifactKind.Coal,
        ArtifactKind.Stone,
        ArtifactKind.Bomb
    };
    [SerializeField] private ArtifactCatalog artifactCatalog;
    [SerializeField] private bool avoidImmediateArtifactRespawnRepeat = true;
    [SerializeField, Range(0f, 100f)] private float activeArtifactCandidatePercent = 50f;
    [SerializeField, Min(0)] private int maxActiveArtifactCount = 3;

    [Header("Robot Dummy Motion")]
    [SerializeField] private Vector2 robotPathCenter = Vector2.zero;
    [SerializeField] private Vector2 robotPathRadius = new Vector2(1.1f, 0.7f);
    [SerializeField] private float robotPathSpeed = 0.6f;

    private Vector2 currentArtifactPosition;
    private ArtifactKind currentArtifactKind;
    private int currentArtifactIndex = -1;
    private readonly List<ArtifactInstance> activeArtifacts = new List<ArtifactInstance>();
    private readonly List<int> activeArtifactIndices = new List<int>();

    private void Awake()
    {
        if (artifactSpawnCandidates == null || artifactSpawnCandidates.Length == 0)
        {
            artifactSpawnCandidates = new[] { Vector2.zero };
        }

        if (artifactKinds == null || artifactKinds.Length == 0)
        {
            artifactKinds = new[] { ArtifactKind.Coal, ArtifactKind.Stone, ArtifactKind.Bomb };
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

    public ArtifactKind GetArtifactKind()
    {
        return currentArtifactKind;
    }

    public IReadOnlyList<ArtifactInstance> GetActiveArtifacts()
    {
        return activeArtifacts;
    }

    public Vector2 ResetArtifactSpawn()
    {
        ResetArtifactSpawns();
        return currentArtifactPosition;
    }

    public IReadOnlyList<ArtifactInstance> ResetArtifactSpawns()
    {
        activeArtifacts.Clear();
        activeArtifactIndices.Clear();
        currentArtifactIndex = -1;
        currentArtifactPosition = Vector2.zero;
        currentArtifactKind = ArtifactKind.Coal;

        int targetCount = CalculateActiveArtifactCount();
        for (int i = 0; i < targetCount; i++)
        {
            AddRandomArtifact(forceDifferentFromCurrent: false);
        }

        return activeArtifacts;
    }

    public Vector2 RespawnArtifact()
    {
        RespawnArtifacts();
        return currentArtifactPosition;
    }

    public IReadOnlyList<ArtifactInstance> CollectArtifactAt(int activeArtifactIndex)
    {
        if (activeArtifactIndex >= 0 && activeArtifactIndex < activeArtifacts.Count)
        {
            currentArtifactIndex = activeArtifactIndices[activeArtifactIndex];
            activeArtifacts.RemoveAt(activeArtifactIndex);
            activeArtifactIndices.RemoveAt(activeArtifactIndex);
        }

        FillActiveArtifacts();
        return activeArtifacts;
    }

    public IReadOnlyList<ArtifactInstance> RespawnArtifacts()
    {
        activeArtifacts.Clear();
        activeArtifactIndices.Clear();
        FillActiveArtifacts();
        return activeArtifacts;
    }

    private int CalculateActiveArtifactCount()
    {
        if (artifactSpawnCandidates == null || artifactSpawnCandidates.Length == 0 || maxActiveArtifactCount <= 0)
        {
            return 0;
        }

        int percentCount = Mathf.CeilToInt(artifactSpawnCandidates.Length * Mathf.Clamp01(activeArtifactCandidatePercent / 100f));
        return Mathf.Clamp(Mathf.Min(percentCount, maxActiveArtifactCount), 0, artifactSpawnCandidates.Length);
    }

    private void FillActiveArtifacts()
    {
        int targetCount = CalculateActiveArtifactCount();
        while (activeArtifacts.Count < targetCount)
        {
            if (!AddRandomArtifact(forceDifferentFromCurrent: avoidImmediateArtifactRespawnRepeat))
            {
                return;
            }
        }
    }

    private bool AddRandomArtifact(bool forceDifferentFromCurrent)
    {
        if (artifactSpawnCandidates == null || artifactSpawnCandidates.Length == 0)
        {
            return false;
        }

        if (activeArtifactIndices.Count >= artifactSpawnCandidates.Length)
        {
            return false;
        }

        int nextIndex = SelectArtifactIndex(forceDifferentFromCurrent);
        if (nextIndex < 0)
        {
            return false;
        }

        currentArtifactIndex = nextIndex;
        currentArtifactPosition = artifactSpawnCandidates[currentArtifactIndex];
        currentArtifactKind = SelectArtifactKind();
        activeArtifactIndices.Add(currentArtifactIndex);
        activeArtifacts.Add(new ArtifactInstance(currentArtifactPosition, currentArtifactKind));
        return true;
    }

    private ArtifactKind SelectArtifactKind()
    {
        if (artifactCatalog != null && artifactCatalog.TryGetRandomKindByWeight(out ArtifactKind weightedKind))
        {
            return weightedKind;
        }

        return artifactKinds[Random.Range(0, artifactKinds.Length)];
    }

    private int SelectArtifactIndex(bool forceDifferentFromCurrent)
    {
        if (artifactSpawnCandidates.Length == 1)
        {
            return activeArtifactIndices.Contains(0) ? -1 : 0;
        }

        int startIndex = Random.Range(0, artifactSpawnCandidates.Length);
        for (int offset = 0; offset < artifactSpawnCandidates.Length; offset++)
        {
            int nextIndex = (startIndex + offset) % artifactSpawnCandidates.Length;
            if (activeArtifactIndices.Contains(nextIndex))
            {
                continue;
            }

            if (forceDifferentFromCurrent && currentArtifactIndex >= 0 && nextIndex == currentArtifactIndex)
            {
                continue;
            }

            return nextIndex;
        }

        for (int index = 0; index < artifactSpawnCandidates.Length; index++)
        {
            if (!activeArtifactIndices.Contains(index))
            {
                return index;
            }
        }

        return -1;
    }
}
