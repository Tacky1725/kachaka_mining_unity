using RosMessageTypes.Nav;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

public class MapSubscriber : MonoBehaviour
{
    [SerializeField] private string topicName = "/original_map_ros2";

    private readonly object dataLock = new object();
    private OccupancyGridMsg latestMapMessage;
    private float lastMessageReceivedAt = -1f;
    private bool hasMap;

    private void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<OccupancyGridMsg>(topicName, MapMessageCallback);
    }

    private void MapMessageCallback(OccupancyGridMsg message)
    {
        if (message == null)
        {
            return;
        }

        lock (dataLock)
        {
            latestMapMessage = message;
            lastMessageReceivedAt = Time.realtimeSinceStartup;
            hasMap = true;
        }
    }

    public bool TryGetLatestMap(out OccupancyGridMsg mapMessage, out float receivedAt)
    {
        lock (dataLock)
        {
            if (!hasMap || latestMapMessage == null || latestMapMessage.info.width == 0 || latestMapMessage.info.height == 0)
            {
                mapMessage = null;
                receivedAt = -1f;
                return false;
            }

            mapMessage = latestMapMessage;
            receivedAt = lastMessageReceivedAt;
            return true;
        }
    }
}
