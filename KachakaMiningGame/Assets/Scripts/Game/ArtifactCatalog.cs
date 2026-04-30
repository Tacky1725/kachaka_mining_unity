using UnityEngine;

[CreateAssetMenu(menuName = "Kachaka Mining/Artifact Catalog")]
public class ArtifactCatalog : ScriptableObject
{
    [SerializeField] private ArtifactDefinition[] definitions =
    {
        new ArtifactDefinition(ArtifactKind.Coal, 1, false),
        new ArtifactDefinition(ArtifactKind.Stone, 3, false),
        new ArtifactDefinition(ArtifactKind.Bomb, 0, true)
    };

    public ArtifactDefinition GetDefinition(ArtifactKind kind)
    {
        if (definitions == null)
        {
            return null;
        }

        for (int i = 0; i < definitions.Length; i++)
        {
            ArtifactDefinition definition = definitions[i];
            if (definition != null && definition.Kind == kind)
            {
                return definition;
            }
        }

        return null;
    }

    public bool TryGetRandomKindByWeight(out ArtifactKind kind)
    {
        float totalWeight = 0f;
        if (definitions != null)
        {
            for (int i = 0; i < definitions.Length; i++)
            {
                ArtifactDefinition definition = definitions[i];
                if (definition != null && definition.SpawnWeight > 0f)
                {
                    totalWeight += definition.SpawnWeight;
                }
            }
        }

        if (totalWeight <= 0f)
        {
            kind = ArtifactKind.Coal;
            return false;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        for (int i = 0; i < definitions.Length; i++)
        {
            ArtifactDefinition definition = definitions[i];
            if (definition == null || definition.SpawnWeight <= 0f)
            {
                continue;
            }

            currentWeight += definition.SpawnWeight;
            if (randomValue <= currentWeight)
            {
                kind = definition.Kind;
                return true;
            }
        }

        kind = ArtifactKind.Coal;
        return false;
    }
}
