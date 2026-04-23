using UnityEngine;

public class ArtifactMarkerController : MonoBehaviour
{
    [SerializeField] private float markerSize = 0.32f;
    [SerializeField] private Color fossilColor = new Color(0.95f, 0.78f, 0.32f);

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        EnsureRenderer();
    }

    public void SetPosition(Vector3 localPosition)
    {
        EnsureRenderer();
        transform.localPosition = new Vector3(localPosition.x, localPosition.y, -0.8f);
    }

    public void SetVisible(bool visible)
    {
        EnsureRenderer();
        spriteRenderer.enabled = visible;
    }

    private void EnsureRenderer()
    {
        if (spriteRenderer != null)
        {
            return;
        }

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = SpriteFactory.CreateCircleSprite("FossilMarkerSprite", fossilColor);
        spriteRenderer.sortingOrder = 8;
        transform.localScale = Vector3.one * markerSize;
    }
}
