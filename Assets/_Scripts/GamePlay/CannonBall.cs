using Cinemachine;
using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CannonBall : MonoBehaviour
{
    [Header("Debug")]
    [HideInInspector]
    public Vector3 velocity;

    [HideInInspector]
    public float totalVelocity;

    [HideInInspector]
    public float gravity;

    [Header("Initial Velocity")]
    public float initialVelocityX;
    public float initialVelocityY;
    public float initialVelocityMagnitude;

    [Header("Tap Velocity")]
    public float velocityX;
    public float velocityY;
    public float tapVelocityMagnitude;

    [Header("Velocity UI")]
    public TMP_Text velocityXText;
    public TMP_Text velocityYText;

    Rigidbody rb;

    float currentVelocityXFormula;
    float currentVelocityYFormula;
    float savedVelocityXFormula;
    float savedVelocityYFormula;
    Vector3 horizontalDirection = Vector3.forward;

    bool isPaused;
    bool challengeShotActive;
    bool challengeAnswerCorrect;
    float challengeFlightElapsed;
    float challengeFlightTime;
    Vector3 challengeImpactPoint;
    const float ChallengeImpactPointTolerance = 0.5f;

    [Header("Impact FX")]
    public GameObject waterImpactPrefab;

    public GameObject hitImpactPrefab;

    public GameObject waterSplashSoundFX;

    [Header("FX Offset")]
    public Vector3 fxSpawnOffset = Vector3.zero;

    [Header("Tap Area")]
    public float tapColliderRadius = 0.75f;
    public Vector3 tapColliderCenter = Vector3.zero;

    [Header("Cannon Ball Canvas")]
    public CanvasGroup cannonBallCanvasGroup;
    public float cannonBallCanvasFadeDuration = 1f;

    FireCanonManager fireCanonManager;
    CinemachineVirtualCamera tapCamera;
    SphereCollider tapCollider;
    CannonBallTapArea tapAreaProxy;
    bool isTapPaused;
    Coroutine cannonBallCanvasFadeRoutine;

    // ====================================
    // UNITY
    // ====================================

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        tapCamera = GetComponentInChildren<CinemachineVirtualCamera>(true);
        if (cannonBallCanvasGroup == null)
        {
            cannonBallCanvasGroup = GetComponentInChildren<CanvasGroup>(true);
        }

        InitializeCannonBallCanvasGroup();
        EnsureTapCollider();

        if (tapCamera != null)
        {
            tapCamera.gameObject.SetActive(false);
        }
    }

    void InitializeCannonBallCanvasGroup()
    {
        if (cannonBallCanvasGroup == null)
        {
            return;
        }

        cannonBallCanvasGroup.alpha = 0f;
        cannonBallCanvasGroup.interactable = false;
        cannonBallCanvasGroup.blocksRaycasts = false;
    }

    void EnsureTapCollider()
    {
        Transform tapAreaTransform =
            transform.Find("Tap Area");

        if (tapAreaTransform == null)
        {
            GameObject tapAreaObject =
                new GameObject("Tap Area");

            tapAreaTransform = tapAreaObject.transform;
            tapAreaTransform.SetParent(transform, false);
        }

        tapAreaTransform.localPosition = tapColliderCenter;
        tapAreaTransform.localRotation = Quaternion.identity;
        tapAreaTransform.localScale = Vector3.one;

        int tapLayer =
            LayerMask.NameToLayer("TransparentFX");

        if (tapLayer >= 0)
        {
            tapAreaTransform.gameObject.layer = tapLayer;
            IgnoreTapLayerPhysics(tapLayer);
        }

        tapCollider = tapAreaTransform.GetComponent<SphereCollider>();

        if (tapCollider == null)
        {
            tapCollider =
                tapAreaTransform.gameObject.AddComponent<SphereCollider>();
        }

        tapCollider.isTrigger = true;
        tapCollider.radius = Mathf.Max(0.01f, tapColliderRadius);

        tapAreaProxy =
            tapAreaTransform.GetComponent<CannonBallTapArea>();

        if (tapAreaProxy == null)
        {
            tapAreaProxy =
                tapAreaTransform.gameObject.AddComponent<CannonBallTapArea>();
        }

        tapAreaProxy.SetOwner(this);
    }

    void IgnoreTapLayerPhysics(int tapLayer)
    {
        for (int layer = 0; layer < 32; layer++)
        {
            if (layer == tapLayer)
            {
                continue;
            }

            Physics.IgnoreLayerCollision(tapLayer, layer, true);
        }
    }

    // ====================================
    // INITIALIZE
    // ====================================

    public void Initialize(
        Vector3 startVelocity,
        float customGravity
    )
    {
        Vector3 projectedHorizontalDirection = new Vector3(
            startVelocity.x,
            0f,
            startVelocity.z
        );

        if (projectedHorizontalDirection.sqrMagnitude <= 0.0001f)
        {
            projectedHorizontalDirection = Vector3.forward;
        }

        horizontalDirection = projectedHorizontalDirection.normalized;
        currentVelocityXFormula = initialVelocityX;
        currentVelocityYFormula = initialVelocityY;
        totalVelocity = Mathf.Sqrt(
            currentVelocityXFormula * currentVelocityXFormula +
            currentVelocityYFormula * currentVelocityYFormula
        );

        gravity = customGravity;
        challengeFlightElapsed = 0f;

        rb.useGravity = false;

        velocity =
            horizontalDirection * currentVelocityXFormula +
            Vector3.up * currentVelocityYFormula;
        rb.linearVelocity = velocity;
        CaptureTapDebugMath();
        UpdateVelocityText();
    }

    public void SetFireCanonManager(FireCanonManager manager)
    {
        fireCanonManager = manager;
    }

    public void SetChallengeShotOutcome(
        bool answerCorrect,
        Vector3 impactPoint,
        float flightTime
    )
    {
        challengeShotActive = true;
        challengeAnswerCorrect = answerCorrect;
        challengeImpactPoint = impactPoint;
        challengeFlightTime = Mathf.Max(0f, flightTime);
        challengeFlightElapsed = 0f;
    }

    public void SetLaunchDebugMath(
        float launchVelocity,
        float angleDegrees
    )
    {
        initialVelocityMagnitude = launchVelocity;

        float radians = Mathf.Abs(angleDegrees) * Mathf.Deg2Rad;
        float cosine = Mathf.Cos(radians);
        float sine = Mathf.Sin(radians);

        initialVelocityX = initialVelocityMagnitude * cosine;
        initialVelocityY = initialVelocityMagnitude * sine;
    }

    void CaptureTapDebugMath()
    {
        velocityX = Mathf.Abs(currentVelocityXFormula);
        velocityY = currentVelocityYFormula;
        tapVelocityMagnitude =
            Mathf.Sqrt(
                velocityX * velocityX +
                velocityY * velocityY
            );

        if (tapVelocityMagnitude <= 0.0001f)
        {
            velocityX = 0f;
            velocityY = 0f;
            return;
        }
    }

    // ====================================
    // PHYSICS
    // ====================================

    void FixedUpdate()
    {
        // ====================================
        // CINEMATIC PAUSE
        // ====================================

        GameSettings settings = GameSettings.Instance;

        if (settings != null && settings.cinematicPause)
        {
            // PAUSAR SOLO UNA VEZ
            if (!isPaused)
            {
                savedVelocityXFormula = currentVelocityXFormula;
                savedVelocityYFormula = currentVelocityYFormula;
                totalVelocity = Mathf.Sqrt(
                    currentVelocityXFormula * currentVelocityXFormula +
                    currentVelocityYFormula * currentVelocityYFormula
                );

                rb.linearVelocity =
                    Vector3.zero;

                isPaused = true;
                CaptureTapDebugMath();
                UpdateVelocityText();
            }

            return;
        }

        // ====================================
        // RESUME
        // ====================================

        if (isPaused)
        {
            currentVelocityXFormula = savedVelocityXFormula;
            currentVelocityYFormula = savedVelocityYFormula;
            totalVelocity = Mathf.Sqrt(
                currentVelocityXFormula * currentVelocityXFormula +
                currentVelocityYFormula * currentVelocityYFormula
            );

            velocity =
                horizontalDirection * currentVelocityXFormula +
                Vector3.up * currentVelocityYFormula;

            rb.linearVelocity = velocity;

            isPaused = false;
            CaptureTapDebugMath();
            UpdateVelocityText();
        }

        // ====================================
        // GRAVEDAD MANUAL
        // ====================================

        currentVelocityYFormula -= gravity * Time.fixedDeltaTime;

        velocity =
            horizontalDirection * currentVelocityXFormula +
            Vector3.up * currentVelocityYFormula;

        totalVelocity = Mathf.Sqrt(
            currentVelocityXFormula * currentVelocityXFormula +
            currentVelocityYFormula * currentVelocityYFormula
        );

        rb.linearVelocity = velocity;

        CaptureTapDebugMath();
        UpdateVelocityText();

        if (challengeShotActive &&
            challengeAnswerCorrect &&
            fireCanonManager != null &&
            fireCanonManager.physicsMode != CannonPhysicsMode.Tutorial)
        {
            challengeFlightElapsed += Time.fixedDeltaTime;

            Vector3 currentHorizontalPosition =
                Vector3.ProjectOnPlane(transform.position, Vector3.up);
            Vector3 impactHorizontalPosition =
                Vector3.ProjectOnPlane(challengeImpactPoint, Vector3.up);

            if (challengeFlightElapsed >= challengeFlightTime &&
                (currentHorizontalPosition - impactHorizontalPosition).sqrMagnitude <=
                ChallengeImpactPointTolerance *
                ChallengeImpactPointTolerance)
            {
                GameObject currentObjective =
                    StartGamePlay.Instance != null
                        ? StartGamePlay.Instance.currentObjective
                        : null;
                bool objectiveAvailable =
                    currentObjective != null &&
                    currentObjective.activeInHierarchy;

                if (objectiveAvailable)
                {
                    if (hitImpactPrefab != null)
                    {
                        Instantiate(
                            hitImpactPrefab,
                            GetFxSpawnPosition(challengeImpactPoint),
                            Quaternion.identity
                        );
                    }

                    fireCanonManager.TryApplyChallengeDamage(
                        null,
                        true
                    );
                }
                else
                {
                    if (waterImpactPrefab != null)
                    {
                        Instantiate(
                            waterImpactPrefab,
                            GetFxSpawnPosition(challengeImpactPoint),
                            Quaternion.identity
                        );
                    }

                    if (waterSplashSoundFX != null)
                    {
                        Instantiate(waterSplashSoundFX);
                    }
                }

                Destroy(gameObject);
            }
        }
    }

    void UpdateVelocityText()
    {
        if (velocityXText != null)
        {
            velocityXText.text = velocityX.ToString("F2");
        }

        if (velocityYText != null)
        {
            velocityYText.text = velocityY.ToString("F2");
        }
    }

    public void HandleTapRequested()
    {
        if (isTapPaused)
        {
            return;
        }

        GameSettings settings = GameSettings.Instance;

        if (settings == null)
        {
            return;
        }

        isTapPaused = true;
        settings.cannonBallViewActive = true;

        CaptureTapDebugMath();

        FadeCannonBallCanvas(1f);

        if (fireCanonManager != null)
        {
            fireCanonManager.EnterCannonBallView();
        }

        if (tapCamera != null)
        {
            tapCamera.gameObject.SetActive(true);
            tapCamera.m_Priority = 100;
        }

        settings.StartCannonBallViewPause();
    }

    void OnMouseDown()
    {
        HandleTapRequested();
    }

    public void ExitCannonBallView()
    {
        if (!isTapPaused)
        {
            return;
        }

        GameSettings settings = GameSettings.Instance;
        if (settings != null)
        {
            settings.EndCannonBallViewPause();
        }

        if (tapCamera != null)
        {
            tapCamera.m_Priority = 0;
            tapCamera.gameObject.SetActive(false);
        }

        FadeCannonBallCanvas(0f);

        if (fireCanonManager != null)
        {
            fireCanonManager.ExitCannonBallView();
        }

        isTapPaused = false;
    }

    void OnTriggerEnter(Collider other)
    {
        Vector3 hitPosition =
            transform.position;

        // ====================================
        // GROUND
        // ====================================

        if (
            other.CompareTag("Ground")
        )
        {
            if (fireCanonManager != null &&
                fireCanonManager.physicsMode != CannonPhysicsMode.Tutorial &&
                challengeShotActive &&
                challengeAnswerCorrect)
            {
                return;
            }

            // WATER FX
            if (waterImpactPrefab != null)
            {
                Instantiate(
                    waterImpactPrefab,
                    GetFxSpawnPosition(hitPosition),
                    Quaternion.identity
                );
            }

            Instantiate(waterSplashSoundFX);
            Destroy(gameObject);

            return;
        }

        // ====================================
        // ENEMY
        // ====================================

        if (
            other.CompareTag("Enemy")
        )
        {
            if (
                fireCanonManager != null &&
                fireCanonManager.physicsMode != CannonPhysicsMode.Tutorial
            )
            {
                return;
            }

            // HIT FX
            if (hitImpactPrefab != null)
            {
                Instantiate(
                    hitImpactPrefab,
                    GetFxSpawnPosition(hitPosition),
                    Quaternion.identity
                );
            }

            // CALL METHOD
            if (fireCanonManager != null &&
                fireCanonManager.physicsMode != CannonPhysicsMode.Tutorial)
            {
                fireCanonManager.TryApplyChallengeDamage(other.gameObject);
            }
            else
            {
                other.SendMessage(
                    "OnCannonBallHit",
                    SendMessageOptions
                        .DontRequireReceiver
                );
            }

            Destroy(gameObject);

            return;
        }

        // ====================================
        // PLAYER
        // ====================================

        if (
            other.CompareTag("Player")
        )
        {
            if (
                fireCanonManager != null &&
                fireCanonManager.physicsMode != CannonPhysicsMode.Tutorial
            )
            {
                return;
            }

            // HIT FX
            if (hitImpactPrefab != null)
            {
                Instantiate(
                    hitImpactPrefab,
                    GetFxSpawnPosition(hitPosition),
                    Quaternion.identity
                );
            }

            // CALL METHOD
            other.SendMessage(
                "SelfHarm",
                SendMessageOptions
                    .DontRequireReceiver
            );

            Destroy(gameObject);

            return;
        }
    }

    void OnDestroy()
    {
        if (GameSettings.Instance != null && isTapPaused)
        {
            GameSettings.Instance.EndCannonBallViewPause();
        }

        if (cannonBallCanvasFadeRoutine != null)
        {
            StopCoroutine(cannonBallCanvasFadeRoutine);
            cannonBallCanvasFadeRoutine = null;
        }

        if (fireCanonManager != null)
        {
            fireCanonManager.StopFlightTimer();
            fireCanonManager.ClearActiveCannonBall(this);
        }
    }

    void FadeCannonBallCanvas(float targetAlpha)
    {
        if (cannonBallCanvasGroup == null)
        {
            return;
        }

        if (cannonBallCanvasFadeRoutine != null)
        {
            StopCoroutine(cannonBallCanvasFadeRoutine);
            cannonBallCanvasFadeRoutine = null;
        }

        cannonBallCanvasFadeRoutine =
            StartCoroutine(FadeCannonBallCanvasRoutine(targetAlpha));
    }

    IEnumerator FadeCannonBallCanvasRoutine(float targetAlpha)
    {
        float startAlpha = cannonBallCanvasGroup.alpha;
        float duration = Mathf.Max(0.01f, cannonBallCanvasFadeDuration);
        float elapsed = 0f;

        cannonBallCanvasGroup.gameObject.SetActive(true);
        cannonBallCanvasGroup.interactable = false;
        cannonBallCanvasGroup.blocksRaycasts = false;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            cannonBallCanvasGroup.alpha =
                Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        cannonBallCanvasGroup.alpha = targetAlpha;
        bool visible = targetAlpha > 0.95f;
        cannonBallCanvasGroup.interactable = visible;
        cannonBallCanvasGroup.blocksRaycasts = visible;

        if (!visible)
        {
            cannonBallCanvasGroup.gameObject.SetActive(false);
        }

        cannonBallCanvasFadeRoutine = null;
    }

    Vector3 GetFxSpawnPosition(Vector3 basePosition)
    {
        return basePosition + fxSpawnOffset;
    }
}
