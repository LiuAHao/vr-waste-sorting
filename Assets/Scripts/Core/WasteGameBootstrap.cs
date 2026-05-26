using UnityEngine;
using UnityEngine.SceneManagement;

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
    private TimedChallengeModeController _timedChallengeController;
    private StageProgressionModeController _stageProgressionController;

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
        _startView = WasteStartView.Create(RestartActiveScene, null, null);
        _hudView = WasteHudView.Create();
        _resultView = WasteResultView.Create(null);
        _transitionView = StageTransitionView.Create();
        _difficultySelectView = StageDifficultySelectView.Create();
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

    private void Update()
    {
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

        if (_timedChallengeController != null && _timedChallengeController.IsSessionActive)
        {
            _timedChallengeController.HandleClassification(result);
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

        System.Action timedChallengeAction = null;
        if (_timedChallengeController != null)
        {
            _timedChallengeController.Configure(_hudView, _resultView, _analytics, RestartActiveScene);
            timedChallengeAction = BeginTimedChallengeSession;
        }

        System.Action stageProgressionAction = null;
        if (_stageProgressionController != null)
        {
            _stageProgressionController.Configure(_hudView, _resultView, _transitionView, _analytics, ReturnToStartMenu);
            stageProgressionAction = BeginStageProgressionSession;
        }

        _flowController.BindScene(
            _startView,
            _hudView,
            _resultView,
            _analytics,
            RestartActiveScene,
            timedChallengeAction,
            stageProgressionAction);
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
        _timedChallengeController.StartChallenge();
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

    public void ReturnToStartMenu()
    {
        if (_stageProgressionController != null)
        {
            _stageProgressionController.AbortSession();
        }

        if (_difficultySelectView != null)
        {
            _difficultySelectView.Hide();
        }

        _resultView.Hide();
        _transitionView.Hide();
        _hudView.SetVisible(false);
        _hudView.HideFeedback();

        System.Action timedChallengeAction = _timedChallengeController != null
            ? BeginTimedChallengeSession
            : null;
        System.Action stageProgressionAction = _stageProgressionController != null
            ? BeginStageProgressionSession
            : null;

        _flowController.ShowStartMenu(_startView, timedChallengeAction, stageProgressionAction);
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
}
