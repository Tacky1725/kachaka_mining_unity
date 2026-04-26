using UnityEngine;

public class ArtifactMarkerController : MonoBehaviour
{
    [SerializeField] private float markerSize = 0.32f;
    [SerializeField] private Color fossilColor = new Color(0.95f, 0.78f, 0.32f);

    private SpriteRenderer spriteRenderer;
    private SpriteRenderer[] prefabRenderers;
    private bool usesPrefabRenderers;

    private void Awake()
    {
        EnsureRenderer();
    }

    public void SetPosition(Vector3 localPosition)
    {
        EnsureRenderer();
        transform.localPosition = new Vector3(localPosition.x, localPosition.y, -0.8f);
    }

    private void EnsureRenderer()
    {
        if (spriteRenderer != null || usesPrefabRenderers)
        {
            return;
        }

        prefabRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (prefabRenderers.Length > 0)
        {
            usesPrefabRenderers = true;
            foreach (SpriteRenderer renderer in prefabRenderers)
            {
                renderer.sortingOrder += 8;
            }

            return;
        }

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = SpriteFactory.CreateCircleSprite("FossilMarkerSprite", fossilColor);
        spriteRenderer.sortingOrder = 8;
        transform.localScale = Vector3.one * markerSize;
    }

    public void SetVisible(bool visible)
    {
        EnsureRenderer();

        if (usesPrefabRenderers && prefabRenderers != null)
        {
            foreach (SpriteRenderer renderer in prefabRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }

            return;
        }

        spriteRenderer.enabled = visible;
    }
}
