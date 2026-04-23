using UnityEngine;

public class RobotBeaconController : MonoBehaviour
{
    [SerializeField] private bool drivePoseFromSubscriber = false;
    [SerializeField] private RobotPoseSubscriber robotPoseSubscriber;
    [SerializeField] private Transform beaconsRoot;
    [SerializeField] private GameObject robotBeaconPrefab;
    [SerializeField] private string robotBeaconName = "RobotBeacon";
    [SerializeField] private float beaconWorldScale = 1.0f;
    [SerializeField] private float headingOffsetDegrees = 0.0f;
    [SerializeField] private float poseTimeoutSeconds = 0.5f;
    [SerializeField] private float positionLerpSpeed = 20f;
    [SerializeField] private float rotationLerpSpeed = 20f;

    private GameObject robotBeaconInstance;
    private bool hasAppliedInitialPose;
    private Vector3 beaconBaseLocalScale = Vector3.one;
    private bool hasBeaconBaseScale;
    private Renderer[] beaconRenderers;

    private void Start()
    {
        SetRobotBeaconInstance(gameObject);

        if (!drivePoseFromSubscriber)
        {
            return;
        }

        ResolveReferences();
        EnsureRobotBeacon();
        SetRobotBeaconVisible(false);
    }

    private void Update()
    {
        if (!drivePoseFromSubscriber)
        {
            return;
        }

        EnsureRobotBeacon();

        if (robotPoseSubscriber == null || robotBeaconInstance == null)
        {
            return;
        }

        ApplyBeaconWorldScale();

        if (!robotPoseSubscriber.TryGetLatestPose(out Vector3 rosPosition, out Quaternion rosRotation, out float receivedAt))
        {
            SetRobotBeaconVisible(false);
            return;
        }

        if (Time.realtimeSinceStartup - receivedAt > poseTimeoutSeconds)
        {
            SetRobotBeaconVisible(false);
            return;
        }

        SetRobotBeaconVisible(true);
        Vector3 targetPosition = ConvertRosToUnityPosition(rosPosition);
        Quaternion targetRotation = ConvertRosToUnityRotation(rosRotation, headingOffsetDegrees);

        if (!hasAppliedInitialPose)
        {
            robotBeaconInstance.transform.position = targetPosition;
            robotBeaconInstance.transform.rotation = targetRotation;
            hasAppliedInitialPose = true;
            return;
        }

        float positionBlend = 1f - Mathf.Exp(-positionLerpSpeed * Time.deltaTime);
        float rotationBlend = 1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime);

        robotBeaconInstance.transform.position = Vector3.Lerp(robotBeaconInstance.transform.position, targetPosition, positionBlend);
        robotBeaconInstance.transform.rotation = Quaternion.Slerp(robotBeaconInstance.transform.rotation, targetRotation, rotationBlend);
    }

    private void ResolveReferences()
    {
        if (robotPoseSubscriber == null)
        {
            robotPoseSubscriber = GameObject.Find("ROSConnector")?.GetComponent<RobotPoseSubscriber>();
        }

        if (beaconsRoot == null)
        {
            GameObject beaconsObject = GameObject.Find("Beacons");
            beaconsRoot = beaconsObject != null ? beaconsObject.transform : transform;
        }
    }

    private void EnsureRobotBeacon()
    {
        if (robotBeaconInstance != null)
        {
            return;
        }

        if (beaconsRoot == null)
        {
            ResolveReferences();
            if (beaconsRoot == null)
            {
                return;
            }
        }

        Transform existingBeacon = beaconsRoot.Find(robotBeaconName);
        if (existingBeacon != null)
        {
            SetRobotBeaconInstance(existingBeacon.gameObject);
            return;
        }

        if (robotBeaconPrefab != null)
        {
            GameObject createdBeacon = Instantiate(robotBeaconPrefab, beaconsRoot);
            createdBeacon.name = robotBeaconName;
            SetRobotBeaconInstance(createdBeacon);
            return;
        }

        SetRobotBeaconInstance(gameObject);
    }

    private void SetRobotBeaconVisible(bool isVisible)
    {
        if (robotBeaconInstance == null || robotBeaconInstance.activeSelf == isVisible)
        {
            return;
        }

        robotBeaconInstance.SetActive(isVisible);

        if (!isVisible)
        {
            hasAppliedInitialPose = false;
        }
    }

    private static Quaternion ConvertRosToUnityRotation(Quaternion rosQuaternion, float offsetDegrees)
    {
        float x = rosQuaternion.x;
        float y = rosQuaternion.y;
        float z = rosQuaternion.z;
        float w = rosQuaternion.w;

        float sinyCosp = 2f * (w * z + x * y);
        float cosyCosp = 1f - 2f * (y * y + z * z);
        float yawRos = Mathf.Atan2(sinyCosp, cosyCosp);
        float yawUnity = yawRos - 90.0f * Mathf.Deg2Rad + offsetDegrees * Mathf.Deg2Rad;
        return Quaternion.Euler(0f, 0f, yawUnity * Mathf.Rad2Deg);
    }

    private static Vector3 ConvertRosToUnityPosition(Vector3 rosPosition)
    {
        return new Vector3(rosPosition.x, rosPosition.y, -rosPosition.z);
    }

    private void SetRobotBeaconInstance(GameObject beacon)
    {
        robotBeaconInstance = beacon;
        beaconBaseLocalScale = robotBeaconInstance.transform.localScale;
        hasBeaconBaseScale = true;
        beaconRenderers = robotBeaconInstance.GetComponentsInChildren<Renderer>(true);
        ApplyBeaconWorldScale();
    }

    private void ApplyBeaconWorldScale()
    {
        if (robotBeaconInstance == null || !hasBeaconBaseScale)
        {
            return;
        }

        float targetWorldScale = Mathf.Max(0.0001f, beaconWorldScale);
        float currentVisualScale = GetCurrentVisualWorldScale();
        if (currentVisualScale > 0.0001f)
        {
            float scaleFactor = targetWorldScale / currentVisualScale;
            if (Mathf.Abs(1.0f - scaleFactor) > 0.0001f)
            {
                robotBeaconInstance.transform.localScale *= scaleFactor;
            }

            return;
        }

        Transform parent = robotBeaconInstance.transform.parent;
        Vector3 parentLossyScale = parent != null ? parent.lossyScale : Vector3.one;
        float safeParentX = Mathf.Abs(parentLossyScale.x) < 0.0001f ? 1.0f : parentLossyScale.x;
        float baseWorldScaleX = beaconBaseLocalScale.x * safeParentX;
        if (Mathf.Abs(baseWorldScaleX) < 0.0001f)
        {
            baseWorldScaleX = 1.0f;
        }

        float fallbackScaleFactor = targetWorldScale / Mathf.Abs(baseWorldScaleX);
        robotBeaconInstance.transform.localScale = beaconBaseLocalScale * fallbackScaleFactor;
    }

    private float GetCurrentVisualWorldScale()
    {
        if (beaconRenderers == null || beaconRenderers.Length == 0)
        {
            return -1.0f;
        }

        Bounds mergedBounds = new Bounds();
        bool hasBounds = false;
        foreach (Renderer beaconRenderer in beaconRenderers)
        {
            if (beaconRenderer == null || !beaconRenderer.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                mergedBounds = beaconRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                mergedBounds.Encapsulate(beaconRenderer.bounds);
            }
        }

        if (!hasBounds)
        {
            return -1.0f;
        }

        return Mathf.Max(mergedBounds.size.x, mergedBounds.size.y);
    }
}
