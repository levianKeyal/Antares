using System.Collections;
using System.Collections.Generic;
using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Controls the real login UI for Antares using the existing Firebase and Google authentication services.
/// This controller does not implement authentication logic; it only forwards UI actions and presents user-facing status.
/// </summary>
public class LoginUIController : MonoBehaviour
{
    private const int MaxVisibleLogLines = 2;
    private const string SuccessMessage = "SesiÃ³n iniciada con Ã©xito";
    private const string SignedOutMessage = "SesiÃ³n cerrada";
    private const string GenericErrorMessage = "No se pudo iniciar sesiÃ³n";
    private const string ValidatingAccountMessage = "Validando cuenta...";
    private const string SuspendedAccountMessage = "Cuenta suspendida. Contactar a un Administrador para mÃ¡s informaciÃ³n.";
    private const string DisabledAccountMessage = "Cuenta deshabilitada. Contactar a un Administrador para mÃ¡s informaciÃ³n.";

    [SerializeField]
    private TMP_Text userText;

    [SerializeField]
    [FormerlySerializedAs("statusText")]
    private TMP_Text statusLogText;

    [SerializeField]
    private float logMessageInterval = 0.5f;

    [SerializeField]
    private CanvasGroup loginCanvasGroup;

    [SerializeField]
    private CanvasGroup sceneCanvasGroup;

    [SerializeField]
    [Min(0f)]
    private float successfulLoginDelay = 2f;

    [SerializeField]
    [Min(0f)]
    private float fadeDuration = 0.35f;

    private readonly Queue<string> visibleLogMessages = new Queue<string>(MaxVisibleLogLines);
    private readonly Queue<string> pendingLogMessages = new Queue<string>();

    private GoogleAuthService subscribedGoogleAuthService;
    private FirebaseAuthService subscribedFirebaseAuthService;
    private AccountAccessService subscribedAccountAccessService;
    private Coroutine logSequenceCoroutine;
    private Coroutine screenTransitionCoroutine;
    private bool signInInProgress;
    private bool signOutInProgress;
    private bool isSceneSelectionVisible;
    private bool manualLoginScreenRequested;
    private bool screenGroupsInitialized;
    private bool missingCanvasGroupWarningShown;
    private bool hasAuthenticatedSession;
    private string authenticatedSessionUid = string.Empty;

    private void OnEnable()
    {
        TrySubscribeToServices();
        InitializeScreenGroupsIfNeeded();
        RefreshUserText();
        RefreshSessionStatusWithoutAnimation();
    }

    private void Start()
    {
        TrySubscribeToServices();
        InitializeScreenGroupsIfNeeded();
        RefreshUserText();
        RefreshSessionStatusWithoutAnimation();
    }

    private void Update()
    {
        TrySubscribeToServices();
        RefreshUserText();
    }

    private void OnDisable()
    {
        StopLogSequence();
        CancelScreenTransition();
        UnsubscribeFromServices();
    }

    private void OnDestroy()
    {
        StopLogSequence();
        CancelScreenTransition();
        UnsubscribeFromServices();
    }

    public void HandleSignInClicked()
    {
        GoogleAuthService googleAuthService = GoogleAuthService.Instance;

        if (googleAuthService == null)
        {
            Debug.LogWarning("[LoginUIController] GoogleAuthService no esta disponible.");
            ClearCurrentUserIfNeeded();
            ShowSingleStatusMessage(GenericErrorMessage);
            return;
        }

        manualLoginScreenRequested = false;
        CancelScreenTransition();
        ShowLoginScreenImmediate();
        ClearCurrentUserIfNeeded();
        signInInProgress = true;
        RestartLogSequence(
            "Iniciando acceso...",
            ValidatingAccountMessage,
            "Autenticando usuario..."
        );

        googleAuthService.SignInWithGoogle();
    }

    public async void HandleSignOutClicked()
    {
        if (signOutInProgress)
        {
            return;
        }

        FirebaseAuthService firebaseAuthService = FirebaseAuthService.Instance;

        if (firebaseAuthService == null)
        {
            Debug.LogWarning("[LoginUIController] FirebaseAuthService no esta disponible.");
            HandleSignedOutVisualState();
            ShowSingleStatusMessage(SignedOutMessage);
            return;
        }

        signOutInProgress = true;

        try
        {
            if (StudentData.IsReady)
            {
                try
                {
                    StudentUsageTracker.CommitPendingTimeIfAvailable();
                    bool succeeded = await StudentData.FlushAsync();
                    if (!succeeded)
                    {
                        Debug.LogWarning("[StudentDataAutoSave] Logout flush FAILED. Continuing logout.");
                    }
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning($"[StudentDataAutoSave] Logout flush EXCEPTION. Continuing logout. {exception}");
                }
            }

            firebaseAuthService.SignOut();
            HandleSignedOutVisualState();
            ShowSingleStatusMessage(SignedOutMessage);
        }
        finally
        {
            signOutInProgress = false;
        }
    }

    public void ShowLoginScreen()
    {
        Debug.Log("[LoginUI] ShowLoginScreen requested.");
        manualLoginScreenRequested = true;
        CancelScreenTransition();
        StartScreenTransition(showSceneSelection: false);
    }

    private void TrySubscribeToServices()
    {
        GoogleAuthService googleAuthService = GoogleAuthService.Instance;
        if (googleAuthService != null && subscribedGoogleAuthService != googleAuthService)
        {
            if (subscribedGoogleAuthService != null)
            {
                subscribedGoogleAuthService.StateChanged -= HandleGoogleAuthStateChanged;
            }

            subscribedGoogleAuthService = googleAuthService;
            subscribedGoogleAuthService.StateChanged += HandleGoogleAuthStateChanged;
        }

        FirebaseAuthService firebaseAuthService = FirebaseAuthService.Instance;
        if (firebaseAuthService != null && subscribedFirebaseAuthService != firebaseAuthService)
        {
            if (subscribedFirebaseAuthService != null)
            {
                subscribedFirebaseAuthService.StateChanged -= HandleFirebaseAuthStateChanged;
            }

            subscribedFirebaseAuthService = firebaseAuthService;
            subscribedFirebaseAuthService.StateChanged += HandleFirebaseAuthStateChanged;
        }

        AccountAccessService accountAccessService = AccountAccessService.Instance;
        if (accountAccessService != null && subscribedAccountAccessService != accountAccessService)
        {
            if (subscribedAccountAccessService != null)
            {
                subscribedAccountAccessService.OnAccountAccessChanged -= HandleAccountAccessChanged;
            }

            subscribedAccountAccessService = accountAccessService;
            subscribedAccountAccessService.OnAccountAccessChanged += HandleAccountAccessChanged;
        }
    }

    private void UnsubscribeFromServices()
    {
        if (subscribedGoogleAuthService != null)
        {
            subscribedGoogleAuthService.StateChanged -= HandleGoogleAuthStateChanged;
            subscribedGoogleAuthService = null;
        }

        if (subscribedFirebaseAuthService != null)
        {
            subscribedFirebaseAuthService.StateChanged -= HandleFirebaseAuthStateChanged;
            subscribedFirebaseAuthService = null;
        }

        if (subscribedAccountAccessService != null)
        {
            subscribedAccountAccessService.OnAccountAccessChanged -= HandleAccountAccessChanged;
            subscribedAccountAccessService = null;
        }
    }

    private void HandleGoogleAuthStateChanged(GoogleAuthState state)
    {
        if (state == GoogleAuthState.Error)
        {
            signInInProgress = false;
            CancelScreenTransition();
            RefreshUserText();
            ShowSingleStatusMessage(GenericErrorMessage);
            return;
        }

        if (state == GoogleAuthState.Canceled)
        {
            signInInProgress = false;
            CancelScreenTransition();
            RefreshUserText();
            ShowSingleStatusMessage(GenericErrorMessage);
            return;
        }

        TryShowSuccessfulLogin();
    }

    private void HandleFirebaseAuthStateChanged(FirebaseUser user)
    {
        RefreshUserText(user);

        if (!IsAuthenticated(user))
        {
            HandleSignedOutVisualState();
            ShowSingleStatusMessage(SignedOutMessage);
            return;
        }

        RegisterAuthenticatedSession(user);

        if (!signInInProgress)
        {
            RefreshAccountAccessStatus();
            return;
        }

        TryShowSuccessfulLogin();
    }

    private void HandleAccountAccessChanged()
    {
        RefreshUserText();

        if (!HasAuthenticatedUser())
        {
            return;
        }

        RefreshAccountAccessStatus();
    }

    private void TryShowSuccessfulLogin()
    {
        if (!signInInProgress)
        {
            return;
        }

        GoogleAuthService googleAuthService = GoogleAuthService.Instance;
        FirebaseAuthService firebaseAuthService = FirebaseAuthService.Instance;

        bool googleSucceeded =
            googleAuthService != null && googleAuthService.State == GoogleAuthState.Success;

        bool authenticated =
            firebaseAuthService != null &&
            firebaseAuthService.IsAuthenticated &&
            firebaseAuthService.CurrentUser != null;

        if (!googleSucceeded || !authenticated)
        {
            return;
        }

        RegisterAuthenticatedSession(firebaseAuthService.CurrentUser);
        RefreshUserText(firebaseAuthService.CurrentUser);
        RefreshAccountAccessStatus();
    }

    private void RefreshSessionStatusWithoutAnimation()
    {
        if (signInInProgress)
        {
            return;
        }

        FirebaseAuthService firebaseAuthService = FirebaseAuthService.Instance;
        FirebaseUser currentUser = firebaseAuthService != null ? firebaseAuthService.CurrentUser : null;
        if (!IsAuthenticated(currentUser))
        {
            ShowSingleStatusMessage(SignedOutMessage);
            return;
        }

        RegisterAuthenticatedSession(currentUser);
        RefreshAccountAccessStatus();
    }

    private void RefreshAccountAccessStatus()
    {
        if (!HasAuthenticatedUser())
        {
            return;
        }

        AccountAccessService accountAccessService = AccountAccessService.Instance;
        if (accountAccessService == null || !accountAccessService.HasResolvedStatus)
        {
            CancelScreenTransition();
            ReturnToLoginForRestrictedAccount();
            ShowSingleStatusMessage(ValidatingAccountMessage);
            return;
        }

        signInInProgress = false;

        if (!accountAccessService.CanUseApplication)
        {
            Debug.Log("[LoginUI] Access restricted. Returning to login screen.");
            CancelScreenTransition();
            ReturnToLoginForRestrictedAccount();

            switch (accountAccessService.CurrentStatus)
            {
                case UserStatus.Suspended:
                    ShowSingleStatusMessage(SuspendedAccountMessage);
                    return;
                case UserStatus.Disabled:
                    ShowSingleStatusMessage(DisabledAccountMessage);
                    return;
                case UserStatus.Active:
                default:
                    ShowSingleStatusMessage(accountAccessService.LastRestrictionMessage);
                    return;
            }
        }

        ShowSingleStatusMessage(SuccessMessage);
        Debug.Log("[LoginUI] Authorized. Waiting for StudentData.");
        TryStartAuthorizedSceneTransition();
    }

    private bool HasAuthenticatedUser()
    {
        FirebaseAuthService firebaseAuthService = FirebaseAuthService.Instance;
        return firebaseAuthService != null && IsAuthenticated(firebaseAuthService.CurrentUser);
    }

    private bool IsAuthenticated(FirebaseUser user)
    {
        return user != null && !string.IsNullOrWhiteSpace(user.Email);
    }

    private void RegisterAuthenticatedSession(FirebaseUser user)
    {
        if (!IsAuthenticated(user))
        {
            return;
        }

        string uid = user.UserId ?? string.Empty;
        if (!hasAuthenticatedSession || !string.Equals(authenticatedSessionUid, uid, System.StringComparison.Ordinal))
        {
            manualLoginScreenRequested = false;
            hasAuthenticatedSession = true;
            authenticatedSessionUid = uid;
        }
    }

    private void RefreshUserText(FirebaseUser user = null)
    {
        if (userText == null)
        {
            return;
        }

        FirebaseUser currentUser = user;
        if (currentUser == null && FirebaseAuthService.Instance != null)
        {
            currentUser = FirebaseAuthService.Instance.CurrentUser;
        }

        if (!IsAuthenticated(currentUser))
        {
            ClearUserText();
            return;
        }

        userText.text = currentUser.Email;
    }

    private void ClearCurrentUserIfNeeded()
    {
        FirebaseAuthService firebaseAuthService = FirebaseAuthService.Instance;
        if (firebaseAuthService == null || !firebaseAuthService.IsAuthenticated)
        {
            ClearUserText();
        }
    }

    private void ClearUserText()
    {
        if (userText != null)
        {
            userText.text = string.Empty;
        }
    }

    private void InitializeScreenGroupsIfNeeded()
    {
        if (screenGroupsInitialized)
        {
            return;
        }

        screenGroupsInitialized = true;
        ShowLoginScreenImmediate();
    }

    private void HandleSignedOutVisualState()
    {
        Debug.Log("[LoginUI] Transition canceled by logout.");
        signInInProgress = false;
        manualLoginScreenRequested = false;
        hasAuthenticatedSession = false;
        authenticatedSessionUid = string.Empty;
        CancelScreenTransition();
        ClearUserText();
        ShowLoginScreenImmediate();
    }

    private void TryStartAuthorizedSceneTransition()
    {
        if (manualLoginScreenRequested || isSceneSelectionVisible || screenTransitionCoroutine != null)
        {
            return;
        }

        if (!IsAuthorizedForSceneSelection())
        {
            return;
        }

        if (!HasScreenGroups())
        {
            return;
        }

        screenTransitionCoroutine = StartCoroutine(WaitForAuthorizedDataAndShowScenes());
    }

    private IEnumerator WaitForAuthorizedDataAndShowScenes()
    {
        SetCanvasGroupState(sceneCanvasGroup, 0f, false);

        while (!StudentData.IsReady)
        {
            if (!IsAuthorizedForSceneSelection())
            {
                screenTransitionCoroutine = null;
                yield break;
            }

            yield return null;
        }

        Debug.Log("[LoginUI] StudentData ready.");

        float delay = Mathf.Max(0f, successfulLoginDelay);
        Debug.Log($"[LoginUI] Success delay started. Seconds={delay}");
        float elapsed = 0f;
        while (elapsed < delay)
        {
            if (!IsAuthorizedForSceneSelection())
            {
                screenTransitionCoroutine = null;
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!IsAuthorizedForSceneSelection())
        {
            screenTransitionCoroutine = null;
            yield break;
        }

        yield return FadeScreenGroups(showSceneSelection: true);
        screenTransitionCoroutine = null;
    }

    private bool IsAuthorizedForSceneSelection()
    {
        if (manualLoginScreenRequested || !HasAuthenticatedUser())
        {
            return false;
        }

        AccountAccessService accountAccessService = AccountAccessService.Instance;
        return accountAccessService != null &&
               accountAccessService.HasResolvedStatus &&
               accountAccessService.CanUseApplication;
    }

    private void StartScreenTransition(bool showSceneSelection)
    {
        if (!HasScreenGroups())
        {
            if (showSceneSelection)
            {
                return;
            }

            isSceneSelectionVisible = false;
            return;
        }

        screenTransitionCoroutine = StartCoroutine(ScreenTransitionRoutine(showSceneSelection));
    }

    private IEnumerator ScreenTransitionRoutine(bool showSceneSelection)
    {
        yield return FadeScreenGroups(showSceneSelection);
        screenTransitionCoroutine = null;
    }

    private IEnumerator FadeScreenGroups(bool showSceneSelection)
    {
        Debug.Log(showSceneSelection ? "[LoginUI] Fade Login->Scene started." : "[LoginUI] Fade Scene->Login started.");
        SetCanvasGroupInteraction(loginCanvasGroup, false);
        SetCanvasGroupInteraction(sceneCanvasGroup, false);

        float loginStartAlpha = loginCanvasGroup.alpha;
        float sceneStartAlpha = sceneCanvasGroup.alpha;
        float loginTargetAlpha = showSceneSelection ? 0f : 1f;
        float sceneTargetAlpha = showSceneSelection ? 1f : 0f;
        float duration = Mathf.Max(0f, fadeDuration);

        if (duration <= 0f)
        {
            ApplyFinalScreenState(showSceneSelection);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            loginCanvasGroup.alpha = Mathf.Lerp(loginStartAlpha, loginTargetAlpha, t);
            sceneCanvasGroup.alpha = Mathf.Lerp(sceneStartAlpha, sceneTargetAlpha, t);
            yield return null;
        }

        ApplyFinalScreenState(showSceneSelection);
    }

    private void ReturnToLoginForRestrictedAccount()
    {
        manualLoginScreenRequested = false;
        if (isSceneSelectionVisible)
        {
            StartScreenTransition(showSceneSelection: false);
            return;
        }

        ShowLoginScreenImmediate();
    }

    private void ShowLoginScreenImmediate()
    {
        if (!HasScreenGroups())
        {
            isSceneSelectionVisible = false;
            return;
        }

        ApplyFinalScreenState(showSceneSelection: false);
    }

    private void ApplyFinalScreenState(bool showSceneSelection)
    {
        isSceneSelectionVisible = showSceneSelection;
        SetCanvasGroupState(loginCanvasGroup, showSceneSelection ? 0f : 1f, !showSceneSelection);
        SetCanvasGroupState(sceneCanvasGroup, showSceneSelection ? 1f : 0f, showSceneSelection);
        Debug.Log(showSceneSelection ? "[LoginUI] Scene selection visible." : "[LoginUI] Login screen visible.");
    }

    private bool HasScreenGroups()
    {
        bool hasGroups = loginCanvasGroup != null && sceneCanvasGroup != null;
        if (!hasGroups && !missingCanvasGroupWarningShown)
        {
            Debug.LogWarning("[LoginUIController] Login and Scene CanvasGroup references are required for screen transitions.");
            missingCanvasGroupWarningShown = true;
        }

        return hasGroups;
    }

    private void SetCanvasGroupState(CanvasGroup canvasGroup, float alpha, bool interactive)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = alpha;
        SetCanvasGroupInteraction(canvasGroup, interactive);
    }

    private void SetCanvasGroupInteraction(CanvasGroup canvasGroup, bool interactive)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.interactable = interactive;
        canvasGroup.blocksRaycasts = interactive;
    }

    private void CancelScreenTransition()
    {
        if (screenTransitionCoroutine != null)
        {
            StopCoroutine(screenTransitionCoroutine);
            screenTransitionCoroutine = null;
        }
    }

    private void RestartLogSequence(params string[] messages)
    {
        StopLogSequence();
        ClearStatusLog();

        foreach (string message in messages)
        {
            QueueLogMessage(message);
        }

        logSequenceCoroutine = StartCoroutine(PlayLogSequence());
    }

    private void QueueLogMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        string trimmedMessage = message.Trim();
        if (pendingLogMessages.Contains(trimmedMessage))
        {
            return;
        }

        pendingLogMessages.Enqueue(trimmedMessage);
    }

    private IEnumerator PlayLogSequence()
    {
        while (pendingLogMessages.Count > 0)
        {
            EnqueueLogMessage(pendingLogMessages.Dequeue());
            yield return new WaitForSeconds(Mathf.Max(0.1f, logMessageInterval));
        }

        logSequenceCoroutine = null;
    }

    private void ShowSingleStatusMessage(string message)
    {
        StopLogSequence();
        ClearStatusLog();
        EnqueueLogMessage(message);
    }

    private void EnqueueLogMessage(string message)
    {
        if (statusLogText == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        string trimmedMessage = message.Trim();
        if (visibleLogMessages.Contains(trimmedMessage))
        {
            return;
        }

        visibleLogMessages.Enqueue(trimmedMessage);
        while (visibleLogMessages.Count > MaxVisibleLogLines)
        {
            visibleLogMessages.Dequeue();
        }

        statusLogText.text = string.Join("\n", visibleLogMessages.ToArray());
    }

    private void ClearStatusLog()
    {
        pendingLogMessages.Clear();
        visibleLogMessages.Clear();

        if (statusLogText != null)
        {
            statusLogText.text = string.Empty;
        }
    }

    private void StopLogSequence()
    {
        if (logSequenceCoroutine == null)
        {
            return;
        }

        StopCoroutine(logSequenceCoroutine);
        logSequenceCoroutine = null;
    }
}
