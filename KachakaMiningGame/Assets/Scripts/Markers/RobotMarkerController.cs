using System.Collections.Generic;
using UnityEngine;

public class RobotMarkerController : MonoBehaviour
{
    private const string RobotTextureProperty = "_RobotMainTex";
    private const string RobotMarkerShaderName = "KachakaMining/RobotMarkerUnlitTexture";
    private const string RobotMarkerShaderResourcePath = "Shaders/RobotMarkerUnlitTexture";

    [SerializeField] private float markerSize = 0.35f;
    [SerializeField] private Color markerColor = new Color(0.1f, 0.72f, 0.95f);
    [SerializeField] private bool useUnlitTextureMaterials = true;
    [SerializeField] private string bodyRendererName = "Body";
    [SerializeField] private Transform sensorTransform;
    [SerializeField] private string sensorObjectName = "Sensor";
    [SerializeField] private float rotationOffsetDegrees = 0f;
    [SerializeField] private float markerZPosition = -1f;

    private SpriteRenderer spriteRenderer;
    private bool usesPrefabRenderers;
    private Vector3 lastLocalPosition;
    private float lastHeadingDegrees;
    private readonly List<Material> runtimeMaterials = new List<Material>();

    private void Awake()
    {
        EnsureRenderer();
        EnsureSensorTransform();
    }

    private void OnDestroy()
    {
        foreach (Material material in runtimeMaterials)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

        runtimeMaterials.Clear();
    }

    public Transform SensorTransform
    {
        get
        {
            EnsureSensorTransform();
            return sensorTransform != null ? sensorTransform : transform;
        }
    }

    public void SetPose(Vector3 localPosition, float headingDegrees)
    {
        EnsureRenderer();
        lastLocalPosition = new Vector3(localPosition.x, localPosition.y, markerZPosition);
        lastHeadingDegrees = headingDegrees;
        transform.localPosition = lastLocalPosition;
        transform.localRotation = Quaternion.Euler(0f, 0f, lastHeadingDegrees + rotationOffsetDegrees);
    }

    public void SetRotationOffset(float offsetDegrees)
    {
        rotationOffsetDegrees = offsetDegrees;
        transform.localRotation = Quaternion.Euler(0f, 0f, lastHeadingDegrees + rotationOffsetDegrees);
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

            ApplyUnlitTextureMaterials(childRenderers);
            return;
        }

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = SpriteFactory.CreateTriangleSprite("RobotMarkerSprite", markerColor);
        spriteRenderer.sortingOrder = 10;
        transform.localScale = Vector3.one * markerSize;
    }

    private void ApplyUnlitTextureMaterials(SpriteRenderer[] renderers)
    {
        if (!useUnlitTextureMaterials)
        {
            return;
        }

        Shader robotMarkerShader = FindRobotMarkerShader();
        if (robotMarkerShader == null)
        {
            Debug.LogWarning("[RobotMarkerController] Robot marker unlit shader was not found. Robot marker materials were left unchanged.");
            return;
        }

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || renderer.sharedMaterial == null)
            {
                continue;
            }

            Material sourceMaterial = renderer.sharedMaterial;
            if (sourceMaterial.shader == robotMarkerShader)
            {
                continue;
            }

            Material unlitMaterial = new Material(robotMarkerShader)
            {
                name = $"{sourceMaterial.name}_RuntimeUnlit"
            };

            if (sourceMaterial.HasProperty("_MainTex"))
            {
                unlitMaterial.SetTexture(RobotTextureProperty, sourceMaterial.mainTexture);
                unlitMaterial.SetTextureScale(RobotTextureProperty, sourceMaterial.mainTextureScale);
                unlitMaterial.SetTextureOffset(RobotTextureProperty, sourceMaterial.mainTextureOffset);
            }

            if (sourceMaterial.HasProperty("_Color"))
            {
                unlitMaterial.color = sourceMaterial.color;
            }

            renderer.sharedMaterial = unlitMaterial;
            renderer.color = Color.white;
            runtimeMaterials.Add(unlitMaterial);
        }
    }

    private static Shader FindRobotMarkerShader()
    {
        Shader shader = Shader.Find(RobotMarkerShaderName);
        if (shader != null)
        {
            return shader;
        }

        return Resources.Load<Shader>(RobotMarkerShaderResourcePath);
    }

    private void EnsureSensorTransform()
    {
        if (sensorTransform != null)
        {
            return;
        }

        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child != null && child.name == sensorObjectName)
            {
                sensorTransform = child;
                return;
            }
        }

        sensorTransform = transform;
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
