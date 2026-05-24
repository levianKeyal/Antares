using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoatMover : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float acceleration = 20f;
    public float rotationSpeed = 2f;

    [Header("Area")]
    public MovementArea movementArea;

    [Header("Avoidance")]
    public float avoidanceRadius = 5f;
    public float avoidanceStrength = 3f;
    public LayerMask avoidanceLayers;

    Rigidbody rb;

    float currentSpeed;

    // DIRECCIÓN ACTUAL SUAVIZADA
    Vector3 currentDirection;

    // DIRECCIÓN OBJETIVO
    Vector3 desiredDirection;

    // DIRECCIÓN GUARDADA
    Vector3 savedDirection;

    // STATES
    bool isStopped;
    bool isReturningToPath;

    // CACHE AVOIDANCE
    Collider[] avoidanceHits =
        new Collider[16];

    // ====================================
    // UNITY
    // ====================================

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        ChooseRandomDirection();

        currentDirection = desiredDirection;
    }

    void FixedUpdate()
    {
        if (isStopped)
        {
            rb.linearVelocity = Vector3.zero;

            return;
        }

        UpdateDirection();

        MoveBoat();

        KeepInsideArea();
    }

    // ====================================
    // DIRECTION
    // ====================================

    void UpdateDirection()
    {
        // =========================
        // RETURN TO SAVED PATH
        // =========================

        if (isReturningToPath)
        {
            desiredDirection =
                Vector3.Slerp(
                    desiredDirection,
                    savedDirection,
                    0.5f * Time.fixedDeltaTime
                ).normalized;

            float angle =
                Vector3.Angle(
                    desiredDirection,
                    savedDirection
                );

            if (angle < 1f)
            {
                desiredDirection =
                    savedDirection;

                isReturningToPath = false;
            }
        }

        // =========================
        // SUAVIZAR DIRECCIÓN
        // =========================

        currentDirection =
            Vector3.Slerp(
                currentDirection,
                desiredDirection,
                rotationSpeed *
                Time.fixedDeltaTime
            ).normalized;

        // =========================
        // AVOIDANCE
        // =========================

        Vector3 avoidance =
            GetAvoidanceDirection();

        if (avoidance != Vector3.zero)
        {
            currentDirection +=
                avoidance *
                avoidanceStrength;

            currentDirection.Normalize();
        }
    }

    // ====================================
    // MOVEMENT
    // ====================================

    void MoveBoat()
    {
        Quaternion targetRotation =
            Quaternion.LookRotation(currentDirection);

        Quaternion smoothRotation =
            Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                rotationSpeed *
                Time.fixedDeltaTime
            );

        rb.MoveRotation(smoothRotation);

        // =========================
        // ACCELERATION
        // =========================

        currentSpeed =
            Mathf.MoveTowards(
                currentSpeed,
                moveSpeed,
                acceleration *
                Time.fixedDeltaTime
            );

        // =========================
        // VELOCITY
        // =========================

        Vector3 velocity =
            smoothRotation *
            Vector3.forward *
            currentSpeed;

        velocity.y = rb.linearVelocity.y;

        rb.linearVelocity = velocity;
    }

    // ====================================
    // AREA LIMIT
    // ====================================

    void KeepInsideArea()
    {
        if (movementArea == null)
            return;

        Vector3 center =
            movementArea.transform.position;

        Vector3 flatPosition =
            transform.position;

        flatPosition.y = center.y;

        float distance =
            Vector3.Distance(
                flatPosition,
                center
            );

        // FUERA DEL ÁREA
        if (distance >= movementArea.radius)
        {
            Vector3 directionToCenter =
                (center - transform.position)
                .normalized;

            directionToCenter.y = 0;

            desiredDirection =
                directionToCenter;
        }
    }

    // ====================================
    // AVOIDANCE
    // ====================================

    Vector3 GetAvoidanceDirection()
    {
        int hitCount =
            Physics.OverlapSphereNonAlloc(
                transform.position,
                avoidanceRadius,
                avoidanceHits,
                avoidanceLayers
            );

        Vector3 avoidance =
            Vector3.zero;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit =
                avoidanceHits[i];

            // IGNORAR A SÍ MISMO
            if (hit.transform == transform)
                continue;

            Vector3 away =
                transform.position -
                hit.transform.position;

            away.y = 0;

            float strength =
                1f -
                (away.magnitude /
                 avoidanceRadius);

            avoidance +=
                away.normalized *
                strength;
        }

        return avoidance.normalized;
    }

    // ====================================
    // RANDOM DIRECTION
    // ====================================

    void ChooseRandomDirection()
    {
        Vector2 random =
            Random.insideUnitCircle.normalized;

        desiredDirection =
            new Vector3(
                random.x,
                0,
                random.y
            );
    }

    // ====================================
    // STOP MOVEMENT
    // ====================================

    public void StopMovementImmediately()
    {
        // GUARDAR DIRECCIÓN
        savedDirection = desiredDirection;

        isStopped = true;

        currentSpeed = 0;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // IMPORTANTE PARA MOBILE
        rb.isKinematic = true;
    }

    // ====================================
    // RESUME MOVEMENT
    // ====================================

    public void ResumeMovement()
    {
        rb.isKinematic = false;

        isStopped = false;

        // CONTINUAR DESDE
        // LA DIRECCIÓN ACTUAL
        currentDirection =
            transform.forward.normalized;

        desiredDirection =
            currentDirection;

        // REGRESAR SUAVEMENTE
        // A LA DIRECCIÓN ORIGINAL
        isReturningToPath = true;
    }

    // ====================================
    // DEBUG GIZMOS
    // ====================================

    void OnDrawGizmosSelected()
    {
        // DIRECCIÓN ACTUAL
        Gizmos.color = Color.green;

        Gizmos.DrawLine(
            transform.position,
            transform.position +
            currentDirection * 3f
        );

        // DIRECCIÓN OBJETIVO
        Gizmos.color = Color.blue;

        Gizmos.DrawLine(
            transform.position,
            transform.position +
            desiredDirection * 3f
        );

        // AVOIDANCE
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            avoidanceRadius
        );
    }
}