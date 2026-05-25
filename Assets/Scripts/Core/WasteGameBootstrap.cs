using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class WasteGameBootstrap : MonoBehaviour
{
    private static WasteGameBootstrap _instance;

    private WasteGameFlowController _flowController;
    private WasteAnalyticsTracker _analytics;
    private WasteStartView _startView;
    private WasteHudView _hudView;
    private WasteResultView _resultView;
    private TimedChallengeModeController _timedChallengeController;

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
        _startView = WasteStartView.Create(RestartActiveScene, null);
        _hudView = WasteHudView.Create();
        _resultView = WasteResultView.Create(RestartActiveScene);
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
        BindCurrentScene();
    }

    private void HandleClassified(ClassificationResult result)
    {
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
        _timedChallengeController = Object.FindObjectOfType<TimedChallengeModeController>();

        System.Action timedChallengeAction = null;
        if (_timedChallengeController != null)
        {
            _timedChallengeController.Configure(_hudView, _resultView, _analytics, RestartActiveScene);
            timedChallengeAction = BeginTimedChallengeSession;
        }

        _flowController.BindScene(_startView, _hudView, _resultView, _analytics, RestartActiveScene, timedChallengeAction);
    }

    private void BeginTimedChallengeSession()
    {
        if (_timedChallengeController == null)
        {
            return;
        }

        _startView.Hide();
        _resultView.Hide();
        _timedChallengeController.StartChallenge();
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
