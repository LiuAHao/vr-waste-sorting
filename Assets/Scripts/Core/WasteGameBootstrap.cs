using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public sealed class WasteGameBootstrap : MonoBehaviour
{
    private static WasteGameBootstrap _instance;

    public static WasteGameBootstrap Instance => _instance;

    private WasteGameFlowController _flowController;
    private WasteAnalyticsTracker _analytics;
    private WasteStartView _startView;
    private WasteHudView _hudView;
    private WasteResultView _resultView;
    private StageTransitionView _transitionView;
    private StageDifficultySelectView _difficultySelectView;
    private WastePauseView _pauseView;
    private WasteDashboardView _dashboardView;
    private FreePlayModeController _freePlayController;
    private TimedChallengeModeController _timedChallengeController;
    private StageProgressionModeController _stageProgressionController;
    private EndlessScoreModeController _endlessScoreController;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private static void EnsureInstance()
    {
        if (_instance != null)
        {
            return;
        }

        GameObject go = new GameObject("WasteGameBootstrap");
        _instance = go.AddComponent<WasteGameBootstrap>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _analytics = new WasteAnalyticsTracker();
        _flowController = new WasteGameFlowController();
        _startView = WasteStartView.Create(RestartActiveScene, null, null, null);
        _hudView = WasteHudView.Create();
        _resultView = WasteResultView.Create(RestartActiveScene, ReturnToMainMenu);
        _transitionView = StageTransitionView.Create();
        _difficultySelectView = StageDifficultySelectView.Create();
        _pauseView = WastePauseView.Create();
        _dashboardView = WasteDashboardView.Create();
        _freePlayController = FindObjectOfType<FreePlayModeController>();
        WasteUiFactory.EnsureEventSystem();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ClassificationEvents.OnClassified += HandleClassified;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        ClassificationEvents.OnClassified -= HandleClassified;
    }

    private void Start()
    {
        BindCurrentScene();
    }

    /// <summary>检测 VR 手柄的 X 按钮（PICO4 / Oculus Touch）</summary>
    private bool IsVRMenuButtonPressed()
    {
        // 检测左手柄的 X 按钮（primaryButton）
        // 使用 GetKeyDown 语义：只在按下的那一帧返回 true
        InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        if (!leftHand.isValid) return false;

        // 读取当前帧的按钮状态
        bool currentPressed = false;
        leftHand.TryGetFeatureValue(CommonUsages.primaryButton, out currentPressed);

        // 检测上升沿（上一帧未按下，这一帧按下）
        bool wasPressed = _lastXButtonState;
        _lastXButtonState = currentPressed;

        return currentPressed && !wasPressed;
    }

    private bool _lastXButtonState = false;

    private void Update()
    {
        // 键盘 ESC 或 VR 手柄 Menu 键触发暂停
        bool pausePressed = Input.GetKeyDown(KeyCode.Escape) || IsVRMenuButtonPressed();

        if (pausePressed)
        {
            if (_freePlayController != null && _freePlayController.IsSessionActive)
            {
                _freePlayController.TogglePause();
                return;
            }

            if (_stageProgressionController != null && _stageProgressionController.IsSessionActive)
            {
                _stageProgressionController.TogglePause();
                return;
            }

            if (_timedChallengeController != null && _timedChallengeController.IsSessionActive)
            {
                _timedChallengeController.TogglePause();
                return;
            }

            if (_endlessScoreController != null && _endlessScoreController.IsSessionActive)
            {
                _endlessScoreController.TogglePause();
                return;
            }
        }

        if (_freePlayController != null && _freePlayController.IsSessionActive)
        {
            _freePlayController.Tick(Time.deltaTime);
            return;
        }

        if (_stageProgressionController != null && _stageProgressionController.IsSessionActive)
        {
            _stageProgressionController.Tick(Time.deltaTime);
            return;
        }

        if (_timedChallengeController != null && _timedChallengeController.IsSessionActive)
        {
            _timedChallengeController.Tick(Time.deltaTime);
            return;
        }

        if (_endlessScoreController != null && _endlessScoreController.IsSessionActive)
        {
            _endlessScoreController.Tick(Time.deltaTime);
            return;
        }

        if (_flowController != null)
        {
            _flowController.Tick(Time.deltaTime);
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LegacySceneGarbageUtility.ResetSuppressionFlag();
        BindCurrentScene();
    }

    private void HandleClassified(ClassificationResult result)
    {
        if (_stageProgressionController != null && _stageProgressionController.IsSessionActive)
        {
            _stageProgressionController.HandleClassification(result);
            return;
        }

        if (_freePlayController != null && _freePlayController.IsSessionActive)
        {
            _freePlayController.HandleClassification(result);
            return;
        }

        if (_timedChallengeController != null && _timedChallengeController.IsSessionActive)
        {
            _timedChallengeController.HandleClassification(result);
            return;
        }

        if (_endlessScoreController != null && _endlessScoreController.IsSessionActive)
        {
            _endlessScoreController.HandleClassification(result);
            return;
        }

        if (_flowController != null)
        {
            _flowController.HandleClassification(result);
        }
    }

    private void BindCurrentScene()
    {
        if (_flowController == null || _hudView == null || _resultView == null || _analytics == null)
        {
            return;
        }

        WasteUiFactory.EnsureEventSystem();
        LegacySceneGarbageUtility.SuppressLegacyGarbage();
        _timedChallengeController = Object.FindObjectOfType<TimedChallengeModeController>();
        _stageProgressionController = Object.FindObjectOfType<StageProgressionModeController>();
        _endlessScoreController = Object.FindObjectOfType<EndlessScoreModeController>();
        _freePlayController = Object.FindObjectOfType<FreePlayModeController>();
        EnsureFreePlaySceneSetup();
        _freePlayController = Object.FindObjectOfType<FreePlayModeController>();

        System.Action timedChallengeAction = null;
        if (_timedChallengeController != null)
        {
            _timedChallengeController.Configure(_hudView, _resultView, _analytics, RestartActiveScene);
            _timedChallengeController.ConfigurePauseView(_pauseView, ReturnToMainMenu);
            timedChallengeAction = BeginTimedChallengeSession;
        }

        System.Action stageProgressionAction = null;
        if (_stageProgressionController != null)
        {
            _stageProgressionController.Configure(_hudView, _resultView, _transitionView, _analytics, ReturnToStartMenu);
            _stageProgressionController.ConfigurePauseView(_pauseView, ReturnToStartMenu);
            stageProgressionAction = BeginStageProgressionSession;
        }

        System.Action startGameAction = null;
        if (_freePlayController != null)
        {
            _freePlayController.Configure(_hudView, _resultView, _analytics, ReturnToStartMenu);
            _freePlayController.ConfigurePauseView(_pauseView);
            startGameAction = BeginFreePlaySession;
        }

        System.Action endlessScoreAction = null;
        if (_endlessScoreController == null)
        {
            GameObject controllerObject = new GameObject("EndlessScoreModeController");
            _endlessScoreController = controllerObject.AddComponent<EndlessScoreModeController>();
        }

        if (_endlessScoreController != null)
        {
            _endlessScoreController.Configure(_hudView, _resultView, _analytics, RestartActiveScene);
            _endlessScoreController.ConfigurePauseView(_pauseView, ReturnToMainMenu);
            endlessScoreAction = BeginEndlessScoreSession;
        }

        System.Action dashboardAction = ShowDashboard;

        _flowController.BindScene(
            _startView,
            _hudView,
            _resultView,
            _analytics,
            RestartActiveScene,
            startGameAction,
            timedChallengeAction,
            stageProgressionAction,
            endlessScoreAction,
            dashboardAction);
    }

    private void EnsureFreePlaySceneSetup()
    {
        if (_freePlayController != null)
        {
            return;
        }

        FreePlaySceneSetup existingSetup = Object.FindObjectOfType<FreePlaySceneSetup>();
        if (existingSetup != null)
        {
            return;
        }

        GameObject controllerObject = new GameObject("FreePlaySceneSetup");
        controllerObject.AddComponent<FreePlaySceneSetup>();
    }

    private void BeginTimedChallengeSession()
    {
        if (_timedChallengeController == null)
        {
            return;
        }

        _startView.Hide();
        _resultView.Hide();
        _transitionView.Hide();
        if (_difficultySelectView != null)
        {
            _difficultySelectView.Hide();
        }

        _timedChallengeController.StartChallenge();
    }

    private void BeginFreePlaySession()
    {
        if (_freePlayController == null)
        {
            return;
        }

        _startView.Hide();
        _resultView.Hide();
        _transitionView.Hide();
        if (_difficultySelectView != null)
        {
            _difficultySelectView.Hide();
        }

        _freePlayController.StartFreePlay();
    }

    private void BeginStageProgressionSession()
    {
        if (_stageProgressionController == null || _difficultySelectView == null)
        {
            return;
        }

        StageProgressionConfig config = _stageProgressionController.Config;
        if (config == null || _stageProgressionController.StageCount <= 0)
        {
            Debug.LogWarning("WasteGameBootstrap: 未找到标准闯关难度配置。");
            return;
        }

        _startView.Hide();
        _resultView.Hide();
        _transitionView.Hide();
        _difficultySelectView.Show(config, BeginStageProgressionAtDifficulty, ReturnToStartMenu);
    }

    private void BeginStageProgressionAtDifficulty(int stageIndex)
    {
        if (_stageProgressionController == null)
        {
            return;
        }

        _difficultySelectView.Hide();
        _resultView.Hide();
        _transitionView.Hide();
        _stageProgressionController.StartProgression(stageIndex);
    }

    private void BeginEndlessScoreSession()
    {
        if (_endlessScoreController == null)
        {
            return;
        }

        _startView.Hide();
        _resultView.Hide();
        _transitionView.Hide();
        if (_difficultySelectView != null)
        {
            _difficultySelectView.Hide();
        }

        _endlessScoreController.StartEndless();
    }

    public void ReturnToStartMenu()
    {
        if (_stageProgressionController != null)
        {
            _stageProgressionController.AbortSession();
        }

        if (_freePlayController != null)
        {
            _freePlayController.AbortSession();
        }

        if (_timedChallengeController != null)
        {
            _timedChallengeController.AbortSession();
        }

        if (_endlessScoreController != null)
        {
            _endlessScoreController.AbortSession();
        }

        if (_difficultySelectView != null)
        {
            _difficultySelectView.Hide();
        }

        _resultView.Hide();
        _transitionView.Hide();
        _hudView.SetVisible(false);
        _hudView.HideFeedback();
        _pauseView.Hide();
        _dashboardView.Hide();

        System.Action timedChallengeAction = _timedChallengeController != null
            ? (System.Action)BeginTimedChallengeSession
            : null;
        System.Action startGameAction = _freePlayController != null
            ? (System.Action)BeginFreePlaySession
            : null;
        System.Action stageProgressionAction = _stageProgressionController != null
            ? (System.Action)BeginStageProgressionSession
            : null;
        System.Action endlessScoreAction = _endlessScoreController != null
            ? (System.Action)BeginEndlessScoreSession
            : null;
        System.Action dashboardAction = ShowDashboard;

        _flowController.ShowStartMenu(_startView, startGameAction, timedChallengeAction, stageProgressionAction, endlessScoreAction, dashboardAction);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ShowDashboard()
    {
        if (_dashboardView == null || _analytics == null)
        {
            return;
        }

        _startView.Hide();
        _hudView.SetVisible(false);
        _resultView.Hide();
        _transitionView.Hide();
        if (_difficultySelectView != null)
        {
            _difficultySelectView.Hide();
        }

        _dashboardView.Show(_analytics.SessionHistory, ReturnToStartMenu);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void RestartActiveScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid())
        {
            SceneManager.LoadScene(activeScene.buildIndex);
        }
    }

    private void ReturnToMainMenu()
    {
        if (_freePlayController != null)
        {
            _freePlayController.AbortSession();
        }

        if (_timedChallengeController != null)
        {
            _timedChallengeController.AbortSession();
        }

        if (_stageProgressionController != null)
        {
            _stageProgressionController.AbortSession();
        }

        if (_endlessScoreController != null)
        {
            _endlessScoreController.AbortSession();
        }

        RestartActiveScene();
    }
}
