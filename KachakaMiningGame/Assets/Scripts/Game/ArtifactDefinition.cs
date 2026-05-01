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
    [Range(0f, 1f)] public float RumbleAmplitude = 0.6f;
    [Min(1)] public int RumbleDurationMilliseconds = 300;

    public ArtifactDefinition()
    {
    }

    public ArtifactDefinition(ArtifactKind kind, int scoreAmount, bool triggersSpin)
    {
        Kind = kind;
        ScoreAmount = scoreAmount;
        TriggersSpin = triggersSpin;
        RumbleAmplitude = 0.6f;
        RumbleDurationMilliseconds = 300;
    }
}
