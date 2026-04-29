using UnityEngine;

public class RadarSweepController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RobotMarkerController robotMarker;
    [SerializeField] private Transform sensorTransform;

    [Header("Radar Shape")]
    [SerializeField] private float rangeMeters = 3.0f;
    [SerializeField] [Range(1f, 180f)] private float angleDegrees = 30f;
    [SerializeField] [Range(3, 64)] private int segmentCount = 24;

    [Header("Radar Motion")]
    [SerializeField] private float rotationSpeedDegreesPerSecond = 120f;
    [SerializeField] private float startAngleOffsetDegrees = 0f;

    [Header("Appearance")]
    [SerializeField] private Color radarColor = new Color(0.1f, 0.85f, 0.45f, 0.28f);
    [SerializeField] private int sortingOrder = 3;

    private const string RadarObjectName = "RadarSweep";

    private GameObject radarObject;
    private Transform radarTransform;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Material radarMaterial;
    private float currentRotationDegrees;
    private float lastRangeMeters = -1f;
    private float lastAngleDegrees = -1f;
    private int lastSegmentCount = -1;

    public Transform SensorTransform
    {
        get
        {
            ResolveReferences();
            return sensorTransform != null ? sensorTransform : transform;
        }
    }

    private void Awake()
    {
        ResolveReferences();
        EnsureRadarObject();
        RebuildMeshIfNeeded(force: true);
    }

    private void LateUpdate()
    {
        ResolveReferences();
        EnsureRadarObject();
        RebuildMeshIfNeeded(force: false);

        currentRotationDegrees += rotationSpeedDegreesPerSecond * Time.deltaTime;
        Transform sensor = SensorTransform;
        radarTransform.position = sensor.position;
        radarTransform.rotation = sensor.rotation * Quaternion.Euler(0f, 0f, startAngleOffsetDegrees + currentRotationDegrees);
        radarTransform.localScale = Vector3.one;
    }

    public bool ContainsWorldPoint(Vector3 worldPoint)
    {
        if (radarTransform == null)
        {
            return false;
        }

        Vector2 center = radarTransform.position;
        Vector2 target = worldPoint;
        Vector2 toTarget = target - center;
        if (toTarget.sqrMagnitude > rangeMeters * rangeMeters)
        {
            return false;
        }

        if (toTarget.sqrMagnitude <= Mathf.Epsilon)
        {
            return true;
        }

        Vector2 radarForward = radarTransform.right;
        float angleToTarget = Vector2.Angle(radarForward, toTarget);
        return angleToTarget <= angleDegrees * 0.5f;
    }

    private void ResolveReferences()
    {
        if (robotMarker == null)
        {
            robotMarker = GetComponentInParent<RobotMarkerController>();
        }

        if (sensorTransform == null && robotMarker != null)
        {
            sensorTransform = robotMarker.SensorTransform;
        }
    }

    private void EnsureRadarObject()
    {
        if (radarObject != null)
        {
            return;
        }

        Transform parent = transform.parent != null ? transform.parent : null;
        Transform existing = parent != null ? parent.Find(RadarObjectName) : null;
        radarObject = existing != null ? existing.gameObject : new GameObject(RadarObjectName);
        radarObject.transform.SetParent(parent, false);
        radarTransform = radarObject.transform;

        meshFilter = radarObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = radarObject.AddComponent<MeshFilter>();
        }

        meshRenderer = radarObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = radarObject.AddComponent<MeshRenderer>();
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            radarMaterial = new Material(shader);
            radarMaterial.color = radarColor;
            meshRenderer.sharedMaterial = radarMaterial;
        }

        meshRenderer.sortingOrder = sortingOrder;
    }

    private void RebuildMeshIfNeeded(bool force)
    {
        int safeSegments = Mathf.Max(3, segmentCount);
        float safeRange = Mathf.Max(0f, rangeMeters);
        float safeAngle = Mathf.Clamp(angleDegrees, 1f, 180f);
        if (!force && Mathf.Approximately(lastRangeMeters, safeRange) && Mathf.Approximately(lastAngleDegrees, safeAngle) && lastSegmentCount == safeSegments)
        {
            return;
        }

        lastRangeMeters = safeRange;
        lastAngleDegrees = safeAngle;
        lastSegmentCount = safeSegments;

        Mesh mesh = new Mesh();
        mesh.name = "RadarSweepMesh";

        Vector3[] vertices = new Vector3[safeSegments + 2];
        int[] triangles = new int[safeSegments * 3];
        vertices[0] = new Vector3(0f, 0f, -0.2f);

        float halfAngle = safeAngle * 0.5f;
        for (int i = 0; i <= safeSegments; i++)
        {
            float angle = -halfAngle + safeAngle * (i / (float)safeSegments);
            float radians = angle * Mathf.Deg2Rad;
            vertices[i + 1] = new Vector3(Mathf.Cos(radians) * safeRange, Mathf.Sin(radians) * safeRange, -0.2f);
        }

        for (int i = 0; i < safeSegments; i++)
        {
            int triangleIndex = i * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        meshFilter.sharedMesh = mesh;

        if (radarMaterial != null)
        {
            radarMaterial.color = radarColor;
        }

        if (meshRenderer != null)
        {
            meshRenderer.sortingOrder = sortingOrder;
        }
    }
}
