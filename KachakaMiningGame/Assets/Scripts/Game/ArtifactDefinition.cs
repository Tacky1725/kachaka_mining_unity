using UnityEngine;

[System.Serializable]
public class ArtifactDefinition
{
    public ArtifactKind Kind;
    public int ScoreAmount;
    public GameObject MarkerPrefab;
    public GameObject CollectedMarkerPrefab;
    public AudioClip CollectSound;
    public bool TriggersSpin;
    [Min(0f)] public float SpawnWeight = 1f;

    public ArtifactDefinition()
    {
    }

    public ArtifactDefinition(ArtifactKind kind, int scoreAmount, bool triggersSpin)
    {
        Kind = kind;
        ScoreAmount = scoreAmount;
        TriggersSpin = triggersSpin;
    }
}
