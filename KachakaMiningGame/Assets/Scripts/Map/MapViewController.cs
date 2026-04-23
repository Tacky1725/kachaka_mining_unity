using UnityEngine;
using RosMessageTypes.Nav;
using extension;

public class MapViewController : MonoBehaviour
{
    [Header("Map Size")]
    [SerializeField] private Vector2 defaultMapSizeMeters = new Vector2(4f, 3f);

    [Header("Camera Fit")]
    [SerializeField] private Camera mapCamera;
    [SerializeField] private float cameraPaddingUnits = 0.5f;
    [SerializeField] private float cameraViewRotationDegrees = 0f;
    [SerializeField] private bool logMapWorldSize = true;

    [Header("Marker References")]
    [SerializeField] private RobotMarkerController robotMarker;
    [SerializeField] private ArtifactMarkerController artifactMarker;
    [SerializeField] private GameObject robotBeaconPrefab;
    [SerializeField] private GameObject originMarkerPrefab;

    private Vector2 mapOriginMeters;
    private Vector2 mapSizeMeters;
    private Vector2 displaySizeUnits;
    private SpriteRenderer mapBackgroundRenderer;
    private Transform originMarkerInstance;
    private Transform markerRoot;
    private Transform gridRoot;
    private bool initialized;

    public void Initialize()
    {
        if (initialized)
        {
            return;
        }

        gameObject.name = "MapRoot";
        transform.position = Vector3.zero;
        mapOriginMeters = -defaultMapSizeMeters * 0.5f;
        mapSizeMeters = defaultMapSizeMeters;
        displaySizeUnits = mapSizeMeters;

        CreateMapBackground();
        AdjustCameraToFitMap();
        markerRoot = CreateChild("MarkerRoot").transform;
        robotMarker = EnsureRobotMarker();
        artifactMarker = EnsureMarker<ArtifactMarkerController>("ArtifactMarker");

        initialized = true;
    }

    public void UpdateOccupancyGrid(OccupancyGridMsg grid)
    {
        if (grid == null || grid.info.width == 0 || grid.info.height == 0 || grid.data == null)
        {
            return;
        }

        Initialize();

        Vector3 convertedOrigin = grid.info.origin.position.rosMsg2Unity();
        Vector2 convertedMapSize = new Vector2(grid.info.width * grid.info.resolution, grid.info.height * grid.info.resolution);
        mapOriginMeters = new Vector2(convertedOrigin.x, convertedOrigin.y);
        mapSizeMeters = convertedMapSize.x > 0f && convertedMapSize.y > 0f ? convertedMapSize : defaultMapSizeMeters;
        displaySizeUnits = mapSizeMeters;

        Texture2D texture = CreateOccupancyTexture(grid);
        mapBackgroundRenderer.sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            texture.width / displaySizeUnits.x);
        mapBackgroundRenderer.sortingOrder = -10;
        mapBackgroundRenderer.transform.localScale = Vector3.one;
        CreateGridLines();
        AdjustCameraToFitMap();
        LogMapWorldSize();

        UpdateOriginMarker(grid);
    }

    public void UpdateRobot(Vector2 mapPositionMeters, float headingDegrees)
    {
        robotMarker.SetPose(MapToLocalPosition(mapPositionMeters), headingDegrees);
    }

    public void UpdateArtifact(Vector2 mapPositionMeters, bool visible)
    {
        artifactMarker.SetPosition(MapToLocalPosition(mapPositionMeters));
        artifactMarker.SetVisible(visible);
    }

    public Vector3 MapToLocalPosition(Vector2 mapPositionMeters)
    {
        float x = Mathf.InverseLerp(mapOriginMeters.x, mapOriginMeters.x + mapSizeMeters.x, mapPositionMeters.x);
        float y = Mathf.InverseLerp(mapOriginMeters.y, mapOriginMeters.y + mapSizeMeters.y, mapPositionMeters.y);

        return new Vector3(
            Mathf.Lerp(-displaySizeUnits.x * 0.5f, displaySizeUnits.x * 0.5f, x),
            Mathf.Lerp(-displaySizeUnits.y * 0.5f, displaySizeUnits.y * 0.5f, y),
            0f);
    }

    private void CreateMapBackground()
    {
        GameObject background = CreateChild("MapBackground");
        mapBackgroundRenderer = background.GetComponent<SpriteRenderer>();
        if (mapBackgroundRenderer == null)
        {
            mapBackgroundRenderer = background.AddComponent<SpriteRenderer>();
        }

        mapBackgroundRenderer.sprite = SpriteFactory.CreateSolidSprite("MapBackgroundSprite", new Color(0.12f, 0.18f, 0.16f));
        mapBackgroundRenderer.sortingOrder = -10;
        background.transform.localScale = new Vector3(displaySizeUnits.x, displaySizeUnits.y, 1f);

        CreateGridLines();
    }

    private Texture2D CreateOccupancyTexture(OccupancyGridMsg grid)
    {
        int width = (int)grid.info.width;
        int height = (int)grid.info.height;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] colors = new Color[width * height];

        for (int i = 0; i < colors.Length; i++)
        {
            sbyte value = i < grid.data.Length ? grid.data[i] : (sbyte)-1;

            if (value == 0)
            {
                colors[i] = new Color(0.92f, 0.95f, 0.92f);
            }
            else if (value >= 100)
            {
                colors[i] = new Color(0.08f, 0.09f, 0.1f);
            }
            else
            {
                colors[i] = new Color(0.45f, 0.48f, 0.47f);
            }
        }

        texture.SetPixels(colors);
        texture.filterMode = FilterMode.Point;
        texture.Apply();
        return texture;
    }

    private void CreateGridLines()
    {
        gridRoot = CreateChild("GridRoot").transform;
        ClearChildren(gridRoot);

        Shader spriteShader = Shader.Find("Sprites/Default");
        Material lineMaterial = spriteShader != null ? new Material(spriteShader) : null;
        Color lineColor = new Color(0.45f, 0.62f, 0.54f, 0.4f);

        for (int i = 1; i < 4; i++)
        {
            float x = Mathf.Lerp(-displaySizeUnits.x * 0.5f, displaySizeUnits.x * 0.5f, i / 4f);
            CreateLine($"GridVertical{i}", new Vector3(x, -displaySizeUnits.y * 0.5f, -0.1f), new Vector3(x, displaySizeUnits.y * 0.5f, -0.1f), lineMaterial, lineColor);
        }

        for (int i = 1; i < 3; i++)
        {
            float y = Mathf.Lerp(-displaySizeUnits.y * 0.5f, displaySizeUnits.y * 0.5f, i / 3f);
            CreateLine($"GridHorizontal{i}", new Vector3(-displaySizeUnits.x * 0.5f, y, -0.1f), new Vector3(displaySizeUnits.x * 0.5f, y, -0.1f), lineMaterial, lineColor);
        }
    }

    private void CreateLine(string objectName, Vector3 start, Vector3 end, Material material, Color color)
    {
        GameObject lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(gridRoot != null ? gridRoot : transform, false);

        LineRenderer line = lineObject.GetComponent<LineRenderer>();
        if (line == null)
        {
            line = lineObject.AddComponent<LineRenderer>();
        }

        line.positionCount = 2;
        line.useWorldSpace = false;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = 0.025f;
        line.endWidth = 0.025f;
        if (material != null)
        {
            line.material = material;
        }

        line.startColor = color;
        line.endColor = color;
        line.sortingOrder = -5;
    }

    private T EnsureMarker<T>(string objectName) where T : Component
    {
        GameObject markerObject = CreateChild(objectName);
        markerObject.transform.SetParent(markerRoot, false);
        T component = markerObject.GetComponent<T>();
        return component != null ? component : markerObject.AddComponent<T>();
    }

    private RobotMarkerController EnsureRobotMarker()
    {
        Transform existing = markerRoot.Find("RobotMarker");
        if (robotBeaconPrefab == null && existing != null)
        {
            RobotMarkerController existingController = existing.GetComponent<RobotMarkerController>();
            return existingController != null ? existingController : existing.gameObject.AddComponent<RobotMarkerController>();
        }

        if (robotBeaconPrefab != null && existing != null)
        {
            existing.gameObject.SetActive(false);
        }

        GameObject markerObject = robotBeaconPrefab != null ? Instantiate(robotBeaconPrefab, markerRoot) : new GameObject("RobotMarker");

        markerObject.name = "RobotMarker";
        markerObject.transform.SetParent(markerRoot, false);

        RobotMarkerController controller = markerObject.GetComponent<RobotMarkerController>();
        return controller != null ? controller : markerObject.AddComponent<RobotMarkerController>();
    }

    private void UpdateOriginMarker(OccupancyGridMsg grid)
    {
        if (originMarkerPrefab == null)
        {
            return;
        }

        if (originMarkerInstance == null)
        {
            GameObject markerObject = Instantiate(originMarkerPrefab, markerRoot);
            markerObject.name = "OriginMarker";
            originMarkerInstance = markerObject.transform;
        }

        Vector3 originPosition = grid.info.origin.position.rosMsg2Unity();
        Quaternion originRotation = grid.info.origin.orientation.rosMsg2Unity().Ros2Unity();
        Vector3 markerPosition = MapToLocalPosition(new Vector2(originPosition.x, originPosition.y));
        originMarkerInstance.localPosition = new Vector3(markerPosition.x, markerPosition.y, -0.9f);
        originMarkerInstance.localRotation = originRotation;
    }

    private void AdjustCameraToFitMap()
    {
        if (mapCamera == null)
        {
            mapCamera = Camera.main;
        }

        if (mapCamera == null)
        {
            return;
        }

        mapCamera.orthographic = true;
        float aspect = mapCamera.aspect > 0f ? mapCamera.aspect : 16f / 9f;
        float paddedWidth = displaySizeUnits.x + cameraPaddingUnits * 2f;
        float paddedHeight = displaySizeUnits.y + cameraPaddingUnits * 2f;
        mapCamera.orthographicSize = Mathf.Max(paddedHeight * 0.5f, paddedWidth * 0.5f / aspect);
        mapCamera.transform.position = new Vector3(transform.position.x, transform.position.y, mapCamera.transform.position.z);
        mapCamera.transform.rotation = Quaternion.Euler(0f, 0f, cameraViewRotationDegrees);
    }

    private void LogMapWorldSize()
    {
        if (!logMapWorldSize || mapBackgroundRenderer == null)
        {
            return;
        }

        Bounds bounds = mapBackgroundRenderer.bounds;
        string robotMarkerSize = "unavailable";
        if (robotMarker != null && robotMarker.TryGetWorldBounds(out Bounds robotBounds))
        {
            robotMarkerSize = $"{robotBounds.size.x:F3} x {robotBounds.size.y:F3}";
        }

        string robotBodySize = "unavailable";
        if (robotMarker != null && robotMarker.TryGetBodyWorldBounds(out Bounds robotBodyBounds))
        {
            robotBodySize = $"{robotBodyBounds.size.x:F3} x {robotBodyBounds.size.y:F3}";
        }

        Debug.Log(
            $"[MapViewController] Map size - ROS meters: {mapSizeMeters.x:F3} x {mapSizeMeters.y:F3}, " +
            $"Unity display units: {displaySizeUnits.x:F3} x {displaySizeUnits.y:F3}, " +
            $"world bounds: {bounds.size.x:F3} x {bounds.size.y:F3}, " +
            $"transform lossyScale: {mapBackgroundRenderer.transform.lossyScale.x:F3} x {mapBackgroundRenderer.transform.lossyScale.y:F3}, " +
            $"RobotMarker world bounds: {robotMarkerSize}, " +
            $"RobotMarker Body world bounds: {robotBodySize}");
    }

    private GameObject CreateChild(string objectName)
    {
        Transform existing = transform.Find(objectName);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject child = new GameObject(objectName);
        child.transform.SetParent(transform, false);
        return child;
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
}
