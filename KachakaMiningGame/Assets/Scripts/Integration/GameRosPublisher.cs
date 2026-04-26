using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

public class GameRosPublisher : MonoBehaviour
{
    [SerializeField] private string stateTopicName = "/game/state";
    [SerializeField] private string scoreTopicName = "/game/score";
    [SerializeField] private string timeRemainingTopicName = "/game/time_remaining";
    [SerializeField] private string startTopicName = "/game/start";

    private ROSConnection rosConnection;
    private bool initialized;

    public void Initialize()
    {
        if (initialized)
        {
            return;
        }

        rosConnection = ROSConnection.GetOrCreateInstance();
        rosConnection.RegisterPublisher<StringMsg>(stateTopicName);
        rosConnection.RegisterPublisher<Int32Msg>(scoreTopicName);
        rosConnection.RegisterPublisher<Int32Msg>(timeRemainingTopicName);
        rosConnection.RegisterPublisher<EmptyMsg>(startTopicName);
        initialized = true;
    }

    public void PublishState(string state)
    {
        if (!initialized)
        {
            return;
        }

        rosConnection.Publish(stateTopicName, new StringMsg(state));
    }

    public void PublishScore(int score)
    {
        if (!initialized)
        {
            return;
        }

        rosConnection.Publish(scoreTopicName, new Int32Msg(score));
    }

    public void PublishTimeRemaining(int secondsRemaining)
    {
        if (!initialized)
        {
            return;
        }

        rosConnection.Publish(timeRemainingTopicName, new Int32Msg(secondsRemaining));
    }

    public void PublishStart()
    {
        if (!initialized)
        {
            return;
        }

        rosConnection.Publish(startTopicName, new EmptyMsg());
    }
}
