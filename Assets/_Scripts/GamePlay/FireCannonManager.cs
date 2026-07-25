using Fungus;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum CannonPhysicsMode
{
    Tutorial,
    SolveInitialVelocity,
    SolveRange,
    SolveAngle
}

public class FireCanonManager : MonoBehaviour
{

    public FormulaSustitution formulaSustition;

    [Header("Physics Mode")]
    public CannonPhysicsMode physicsMode = CannonPhysicsMode.Tutorial;

    [Header("UI")]
    public TMP_Text angleText;
    public TMP_Text velocityValue;
    public Slider velocitySlider;

    [Header("UI Holders")]
    public GameObject cannonHolder;
    public CanvasGroup cannonHolderCanvasGroup;
    public GameObject velocityHolder;
    public CanvasGroup velocityHolderCanvasGroup;
    public GameObject fireButtonHolder;

    [Header("Answer Input")]
    public GameObject answerHolder;
    public GameObject answerKeyboardHolder;
    public InGameAnswerKeyboard answerKeyboard;
    public TMP_InputField answerInputField;
    public TMP_Text answerPromptText;

    [Header("UI Rotation")]
    public RectTransform angleTextTransform;

    [Header("References")]
    public Transform cannonMuzzle;
    public GameObject cannonBallPrefab;

    [Header("Cannon")]
    public CannonAimUI cannonAimUI;
    public GameObject cannonFireFx;
    public GameObject cannonballFiredFx;
    public Button fireButton;

    [Header("Physics")]

    [Header("Velocity Range")]
    public float minInitialVelocity = 5f;
    public float maxInitialVelocity = 50f;
    public float initialVelocity = 20f;
    public float gravity = 9.81f;
    public float currentAngle;
    public float currentRange;

    [Header("Challenge Data")]
    [Tooltip("Range given by the problem when the mode needs it.")]
    public float challengeRange = 25f;

    [Tooltip("Initial velocity given by the problem when the mode needs it.")]
    public float challengeInitialVelocity = 20f;

    [Tooltip("Angle given by the problem when the mode needs it.")]
    [Range(0f, 89.9f)]
    public float challengeAngle = 35f;
    [Header("Challenge Data UI")]
    public TMP_Text challengeRangeText;
    public TMP_Text challengeInitialVelocityText;
    public TMP_Text challengeAngleText;


    [Header("Challenge Seeds")]
    [Tooltip("Angle used to generate challenge values when the mode needs a seed angle.")]
    [Range(0f, 89.9f)]
    [HideInInspector]
    public float challengeAngleSeed = 35f;

    [Tooltip("Initial velocity used to generate challenge values when the mode needs a seed velocity.")]
    [HideInInspector]
    public float challengeInitialVelocitySeed = 20f;

    [Tooltip("Minimum range allowed when randomizing Solve Range challenges.")]

    [Header("Solve Range Movement")]
    [HideInInspector]
    public float solveRangeMoveDuration = 0.75f;
    [HideInInspector]
    public float solveRangeFireDelay = 1f;

    bool isSolveRangeSequenceRunning;
    bool isFireButtonDelayRunning;
    CannonBall activeCannonBall;

    [Header("Challenge Target")]
    [HideInInspector]
    public Vector3 challengeTargetCenter;

    [HideInInspector]
    public Vector3 challengePlayerPosition;

    [HideInInspector]
    public float challengeTargetDistance;

    [Header("User Answer")]
    [Tooltip("Value entered by the player. UI can write here later.")]
    public float userAnswerValue;

    [Header("Resolved Launch")]
    [HideInInspector]
    public float resolvedLaunchVelocity;

    [HideInInspector]
    public float resolvedLaunchAngle;

    [HideInInspector]
    public float resolvedLaunchRange;

    public float maxRange => (maxInitialVelocity * maxInitialVelocity) / gravity;

    [Header("Trajectory")]
    public bool showTrajectory = true;

    public GameObject trajectoryDotPrefab;

    public int maxDots = 100;

    public float dotSpacing = 0.15f;

    public float trajectoryTimeStep = 0.1f;

    [Header("Dot Animation")]
    public bool enableDotPulse = true;

    public float dotPulseSpeed = 6f;

    public float dotMinScale = 0.8f;

    public float dotMaxScale = 1.2f;

    [Space]

    public bool enableDotFade = true;

    public float dotFadeSpeed = 4f;

    public float dotFadeOffset = 0.15f;

    public float dotMinAlpha = 0.1f;

    public float dotMaxAlpha = 1f;

    [Header("Ground")]
    public LayerMask groundLayer;

    List<GameObject> trajectoryDots =
        new List<GameObject>();

    // UNITY
    // ====================================

    void Start()
    {
        formulaSustition = GetComponent<FormulaSustitution>();
        CreateTrajectoryPool();
        if (physicsMode == CannonPhysicsMode.Tutorial)
        {
            InitializeVelocitySlider();
        }
        else if (velocitySlider != null)
        {
            velocitySlider.interactable = false;
        }

        InitializeAnswerInput();
        UpdateAnswerPrompt();
        UpdateAnswerInputVisibility();
        UpdateTutorialUIVisibility();
        HideAnswerKeyboard();
        UpdateFireButtonInteractable();
        SyncModeValues();
        UpdateResolvedLaunchValues();
    }

    void Update()
    {
        // ====================================
        // CINEMATIC PAUSE
        // ====================================

        GameSettings settings = GameSettings.Instance;
        UpdateAnswerInputVisibility();
        UpdateTutorialUIVisibility();
        if (settings != null && settings.cinematicPause)
        {
            return;
        }

        UpdateFireButtonInteractable();

        if (physicsMode == CannonPhysicsMode.Tutorial)
        {
            // ====================================
            // SLIDER INTERACTION
            // ====================================

            if (velocitySlider != null)
            {
                velocitySlider.interactable =
                    settings == null || !settings.cinematicPause;
            }

            CalculateRange();
        }
        else
        {
            SyncModeValues();
        }

        if (formulaSustition != null)
        {
            formulaSustition.UpdateFormulaValues();
        }

        UpdateTrajectory();

        // TEST FIRE

        bool encounterActive =
            StartGamePlay.Instance != null
            &&
            StartGamePlay.Instance.encounterActive;

        if (
            encounterActive
            &&
            Input.GetKeyDown(KeyCode.Space)
            )
        {
            if (Fire())
            {
                Instantiate(cannonballFiredFx);
                Instantiate(cannonFireFx, cannonMuzzle.position, Quaternion.identity);
            }
        }
    }
    public void FireButton()
    {
        // ====================================
        // CINEMATIC PAUSE
        // ====================================

        GameSettings settings = GameSettings.Instance;

        if (settings != null && settings.cinematicPause)
            return;

        bool encounterActive =
           StartGamePlay.Instance != null
           &&
           StartGamePlay.Instance.encounterActive;

        if (!encounterActive)
            return;

        if (HasActiveCannonBall())
        {
            return;
        }

        HideAnswerKeyboard();

        if (physicsMode == CannonPhysicsMode.SolveRange)
        {
            if (!isSolveRangeSequenceRunning)
            {
                StartCoroutine(PrepareSolveRangeShotSequence());
            }

            return;
        }

        if (Fire())
        {
            Instantiate(cannonballFiredFx);
            Instantiate(cannonFireFx, cannonMuzzle.position, Quaternion.identity);
        }
    }

    public void FireButtonAfterDelay(float delaySeconds)
    {
        if (isFireButtonDelayRunning)
        {
            return;
        }

        StartCoroutine(FireButtonAfterDelayRoutine(delaySeconds));
    }

    IEnumerator FireButtonAfterDelayRoutine(float delaySeconds)
    {
        isFireButtonDelayRunning = true;

        HideAnswerKeyboard();

        if (delaySeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);
        }

        if (!CanFireNow())
        {
            isFireButtonDelayRunning = false;
            yield break;
        }

        FireButton();

        isFireButtonDelayRunning = false;
    }

    public bool CanOpenAnswerKeyboard()
    {
        return physicsMode != CannonPhysicsMode.Tutorial &&
               !HasActiveCannonBall();
    }

    bool CanFireNow()
    {
        GameSettings settings = GameSettings.Instance;

        if (settings != null && settings.cinematicPause)
        {
            return false;
        }

        if (StartGamePlay.Instance == null ||
            !StartGamePlay.Instance.encounterActive)
        {
            return false;
        }

        return !HasActiveCannonBall();
    }

    // ====================================
    // CALCULATE RANGE
    // ====================================    // ====================================

    void CalculateRange()
    {
        // ====================================
        // VISUAL ANGLE
        // ====================================

        float visualAngle =
            cannonAimUI != null
                ? cannonAimUI.GetCurrentAngle()
                : currentAngle;

        // ====================================
        // PHYSICAL ANGLE
        // ====================================

        currentAngle =
            Mathf.Abs(visualAngle);

        // ====================================
        // UPDATE UI
        // ====================================

        if (angleText != null)
        {
            angleText.text =
                currentAngle
                .ToString("F1") + "°";
        }

        // ====================================
        // KEEP TEXT READABLE
        // ====================================

        if (angleTextTransform != null)
        {
            angleTextTransform.rotation =
                Quaternion.identity;
        }

        // ====================================
        // CALCULATE RANGE
        // ====================================

        currentRange =
            CalculateRangeFrom(
                initialVelocity,
                currentAngle
            );
    }

    public float CalculateRangeFrom(float velocity, float angleDegrees)
    {
        float radians =
            Mathf.Abs(angleDegrees) *
            Mathf.Deg2Rad;

        return (
            velocity *
            velocity *
            Mathf.Sin(2f * radians)
        ) / gravity;
    }

    public bool TryCalculateInitialVelocity(
        float range,
        float angleDegrees,
        out float velocity)
    {
        velocity = 0f;

        if (gravity <= 0f || range < 0f)
        {
            return false;
        }

        float radians =
            Mathf.Abs(angleDegrees) *
            Mathf.Deg2Rad;

        float sinDoubleAngle =
            Mathf.Sin(2f * radians);

        if (Mathf.Abs(sinDoubleAngle) < 0.0001f)
        {
            return false;
        }

        float value =
            (range * gravity) /
            sinDoubleAngle;

        if (value < 0f)
        {
            return false;
        }

        velocity =
            Mathf.Sqrt(value);

        return !float.IsNaN(velocity) &&
               !float.IsInfinity(velocity);
    }

    public bool TryCalculateRange(
        float velocity,
        float angleDegrees,
        out float range)
    {
        range = CalculateRangeFrom(
            velocity,
            angleDegrees
        );

        return !float.IsNaN(range) &&
               !float.IsInfinity(range);
    }

    public bool TryCalculatePrincipalAngle(
        float range,
        float velocity,
        out float angleDegrees)
    {
        angleDegrees = 0f;

        if (gravity <= 0f || velocity <= 0f || range < 0f)
        {
            return false;
        }

        float ratio =
            (range * gravity) /
            (velocity * velocity);

        if (ratio < -1f || ratio > 1f)
        {
            return false;
        }

        float doubleAngleRadians =
            Mathf.Asin(ratio);

        angleDegrees =
            (doubleAngleRadians * Mathf.Rad2Deg) / 2f;

        return !float.IsNaN(angleDegrees) &&
               !float.IsInfinity(angleDegrees);
    }

    float GetSolveAngleChallengeVelocity()
    {
        return Mathf.Max(0.1f, maxInitialVelocity);
    }

    public bool TryGetExpectedAnswer(out float expectedAnswer)
    {
        expectedAnswer = 0f;

        switch (physicsMode)
        {
            case CannonPhysicsMode.SolveInitialVelocity:
                return TryCalculateInitialVelocity(
                    challengeRange,
                    challengeAngle,
                    out expectedAnswer
                );

            case CannonPhysicsMode.SolveRange:
                return TryCalculateRange(
                    GetSolveAngleChallengeVelocity(),
                    challengeAngle,
                    out expectedAnswer
                );

            case CannonPhysicsMode.SolveAngle:
                return TryCalculatePrincipalAngle(
                    challengeRange,
                    GetSolveAngleChallengeVelocity(),
                    out expectedAnswer
                );

            case CannonPhysicsMode.Tutorial:
            default:
                expectedAnswer = CalculateRangeFrom(
                    initialVelocity,
                    currentAngle
                );
                return true;
        }
    }

    public bool ValidateUserAnswer()
    {
        return ValidateUserAnswer(userAnswerValue);
    }

    public bool ValidateUserAnswer(float value)
    {
        if (!TryGetExpectedAnswer(out float expectedAnswer))
        {
            return false;
        }

        GameSettings settings =
            GameSettings.Instance;

        if (settings == null)
        {
            return Mathf.Approximately(
                value,
                expectedAnswer
            );
        }

        return MathValidator.Validate(
            (decimal)expectedAnswer,
            (decimal)value,
            settings.decimals,
            settings.validationMode
        );
    }

    public void SetUserAnswerValue(float value)
    {
        userAnswerValue = value;
        UpdateResolvedLaunchValues();
    }

    public void SetUserAnswerValue(string value)
    {
        userAnswerValue = ParseAnswerValue(value);
        UpdateResolvedLaunchValues();
    }

    float ParseAnswerValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0f;
        }

        value = value.Trim();

        if (float.TryParse(
            value,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float parsedValue))
        {
            return parsedValue;
        }

        value = value.Replace(',', '.');

        if (float.TryParse(
            value,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out parsedValue))
        {
            return parsedValue;
        }

        if (float.TryParse(value, out parsedValue))
        {
            return parsedValue;
        }

        return 0f;
    }
    public void SyncUserAnswerFromInputField()
    {
        if (answerInputField == null)
        {
            return;
        }

        SetUserAnswerValue(answerInputField.text);
    }

    public bool TryGetResolvedLaunchData(
        out float launchVelocity,
        out float launchAngle,
        out float launchRange)
    {
        UpdateResolvedLaunchValues();

        launchVelocity = resolvedLaunchVelocity;
        launchAngle = resolvedLaunchAngle;
        launchRange = resolvedLaunchRange;

        return !float.IsNaN(launchVelocity) &&
               !float.IsNaN(launchAngle) &&
               !float.IsNaN(launchRange);
    }


    void InitializeAnswerInput()
    {
        if (answerInputField == null)
        {
            return;
        }

        answerInputField.onValueChanged.AddListener(SetUserAnswerValue);
        answerInputField.onEndEdit.AddListener(SetUserAnswerValue);
        answerInputField.text = string.Empty;
    }

    void UpdateAnswerPrompt()
    {
        if (answerPromptText != null)
        {
            answerPromptText.text = GetModePrompt();
        }
    }

    void UpdateChallengeDataUI()
    {
        bool isSolveInitialVelocity =
            physicsMode == CannonPhysicsMode.SolveInitialVelocity;
        bool isSolveRange = physicsMode == CannonPhysicsMode.SolveRange;
        bool isSolveAngle = physicsMode == CannonPhysicsMode.SolveAngle;

        if (challengeRangeText != null)
        {
            challengeRangeText.text = isSolveInitialVelocity || isSolveAngle
                ? challengeRange.ToString("F2") + " m"
                : isSolveRange
                    ? "??"
                    : challengeRange.ToString("F2") + " m";
        }

        if (challengeInitialVelocityText != null)
        {
            challengeInitialVelocityText.text = isSolveRange || isSolveAngle
                ? challengeInitialVelocity.ToString("F2") + " m/s"
                : isSolveInitialVelocity
                    ? "??"
                    : challengeInitialVelocity.ToString("F2") + " m/s";
        }

        if (challengeAngleText != null)
        {
            challengeAngleText.text = isSolveInitialVelocity || isSolveRange
                ? challengeAngle.ToString("F1") + "°"
                : isSolveAngle
                    ? "??"
                    : challengeAngle.ToString("F1") + "°";
        }
    }

    void UpdateAnswerInputVisibility()
    {
        bool showAnswerInput =
            physicsMode != CannonPhysicsMode.Tutorial;

        SetGameObjectActive(answerHolder, showAnswerInput);

        if (!showAnswerInput)
        {
            HideAnswerKeyboard();
        }

        if (answerInputField != null)
        {
            answerInputField.interactable = showAnswerInput;
            answerInputField.readOnly =
                showAnswerInput &&
                Application.isMobilePlatform &&
                !Application.isEditor;
            answerInputField.shouldHideMobileInput = true;
        }
    }

    void UpdateTutorialUIVisibility()
    {
        bool showTutorialUI = physicsMode == CannonPhysicsMode.Tutorial;

        SetCanvasGroupState(cannonHolderCanvasGroup, showTutorialUI);
        SetCanvasGroupState(velocityHolderCanvasGroup, showTutorialUI);
        SetGameObjectActive(fireButtonHolder, showTutorialUI);
    }

    void SetCanvasGroupState(CanvasGroup canvasGroup, bool visible)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    void SetGameObjectActive(GameObject target, bool active)
    {
        if (target == null)
        {
            return;
        }

        if (target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }

    public void SetAnswerInputText(string value)
    {
        if (answerInputField == null)
        {
            return;
        }

        answerInputField.SetTextWithoutNotify(value);
        SetUserAnswerValue(value);
    }

    public void AppendAnswerInputText(string value)
    {
        if (answerInputField == null || string.IsNullOrEmpty(value))
        {
            return;
        }

        SetAnswerInputText(answerInputField.text + value);
    }

    public void BackspaceAnswerInputText()
    {
        if (answerInputField == null)
        {
            return;
        }

        string currentText = answerInputField.text ?? string.Empty;

        if (currentText.Length == 0)
        {
            return;
        }

        SetAnswerInputText(
            currentText.Substring(0, currentText.Length - 1)
        );
    }

    public void ClearAnswerInputText()
    {
        SetAnswerInputText(string.Empty);
    }

    public void ShowAnswerKeyboard()
    {
        if (physicsMode == CannonPhysicsMode.Tutorial)
        {
            return;
        }

        if (answerKeyboard == null)
        {
            if (answerKeyboardHolder != null)
            {
                SetGameObjectActive(answerKeyboardHolder, true);
            }

            return;
        }

        answerKeyboard.ShowKeyboard();
    }

    public void HideAnswerKeyboard()
    {
        if (answerKeyboard != null)
        {
            answerKeyboard.HideKeyboard();
            return;
        }

        if (answerKeyboardHolder != null)
        {
            SetGameObjectActive(answerKeyboardHolder, false);
        }
    }

    void UpdateResolvedLaunchValues()
    {
        switch (physicsMode)
        {
            case CannonPhysicsMode.SolveInitialVelocity:
                resolvedLaunchAngle = challengeAngle;
                resolvedLaunchVelocity = userAnswerValue;
                resolvedLaunchRange = CalculateRangeFrom(
                    resolvedLaunchVelocity,
                    resolvedLaunchAngle
                );
                break;

            case CannonPhysicsMode.SolveRange:
                resolvedLaunchVelocity = GetSolveAngleChallengeVelocity();
                resolvedLaunchAngle = challengeAngle;
                resolvedLaunchRange = CalculateRangeFrom(
                    resolvedLaunchVelocity,
                    resolvedLaunchAngle
                );
                break;

            case CannonPhysicsMode.SolveAngle:
                resolvedLaunchVelocity = GetSolveAngleChallengeVelocity();
                resolvedLaunchAngle = userAnswerValue;
                resolvedLaunchRange = CalculateRangeFrom(
                    resolvedLaunchVelocity,
                    resolvedLaunchAngle
                );
                break;

            case CannonPhysicsMode.Tutorial:
            default:
                resolvedLaunchRange = CalculateRangeFrom(
                    initialVelocity,
                    currentAngle
                );
                resolvedLaunchAngle = currentAngle;
                resolvedLaunchVelocity = initialVelocity;
                break;
        }
    }

    void ApplyResolvedLaunchToCannon()
    {
        UpdateResolvedLaunchValues();

        if (physicsMode == CannonPhysicsMode.Tutorial)
        {
            currentAngle =
                cannonAimUI != null
                    ? Mathf.Abs(cannonAimUI.GetCurrentAngle())
                    : currentAngle;

            currentRange =
                CalculateRangeFrom(
                    initialVelocity,
                    currentAngle
                );

            return;
        }

        if (cannonAimUI != null)
        {
            cannonAimUI.SetCurrentAngle(-resolvedLaunchAngle);
        }

        currentAngle = resolvedLaunchAngle;
        currentRange = resolvedLaunchRange;
        initialVelocity = resolvedLaunchVelocity;
    }

    public string GetModePrompt()
    {
        switch (physicsMode)
        {
            case CannonPhysicsMode.SolveInitialVelocity:
                return "Ingresa la velocidad inicial";

            case CannonPhysicsMode.SolveRange:
                return "Ingresa el rango";

            case CannonPhysicsMode.SolveAngle:
                return "Ingresa el angulo principal";

            case CannonPhysicsMode.Tutorial:
            default:
                return "Tutorial";
        }
    }

    void GenerateRandomSolveRangeChallenge()
    {
        challengeAngle =
            UnityEngine.Random.Range(10f, 80f);

        challengeInitialVelocity =
            GetSolveAngleChallengeVelocity();

        challengeRange =
            CalculateRangeFrom(
                challengeInitialVelocity,
                challengeAngle
            );
        UpdateChallengeDataUI();
    }
    public void PrepareChallengeFromEnemy(
        Vector3 playerPosition,
        Vector3 targetCenter)
    {
        challengePlayerPosition =
            playerPosition;

        challengeTargetCenter =
            targetCenter;

        Vector3 cannonOrigin =
            cannonMuzzle != null
                ? cannonMuzzle.position
                : playerPosition;

        challengeTargetDistance =
            GetHorizontalDistance(
                cannonOrigin,
                targetCenter
            );

        challengeRange =
            challengeTargetDistance;

        switch (physicsMode)
        {
            case CannonPhysicsMode.SolveInitialVelocity:
                challengeAngle =
                    challengeAngleSeed;

                if (!TryCalculateInitialVelocity(
                    challengeRange,
                    challengeAngle,
                    out challengeInitialVelocity
                ))
                {
                    challengeInitialVelocity = 0f;
                }
                break;

            case CannonPhysicsMode.SolveRange:
                challengeInitialVelocity = GetSolveAngleChallengeVelocity();
                GenerateRandomSolveRangeChallenge();
                break;

            case CannonPhysicsMode.SolveAngle:
                challengeInitialVelocity = GetSolveAngleChallengeVelocity();

                if (!TryCalculatePrincipalAngle(
                    challengeRange,
                    challengeInitialVelocity,
                    out challengeAngle
                ))
                {
                    challengeAngle = 0f;
                }
                break;

            case CannonPhysicsMode.Tutorial:
            default:
                challengeAngle = currentAngle;
                challengeInitialVelocity = initialVelocity;
                break;
        }

        SyncModeValues();
        UpdateChallengeDataUI();
        UpdateAnswerPrompt();

        if (formulaSustition != null)
        {
            formulaSustition.UpdateFormulaValues();
        }

        UpdateResolvedLaunchValues();
    }

    public float GetHorizontalDistance(
        Vector3 a,
        Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }

    void SyncModeValues()
    {
        if (cannonAimUI != null)
        {
            cannonAimUI.inputBlocked =
                physicsMode == CannonPhysicsMode.SolveInitialVelocity ||
                physicsMode == CannonPhysicsMode.SolveRange ||
                physicsMode == CannonPhysicsMode.SolveAngle;
        }

        if (physicsMode == CannonPhysicsMode.Tutorial)
        {
            return;
        }

        switch (physicsMode)
        {
            case CannonPhysicsMode.SolveInitialVelocity:
                currentAngle = challengeAngle;
                currentRange = challengeRange;
                initialVelocity = userAnswerValue;
                break;

            case CannonPhysicsMode.SolveRange:
                challengeInitialVelocity = GetSolveAngleChallengeVelocity();
                currentAngle = challengeAngle;
                initialVelocity = challengeInitialVelocity;
                challengeRange = CalculateRangeFrom(
                    challengeInitialVelocity,
                    challengeAngle
                );
                currentRange = challengeRange;
                break;

            case CannonPhysicsMode.SolveAngle:
                challengeInitialVelocity = GetSolveAngleChallengeVelocity();
                currentAngle = userAnswerValue;
                initialVelocity = challengeInitialVelocity;
                currentRange = challengeRange;
                break;
        }
        if (physicsMode != CannonPhysicsMode.Tutorial && cannonAimUI != null)
        {
            cannonAimUI.SetCurrentAngle(-currentAngle);
        }

        if (velocityValue != null)
        {
            velocityValue.text = initialVelocity.ToString("f2") + (" m/s");
        }

        if (angleText != null)
        {
            angleText.text = currentAngle.ToString("F1") + "°";
        }

        if (angleTextTransform != null)
        {
            angleTextTransform.rotation = Quaternion.identity;
        }
    }

    // ====================================
    // CREATE DOT POOL
    // ====================================

    void CreateTrajectoryPool()
    {
        // ====================================
        // EVITAR DUPLICADOS
        // ====================================

        if (trajectoryDots.Count > 0)
            return;

        // ====================================
        // CREAR DOTS
        // ====================================

        for (int i = 0; i < maxDots; i++)
        {
            GameObject dot =
                Instantiate(
                    trajectoryDotPrefab,
                    transform
                );

            dot.SetActive(false);

            // ====================================
            // GUARDAR EN LISTA
            // ====================================

            trajectoryDots.Add(dot);

            // ====================================
            // ASIGNAR MANAGER
            // ====================================

            TrajectoryDot trajectoryDot =
                dot.GetComponent<TrajectoryDot>();

            if (trajectoryDot != null)
            {
                trajectoryDot.manager = this;
            }
        }
    }

    void ExpandDotPool(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            GameObject dot =
                Instantiate(
                    trajectoryDotPrefab,
                    transform
                );

            dot.SetActive(false);

            trajectoryDots.Add(dot);

            TrajectoryDot trajectoryDot =
                dot.GetComponent<TrajectoryDot>();

            if (trajectoryDot != null)
            {
                trajectoryDot.manager = this;
            }
        }
    }

    void InitializeVelocitySlider()
    {
        if (velocitySlider == null)
            return;

        // ====================================
        // CONFIGURAR RANGO
        // ====================================

        velocitySlider.minValue =
            minInitialVelocity;

        velocitySlider.maxValue =
            maxInitialVelocity;

        // ====================================
        // VALOR ACTUAL
        // ====================================

        velocitySlider.value =
            initialVelocity;

        // ====================================
        // LISTENER
        // ====================================

        velocitySlider.onValueChanged
            .AddListener(UpdateVelocityFromSlider);

        if (velocityValue != null)
        {
            velocityValue.text = initialVelocity.ToString("f2") + (" m/s");
        }
    }

    void UpdateVelocityFromSlider(float value)
    {
        initialVelocity =
            Mathf.Clamp(
                value,
                minInitialVelocity,
                maxInitialVelocity
            );

        if (velocityValue != null)
        {
            velocityValue.text = initialVelocity.ToString("f2") + (" m/s");
        }

        formulaSustition.UpdateFormulaValues();
    }

    public void RefreshVelocitySlider()
    {
        if (velocitySlider == null)
            return;

        velocitySlider.minValue =
            minInitialVelocity;

        velocitySlider.maxValue =
            maxInitialVelocity;

        velocitySlider.value =
            initialVelocity;
    }

    // ====================================
    // UPDATE TRAJECTORY
    // ====================================

    void UpdateTrajectory()
    {
        // ====================================
        // ENCOUNTER STATE
        // ====================================

        bool encounterActive =
            StartGamePlay.Instance != null
            &&
            StartGamePlay.Instance.encounterActive;

        bool visible =
            showTrajectory &&
            encounterActive;

        // ====================================
        // HIDE ALL
        // ====================================

        if (!visible)
        {
            for (int i = 0; i < trajectoryDots.Count; i++)
            {
                trajectoryDots[i].SetActive(false);
            }

            return;
        }

        // ====================================
        // INITIAL DATA
        // ====================================

        Vector3 currentPosition =
            cannonMuzzle.position;

        Vector3 currentVelocity =
            cannonMuzzle.forward.normalized *
            initialVelocity;

        int dotIndex = 0;

        float accumulatedDistance = 0f;

        Vector3 lastDotPosition =
            currentPosition;

        // ====================================
        // SIMULATION
        // ====================================

        while (true)
        {
            Vector3 previousPosition =
                currentPosition;

            // ====================================
            // APPLY GRAVITY
            // ====================================

            currentVelocity +=
                Vector3.down *
                gravity *
                trajectoryTimeStep;

            // ====================================
            // MOVE
            // ====================================

            currentPosition +=
                currentVelocity *
                trajectoryTimeStep;

            // ====================================
            // DISTANCE
            // ====================================

            accumulatedDistance +=
                Vector3.Distance(
                    previousPosition,
                    currentPosition
                );

            // ====================================
            // PLACE DOT
            // ====================================

            if (accumulatedDistance >= dotSpacing)
            {
                accumulatedDistance = 0f;

                // ====================================
                // EXPAND POOL
                // ====================================

                if (dotIndex >= trajectoryDots.Count)
                {
                    ExpandDotPool(20);
                }

                GameObject dot =
                    trajectoryDots[dotIndex];
                if (dot == null)
                {
                    continue;
                }

                dot.SetActive(true);

                dot.transform.position =
                    currentPosition;

                lastDotPosition =
                    currentPosition;

                dotIndex++;
            }

            // ====================================
            // HIT GROUND
            // ====================================

            if (
                Physics.Linecast(
                    previousPosition,
                    currentPosition,
                    groundLayer
                )
            )
            {
                break;
            }

            // ====================================
            // SAFETY
            // ====================================

            if (dotIndex > 1000)
            {
                break;
            }
        }

        // ====================================
        // HIDE UNUSED
        // ====================================

        for (
            int i = dotIndex;
            i < trajectoryDots.Count;
            i++
        )
        {
            if (trajectoryDots[i] != null)
            {
                trajectoryDots[i].SetActive(false);
            }
        }
    }

    // ====================================
    // FIRE
    // ====================================

    public bool Fire()
    {
        if (cannonBallPrefab == null)
            return false;

        if (HasActiveCannonBall())
            return false;

        HideAnswerKeyboard();
        ApplyResolvedLaunchToCannon();

        // ====================================
        // CREATE BALL
        // ====================================

        GameObject ball =
            Instantiate(
                cannonBallPrefab,
                cannonMuzzle.position,
                Quaternion.identity
            );

        // ====================================
        // GET SCRIPT
        // ====================================

        CannonBall cannonBall =
            ball.GetComponent<CannonBall>();

        if (cannonBall == null)
            return false;

        cannonBall.SetFireCanonManager(this);
        RegisterActiveCannonBall(cannonBall);

        // ====================================
        // FORWARD DIRECTION
        // ====================================

        Vector3 forward =
            cannonMuzzle.forward.normalized;

        // ====================================
        // INITIAL VELOCITY
        // ====================================

        Vector3 velocity =
            forward *
            resolvedLaunchVelocity;

        // ====================================
        // INITIALIZE BALL
        // ====================================

        cannonBall.Initialize(
            velocity,
            gravity
        );

        return true;
    }

    public bool HasActiveCannonBall()
    {
        return activeCannonBall != null;
    }

    public void RegisterActiveCannonBall(CannonBall cannonBall)
    {
        activeCannonBall = cannonBall;
        UpdateFireButtonInteractable();
    }

    public void ClearActiveCannonBall(CannonBall cannonBall)
    {
        if (activeCannonBall == cannonBall)
        {
            activeCannonBall = null;
            UpdateFireButtonInteractable();
        }
    }

    void UpdateFireButtonInteractable()
    {
        if (fireButton != null)
        {
            fireButton.interactable = !HasActiveCannonBall();
        }
    }

    Transform GetPlayerTransform()
    {
        if (StartGamePlay.Instance != null && StartGamePlay.Instance.player != null)
        {
            return StartGamePlay.Instance.player.transform;
        }

        PlayerMovement playerMovement =
            FindFirstObjectByType<PlayerMovement>();

        return playerMovement != null
            ? playerMovement.transform
            : null;
    }

    PlayerMovement GetPlayerMovement()
    {
        if (StartGamePlay.Instance != null && StartGamePlay.Instance.player != null)
        {
            return StartGamePlay.Instance.player.GetComponent<PlayerMovement>();
        }

        return FindFirstObjectByType<PlayerMovement>();
    }

    Rigidbody GetPlayerRigidbody()
    {
        Transform playerTransform = GetPlayerTransform();
        return playerTransform != null
            ? playerTransform.GetComponent<Rigidbody>()
            : null;
    }

    float GetHorizontalDistanceToSelectedTarget()
    {
        if (cannonMuzzle == null)
        {
            return 0f;
        }

        Vector3 targetCenter = challengeTargetCenter;
        Vector3 muzzlePosition = cannonMuzzle.position;

        targetCenter.y = 0f;
        muzzlePosition.y = 0f;

        return Vector3.Distance(muzzlePosition, targetCenter);
    }

    Vector3 GetSolveRangeTargetPosition(float targetRange)
    {
        Transform playerTransform = GetPlayerTransform();
        if (playerTransform == null || cannonMuzzle == null)
        {
            return Vector3.zero;
        }

        Vector3 targetPosition = challengeTargetCenter;
        Vector3 cannonOrigin = cannonMuzzle.position;

        targetPosition.y = 0f;
        cannonOrigin.y = 0f;

        Vector3 directionFromCannonToTarget =
            targetPosition - cannonOrigin;
        directionFromCannonToTarget.y = 0f;

        if (directionFromCannonToTarget.sqrMagnitude < 0.0001f)
        {
            directionFromCannonToTarget = playerTransform.forward;
            directionFromCannonToTarget.y = 0f;
        }

        if (directionFromCannonToTarget.sqrMagnitude < 0.0001f)
        {
            directionFromCannonToTarget = Vector3.forward;
        }

        directionFromCannonToTarget.Normalize();

        float currentHorizontalDistance =
            GetHorizontalDistanceToSelectedTarget();

        float moveDistance =
            currentHorizontalDistance - Mathf.Max(0f, targetRange);

        return playerTransform.position +
               directionFromCannonToTarget * moveDistance;
    }

    IEnumerator MovePlayerToPosition(
        Transform playerTransform,
        Rigidbody playerRigidbody,
        Vector3 targetPosition,
        float duration)
    {
        if (playerTransform == null)
        {
            yield break;
        }

        const float minimumCinematicDuration = 1.15f;

        Vector3 startPosition = playerTransform.position;
        float effectiveDuration =
            Mathf.Max(duration, minimumCinematicDuration);

        if (effectiveDuration <= 0f)
        {
            if (playerRigidbody != null)
            {
                playerRigidbody.position = targetPosition;
                playerRigidbody.linearVelocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }
            else
            {
                playerTransform.position = targetPosition;
            }

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < effectiveDuration)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;

            float t = Mathf.Clamp01(elapsed / effectiveDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 nextPosition = Vector3.Lerp(
                startPosition,
                targetPosition,
                easedT
            );

            if (playerRigidbody != null)
            {
                playerRigidbody.MovePosition(nextPosition);
            }
            else
            {
                playerTransform.position = nextPosition;
            }
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.position = targetPosition;
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }
        else
        {
            playerTransform.position = targetPosition;
        }
    }
    IEnumerator SnapCannonMuzzleToSolveRange(
        Transform playerTransform,
        Rigidbody playerRigidbody,
        float targetRange)
    {
        if (playerTransform == null || cannonMuzzle == null)
        {
            yield break;
        }

        const float tolerance = 0.01f;
        const int maxIterations = 8;

        for (int i = 0; i < maxIterations; i++)
        {
            float currentHorizontalDistance =
                GetHorizontalDistanceToSelectedTarget();

            float moveDistance =
                currentHorizontalDistance - Mathf.Max(0f, targetRange);

            if (Mathf.Abs(moveDistance) <= tolerance)
            {
                yield break;
            }

            Vector3 targetCenter = challengeTargetCenter;
            Vector3 cannonOrigin = cannonMuzzle.position;
            targetCenter.y = 0f;
            cannonOrigin.y = 0f;

            Vector3 directionFromCannonToTarget =
                targetCenter - cannonOrigin;
            directionFromCannonToTarget.y = 0f;

            if (directionFromCannonToTarget.sqrMagnitude < 0.0001f)
            {
                directionFromCannonToTarget = playerTransform.forward;
                directionFromCannonToTarget.y = 0f;
            }

            if (directionFromCannonToTarget.sqrMagnitude < 0.0001f)
            {
                yield break;
            }

            directionFromCannonToTarget.Normalize();

            Vector3 correctedPosition =
                playerTransform.position +
                directionFromCannonToTarget * moveDistance;

            if (playerRigidbody != null)
            {
                playerRigidbody.MovePosition(correctedPosition);
                playerRigidbody.linearVelocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }
            else
            {
                playerTransform.position = correctedPosition;
            }

            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator PrepareSolveRangeShotSequence()
    {
        isSolveRangeSequenceRunning = true;

        SyncUserAnswerFromInputField();
        HideAnswerKeyboard();

        string answerText = answerInputField != null ? answerInputField.text : string.Empty;
        float targetRange = answerInputField != null
            ? Mathf.Max(0f, ParseAnswerValue(answerText))
            : Mathf.Max(0f, userAnswerValue);
        float initialHorizontalDistance = GetHorizontalDistanceToSelectedTarget();
        float moveDelta = initialHorizontalDistance - targetRange;

        Debug.Log(
            $"[SolveRange] input text: '{answerText}' | inspector answer: {userAnswerValue:F2} | initial horizontal distance: {initialHorizontalDistance:F2} | target range: {targetRange:F2} | move delta: {moveDelta:F2}"
        );

        Transform playerTransform = GetPlayerTransform();
        Rigidbody playerRigidbody = GetPlayerRigidbody();
        PlayerMovement playerMovement = GetPlayerMovement();

        if (playerMovement != null)
        {
            playerMovement.StopMovementImmediately();
        }

        if (playerTransform != null)
        {
            Vector3 targetPosition =
                GetSolveRangeTargetPosition(targetRange);

            yield return MovePlayerToPosition(
                playerTransform,
                playerRigidbody,
                targetPosition,
                solveRangeMoveDuration
            );
        }

        float finalHorizontalDistance = GetHorizontalDistanceToSelectedTarget();
        Debug.Log(
            $"[SolveRange] final horizontal distance: {finalHorizontalDistance:F2} | expected range: {targetRange:F2}"
        );

        if (solveRangeFireDelay > 0f)
        {
            yield return new WaitForSeconds(solveRangeFireDelay);
        }

        if (Fire())
        {
            Instantiate(cannonballFiredFx);
            Instantiate(cannonFireFx, cannonMuzzle.position, Quaternion.identity);
        }

        isSolveRangeSequenceRunning = false;
    }
}
