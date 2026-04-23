using UnityEngine;

public class RobotMarkerController : MonoBehaviour
{
    [SerializeField] private float markerSize = 0.35f;
    [SerializeField] private Color markerColor = new Color(0.1f, 0.72f, 0.95f);
    [SerializeField] private string bodyRendererName = "Body";

    private SpriteRenderer spriteRenderer;
    private bool usesPrefabRenderers;

    private void Awake()
    {
        EnsureRenderer();
    }

    public void SetPose(Vector3 localPosition, float headingDegrees)
    {
        EnsureRenderer();
        transform.localPosition = new Vector3(localPosition.x, localPosition.y, -1f);
        transform.localRotation = Quaternion.Euler(0f, 0f, headingDegrees);
    }

    public bool TryGetWorldBounds(out Bounds worldBounds)
    {
        EnsureRenderer();

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        worldBounds = new Bounds();
        bool hasBounds = false;
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                worldBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                worldBounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    public bool TryGetBodyWorldBounds(out Bounds worldBounds)
    {
        EnsureRenderer();

        SpriteRenderer bodyRenderer = FindBodyRenderer();
        if (bodyRenderer == null)
        {
            worldBounds = new Bounds();
            return false;
        }

        worldBounds = bodyRenderer.bounds;
        return true;
    }

    private void EnsureRenderer()
    {
        if (spriteRenderer != null || usesPrefabRenderers)
        {
            return;
        }

        SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>();
        if (childRenderers.Length > 0)
        {
            usesPrefabRenderers = true;
            foreach (SpriteRenderer childRenderer in childRenderers)
            {
                childRenderer.sortingOrder += 10;
            }

            return;
        }

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = SpriteFactory.CreateTriangleSprite("RobotMarkerSprite", markerColor);
        spriteRenderer.sortingOrder = 10;
        transform.localScale = Vector3.one * markerSize;
    }

    private SpriteRenderer FindBodyRenderer()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null && renderer.gameObject.name == bodyRendererName)
            {
                return renderer;
            }
        }

        return renderers.Length > 0 ? renderers[0] : null;
    }
}
