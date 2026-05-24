using Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

public class StartGamePlay : MonoBehaviour
{
    public static StartGamePlay Instance;

    [Header("References")]
    public GameObject player;
    public GameObject currentObjective;

    [Header("Controls")]
    public VirtualJoystick virtualJoystick;

    PlayerMovement playerMovement;
    BoatMover currentBoatMover;

    [Header("Battle Cameras")]
    public CinemachineVirtualCamera playerCamera;
    public CinemachineVirtualCamera battleCamera;

    [Header("Battle UI")]
    public GameObject cannonCanvas;

    [Header("Rotation")]
    public float rotationSpeed = 5f;

    bool rotatePlayer;
    bool rotateObjective;

    Quaternion playerTargetRotation;
    Quaternion objectiveTargetRotation;

    [Header("Circle Debug")]
    public bool showCircle;

    Vector3 circleCenter;
    float circleRadius;

    [Header("Debug Points")]
    public Transform edgePoint;
    public Transform centerPoint;

    Camera mainCamera;

    [HideInInspector]
    public bool encounterActive;

    bool IsPointerOverUI()
    {
        // TOUCH
        if (Input.touchCount > 0)
        {
            return EventSystem.current
                .IsPointerOverGameObject(
                    Input.GetTouch(0).fingerId
                );
        }

        // MOUSE
        return EventSystem.current
            .IsPointerOverGameObject();
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        mainCamera = Camera.main;

        // CACHE PLAYER
        player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            playerMovement =
                player.GetComponent<PlayerMovement>();
        }

        if (cannonCanvas != null)
        {
            cannonCanvas.SetActive(false);
        }

        // CREATE DEBUG POINTS ONCE
        CreateDebugPoints();

        // INITIAL CAMERA
        playerCamera.m_Priority = 1;
        battleCamera.m_Priority = 0;
    }

    void Update()
    {
        HandleInput();

        if (encounterActive)
        {
            UpdateRotations();
        }

    }

    // ====================================
    // INPUT
    // ====================================

    void HandleInput()
    {
#if UNITY_EDITOR || UNITY_STANDALONE

        if (Input.GetMouseButtonDown(0))
        {
            CheckTap(Input.mousePosition);
        }

#endif

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                CheckTap(touch.position);
            }
        }
    }

    void CheckTap(Vector2 screenPosition)
    {
        // ====================================
        // IGNORAR INPUT SOBRE UI
        // ====================================

        if (IsPointerOverUI())
        {
            return;
        }

        // ====================================
        // RAYCAST
        // ====================================

        Ray ray =
            mainCamera.ScreenPointToRay(
                screenPosition
            );

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // ====================================
            // OBJECTIVE TAP
            // ====================================

            ObjectiveTap objectiveTap =
                hit.transform.GetComponent<ObjectiveTap>();

            if (objectiveTap != null)
            {
                objectiveTap.OnObjectiveTapped();
            }
            else
            {
                ClearEncounter();
            }
        }
        else
        {
            ClearEncounter();
        }
    }


    // ====================================
    // START ENCOUNTER
    // ====================================

    public void StartPhase1(
        GameObject target,
        bool objectiveLooksAtPlayer
    )
    {
        // BLOQUEAR JOYSTICK
        if (virtualJoystick != null)
        {
            virtualJoystick.inputBlocked = true;
        }

        encounterActive = true;

        currentObjective = target;

        currentBoatMover =
            currentObjective.GetComponent<BoatMover>();

        // STOP PLAYER
        if (playerMovement != null)
        {
            playerMovement.StopMovementImmediately();
        }

        // STOP OBJECTIVE
        if (currentBoatMover != null)
        {
            currentBoatMover.StopMovementImmediately();
        }

        SetupRotations(objectiveLooksAtPlayer);

        CalculateCircle();

        ActivateBattleCamera();

        // ACTIVAR UI DE CAÑÓN
        if (cannonCanvas != null)
        {
            cannonCanvas.SetActive(true);
        }
    }

    // ====================================
    // ROTATIONS
    // ====================================

    void SetupRotations(bool objectiveLooksAtPlayer)
    {
        // PLAYER LOOKS AT OBJECTIVE

        Vector3 playerDirection =
            currentObjective.transform.position -
            player.transform.position;

        playerDirection.y = 0;

        if (playerDirection != Vector3.zero)
        {
            playerTargetRotation =
                Quaternion.LookRotation(playerDirection);

            rotatePlayer = true;
        }

        // OBJECTIVE LOOKS AT PLAYER

        if (objectiveLooksAtPlayer)
        {
            Vector3 objectiveDirection =
                player.transform.position -
                currentObjective.transform.position;

            objectiveDirection.y = 0;

            if (objectiveDirection != Vector3.zero)
            {
                objectiveTargetRotation =
                    Quaternion.LookRotation(objectiveDirection);

                rotateObjective = true;
            }
        }
    }

    void UpdateRotations()
    {
        RotateTransform(
            player?.transform,
            ref rotatePlayer,
            playerTargetRotation
        );

        RotateTransform(
            currentObjective?.transform,
            ref rotateObjective,
            objectiveTargetRotation
        );
    }

    void RotateTransform(
        Transform target,
        ref bool rotating,
        Quaternion targetRotation
    )
    {
        if (!rotating || target == null)
            return;

        target.rotation =
            Quaternion.Slerp(
                target.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

        float angle =
            Quaternion.Angle(
                target.rotation,
                targetRotation
            );

        if (angle < 0.5f)
        {
            target.rotation = targetRotation;

            rotating = false;
        }
    }

    // ====================================
    // CIRCLE
    // ====================================

    void CalculateCircle()
    {
        circleCenter =
            (player.transform.position +
             currentObjective.transform.position) / 2f;

        circleRadius =
            Vector3.Distance(
                player.transform.position,
                currentObjective.transform.position
            ) / 2f;

        Vector3 direction =
            (currentObjective.transform.position -
             player.transform.position)
            .normalized;

        Vector3 rightDirection =
            Vector3.Cross(Vector3.up, direction)
            .normalized;

        Vector3 edgePosition =
            circleCenter +
            rightDirection * circleRadius;

        edgePoint.position = edgePosition;
        centerPoint.position = circleCenter;

        edgePoint.gameObject.SetActive(true);
        centerPoint.gameObject.SetActive(true);

        showCircle = true;
    }

    void CreateDebugPoints()
    {
        if (edgePoint == null)
        {
            GameObject edge =
                new GameObject("EdgePoint");

            edgePoint = edge.transform;
        }

        if (centerPoint == null)
        {
            GameObject center =
                new GameObject("CenterPoint");

            centerPoint = center.transform;
        }

        edgePoint.gameObject.SetActive(false);
        centerPoint.gameObject.SetActive(false);
    }

    // ====================================
    // CAMERA
    // ====================================

    void ActivateBattleCamera()
    {
        playerCamera.m_Priority = 0;
        battleCamera.m_Priority = 1;

        battleCamera.m_LookAt = centerPoint;

        float radius = circleRadius;

        float fov =
            battleCamera.m_Lens.FieldOfView;

        float distance =
            (radius /
            Mathf.Sin(fov * Mathf.Deg2Rad / 2f))
            * 2f;

        Vector3 direction =
            (currentObjective.transform.position -
             player.transform.position)
            .normalized;

        Vector3 cameraDirection =
            Vector3.Cross(Vector3.up, direction)
            .normalized;

        battleCamera.transform.position =
            circleCenter +
            cameraDirection * distance;
    }

    // ====================================
    // CLEAR ENCOUNTER
    // ====================================

    public void ClearEncounter()
    {
        rotatePlayer = false;
        rotateObjective = false;

        if (currentBoatMover != null)
        {
            currentBoatMover.ResumeMovement();
        }

        showCircle = false;

        edgePoint.gameObject.SetActive(false);
        centerPoint.gameObject.SetActive(false);

        playerCamera.m_Priority = 1;
        battleCamera.m_Priority = 0;

        /*
        // DESACTIVAR UI DE CAÑÓN
        if (cannonCanvas != null)
        {
            cannonCanvas.SetActive(false);
        }*/

        // DESBLOQUEAR JOYSTICK
        if (virtualJoystick != null)
        {
            virtualJoystick.inputBlocked = false;
        }

        encounterActive = false;
    }

    // ====================================
    // GIZMOS
    // ====================================

    void OnDrawGizmos()
    {
        if (!showCircle)
            return;

        Gizmos.color = Color.cyan;

        DrawWireCircle(
            circleCenter,
            circleRadius,
            64
        );
    }

    void DrawWireCircle(
        Vector3 center,
        float radius,
        int segments
    )
    {
        float angleStep =
            360f / segments;

        Vector3 previousPoint =
            center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle =
                Mathf.Deg2Rad *
                angleStep *
                i;

            Vector3 nextPoint =
                center +
                new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );

            Gizmos.DrawLine(
                previousPoint,
                nextPoint
            );

            previousPoint = nextPoint;
        }
    }
}