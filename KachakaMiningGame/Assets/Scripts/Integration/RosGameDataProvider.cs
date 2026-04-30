using RosMessageTypes.Nav;
using UnityEngine;

public class RosGameDataProvider : MonoBehaviour
{
    [SerializeField] private MapSubscriber mapSubscriber;
    [SerializeField] private RobotPoseSubscriber robotPoseSubscriber;
    [SerializeField] private float messageTimeoutSeconds = 2f;

    private MapViewController mapView;
    private float lastAppliedMapAt = -1f;
    private float lastRobotPoseAt = -1f;
    private bool robotPoseUpdatesEnabled = true;

    public bool HasLiveRobotPose
    {
        get
        {
            return lastRobotPoseAt >= 0f && Time.realtimeSinceStartup - lastRobotPoseAt <= messageTimeoutSeconds;
        }
    }

    public bool TryGetLiveRobotPose(out Vector2 position, out float headingDegrees)
    {
        if (!HasLiveRobotPose || robotPoseSubscriber == null || !robotPoseSubscriber.TryGetLatestPose(out position, out headingDegrees, out float receivedAt))
        {
            position = Vector2.zero;
            headingDegrees = 0f;
            return false;
        }

        lastRobotPoseAt = receivedAt;
        return true;
    }

    public void Initialize(MapViewController targetMapView)
    {
        mapView = targetMapView;

        if (mapSubscriber == null)
        {
            mapSubscriber = GetComponent<MapSubscriber>();
        }

        if (robotPoseSubscriber == null)
        {
            robotPoseSubscriber = GetComponent<RobotPoseSubscriber>();
        }
    }

    public void SetRobotPoseUpdatesEnabled(bool enabled)
    {
        robotPoseUpdatesEnabled = enabled;
    }

    private void Update()
    {
        if (mapView == null)
        {
            return;
        }

        ApplyMapIfUpdated();
        ApplyRobotPoseIfAvailable();
    }

    private void ApplyMapIfUpdated()
    {
        if (mapSubscriber == null || !mapSubscriber.TryGetLatestMap(out OccupancyGridMsg mapMessage, out float receivedAt))
        {
            return;
        }

        if (Mathf.Approximately(receivedAt, lastAppliedMapAt))
        {
            return;
        }

        mapView.UpdateOccupancyGrid(mapMessage);
        lastAppliedMapAt = receivedAt;
    }

    private void ApplyRobotPoseIfAvailable()
    {
        if (!robotPoseUpdatesEnabled)
        {
            return;
        }

        if (robotPoseSubscriber == null || !robotPoseSubscriber.TryGetLatestPose(out Vector2 position, out float headingDegrees, out float receivedAt))
        {
            return;
        }

        lastRobotPoseAt = receivedAt;
        mapView.UpdateRobot(position, headingDegrees);
    }
}
