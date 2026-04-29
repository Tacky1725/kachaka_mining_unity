using UnityEngine;
using RosMessageTypes.Nav;
using extension;

public class MapViewController : MonoBehaviour
{
    [Header("Map Size")]
    [SerializeField] private Vector2 defaultMapSizeMeters = new Vector2(4f, 3f);

    [Header("Map Appearance")]
    [SerializeField] private Color backgroundColor = new Color(0.12f, 0.18f, 0.16f);
    [SerializeField] private Color freeCellColor = new Color(0.92f, 0.95f, 0.92f);
    [SerializeField] private Color occupiedCellColor = new Color(0.08f, 0.09f, 0.1f);
    [SerializeField] private Color unknownCellColor = new Color(0.45f, 0.48f, 0.47f);
    [SerializeField] private bool flipMapHorizontally = false;
    [SerializeField] private bool flipMapVertically = false;

    [Header("Free Cell Ground")]
    [SerializeField] private Material freeCellGroundMaterial;
    [SerializeField] private Vector2 freeCellTextureTileSizeMeters = new Vector2(0.01f, 0.01f);
    [SerializeField] private int freeCellGroundSortingOrder = -20;

    [Header("Camera Fit")]
    [SerializeField] private Camera mapCamera;
    [SerializeField] private float cameraPaddingUnits = 0.5f;
    [SerializeField] [Range(0.6f, 1.2f)] private float cameraZoomScale = 0.9f;
    [SerializeField] private float cameraViewRotationDegrees = 0f;
    [SerializeField] private bool logMapWorldSize = true;

    [Header("Grid Overlay")]
    [SerializeField] private int verticalGridDivisions = 4;
    [SerializeField] private int horizontalGridDivisions = 3;
    [SerializeField] private float gridLineWidth = 0.025f;
    [SerializeField] private Color gridLineColor = new Color(0.45f, 0.62f, 0.54f, 0.4f);

    [Header("Marker References")]
    [SerializeField] private RobotMarkerController robotMarker;
    [SerializeField] private RadarSweepController radarSweep;
    [SerializeField] private ArtifactMarkerController artifactMarker;
    [SerializeField] private GameObject robotBeaconPrefab;
    [SerializeField] private GameObject artifactMarkerPrefab;
    [SerializeField] private GameObject originMarkerPrefab;
    [SerializeField] private bool alignArtifactMarkerToCamera = true;

    [Header("Radar Visibility")]
    [SerializeField] private bool limitArtifactVisibilityToRadar = true;

    private Vector2 mapOriginMeters;
    private Vector2 mapSizeMeters;
    private Vector2 displaySizeUnits;
    private MeshRenderer freeCellGroundRenderer;
    private MeshFilter freeCellGroundMeshFilter;
    private Material freeCellGroundMaterialInstance;
    private SpriteRenderer mapBackgroundRenderer;
    private Transform originMarkerInstance;
    private Transform markerRoot;
    private Transform gridRoot;
    private bool initialized;
    private Vector2 currentArtifactMapPosition;
    private bool artifactExists;
    private Vector2Int lastGridDimensions;
    private float lastGridResolution;

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        UpdateFreeCellGroundTransform();
    }

    public void Initialize()
    {
        if (initialized)
        {
            return;
        }

        gameObject.name = "MapRoot";
        transform.position = Vector3.zero;
        mapOriginMeters = Vector2.zero;
        mapSizeMeters = defaultMapSizeMeters;
        displaySizeUnits = mapSizeMeters;

        CreateFreeCellGround();
        CreateMapBackground();
        AdjustCameraToFitMap();
        markerRoot = CreateChild("MarkerRoot").transform;
        robotMarker = EnsureRobotMarker();
        EnsureRadarSweep(robotMarker);
        artifactMarker = EnsureArtifactMarker();

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
        lastGridDimensions = new Vector2Int((int)grid.info.width, (int)grid.info.height);
        lastGridResolution = grid.info.resolution;

        Texture2D texture = CreateOccupancyTexture(grid);
        mapBackgroundRenderer.sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            texture.width / displaySizeUnits.x);
        mapBackgroundRenderer.sortingOrder = -10;
        UpdateFreeCellGroundTransform();
        UpdateMapBackgroundTransform();
        CreateGridLines();
        AdjustCameraToFitMap();
        LogMapWorldSize();

        UpdateOriginMarker(grid);
    }

    public void UpdateRobot(Vector2 mapPositionMeters, float headingDegrees)
    {
        robotMarker.SetPose(MapToLocalPosition(mapPositionMeters), headingDegrees);
        RefreshArtifactVisibility();
    }

    public void UpdateArtifact(Vector2 mapPositionMeters, bool visible)
    {
        currentArtifactMapPosition = mapPositionMeters;
        artifactExists = visible;
        artifactMarker.SetPosition(MapToLocalPosition(mapPositionMeters));
        RefreshArtifactVisibility();
    }

    public Vector3 MapToLocalPosition(Vector2 mapPositionMeters)
    {
        return new Vector3(mapPositionMeters.x, mapPositionMeters.y, 0f);
    }

    private void CreateFreeCellGround()
    {
        if (freeCellGroundMaterial == null)
        {
            return;
        }

        GameObject ground = CreateChild("MapGround");
        freeCellGroundMeshFilter = ground.GetComponent<MeshFilter>();
        if (freeCellGroundMeshFilter == null)
        {
            freeCellGroundMeshFilter = ground.AddComponent<MeshFilter>();
        }

        freeCellGroundRenderer = ground.GetComponent<MeshRenderer>();
        if (freeCellGroundRenderer == null)
        {
            freeCellGroundRenderer = ground.AddComponent<MeshRenderer>();
        }

        freeCellGroundMeshFilter.sharedMesh = CreateFreeCellGroundMesh();
        freeCellGroundMaterialInstance = new Material(freeCellGroundMaterial);
        freeCellGroundRenderer.sharedMaterial = freeCellGroundMaterialInstance;
        freeCellGroundRenderer.sortingOrder = freeCellGroundSortingOrder;
        UpdateFreeCellGroundTransform();
    }

    private Mesh CreateFreeCellGroundMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "MapGroundMesh";
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f)
        };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        mesh.RecalculateBounds();
        return mesh;
    }

    private void CreateMapBackground()
    {
        GameObject background = CreateChild("MapBackground");
        mapBackgroundRenderer = background.GetComponent<SpriteRenderer>();
        if (mapBackgroundRenderer == null)
        {
            mapBackgroundRenderer = background.AddComponent<SpriteRenderer>();
        }

        mapBackgroundRenderer.sprite = SpriteFactory.CreateSolidSprite("MapBackgroundSprite", backgroundColor);
        mapBackgroundRenderer.sortingOrder = -10;
        UpdateMapBackgroundTransform();

        CreateGridLines();
    }

    private Texture2D CreateOccupancyTexture(OccupancyGridMsg grid)
    {
        int width = (int)grid.info.width;
        int height = (int)grid.info.height;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] colors = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int sourceX = flipMapHorizontally ? width - 1 - x : x;
                int sourceY = flipMapVertically ? height - 1 - y : y;
                int sourceIndex = sourceY * width + sourceX;
                sbyte value = sourceIndex < grid.data.Length ? grid.data[sourceIndex] : (sbyte)-1;
                colors[y * width + x] = GetCellColor(value);
            }
        }

        texture.SetPixels(colors);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();
        return texture;
    }

    private void CreateGridLines()
    {
        gridRoot = CreateChild("GridRoot").transform;
        ClearChildren(gridRoot);

        Shader spriteShader = Shader.Find("Sprites/Default");
        Material lineMaterial = spriteShader != null ? new Material(spriteShader) : null;
        int safeVerticalDivisions = Mathf.Max(1, verticalGridDivisions);
        int safeHorizontalDivisions = Mathf.Max(1, horizontalGridDivisions);
        float minX = mapOriginMeters.x;
        float maxX = mapOriginMeters.x + mapSizeMeters.x;
        float minY = mapOriginMeters.y;
        float maxY = mapOriginMeters.y + mapSizeMeters.y;

        for (int i = 1; i < safeVerticalDivisions; i++)
        {
            float x = Mathf.Lerp(minX, maxX, i / (float)safeVerticalDivisions);
            CreateLine($"GridVertical{i}", new Vector3(x, minY, -0.1f), new Vector3(x, maxY, -0.1f), lineMaterial, gridLineColor);
        }

        for (int i = 1; i < safeHorizontalDivisions; i++)
        {
            float y = Mathf.Lerp(minY, maxY, i / (float)safeHorizontalDivisions);
            CreateLine($"GridHorizontal{i}", new Vector3(minX, y, -0.1f), new Vector3(maxX, y, -0.1f), lineMaterial, gridLineColor);
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
        line.startWidth = gridLineWidth;
        line.endWidth = gridLineWidth;
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
            RobotMarkerController existingRobotMarker = existingController != null ? existingController : existing.gameObject.AddComponent<RobotMarkerController>();
            EnsureRadarSweep(existingRobotMarker);
            return existingRobotMarker;
        }

        if (robotBeaconPrefab != null && existing != null)
        {
            existing.gameObject.SetActive(false);
        }

        GameObject markerObject = robotBeaconPrefab != null ? Instantiate(robotBeaconPrefab, markerRoot) : new GameObject("RobotMarker");

        markerObject.name = "RobotMarker";
        markerObject.transform.SetParent(markerRoot, false);

        RobotMarkerController controller = markerObject.GetComponent<RobotMarkerController>();
        controller = controller != null ? controller : markerObject.AddComponent<RobotMarkerController>();
        EnsureRadarSweep(controller);
        return controller;
    }

    private RadarSweepController EnsureRadarSweep(RobotMarkerController controller)
    {
        if (controller == null)
        {
            return null;
        }

        RadarSweepController controllerRadarSweep = controller.GetComponent<RadarSweepController>();
        if (controllerRadarSweep == null)
        {
            controllerRadarSweep = controller.gameObject.AddComponent<RadarSweepController>();
        }

        radarSweep = controllerRadarSweep;
        return radarSweep;
    }

    private ArtifactMarkerController EnsureArtifactMarker()
    {
        Transform existing = markerRoot.Find("ArtifactMarker");
        if (artifactMarkerPrefab == null && existing != null)
        {
            ArtifactMarkerController existingController = existing.GetComponent<ArtifactMarkerController>();
            return existingController != null ? existingController : existing.gameObject.AddComponent<ArtifactMarkerController>();
        }

        if (artifactMarkerPrefab != null && existing != null)
        {
            existing.gameObject.SetActive(false);
        }

        GameObject markerObject = artifactMarkerPrefab != null ? Instantiate(artifactMarkerPrefab, markerRoot) : new GameObject("ArtifactMarker");
        markerObject.name = "ArtifactMarker";
        markerObject.transform.SetParent(markerRoot, false);
        AlignMarkerToCamera(markerObject.transform);

        ArtifactMarkerController controller = markerObject.GetComponent<ArtifactMarkerController>();
        return controller != null ? controller : markerObject.AddComponent<ArtifactMarkerController>();
    }

    private void AlignMarkerToCamera(Transform markerTransform)
    {
        if (!alignArtifactMarkerToCamera || markerTransform == null || mapCamera == null)
        {
            return;
        }

        markerTransform.rotation = mapCamera.transform.rotation;
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

        originMarkerInstance.localPosition = new Vector3(0f, 0f, -0.9f);
        originMarkerInstance.localRotation = Quaternion.identity;
    }

    private void LateUpdate()
    {
        RefreshArtifactVisibility();
    }

    private void RefreshArtifactVisibility()
    {
        if (!initialized || artifactMarker == null)
        {
            return;
        }

        bool shouldShowArtifact = artifactExists;
        if (shouldShowArtifact && limitArtifactVisibilityToRadar)
        {
            shouldShowArtifact = IsArtifactInsideRadar();
        }

        artifactMarker.SetVisible(shouldShowArtifact);
    }

    private bool IsArtifactInsideRadar()
    {
        if (radarSweep == null)
        {
            radarSweep = robotMarker != null ? robotMarker.GetComponent<RadarSweepController>() : null;
        }

        if (radarSweep == null)
        {
            return false;
        }

        Vector3 artifactWorldPosition = transform.TransformPoint(MapToLocalPosition(currentArtifactMapPosition));
        return radarSweep.ContainsWorldPoint(artifactWorldPosition);
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
        float fittedSize = Mathf.Max(paddedHeight * 0.5f, paddedWidth * 0.5f / aspect);
        mapCamera.orthographicSize = Mathf.Max(0.01f, fittedSize * cameraZoomScale);
        Vector2 mapCenter = mapOriginMeters + mapSizeMeters * 0.5f;
        mapCamera.transform.position = new Vector3(mapCenter.x, mapCenter.y, mapCamera.transform.position.z);
        mapCamera.transform.rotation = Quaternion.Euler(0f, 0f, cameraViewRotationDegrees);
        AlignMarkerToCamera(artifactMarker != null ? artifactMarker.transform : null);
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
            $"grid: {lastGridDimensions.x} x {lastGridDimensions.y} @ {lastGridResolution:F3}m, " +
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

    private Color GetCellColor(sbyte occupancyValue)
    {
        if (occupancyValue == 0)
        {
            if (freeCellGroundMaterial != null)
            {
                return Color.clear;
            }

            return freeCellColor;
        }

        if (occupancyValue >= 100)
        {
            return occupiedCellColor;
        }

        return unknownCellColor;
    }

    private void UpdateFreeCellGroundTransform()
    {
        if (freeCellGroundRenderer == null)
        {
            return;
        }

        Vector2 mapCenter = mapOriginMeters + mapSizeMeters * 0.5f;
        freeCellGroundRenderer.transform.localPosition = new Vector3(mapCenter.x, mapCenter.y, 0.1f);
        freeCellGroundRenderer.transform.localRotation = Quaternion.identity;
        freeCellGroundRenderer.transform.localScale = new Vector3(displaySizeUnits.x, displaySizeUnits.y, 1f);
        freeCellGroundRenderer.sortingOrder = freeCellGroundSortingOrder;
        UpdateFreeCellGroundUv();
    }

    private void UpdateFreeCellGroundUv()
    {
        if (freeCellGroundMeshFilter == null || freeCellGroundMeshFilter.sharedMesh == null)
        {
            return;
        }

        float tileWidth = Mathf.Max(0.01f, freeCellTextureTileSizeMeters.x);
        float tileHeight = Mathf.Max(0.01f, freeCellTextureTileSizeMeters.y);
        float repeatX = mapSizeMeters.x / tileWidth;
        float repeatY = mapSizeMeters.y / tileHeight;

        freeCellGroundMeshFilter.sharedMesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(repeatX, 0f),
            new Vector2(0f, repeatY),
            new Vector2(repeatX, repeatY)
        };
    }

    private void UpdateMapBackgroundTransform()
    {
        if (mapBackgroundRenderer == null)
        {
            return;
        }

        Vector2 mapCenter = mapOriginMeters + mapSizeMeters * 0.5f;
        mapBackgroundRenderer.transform.localPosition = new Vector3(mapCenter.x, mapCenter.y, 0f);
        mapBackgroundRenderer.transform.localRotation = Quaternion.identity;

        if (mapBackgroundRenderer.sprite != null && mapBackgroundRenderer.sprite.texture != null)
        {
            mapBackgroundRenderer.transform.localScale = Vector3.one;
        }
        else
        {
            mapBackgroundRenderer.transform.localScale = new Vector3(displaySizeUnits.x, displaySizeUnits.y, 1f);
        }
    }
}
