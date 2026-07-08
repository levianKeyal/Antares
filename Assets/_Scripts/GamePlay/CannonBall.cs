using Fungus;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CannonBall : MonoBehaviour
{
    [Header("Debug")]
    public Vector3 velocity;

    public float gravity;

    Rigidbody rb;

    Vector3 savedVelocity;

    bool isPaused;

    [Header("Impact FX")]
    public GameObject waterImpactPrefab;

    public GameObject hitImpactPrefab;

    public GameObject waterSplashSoundFX;

    // ====================================
    // UNITY
    // ====================================

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
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

        gravity = customGravity;

        rb.useGravity = false;

        rb.linearVelocity = velocity;
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

                rb.linearVelocity =
                    Vector3.zero;

                isPaused = true;
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

            rb.linearVelocity =
                velocity;

            isPaused = false;
        }

        // ====================================
        // GRAVEDAD MANUAL
        // ====================================

        velocity +=
            Vector3.down *
            gravity *
            Time.fixedDeltaTime;

        rb.linearVelocity =
            velocity;
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
}