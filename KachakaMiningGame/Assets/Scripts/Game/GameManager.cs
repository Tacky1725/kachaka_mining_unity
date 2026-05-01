using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // ゲーム進行は waiting / playing / finished の状態機械で管理する。
    private enum GameSessionState
    {
        Waiting,
        Countdown,
        Playing,
        Paused,
        Finished,
    }

    [Header("Game Settings")]
    [SerializeField]
    private float initialTimeSeconds = 90f;

    [SerializeField]
    private float excavationDistanceThresholdMeters = 0.3f;

    [SerializeField]
    private int startCountdownSeconds = 3;

    [SerializeField]
    private float startTextDisplaySeconds = 0.5f;

    [SerializeField]
    private float collectedMarkerDisplaySeconds = 1f;

    [SerializeField]
    private float bombSpinDurationSeconds = 2f;

    [SerializeField]
    private float bombSpinDegreesPerSecond = 720f;

    [Header("Scene References")]
    [SerializeField]
    private MapViewController mapView;

    [SerializeField]
    private ScoreView scoreView;

    [SerializeField]
    private TimerView timerView;

    [SerializeField]
    private GameStateView stateView;

    [SerializeField]
    private PopupController popupController;

    [SerializeField]
    private StartScreenView startScreenView;

    [SerializeField]
    private FinishScreenView finishScreenView;

    [SerializeField]
    private PauseMenuView pauseMenuView;

    [SerializeField]
    private AudioSource excavationAudioSource;

    [SerializeField]
    private AudioClip excavationSuccessClip;

    [SerializeField]
    private DummyGameDataProvider dummyDataProvider;

    [SerializeField]
    private RosGameDataProvider rosDataProvider;

    [SerializeField]
    private GameRosPublisher gameRosPublisher;

    [Header("Input Feedback")]
    [SerializeField]
    private GameInputController gameInput;

    [SerializeField]
    private JoyconRumbleService joyconRumbleService;

    [Header("SE")]
    [SerializeField]
    private AudioClip startButtonSound;

    [SerializeField]
    private AudioClip backButtonSound;

    [SerializeField]
    private AudioClip countdownNumberSound;

    [SerializeField]
    private AudioClip countdownStartSound;

    [SerializeField]
    private AudioClip timeWarningNumberSound;

    [SerializeField]
    private AudioClip timeUpSound;

    [Header("BGM")]
    [SerializeField]
    private AudioSource bgmAudioSource;

    [SerializeField]
    private AudioClip waitingBgmClip;

    [SerializeField]
    private AudioClip playingBgmClip;

    [SerializeField]
    private AudioClip finishedBgmClip;

    [SerializeField]
    [Range(0f, 1f)]
    private float bgmVolume = 0.35f;

    [Header("Artifact Definitions")]
    [SerializeField]
    private ArtifactCatalog artifactCatalog;

    private int score;
    private float timeRemaining;
    private bool timerRunning;
    private float elapsedTime;
    private Vector2 currentRobotPosition;
    private float currentRobotHeading;
    private Vector2 currentArtifactPosition;
    private ArtifactKind currentArtifactKind = ArtifactKind.Coal;
    private bool artifactAvailable;
    private readonly List<ArtifactInstance> currentArtifacts = new List<ArtifactInstance>();
    private bool robotSpinActive;
    private float robotSpinStartedAt;
    private Vector2 robotSpinPosition;
    private float robotSpinStartHeading;
    private int lastPublishedTimeSeconds = -1;
    private int lastTimeWarningSecondPlayed = -1;
    private GameSessionState currentState = GameSessionState.Waiting;
    private GameSessionState stateBeforePause = GameSessionState.Waiting;
    private ScoreHistoryRepository scoreHistoryRepository;
    private Coroutine startCountdownRoutine;
    private int countdownSecondsRemaining;

    private void Awake()
    {
        EnsureCamera();
        BindSceneReferences();
        EnsureInputController();
        EnsureMap();
        EnsureViews();
        EnsureRosIntegration();
        EnsureAudioRoot();
        EnsureBgmAudioSource();
        EnsureRosPublisher();
        scoreHistoryRepository = new ScoreHistoryRepository();
    }

    private void Start()
    {
        ConfigureUiCallbacks();
        EnterWaitingState();
    }

    // タイマー進行、ロボット位置更新、採掘判定、残り時間のパブリッシュ
    private void Update()
    {
        HandleInputShortcuts();
        if (currentState == GameSessionState.Paused)
        {
            return;
        }

        elapsedTime += Time.deltaTime;

        if (timerRunning)
        {
            // タイマーは playing 中だけ起動
            timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);
            UpdateTimerDisplay(timeRemaining);
            PlayTimeWarningSoundIfNeeded();

            if (timeRemaining <= 0f)
            {
                FinishGame();
            }
        }

        if (robotSpinActive)
        {
            UpdateRobotBombSpin();
        }
        else
        {
            bool appliedLiveRobotPose = false;
            if (
                rosDataProvider != null
                && rosDataProvider.TryGetLiveRobotPose(
                    out Vector2 liveRobotPosition,
                    out float liveRobotHeading
                )
            )
            {
                // ROS から実データが来ている間はそちらを優先して使う
                UpdateRobotPose(liveRobotPosition, liveRobotHeading);
                appliedLiveRobotPose = true;
            }

            if (dummyDataProvider != null && !appliedLiveRobotPose)
            {
                // ROS 未接続時でも確認できるよう、ダミー移動へフォールバック
                Vector2 robotPosition = dummyDataProvider.GetRobotPosition(elapsedTime);
                float robotHeading = dummyDataProvider.GetRobotHeadingDegrees(elapsedTime);
                UpdateRobotPose(robotPosition, robotHeading);
            }
        }

        TryHandleExcavation();
        PublishTimeRemainingIfChanged();
    }

    // waiting 状態から新しいゲームプレイを開始
    public void StartGame()
    {
        if (currentState != GameSessionState.Waiting)
        {
            return;
        }

        StartCountdown();
    }

    private void StartCountdown()
    {
        StopStartCountdown();
        timerRunning = false;
        currentState = GameSessionState.Countdown;
        countdownSecondsRemaining = Mathf.Max(0, startCountdownSeconds);
        SetRobotSpinActive(false);
        UpdateStateDisplay("Countdown");
        PlayBgm(null);
        UpdateArtifactState(Vector2.zero, false);
        SetPauseMenuVisible(false);
        SetStartScreenVisible(false);
        SetFinishScreenVisible(false);
        startCountdownRoutine = StartCoroutine(StartCountdownRoutine(countdownSecondsRemaining));
    }

    private System.Collections.IEnumerator StartCountdownRoutine(int secondsRemaining)
    {
        int countdownSeconds = Mathf.Max(0, secondsRemaining);
        for (int seconds = countdownSeconds; seconds > 0; seconds--)
        {
            countdownSecondsRemaining = seconds;
            PlayOneShot(countdownNumberSound);
            ShowCountdownPopup(seconds.ToString(), 1f);
            yield return new WaitForSeconds(1f);
        }

        countdownSecondsRemaining = 0;
        PlayOneShot(countdownStartSound);
        ShowCountdownPopup("START", startTextDisplaySeconds);
        yield return new WaitForSeconds(Mathf.Max(0f, startTextDisplaySeconds));
        startCountdownRoutine = null;
        BeginPlaying();
    }

    private void ShowCountdownPopup(string text, float displaySeconds)
    {
        if (popupController != null)
        {
            popupController.ShowTextPopup(text, displaySeconds);
        }
    }

    private void StopStartCountdown()
    {
        if (startCountdownRoutine != null)
        {
            StopCoroutine(startCountdownRoutine);
            startCountdownRoutine = null;
        }

        if (popupController != null)
        {
            popupController.HidePopup();
        }
    }

    private void BeginPlaying()
    {
        score = 0;
        timeRemaining = initialTimeSeconds;
        timerRunning = true;
        elapsedTime = 0f;
        currentState = GameSessionState.Playing;
        lastPublishedTimeSeconds = -1;
        lastTimeWarningSecondPlayed = -1;
        SetRobotSpinActive(false);

        IReadOnlyList<ArtifactInstance> artifacts =
            dummyDataProvider != null ? dummyDataProvider.ResetArtifactSpawns() : null;

        // HUD を初期化し、最初の化石を配置して、オーバーレイ画面からプレイ状態へ切り替える
        UpdateScoreDisplay(score);
        UpdateTimerDisplay(timeRemaining);
        UpdateStateDisplay("Playing");
        PlayBgm(playingBgmClip);
        UpdateArtifactState(artifacts, dummyDataProvider != null);
        SetPauseMenuVisible(false);
        SetStartScreenVisible(false);
        SetFinishScreenVisible(false);
        PublishScore();
        PublishTimeRemaining(forcePublish: true);
        PublishPlayingStarted();
    }

    // スコア更新と、採掘成功時の軽い演出をまとめて処理
    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreDisplay(score);
        if (popupController != null)
        {
            popupController.ShowScorePopup(amount);
        }

        PublishScore();
    }

    // 採掘成功後に次の化石出現位置へ再配置
    public void RespawnArtifact()
    {
        IReadOnlyList<ArtifactInstance> artifacts =
            dummyDataProvider != null ? dummyDataProvider.RespawnArtifacts() : null;

        UpdateArtifactState(artifacts, dummyDataProvider != null);
    }

    private void HandleInputShortcuts()
    {
        if (gameInput == null)
        {
            return;
        }

        switch (currentState)
        {
            case GameSessionState.Waiting:
                if (gameInput.GetButtonDown(GameInputAction.Confirm))
                {
                    HandleStartButtonRequested();
                }

                break;
            case GameSessionState.Countdown:
            case GameSessionState.Playing:
                if (gameInput.GetButtonDown(GameInputAction.Confirm))
                {
                    EnterPauseState();
                }

                break;
            case GameSessionState.Paused:
                if (gameInput.GetButtonDown(GameInputAction.Quit))
                {
                    HandlePauseQuitGameRequested();
                }
                else if (gameInput.GetButtonDown(GameInputAction.Confirm))
                {
                    HandlePauseBackRequested();
                }

                break;
            case GameSessionState.Finished:
                if (gameInput.GetButtonDown(GameInputAction.Confirm))
                {
                    HandleBackButtonRequested();
                }

                break;
        }
    }

    private void EnterPauseState()
    {
        if (currentState != GameSessionState.Countdown && currentState != GameSessionState.Playing)
        {
            return;
        }

        stateBeforePause = currentState;
        PlayOneShot(startButtonSound);
        timerRunning = false;
        if (stateBeforePause == GameSessionState.Countdown)
        {
            StopStartCountdown();
        }

        if (stateBeforePause == GameSessionState.Countdown && bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Pause();
        }

        currentState = GameSessionState.Paused;
        UpdateStateDisplay("Paused");
        SetStartScreenVisible(false);
        SetFinishScreenVisible(false);
        SetPauseMenuVisible(true);
    }

    private void ResumeFromPause()
    {
        if (currentState != GameSessionState.Paused)
        {
            return;
        }

        SetPauseMenuVisible(false);
        if (stateBeforePause == GameSessionState.Countdown)
        {
            currentState = GameSessionState.Countdown;
            UpdateStateDisplay("Countdown");
            if (countdownSecondsRemaining > 0)
            {
                startCountdownRoutine = StartCoroutine(
                    StartCountdownRoutine(countdownSecondsRemaining)
                );
            }
            else
            {
                BeginPlaying();
            }

            return;
        }

        currentState = GameSessionState.Playing;
        timerRunning = true;
        UpdateStateDisplay("Playing");
        if (bgmAudioSource != null)
        {
            if (bgmAudioSource.clip == playingBgmClip)
            {
                bgmAudioSource.UnPause();
            }
            else
            {
                PlayBgm(playingBgmClip);
            }
        }
    }

    // ロボット位置の更新
    private void UpdateRobotPose(Vector2 robotPosition, float robotHeading)
    {
        currentRobotPosition = robotPosition;
        currentRobotHeading = robotHeading;
        if (mapView != null)
        {
            mapView.UpdateRobot(robotPosition, robotHeading);
        }
    }

    // 採掘成功判定
    private void TryHandleExcavation()
    {
        if (
            currentState != GameSessionState.Playing
            || !timerRunning
            || !artifactAvailable
            || robotSpinActive
        )
        {
            return;
        }

        int collectedArtifactIndex = FindCollectedArtifactIndex();
        if (collectedArtifactIndex < 0)
        {
            return;
        }

        // 同じ化石で多重加点しないよう、成功時点でいったん非表示にしてからスコア加算と再配置
        ArtifactInstance collectedArtifact = currentArtifacts[collectedArtifactIndex];
        ArtifactKind collectedArtifactKind = collectedArtifact.Kind;
        ArtifactDefinition collectedArtifactDefinition = GetArtifactDefinition(
            collectedArtifactKind
        );
        if (mapView != null)
        {
            mapView.ShowCollectedArtifactMarker(
                collectedArtifact.Position,
                collectedArtifactKind,
                collectedMarkerDisplaySeconds
            );
        }

        if (joyconRumbleService != null)
        {
            if (collectedArtifactDefinition != null)
            {
                joyconRumbleService.PlayArtifactCollectedRumble(
                    collectedArtifactDefinition.RumbleAmplitude,
                    collectedArtifactDefinition.RumbleDurationMilliseconds
                );
            }
            else
            {
                joyconRumbleService.PlayArtifactCollectedRumble();
            }
        }

        ApplyArtifactEffect(collectedArtifactKind);
        IReadOnlyList<ArtifactInstance> artifacts =
            dummyDataProvider != null
                ? dummyDataProvider.CollectArtifactAt(collectedArtifactIndex)
                : null;
        UpdateArtifactState(artifacts, dummyDataProvider != null);
    }

    private int FindCollectedArtifactIndex()
    {
        for (int i = 0; i < currentArtifacts.Count; i++)
        {
            float distanceToArtifact = Vector2.Distance(
                currentRobotPosition,
                currentArtifacts[i].Position
            );
            if (distanceToArtifact <= excavationDistanceThresholdMeters)
            {
                return i;
            }
        }

        return -1;
    }

    private void ApplyArtifactEffect(ArtifactKind artifactKind)
    {
        ArtifactDefinition definition = GetArtifactDefinition(artifactKind);
        int scoreAmount =
            definition != null ? definition.ScoreAmount : GetFallbackScoreAmount(artifactKind);
        if (scoreAmount > 0)
        {
            AddScore(scoreAmount);
        }

        PlayArtifactSound(definition);

        bool triggersSpin =
            definition != null ? definition.TriggersSpin : artifactKind == ArtifactKind.Bomb;
        if (triggersSpin)
        {
            StartRobotBombSpin();
        }
    }

    private ArtifactDefinition GetArtifactDefinition(ArtifactKind artifactKind)
    {
        return artifactCatalog != null ? artifactCatalog.GetDefinition(artifactKind) : null;
    }

    private int GetFallbackScoreAmount(ArtifactKind artifactKind)
    {
        switch (artifactKind)
        {
            case ArtifactKind.Stone:
                return 3;
            case ArtifactKind.Bomb:
                return 0;
            case ArtifactKind.Coal:
            default:
                return 1;
        }
    }

    private void PlayArtifactSound(ArtifactDefinition definition)
    {
        AudioClip collectSound =
            definition != null && definition.CollectSound != null
                ? definition.CollectSound
                : excavationSuccessClip;
        PlayOneShot(collectSound);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (excavationAudioSource == null || clip == null)
        {
            return;
        }

        excavationAudioSource.PlayOneShot(clip);
    }

    private void PlayTimeWarningSoundIfNeeded()
    {
        if (currentState != GameSessionState.Playing || !timerRunning)
        {
            return;
        }

        int remainingSeconds = Mathf.CeilToInt(timeRemaining);
        if (
            remainingSeconds < 1
            || remainingSeconds > 3
            || remainingSeconds == lastTimeWarningSecondPlayed
        )
        {
            return;
        }

        lastTimeWarningSecondPlayed = remainingSeconds;
        PlayOneShot(timeWarningNumberSound != null ? timeWarningNumberSound : countdownNumberSound);
    }

    // 化石の内部状態と画面表示状態を同期
    private void UpdateArtifactState(Vector2 artifactPosition, bool visible)
    {
        UpdateArtifactState(artifactPosition, currentArtifactKind, visible);
    }

    private void UpdateArtifactState(
        Vector2 artifactPosition,
        ArtifactKind artifactKind,
        bool visible
    )
    {
        currentArtifactPosition = artifactPosition;
        currentArtifactKind = artifactKind;
        currentArtifacts.Clear();
        if (visible)
        {
            currentArtifacts.Add(new ArtifactInstance(artifactPosition, artifactKind));
        }

        artifactAvailable = visible;
        if (mapView != null)
        {
            mapView.UpdateArtifact(artifactPosition, visible, artifactKind);
        }
    }

    private void UpdateArtifactState(IReadOnlyList<ArtifactInstance> artifacts, bool visible)
    {
        currentArtifacts.Clear();
        if (visible && artifacts != null)
        {
            for (int i = 0; i < artifacts.Count; i++)
            {
                currentArtifacts.Add(artifacts[i]);
            }
        }

        artifactAvailable = currentArtifacts.Count > 0;
        if (artifactAvailable)
        {
            currentArtifactPosition = currentArtifacts[0].Position;
            currentArtifactKind = currentArtifacts[0].Kind;
        }

        if (mapView != null)
        {
            mapView.UpdateArtifacts(currentArtifacts, visible);
        }
    }

    private void StartRobotBombSpin()
    {
        PublishSpinTrigger();
        SetRobotSpinActive(true);
        robotSpinStartedAt = Time.time;
        robotSpinPosition = currentRobotPosition;
        robotSpinStartHeading = currentRobotHeading;
        UpdateRobotBombSpin();
    }

    private void UpdateRobotBombSpin()
    {
        float elapsedSpinTime = Time.time - robotSpinStartedAt;
        if (elapsedSpinTime >= bombSpinDurationSeconds)
        {
            SetRobotSpinActive(false);
            UpdateRobotPose(
                robotSpinPosition,
                robotSpinStartHeading + bombSpinDurationSeconds * bombSpinDegreesPerSecond
            );
            return;
        }

        UpdateRobotPose(
            robotSpinPosition,
            robotSpinStartHeading + elapsedSpinTime * bombSpinDegreesPerSecond
        );
    }

    private void SetRobotSpinActive(bool active)
    {
        if (robotSpinActive == active)
        {
            if (mapView != null)
            {
                mapView.SetRadarVisible(!active);
            }

            return;
        }

        robotSpinActive = active;
        if (rosDataProvider != null)
        {
            if (active)
            {
                rosDataProvider.PauseRobotPoseStream();
            }
            else
            {
                rosDataProvider.ResumeRobotPoseStreamFresh();
            }
        }

        if (mapView != null)
        {
            mapView.SetRadarVisible(!active);
        }
    }

    // シーン配置済みオブジェクトを探し、無ければ実行時生成へフォールバック
    private void BindSceneReferences()
    {
        if (mapView == null)
        {
            mapView = FindObjectOfType<MapViewController>();
        }

        if (scoreView == null)
        {
            scoreView = FindObjectOfType<ScoreView>();
        }

        if (timerView == null)
        {
            timerView = FindObjectOfType<TimerView>();
        }

        if (stateView == null)
        {
            stateView = FindObjectOfType<GameStateView>();
        }

        if (popupController == null)
        {
            popupController = FindObjectOfType<PopupController>();
        }

        if (startScreenView == null)
        {
            startScreenView = FindObjectOfType<StartScreenView>();
        }

        if (finishScreenView == null)
        {
            finishScreenView = FindObjectOfType<FinishScreenView>();
        }

        if (pauseMenuView == null)
        {
            pauseMenuView = FindObjectOfType<PauseMenuView>(true);
        }

        if (dummyDataProvider == null)
        {
            dummyDataProvider = GetComponent<DummyGameDataProvider>();
        }

        if (rosDataProvider == null)
        {
            rosDataProvider = FindObjectOfType<RosGameDataProvider>();
        }

        if (gameRosPublisher == null)
        {
            gameRosPublisher = FindObjectOfType<GameRosPublisher>();
        }

        if (gameInput == null)
        {
            gameInput = FindObjectOfType<GameInputController>();
        }

        if (joyconRumbleService == null)
        {
            joyconRumbleService = FindObjectOfType<JoyconRumbleService>();
        }

        if (excavationAudioSource == null)
        {
            GameObject audioRoot = GameObject.Find("AudioRoot");
            if (audioRoot != null)
            {
                excavationAudioSource = audioRoot.GetComponent<AudioSource>();
            }
        }

        if (bgmAudioSource == null)
        {
            GameObject bgmRoot = GameObject.Find("BgmAudioRoot");
            if (bgmRoot != null)
            {
                bgmAudioSource = bgmRoot.GetComponent<AudioSource>();
            }
        }
    }

    private void EnsureInputController()
    {
        if (gameInput != null)
        {
            return;
        }

        gameInput = GetComponent<GameInputController>();
        if (gameInput == null)
        {
            gameInput = gameObject.AddComponent<GameInputController>();
        }
    }

    // 地図表示用のカメラ設定
    private void EnsureCamera()
    {
        if (Camera.main != null)
        {
            Camera.main.orthographic = true;
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

    // シーンに地図ルートが無い場合は作成
    private void EnsureMap()
    {
        if (mapView == null)
        {
            GameObject mapRoot = new GameObject("MapRoot");
            mapView = mapRoot.AddComponent<MapViewController>();
        }

        mapView.Initialize();
    }

    // HUD や開始・終了画面がが無い場合は作成
    private void EnsureViews()
    {
        UiBootstrapper.EnsureCanvas(
            out ScoreView createdScoreView,
            out TimerView createdTimerView,
            out GameStateView createdStateView,
            out PopupController createdPopupController,
            out StartScreenView createdStartScreenView,
            out FinishScreenView createdFinishScreenView,
            out PauseMenuView createdPauseMenuView
        );

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

        if (startScreenView == null)
        {
            startScreenView = createdStartScreenView;
        }

        if (finishScreenView == null)
        {
            finishScreenView = createdFinishScreenView;
        }

        if (pauseMenuView == null)
        {
            pauseMenuView = createdPauseMenuView;
        }

        if (dummyDataProvider == null)
        {
            dummyDataProvider = gameObject.AddComponent<DummyGameDataProvider>();
        }
    }

    // 採掘成功 SE 用の AudioSource を利用可能な状態にする
    private void EnsureAudioRoot()
    {
        GameObject audioRoot = GameObject.Find("AudioRoot");
        if (audioRoot == null)
        {
            audioRoot = new GameObject("AudioRoot");
        }

        AudioSource audioSource = audioRoot.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = audioRoot.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;

        if (excavationAudioSource == null)
        {
            excavationAudioSource = audioSource;
        }

        if (excavationSuccessClip != null && excavationAudioSource.clip != excavationSuccessClip)
        {
            excavationAudioSource.clip = excavationSuccessClip;
        }
    }

    private void EnsureBgmAudioSource()
    {
        GameObject bgmRoot = GameObject.Find("BgmAudioRoot");
        if (bgmRoot == null)
        {
            bgmRoot = new GameObject("BgmAudioRoot");
        }

        AudioSource audioSource = bgmRoot.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = bgmRoot.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.volume = bgmVolume;

        if (bgmAudioSource == null)
        {
            bgmAudioSource = audioSource;
        }
    }

    private void PlayBgm(AudioClip clip)
    {
        if (bgmAudioSource == null)
        {
            return;
        }

        bgmAudioSource.volume = bgmVolume;
        bgmAudioSource.loop = true;

        if (clip == null)
        {
            bgmAudioSource.Stop();
            bgmAudioSource.clip = null;
            return;
        }

        if (bgmAudioSource.clip == clip && bgmAudioSource.isPlaying)
        {
            return;
        }

        bgmAudioSource.clip = clip;
        bgmAudioSource.Play();
    }

    // ROS subscriber 群を確保しつつ、手動セットアップ無しでも動くように
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

    // ROS publisher を確保
    private void EnsureRosPublisher()
    {
        if (gameRosPublisher == null)
        {
            GameObject connectorObject = GameObject.Find("ROSConnector");
            if (connectorObject == null)
            {
                connectorObject = new GameObject("ROSConnector");
            }

            gameRosPublisher = connectorObject.GetComponent<GameRosPublisher>();
            if (gameRosPublisher == null)
            {
                gameRosPublisher = connectorObject.AddComponent<GameRosPublisher>();
            }
        }

        gameRosPublisher.Initialize();
    }

    // UI ボタンと GameManager の状態遷移メソッドを接続
    private void ConfigureUiCallbacks()
    {
        if (startScreenView != null)
        {
            startScreenView.Bind(HandleStartButtonRequested);
        }

        if (finishScreenView != null)
        {
            finishScreenView.Bind(HandleBackButtonRequested);
        }

        if (pauseMenuView != null)
        {
            pauseMenuView.Bind(HandlePauseBackRequested, HandlePauseQuitGameRequested);
            pauseMenuView.SetMessage("Paused");
        }
    }

    private void HandleStartButtonRequested()
    {
        PlayOneShot(startButtonSound);
        StartGame();
    }

    private void HandleBackButtonRequested()
    {
        PlayOneShot(backButtonSound);
        EnterWaitingState();
    }

    private void HandlePauseBackRequested()
    {
        PlayOneShot(backButtonSound);
        ResumeFromPause();
    }

    private void HandlePauseQuitGameRequested()
    {
        PlayOneShot(backButtonSound);
        EnterWaitingState();
    }

    // ゲーム開始前の待機状態へ戻し、待機画面を表示
    private void EnterWaitingState()
    {
        StopStartCountdown();
        timerRunning = false;
        elapsedTime = 0f;
        score = 0;
        timeRemaining = initialTimeSeconds;
        currentState = GameSessionState.Waiting;
        lastPublishedTimeSeconds = -1;
        lastTimeWarningSecondPlayed = -1;
        SetRobotSpinActive(false);

        UpdateScoreDisplay(score);
        UpdateTimerDisplay(timeRemaining);
        UpdateStateDisplay("Waiting");
        PlayBgm(waitingBgmClip);
        UpdateArtifactState(Vector2.zero, false);
        SetPauseMenuVisible(false);
        SetFinishScreenVisible(false);
        SetStartScreenVisible(true);
        PublishWaitingState();
        PublishScore();
        PublishTimeRemaining(forcePublish: true);
    }

    // プレイ終了、スコア保存、終了画面表示
    private void FinishGame()
    {
        if (currentState == GameSessionState.Finished)
        {
            return;
        }

        timerRunning = false;
        currentState = GameSessionState.Finished;
        PlayOneShot(timeUpSound != null ? timeUpSound : countdownStartSound);
        StopStartCountdown();
        SetRobotSpinActive(false);
        UpdateStateDisplay("Finished");
        PlayBgm(finishedBgmClip);
        UpdateArtifactState(currentArtifactPosition, false);
        SetPauseMenuVisible(false);

        IReadOnlyList<ScoreHistoryEntry> scoreHistory =
            scoreHistoryRepository.AppendScoreAndLoadDescending(
                score,
                out string currentScoreEntryId
            );
        if (finishScreenView != null)
        {
            finishScreenView.ShowResults(score, scoreHistory, currentScoreEntryId);
        }

        SetStartScreenVisible(false);
        SetFinishScreenVisible(true);
        PublishFinishedState();
        PublishScore();
        PublishTimeRemaining(forcePublish: true);
    }

    // Score 表示 UI があれば表示内容を更新
    private void UpdateScoreDisplay(int latestScore)
    {
        if (scoreView != null)
        {
            scoreView.UpdateScore(latestScore);
        }
    }

    // Time 表示 UI があれば表示内容を更新
    private void UpdateTimerDisplay(float remainingSeconds)
    {
        if (timerView != null)
        {
            timerView.UpdateTime(remainingSeconds);
        }
    }

    // 画面上の状態表示ラベルを更新
    private void UpdateStateDisplay(string stateLabel)
    {
        if (stateView != null)
        {
            stateView.UpdateState(stateLabel);
        }
    }

    // 開始画面の表示・非表示切り替え
    private void SetStartScreenVisible(bool visible)
    {
        if (startScreenView != null)
        {
            startScreenView.SetVisible(visible);
        }
    }

    // 終了画面の表示・非表示切り替え
    private void SetFinishScreenVisible(bool visible)
    {
        if (finishScreenView != null)
        {
            finishScreenView.SetVisible(visible);
        }
    }

    private void SetPauseMenuVisible(bool visible)
    {
        if (pauseMenuView != null)
        {
            pauseMenuView.SetVisible(visible);
        }
    }

    // waiting 状態をパブリッシュ
    private void PublishWaitingState()
    {
        if (gameRosPublisher != null)
        {
            gameRosPublisher.PublishState("waiting");
        }
    }

    // プレイ開始時に playing 状態と start トリガをパブリッシュ
    private void PublishPlayingStarted()
    {
        if (gameRosPublisher == null)
        {
            return;
        }

        gameRosPublisher.PublishState("playing");
        gameRosPublisher.PublishStart();
    }

    private void PublishSpinTrigger()
    {
        if (gameRosPublisher != null)
        {
            gameRosPublisher.PublishSpinTrigger();
        }
    }

    // タイマー終了時に finished 状態をパブリッシュ
    private void PublishFinishedState()
    {
        if (gameRosPublisher != null)
        {
            gameRosPublisher.PublishState("finished");
        }
    }

    // スコア更新時に最新値をパブリッシュ
    private void PublishScore()
    {
        if (gameRosPublisher != null)
        {
            gameRosPublisher.PublishScore(score);
        }
    }

    // 同じ秒数を毎フレームパブリッシュしないように
    private void PublishTimeRemainingIfChanged()
    {
        PublishTimeRemaining(forcePublish: false);
    }

    // 残り時間パブリッシュ
    private void PublishTimeRemaining(bool forcePublish)
    {
        if (gameRosPublisher == null)
        {
            return;
        }

        int currentTimeSeconds = Mathf.CeilToInt(timeRemaining);
        if (!forcePublish && currentTimeSeconds == lastPublishedTimeSeconds)
        {
            return;
        }

        lastPublishedTimeSeconds = currentTimeSeconds;
        gameRosPublisher.PublishTimeRemaining(currentTimeSeconds);
    }
}
