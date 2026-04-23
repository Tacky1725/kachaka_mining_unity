using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private float initialTimeSeconds = 90f;
    [SerializeField] private bool autoStartTimer = true;

    [Header("Scene References")]
    [SerializeField] private MapViewController mapView;
    [SerializeField] private ScoreView scoreView;
    [SerializeField] private TimerView timerView;
    [SerializeField] private GameStateView stateView;
    [SerializeField] private PopupController popupController;
    [SerializeField] private DummyGameDataProvider dummyDataProvider;
    [SerializeField] private RosGameDataProvider rosDataProvider;

    private int score;
    private float timeRemaining;
    private bool timerRunning;
    private float elapsedTime;

    private void Awake()
    {
        EnsureCamera();
        EnsureMap();
        EnsureViews();
        EnsureRosIntegration();
        EnsureAudioRoot();
    }

    private void Start()
    {
        ResetGameState();
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        if (timerRunning)
        {
            timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);
            timerView.UpdateTime(timeRemaining);

            if (timeRemaining <= 0f)
            {
                timerRunning = false;
                stateView.UpdateState("Finished");
            }
        }

        if (dummyDataProvider != null && (rosDataProvider == null || !rosDataProvider.HasLiveRobotPose))
        {
            Vector2 robotPosition = dummyDataProvider.GetRobotPosition(elapsedTime);
            float robotHeading = dummyDataProvider.GetRobotHeadingDegrees(elapsedTime);
            mapView.UpdateRobot(robotPosition, robotHeading);
        }
    }

    public void ResetGameState()
    {
        score = 0;
        timeRemaining = initialTimeSeconds;
        timerRunning = autoStartTimer;
        elapsedTime = 0f;

        Vector2 artifactPosition = dummyDataProvider != null
            ? dummyDataProvider.GetArtifactPosition()
            : Vector2.zero;

        scoreView.UpdateScore(score);
        timerView.UpdateTime(timeRemaining);
        stateView.UpdateState(timerRunning ? "Playing" : "Ready");
        mapView.UpdateArtifact(artifactPosition, true);
    }

    public void AddScore(int amount)
    {
        score += amount;
        scoreView.UpdateScore(score);
        popupController.ShowScorePopup(amount);
    }

    private void EnsureCamera()
    {
        if (Camera.main != null)
        {
            return;
        }

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.backgroundColor = new Color(0.09f, 0.11f, 0.13f);
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        cameraObject.AddComponent<AudioListener>();
    }

    private void EnsureMap()
    {
        if (mapView == null)
        {
            GameObject mapRoot = new GameObject("MapRoot");
            mapView = mapRoot.AddComponent<MapViewController>();
        }

        mapView.Initialize();
    }

    private void EnsureViews()
    {
        UiBootstrapper.EnsureCanvas(
            out ScoreView createdScoreView,
            out TimerView createdTimerView,
            out GameStateView createdStateView,
            out PopupController createdPopupController);

        if (scoreView == null)
        {
            scoreView = createdScoreView;
        }

        if (timerView == null)
        {
            timerView = createdTimerView;
        }

        if (stateView == null)
        {
            stateView = createdStateView;
        }

        if (popupController == null)
        {
            popupController = createdPopupController;
        }

        if (dummyDataProvider == null)
        {
            dummyDataProvider = GetComponent<DummyGameDataProvider>();
        }

        if (dummyDataProvider == null)
        {
            dummyDataProvider = gameObject.AddComponent<DummyGameDataProvider>();
        }
    }

    private void EnsureAudioRoot()
    {
        GameObject audioRoot = GameObject.Find("AudioRoot");
        if (audioRoot == null)
        {
            audioRoot = new GameObject("AudioRoot");
        }

        if (audioRoot.GetComponent<AudioSource>() == null)
        {
            audioRoot.AddComponent<AudioSource>();
        }
    }

    private void EnsureRosIntegration()
    {
        if (rosDataProvider == null)
        {
            rosDataProvider = FindObjectOfType<RosGameDataProvider>();
        }

        if (rosDataProvider == null)
        {
            GameObject connectorObject = GameObject.Find("ROSConnector");
            if (connectorObject == null)
            {
                connectorObject = new GameObject("ROSConnector");
            }

            if (connectorObject.GetComponent<MapSubscriber>() == null)
            {
                connectorObject.AddComponent<MapSubscriber>();
            }

            if (connectorObject.GetComponent<RobotPoseSubscriber>() == null)
            {
                connectorObject.AddComponent<RobotPoseSubscriber>();
            }

            rosDataProvider = connectorObject.GetComponent<RosGameDataProvider>();
            if (rosDataProvider == null)
            {
                rosDataProvider = connectorObject.AddComponent<RosGameDataProvider>();
            }
        }

        rosDataProvider.Initialize(mapView);
    }
}
