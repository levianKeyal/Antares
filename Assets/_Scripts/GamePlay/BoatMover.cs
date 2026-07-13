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
    public float avoidanceRadius = 100f;
    private const float AvoidanceStrength = 15f;
    private const float AvoidanceHoldTime = 0.75f;
    private const float PursuitDistance = 60f;
    private const float PursuitRepathInterval = 0.15f;
    private const float PursuitTurnBias = 15f;
    private const float PursuitLeadSpeed = 2.25f;
    private const float PursuitCloseSpeedBuffer = 0f;
    public float escapeAccelerationMultiplier = 3.5f;
    public float escapeSpeedBurstBonus = 1.75f;
    public float escapeSpeedHoldSeconds = 2.5f;
    public float escapeSpeedFollowBuffer = 0.15f;
    public float escapeSpeedMinimum = 6f;
    private const float EscapeSpeedBuffer = 1.0f;
    private static readonly LayerMask AvoidanceLayers = ~0;
    private const bool AvoidPlayer = true;
    private const bool AvoidOtherBoats = true;

    [Header("Debug")]
    public bool showAvoidanceDebug = true;
    public Color avoidanceDebugColor = new Color(1f, 0.85f, 0.15f, 0.9f);

    Rigidbody rb;

    float currentSpeed;

    Vector3 currentDirection;
    Vector3 desiredDirection;
    Vector3 savedDirection;

    bool isStopped;
    bool isReturningToPath;
    bool onPursuit;
    bool isAvoiding;

    float pursuitTimer;
    float avoidanceTimer;

    Vector3 activePursuitDirection;
    Vector3 activeAvoidanceDirection;

    Transform playerTarget;
    PlayerMovement playerMovement;
    Rigidbody playerRigidbody;

    float escapeSpeedLockTimer;
    float escapeSpeedLockedValue;
    bool escapeSpeedLockActive;

    Collider[] avoidanceHits = new Collider[16];

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

    Transform GetPlayerTarget()
    {
        if (playerTarget != null)
            return playerTarget;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
            return null;

        playerTarget = playerObject.transform;
        return playerTarget;
    }

    bool TryGetPlayerPosition(out Vector3 playerPosition)
    {
        playerPosition = Vector3.zero;

        Transform player = GetPlayerTarget();
        if (player == null)
            return false;

        playerPosition = player.position;
        playerPosition.y = 0f;
        return true;
    }

    PlayerMovement GetPlayerMovement()
    {
        if (playerMovement != null)
            return playerMovement;

        Transform player = GetPlayerTarget();
        if (player == null)
            return null;

        playerMovement = player.GetComponent<PlayerMovement>();
        return playerMovement;
    }

    Rigidbody GetPlayerRigidbody()
    {
        if (playerRigidbody != null)
            return playerRigidbody;

        Transform player = GetPlayerTarget();
        if (player == null)
            return null;

        playerRigidbody = player.GetComponent<Rigidbody>();
        return playerRigidbody;
    }

    float GetPlayerSpeed()
    {
        Rigidbody playerBody = GetPlayerRigidbody();
        if (playerBody != null)
        {
            Vector3 planarVelocity = playerBody.linearVelocity;
            planarVelocity.y = 0f;
            return planarVelocity.magnitude;
        }

        PlayerMovement movement = GetPlayerMovement();
        if (movement != null)
            return movement.moveSpeed;

        return moveSpeed;
    }

    void LockEscapeSpeed(float speed)
    {
        escapeSpeedLockedValue = Mathf.Max(speed, 0.1f);
        escapeSpeedLockTimer = escapeSpeedHoldSeconds;
        escapeSpeedLockActive = true;
    }

    void ClearEscapeSpeedLock()
    {
        escapeSpeedLockActive = false;
        escapeSpeedLockTimer = 0f;
        escapeSpeedLockedValue = 0f;
    }

    float GetDynamicEscapeSpeed(float playerSpeed)
    {
        return Mathf.Max(Mathf.Max(playerSpeed + escapeSpeedFollowBuffer, escapeSpeedMinimum), 0.1f);
    }

    float GetLockedEscapeSpeed(float playerSpeed)
    {
        return Mathf.Max(Mathf.Max(playerSpeed + escapeSpeedBurstBonus, escapeSpeedMinimum + escapeSpeedBurstBonus), 0.1f);
    }

    void StartPursuit(Vector3 playerPosition, bool lockEscapeSpeed = false)
    {
        Vector3 away = transform.position - playerPosition;
        away.y = 0f;

        if (away.sqrMagnitude < 0.0001f)
        {
            away = transform.forward;
            away.y = 0f;
        }

        Vector3 forward = desiredDirection;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = transform.forward;
            forward.y = 0f;
        }

        Vector3 escape = away.normalized;
        Vector3 lateral = Vector3.Cross(Vector3.up, escape).normalized;

        float sideSign = Mathf.Sign(Vector3.Dot(lateral, forward));
        if (Mathf.Approximately(sideSign, 0f))
            sideSign = 1f;

        lateral *= sideSign;

        Vector3 route = (escape + lateral * PursuitTurnBias).normalized;

        if (Vector3.Dot(route, escape) < 0.25f)
            route = escape;

        activePursuitDirection = route;
        pursuitTimer = PursuitRepathInterval;

        if (lockEscapeSpeed)
            LockEscapeSpeed(GetLockedEscapeSpeed(GetPlayerSpeed()));

        onPursuit = true;
        isAvoiding = false;
        activeAvoidanceDirection = Vector3.zero;
    }

    void StartAvoidance(Vector3 threatPosition)
    {
        Vector3 away = transform.position - threatPosition;
        away.y = 0f;

        if (away.sqrMagnitude < 0.0001f)
        {
            away = transform.forward;
            away.y = 0f;
        }

        Vector3 forward = desiredDirection;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = transform.forward;
            forward.y = 0f;
        }

        Vector3 lateral = Vector3.Cross(Vector3.up, away).normalized;

        float sideSign = Mathf.Sign(Vector3.Dot(lateral, forward));
        if (Mathf.Approximately(sideSign, 0f))
            sideSign = 1f;

        lateral *= sideSign;

        Vector3 route = (forward.normalized + lateral * AvoidanceStrength).normalized;

        if (Vector3.Dot(route, forward.normalized) < 0f)
            route = -route;

        activeAvoidanceDirection = route;
        onPursuit = false;
        activePursuitDirection = Vector3.zero;
        avoidanceTimer = AvoidanceHoldTime;
        isAvoiding = true;
        LockEscapeSpeed(GetLockedEscapeSpeed(GetPlayerSpeed()));
    }

    void UpdateDirection()
    {
        if (isReturningToPath)
        {
            desiredDirection = Vector3.Slerp(desiredDirection, savedDirection, 0.5f * Time.fixedDeltaTime).normalized;

            float angle = Vector3.Angle(desiredDirection, savedDirection);
            if (angle < 1f)
            {
                desiredDirection = savedDirection;
                isReturningToPath = false;
            }
        }

        bool hasPlayer = TryGetPlayerPosition(out Vector3 playerPosition);

        if (onPursuit)
        {
            pursuitTimer -= Time.fixedDeltaTime;

            if (!hasPlayer)
            {
                onPursuit = false;
                activePursuitDirection = Vector3.zero;
            }
            else
            {
                float distanceToPlayer = Vector3.Distance(transform.position, playerPosition);

                if (distanceToPlayer > PursuitDistance * 1.15f)
                {
                    onPursuit = false;
                    activePursuitDirection = Vector3.zero;
                }
                else if (pursuitTimer <= 0f)
                {
                    StartPursuit(playerPosition);
                }
            }
        }
        else if (TryGetAvoidanceTargetPosition(out Vector3 avoidanceTarget) && !isAvoiding)
        {
            StartAvoidance(avoidanceTarget);
        }

        if (isAvoiding)
        {
            avoidanceTimer -= Time.fixedDeltaTime;

            if (avoidanceTimer <= 0f)
            {
                if (hasPlayer && Vector3.Distance(transform.position, playerPosition) < PursuitDistance)
                {
                    StartPursuit(playerPosition, true);
                }
                else
                {
                    isAvoiding = false;
                    activeAvoidanceDirection = Vector3.zero;
                }
            }
        }

        Vector3 targetDirection =
            onPursuit && activePursuitDirection != Vector3.zero
                ? activePursuitDirection
                : isAvoiding && activeAvoidanceDirection != Vector3.zero
                    ? activeAvoidanceDirection
                    : desiredDirection;

        desiredDirection = Vector3.Slerp(desiredDirection, targetDirection, 0.5f * Time.fixedDeltaTime).normalized;

        currentDirection = Vector3.Slerp(currentDirection, desiredDirection, rotationSpeed * Time.fixedDeltaTime).normalized;

        Vector3 avoidance = GetAvoidanceDirection();
        if (avoidance != Vector3.zero)
        {
            currentDirection += avoidance * 0.15f;
            currentDirection.Normalize();
        }
    }

    void MoveBoat()
    {
        Quaternion targetRotation = Quaternion.LookRotation(currentDirection);

        Quaternion smoothRotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(smoothRotation);

        float targetSpeed = moveSpeed;

        if (onPursuit || isAvoiding)
        {
            if (escapeSpeedLockActive)
            {
                escapeSpeedLockTimer -= Time.fixedDeltaTime;

                if (escapeSpeedLockTimer <= 0f)
                    ClearEscapeSpeedLock();
            }

            float playerSpeed = GetPlayerSpeed();
            targetSpeed = escapeSpeedLockActive
                ? escapeSpeedLockedValue
                : GetDynamicEscapeSpeed(playerSpeed);
        }
        else
        {
            ClearEscapeSpeedLock();
        }

        float accelerationValue = acceleration;

        if (onPursuit || isAvoiding)
            accelerationValue *= escapeAccelerationMultiplier;

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelerationValue * Time.fixedDeltaTime);

        Vector3 velocity = smoothRotation * Vector3.forward * currentSpeed;
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;
    }

    void KeepInsideArea()
    {
        if (movementArea == null)
            return;

        Vector3 center = movementArea.transform.position;
        Vector3 flatPosition = transform.position;
        flatPosition.y = center.y;

        float distance = Vector3.Distance(flatPosition, center);

        if (distance >= movementArea.radius)
        {
            Vector3 directionToCenter = (center - transform.position).normalized;
            directionToCenter.y = 0f;
            desiredDirection = directionToCenter;
        }
    }

    Vector3 GetAvoidanceDirection()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, avoidanceRadius, avoidanceHits, AvoidanceLayers);
        Vector3 avoidance = Vector3.zero;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = avoidanceHits[i];

            if (hit == null || hit.transform == transform)
                continue;

            bool shouldAvoid = false;
            Vector3 candidatePosition = hit.transform.position;
            candidatePosition.y = 0f;

            if (AvoidPlayer && hit.CompareTag("Player"))
            {
                shouldAvoid = true;
            }
            else if (AvoidOtherBoats)
            {
                BoatMover otherBoat = hit.GetComponentInParent<BoatMover>();
                if (otherBoat != null && otherBoat != this)
                    shouldAvoid = true;
            }

            if (!shouldAvoid)
                continue;

            Vector3 away = transform.position - candidatePosition;
            away.y = 0f;

            if (away.sqrMagnitude < 0.0001f || away.magnitude > avoidanceRadius)
                continue;

            avoidance += away.normalized;
        }

        return avoidance.sqrMagnitude > 0.0001f ? avoidance.normalized : Vector3.zero;
    }

    bool TryGetAvoidanceTargetPosition(out Vector3 targetPosition)
    {
        targetPosition = Vector3.zero;

        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, avoidanceRadius, avoidanceHits, AvoidanceLayers);
        float closestDistance = float.MaxValue;
        bool foundTarget = false;
        Vector3 selfPosition = transform.position;
        selfPosition.y = 0f;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = avoidanceHits[i];

            if (hit == null || hit.transform == transform)
                continue;

            bool shouldAvoid = false;
            Vector3 candidatePosition = hit.transform.position;
            candidatePosition.y = 0f;

            if (AvoidPlayer && hit.CompareTag("Player"))
            {
                shouldAvoid = true;
            }
            else if (AvoidOtherBoats)
            {
                BoatMover otherBoat = hit.GetComponentInParent<BoatMover>();
                if (otherBoat != null && otherBoat != this)
                    shouldAvoid = true;
            }

            if (!shouldAvoid)
                continue;

            Vector3 away = selfPosition - candidatePosition;
            away.y = 0f;

            float distance = away.magnitude;
            if (distance < 0.0001f || distance > avoidanceRadius)
                continue;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                targetPosition = candidatePosition;
                foundTarget = true;
            }
        }

        return foundTarget;
    }

    public void StopMovementImmediately()
    {
        savedDirection = desiredDirection;
        isStopped = true;
        currentSpeed = 0f;
        ClearEscapeSpeedLock();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void ResumeMovement()
    {
        isStopped = false;
        isReturningToPath = true;
        onPursuit = false;
        isAvoiding = false;
        activePursuitDirection = Vector3.zero;
        activeAvoidanceDirection = Vector3.zero;
        currentDirection = transform.forward.normalized;
        desiredDirection = currentDirection;
        ClearEscapeSpeedLock();
    }

    void ChooseRandomDirection()
    {
        Vector2 random = Random.insideUnitCircle.normalized;
        desiredDirection = new Vector3(random.x, 0f, random.y);

        if (desiredDirection.sqrMagnitude < 0.0001f)
            desiredDirection = transform.forward;
    }

    void OnDrawGizmosSelected()
    {
        if (!showAvoidanceDebug)
            return;

        Gizmos.color = avoidanceDebugColor;
        Gizmos.DrawWireSphere(transform.position, avoidanceRadius);
    }
}
