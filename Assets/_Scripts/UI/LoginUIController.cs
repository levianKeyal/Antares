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
    private const string SuccessMessage = "Sesión iniciada con éxito";
    private const string SignedOutMessage = "Sesión cerrada";
    private const string GenericErrorMessage = "No se pudo iniciar sesión";
    private const string ValidatingAccountMessage = "Validando cuenta...";
    private const string SuspendedAccountMessage = "Cuenta suspendida. Contactar a un Administrador para más información.";
    private const string DisabledAccountMessage = "Cuenta deshabilitada. Contactar a un Administrador para más información.";

    [SerializeField]
    private TMP_Text userText;

    [SerializeField]
    [FormerlySerializedAs("statusText")]
    private TMP_Text statusLogText;

    [SerializeField]
    private float logMessageInterval = 0.5f;

    private readonly Queue<string> visibleLogMessages = new Queue<string>(MaxVisibleLogLines);
    private readonly Queue<string> pendingLogMessages = new Queue<string>();

    private GoogleAuthService subscribedGoogleAuthService;
    private FirebaseAuthService subscribedFirebaseAuthService;
    private AccountAccessService subscribedAccountAccessService;
    private Coroutine logSequenceCoroutine;
    private bool signInInProgress;

    private void OnEnable()
    {
        TrySubscribeToServices();
        RefreshUserText();
        RefreshSessionStatusWithoutAnimation();
    }

    private void Start()
    {
        TrySubscribeToServices();
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
        UnsubscribeFromServices();
    }

    private void OnDestroy()
    {
        StopLogSequence();
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

        ClearCurrentUserIfNeeded();
        signInInProgress = true;
        RestartLogSequence(
            "Iniciando acceso...",
            ValidatingAccountMessage,
            "Autenticando usuario..."
        );

        googleAuthService.SignInWithGoogle();
    }

    public void HandleSignOutClicked()
    {
        FirebaseAuthService firebaseAuthService = FirebaseAuthService.Instance;

        if (firebaseAuthService == null)
        {
            Debug.LogWarning("[LoginUIController] FirebaseAuthService no esta disponible.");
            ClearUserText();
            ShowSingleStatusMessage(SignedOutMessage);
            return;
        }

        firebaseAuthService.SignOut();
        signInInProgress = false;
        ClearUserText();
        ShowSingleStatusMessage(SignedOutMessage);
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
            RefreshUserText();
            ShowSingleStatusMessage(GenericErrorMessage);
            return;
        }

        if (state == GoogleAuthState.Canceled)
        {
            signInInProgress = false;
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
            signInInProgress = false;
            ShowSingleStatusMessage(SignedOutMessage);
            return;
        }

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
            ShowSingleStatusMessage(ValidatingAccountMessage);
            return;
        }

        signInInProgress = false;

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
                ShowSingleStatusMessage(SuccessMessage);
                return;
        }
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
        if (logSequenceCoroutine != null)
        {
            StopCoroutine(logSequenceCoroutine);
            logSequenceCoroutine = null;
        }
    }
}
