using UnityEngine;

public struct ArtifactInstance
{
    public Vector2 Position;
    public ArtifactKind Kind;

    public ArtifactInstance(Vector2 position, ArtifactKind kind)
    {
        Position = position;
        Kind = kind;
    }
}
