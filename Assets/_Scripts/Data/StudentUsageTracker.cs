using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StudentUsageTracker : MonoBehaviour
{
    private const string StartFlowPiratesSceneName = "StartFlowPirates";
    private const string TutoSceneName = "TutoScene";
    private const string SolveRangeSceneName = "SolveRange";
    private const string SolveAngleSceneName = "SolveAngle";
    private const string SolveVelocitySceneName = "SolveVelocity";
    private const string SolveTotalFlyingTimeSceneName = "SolveTotalFlyingTime";
    private const string SolveTimeToMaxHeigthSceneName = "SolveTimeToMaxHeigth";
    private const string SolveMaxHeightSceneName = "SolveMaxHeight";

    [SerializeField] private StudentDataDefinition totalPlayTimeSeconds;
    [SerializeField] private StudentDataDefinition tutorialPlayTimeSeconds;
    [SerializeField] private StudentDataDefinition rangePlayTimeSeconds;
    [SerializeField] private StudentDataDefinition anglePlayTimeSeconds;
    [SerializeField] private StudentDataDefinition velocityPlayTimeSeconds;
    [SerializeField] private StudentDataDefinition totalFlyingTimePlayTimeSeconds;
    [SerializeField] private StudentDataDefinition timeToMaxHeightPlayTimeSeconds;
    [SerializeField] private StudentDataDefinition maxHeightPlayTimeSeconds;

    public static StudentUsageTracker Instance { get; private set; }

    private readonly HashSet<string> warningKeys = new HashSet<string>();
    private StudentUsageActivityType currentActivity = StudentUsageActivityType.None;
    private AccountAccessService accountAccessService;
    private double pendingSeconds;
    private bool isApplicationPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        UpdateCurrentActivity(SceneManager.GetActiveScene().name);
    }

    private void OnDisable()
    {
        if (Instance != this)
        {
            return;
        }

        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        CommitPendingTime();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!CanTrackUsage())
        {
            return;
        }

        pendingSeconds += Time.deltaTime;
        TransferWholePendingSeconds();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            CommitPendingTime();
        }

        isApplicationPaused = pauseStatus;
    }

    public static void CommitPendingTimeIfAvailable()
    {
        if (Instance != null)
        {
            Instance.CommitPendingTime();
        }
    }

    public static void ResetSessionIfAvailable()
    {
        if (Instance != null)
        {
            Instance.ResetSession();
        }
    }

    public void CommitPendingTime()
    {
        TransferWholePendingSeconds();
    }

    public void ResetSession()
    {
        pendingSeconds = 0d;
    }

    private void HandleActiveSceneChanged(Scene previousScene, Scene currentScene)
    {
        CommitPendingTime();
        pendingSeconds = 0d;
        UpdateCurrentActivity(currentScene.name);
    }

    private void UpdateCurrentActivity(string sceneName)
    {
        currentActivity = ResolveActivity(sceneName);
    }

    private static StudentUsageActivityType ResolveActivity(string sceneName)
    {
        switch (sceneName)
        {
            case StartFlowPiratesSceneName:
                return StudentUsageActivityType.None;
            case TutoSceneName:
                return StudentUsageActivityType.Tutorial;
            case SolveRangeSceneName:
                return StudentUsageActivityType.Range;
            case SolveAngleSceneName:
                return StudentUsageActivityType.Angle;
            case SolveVelocitySceneName:
                return StudentUsageActivityType.Velocity;
            case SolveTotalFlyingTimeSceneName:
                return StudentUsageActivityType.TotalFlyingTime;
            case SolveTimeToMaxHeigthSceneName:
                return StudentUsageActivityType.TimeToMaxHeight;
            case SolveMaxHeightSceneName:
                return StudentUsageActivityType.MaxHeight;
            default:
                return StudentUsageActivityType.None;
        }
    }

    private bool CanTrackUsage()
    {
        if (isApplicationPaused || !StudentData.IsReady)
        {
            return false;
        }

        AccountAccessService accessService = GetAccountAccessService();
        return accessService != null
            && accessService.HasResolvedStatus
            && accessService.CanUseApplication;
    }

    private AccountAccessService GetAccountAccessService()
    {
        if (accountAccessService == null)
        {
            accountAccessService = AccountAccessService.Instance;
        }

        return accountAccessService;
    }

    private void TransferWholePendingSeconds()
    {
        if (pendingSeconds < 1d)
        {
            return;
        }

        long wholeSeconds = (long)Math.Floor(pendingSeconds);
        if (wholeSeconds <= 0L)
        {
            return;
        }

        if (!TryIncrementUsage(wholeSeconds))
        {
            return;
        }

        pendingSeconds -= wholeSeconds;
    }

    private bool TryIncrementUsage(long seconds)
    {
        if (!StudentData.IsReady)
        {
            return false;
        }

        StudentDataDefinition activityDefinition = GetActivityDefinition();
        if (!ValidateDefinition(totalPlayTimeSeconds, nameof(totalPlayTimeSeconds))
            || !ValidateDefinition(activityDefinition, currentActivity.ToString()))
        {
            return false;
        }

        if (!TryIncrementDefinition(totalPlayTimeSeconds, seconds, nameof(totalPlayTimeSeconds)))
        {
            return false;
        }

        if (activityDefinition == null)
        {
            return true;
        }

        return TryIncrementDefinition(activityDefinition, seconds, currentActivity.ToString());
    }

    private StudentDataDefinition GetActivityDefinition()
    {
        switch (currentActivity)
        {
            case StudentUsageActivityType.Tutorial:
                return tutorialPlayTimeSeconds;
            case StudentUsageActivityType.Range:
                return rangePlayTimeSeconds;
            case StudentUsageActivityType.Angle:
                return anglePlayTimeSeconds;
            case StudentUsageActivityType.Velocity:
                return velocityPlayTimeSeconds;
            case StudentUsageActivityType.TotalFlyingTime:
                return totalFlyingTimePlayTimeSeconds;
            case StudentUsageActivityType.TimeToMaxHeight:
                return timeToMaxHeightPlayTimeSeconds;
            case StudentUsageActivityType.MaxHeight:
                return maxHeightPlayTimeSeconds;
            case StudentUsageActivityType.None:
            default:
                return null;
        }
    }

    private bool ValidateDefinition(StudentDataDefinition definition, string definitionName)
    {
        if (definition != null
            || string.Equals(definitionName, StudentUsageActivityType.None.ToString(), StringComparison.Ordinal))
        {
            return true;
        }

        WarnOnce($"Missing:{definitionName}", $"Missing usage definition: {definitionName}.");
        return false;
    }

    private bool TryIncrementDefinition(
        StudentDataDefinition definition,
        long seconds,
        string definitionName)
    {
        if (definition == null)
        {
            WarnOnce($"Missing:{definitionName}", $"Missing usage definition: {definitionName}.");
            return false;
        }

        if (!StudentData.Increment(definition, seconds))
        {
            WarnOnce($"Increment:{definitionName}", $"Could not increment usage definition: {definitionName}.");
            return false;
        }

        return true;
    }

    private void WarnOnce(string key, string message)
    {
        if (!warningKeys.Add(key))
        {
            return;
        }

        Debug.LogWarning($"[StudentUsageTracker] {message}");
    }
}
