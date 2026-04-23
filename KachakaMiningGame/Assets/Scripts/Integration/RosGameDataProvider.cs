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

    public bool HasLiveRobotPose
    {
        get
        {
            return lastRobotPoseAt >= 0f && Time.realtimeSinceStartup - lastRobotPoseAt <= messageTimeoutSeconds;
        }
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
        if (robotPoseSubscriber == null || !robotPoseSubscriber.TryGetLatestPose(out Vector2 position, out float headingDegrees, out float receivedAt))
        {
            return;
        }

        lastRobotPoseAt = receivedAt;
        mapView.UpdateRobot(position, headingDegrees);
    }
}
