using Cinemachine;
using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CannonBall : MonoBehaviour
{
    [Header("Debug")]
    public Vector3 velocity;
    public float totalVelocity;

    public float gravity;

    [Header("Velocity UI")]
    public TMP_Text velocityXText;
    public TMP_Text velocityYText;

    Rigidbody rb;

    Vector3 savedVelocity;

    bool isPaused;

    [Header("Impact FX")]
    public GameObject waterImpactPrefab;

    public GameObject hitImpactPrefab;

    public GameObject waterSplashSoundFX;

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
        velocity = startVelocity;
        totalVelocity = velocity.magnitude;

        gravity = customGravity;

        rb.useGravity = false;

        rb.linearVelocity = velocity;
        UpdateVelocityText();
    }

    public void SetFireCanonManager(FireCanonManager manager)
    {
        fireCanonManager = manager;
    }

    // ====================================
    // PHYSICS
    // ====================================

    void FixedUpdate()
    {
        // ====================================
        // CINEMATIC PAUSE
        // ====================================

        if (GameSettings.Instance.cinematicPause)
        {
            // PAUSAR SOLO UNA VEZ
            if (!isPaused)
            {
                savedVelocity =
                    velocity;
                totalVelocity = savedVelocity.magnitude;

                rb.linearVelocity =
                    Vector3.zero;

                isPaused = true;
                UpdateVelocityText();
            }

            return;
        }

        // ====================================
        // RESUME
        // ====================================

        if (isPaused)
        {
            velocity =
                savedVelocity;
            totalVelocity = velocity.magnitude;

            rb.linearVelocity =
                velocity;

            isPaused = false;
            UpdateVelocityText();
        }

        // ====================================
        // GRAVEDAD MANUAL
        // ====================================

        velocity +=
            Vector3.down *
            gravity *
            Time.fixedDeltaTime;

        totalVelocity = velocity.magnitude;

        rb.linearVelocity =
            velocity;

        UpdateVelocityText();
    }

    void UpdateVelocityText()
    {
        if (velocityXText != null)
        {
            velocityXText.text = velocity.x.ToString("F2");
        }

        if (velocityYText != null)
        {
            velocityYText.text = velocity.y.ToString("F2");
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
            // WATER FX
            if (waterImpactPrefab != null)
            {
                Instantiate(
                    waterImpactPrefab,
                    hitPosition,
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
            // HIT FX
            if (hitImpactPrefab != null)
            {
                Instantiate(
                    hitImpactPrefab,
                    hitPosition,
                    Quaternion.identity
                );
            }

            // CALL METHOD
            other.SendMessage(
                "OnCannonBallHit",
                SendMessageOptions
                    .DontRequireReceiver
            );

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
            // HIT FX
            if (hitImpactPrefab != null)
            {
                Instantiate(
                    hitImpactPrefab,
                    hitPosition,
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
}
