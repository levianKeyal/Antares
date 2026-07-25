using UnityEngine;

public class VirtualCameraRandomMover : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4.25f;
    public float acceleration = 7.5f;
    public float rotationSpeed = 1.1f;
    public float directionSmoothTime = 1.35f;
    public float rotationSmoothTime = 1.15f;

    [Header("Route")]
    public float newRouteEverySeconds = 4.5f;
    public float destinationReachDistance = 2.25f;
    [Range(0f, 1f)]
    public float areaRadiusPadding = 0.85f;
    [Range(0f, 1f)]
    public float routeForwardBias = 0.82f;
    public float routeMinDistance = 4f;
    public float routeMaxDistance = 10f;
    public float routeTurnAngle = 75f;

    [Header("Area")]
    public MovementArea movementArea;

    [Header("Debug")]
    public bool showRouteGizmos = true;
    public Color routeColor = new Color(0.25f, 0.85f, 1f, 0.9f);

    Vector3 currentDirection;
    Vector3 currentDestination;
    float currentSpeed;
    float routeTimer;
    float destinationReachDistanceSqr;
    bool hasDestination;

    void Start()
    {
        destinationReachDistanceSqr =
            destinationReachDistance * destinationReachDistance;

        currentDirection = transform.forward;
        currentDirection.y = 0f;

        if (currentDirection.sqrMagnitude < 0.0001f)
        {
            currentDirection = Vector3.forward;
        }

        currentDirection.Normalize();
        PickNewDestination();
    }

    void Update()
    {
        if (movementArea == null)
        {
            return;
        }

        KeepInsideArea();
        UpdateRoute();
        UpdateMovement();
    }

    void UpdateRoute()
    {
        routeTimer -= Time.deltaTime;

        if (!hasDestination || routeTimer <= 0f)
        {
            PickNewDestination();
            return;
        }

        Vector3 toDestination =
            currentDestination - transform.position;
        toDestination.y = 0f;

        if (toDestination.sqrMagnitude <= destinationReachDistanceSqr)
        {
            PickNewDestination();
        }
    }

    void UpdateMovement()
    {
        if (!hasDestination)
        {
            return;
        }

        Vector3 toDestination = currentDestination - transform.position;
        toDestination.y = 0f;

        if (toDestination.sqrMagnitude < 0.0001f)
        {
            PickNewDestination();
            return;
        }

        Vector3 targetDirection = toDestination.normalized;
        float directionBlend = 1f - Mathf.Exp(-Mathf.Max(0.01f, directionSmoothTime) * Mathf.Max(0.01f, rotationSpeed) * Time.deltaTime);
        currentDirection =
            Vector3.Slerp(
                currentDirection,
                targetDirection,
                directionBlend
            ).normalized;

        currentSpeed =
            Mathf.MoveTowards(
                currentSpeed,
                moveSpeed,
                acceleration * Time.deltaTime
            );

        transform.position +=
            currentDirection *
            currentSpeed *
            Time.deltaTime;

        if (currentDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(currentDirection, Vector3.up);

            float rotationBlend = 1f - Mathf.Exp(-Mathf.Max(0.01f, rotationSmoothTime) * Mathf.Max(0.01f, rotationSpeed) * Time.deltaTime);
            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationBlend
                );
        }
    }

    void PickNewDestination()
    {
        if (movementArea == null)
        {
            hasDestination = false;
            return;
        }

        Vector3 center = movementArea.transform.position;
        float radius = Mathf.Max(0.1f, movementArea.radius * areaRadiusPadding);
        float minDistance = Mathf.Clamp(routeMinDistance, 0.1f, radius);
        float maxDistance = Mathf.Clamp(routeMaxDistance, minDistance, radius);

        Vector3 forward = currentDirection;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = transform.forward;
            forward.y = 0f;
        }

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();

        float angleOffset = Random.Range(-routeTurnAngle, routeTurnAngle);
        Quaternion turn = Quaternion.AngleAxis(angleOffset, Vector3.up);
        Vector3 randomDirection = (turn * forward).normalized;
        Vector3 biasedDirection = Vector3.Slerp(
            randomDirection,
            forward,
            routeForwardBias
        ).normalized;

        float routeDistance = Random.Range(minDistance, maxDistance);
        Vector3 candidatePosition =
            transform.position + biasedDirection * routeDistance;
        candidatePosition.y = transform.position.y;

        if (!movementArea.IsInsideArea(candidatePosition))
        {
            candidatePosition = movementArea.GetClosestPoint(candidatePosition);
        }

        candidatePosition.y = transform.position.y;
        currentDestination = candidatePosition;

        Vector3 toDestination = currentDestination - transform.position;
        toDestination.y = 0f;

        if (toDestination.sqrMagnitude > 0.0001f)
        {
            currentDirection = Vector3.Slerp(
                currentDirection,
                toDestination.normalized,
                0.15f
            ).normalized;
        }

        currentSpeed = Mathf.Max(currentSpeed, moveSpeed * 0.35f);
        routeTimer = Mathf.Max(0.1f, newRouteEverySeconds);
        hasDestination = true;
    }

    void OnValidate()
    {
        destinationReachDistanceSqr =
            destinationReachDistance * destinationReachDistance;
    }

    void KeepInsideArea()
    {
        if (movementArea == null)
        {
            return;
        }

        Vector3 closestPoint = movementArea.GetClosestPoint(transform.position);
        closestPoint.y = transform.position.y;

        Vector3 offset = closestPoint - transform.position;
        offset.y = 0f;

        if (offset.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.position = closestPoint;
        currentDestination = movementArea.GetClosestPoint(currentDestination);
        currentDestination.y = transform.position.y;

        Vector3 backToCenter = movementArea.transform.position - transform.position;
        backToCenter.y = 0f;

        if (backToCenter.sqrMagnitude > 0.0001f)
        {
            currentDirection = Vector3.Slerp(
                currentDirection,
                backToCenter.normalized,
                0.25f
            ).normalized;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showRouteGizmos || movementArea == null)
        {
            return;
        }

        Gizmos.color = routeColor;
        Gizmos.DrawWireSphere(currentDestination, 0.35f);
    }
}
