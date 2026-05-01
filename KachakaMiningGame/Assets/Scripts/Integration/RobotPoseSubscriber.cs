using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;
using extension;

public class RobotPoseSubscriber : MonoBehaviour
{
    [SerializeField] private string topicName = "/unity/robot_pose_map";

    private readonly object dataLock = new object();
    private PoseStampedMsg latestPoseMessage;
    private float lastMessageReceivedAt = -1f;
    private bool hasPose;
    private bool acceptingMessages = true;

    private void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<PoseStampedMsg>(topicName, PoseMessageCallback);
    }

    private void PoseMessageCallback(PoseStampedMsg message)
    {
        if (message == null)
        {
            return;
        }

        lock (dataLock)
        {
            if (!acceptingMessages)
            {
                return;
            }

            latestPoseMessage = message;
            lastMessageReceivedAt = Time.realtimeSinceStartup;
            hasPose = true;
        }
    }

    public void PauseAndDiscard()
    {
        lock (dataLock)
        {
            acceptingMessages = false;
            ClearLatestPoseLocked();
        }
    }

    public void ResumeFresh()
    {
        lock (dataLock)
        {
            ClearLatestPoseLocked();
            acceptingMessages = true;
        }
    }

    public bool TryGetLatestPose(out Vector2 mapPositionMeters, out float headingDegrees, out float receivedAt)
    {
        lock (dataLock)
        {
            if (!hasPose || latestPoseMessage == null)
            {
                mapPositionMeters = Vector2.zero;
                headingDegrees = 0f;
                receivedAt = -1f;
                return false;
            }

            PoseMsg pose = latestPoseMessage.pose;
            Vector3 unityPosition = pose.position.rosMsg2Unity();
            Quaternion unityRotation = pose.orientation.rosMsg2Unity().Ros2Unity();
            mapPositionMeters = new Vector2(unityPosition.x, unityPosition.y);
            headingDegrees = unityRotation.eulerAngles.z;
            receivedAt = lastMessageReceivedAt;
            return true;
        }
    }

    public bool TryGetLatestPose(out Vector3 rosPosition, out Quaternion rosRotation, out float receivedAt)
    {
        lock (dataLock)
        {
            if (!hasPose || latestPoseMessage == null)
            {
                rosPosition = Vector3.zero;
                rosRotation = Quaternion.identity;
                receivedAt = -1f;
                return false;
            }

            PoseMsg pose = latestPoseMessage.pose;
            rosPosition = pose.position.rosMsg2Unity();
            rosRotation = pose.orientation.rosMsg2Unity();
            receivedAt = lastMessageReceivedAt;
            return true;
        }
    }

    private void ClearLatestPoseLocked()
    {
        latestPoseMessage = null;
        lastMessageReceivedAt = -1f;
        hasPose = false;
    }
}
