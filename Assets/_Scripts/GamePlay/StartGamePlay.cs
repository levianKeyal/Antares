using Cinemachine;
using Fungus;
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
    public BoatMover currentBoatMover;

    [Header("Battle Cameras")]
    public CinemachineVirtualCamera playerCamera;
    public CinemachineVirtualCamera battleCamera;

    public float paddingMultiplier;

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

    Transform battleCameraFocusPoint;
    float battleCameraDistance;

    Vector3 battleObjectiveCenter;
    Camera mainCamera;

    [HideInInspector]
    public bool encounterActive;

    bool IsPointerOverUI()
    {
        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null)
        {
            return false;
        }

        // TOUCH
        if (Input.touchCount > 0)
        {
            return eventSystem
                .IsPointerOverGameObject(
                    Input.GetTouch(0).fingerId
                );
        }

        // MOUSE
        return eventSystem
            .IsPointerOverGameObject();
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        RefreshSceneReferences();

        if (cannonCanvas != null)
        {
            cannonCanvas.SetActive(false);
        }

        // CREATE DEBUG POINTS ONCE
        CreateDebugPoints();
        CreateBattleCameraFocusPoint();

        SetCameraPriorities();
    }
    void RefreshSceneReferences()
    {
        mainCamera = Camera.main;

        player = GameObject.FindWithTag("Player");
        playerMovement =
            player != null
                ? player.GetComponent<PlayerMovement>()
                : null;

        virtualJoystick = FindFirstObjectByType<VirtualJoystick>();

        GameObject cannonCanvasObject = GameObject.Find("Cannon Canvas");
        if (cannonCanvasObject != null)
        {
            cannonCanvas = cannonCanvasObject;
        }

        RebindBattleCameras();
    }

    void RefreshMissingSceneReferences()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (player == null)
        {
            player = GameObject.FindWithTag("Player");
        }

        if (playerMovement == null && player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
        }

        if (virtualJoystick == null)
        {
            virtualJoystick = FindFirstObjectByType<VirtualJoystick>();
        }

        if (playerCamera == null || battleCamera == null)
        {
            RebindBattleCameras();
        }
    }

    void RebindBattleCameras()
    {
        CinemachineVirtualCamera[] cameras =
            FindObjectsByType<CinemachineVirtualCamera>(FindObjectsSortMode.None);

        CinemachineVirtualCamera bestPlayerCamera = null;
        CinemachineVirtualCamera bestBattleCamera = null;
        int bestPlayerPriority = int.MinValue;
        int bestBattlePriority = int.MinValue;

        foreach (CinemachineVirtualCamera cam in cameras)
        {
            if (cam == null)
            {
                continue;
            }

            bool isBattleCamera = cam.gameObject.name == "Battle Camera";

            if (isBattleCamera)
            {
                if (cam.m_Priority >= bestBattlePriority)
                {
                    bestBattlePriority = cam.m_Priority;
                    bestBattleCamera = cam;
                }
            }
            else if (cam.m_Priority >= bestPlayerPriority)
            {
                bestPlayerPriority = cam.m_Priority;
                bestPlayerCamera = cam;
            }
        }

        if (bestPlayerCamera != null)
        {
            playerCamera = bestPlayerCamera;
        }

        if (bestBattleCamera != null)
        {
            battleCamera = bestBattleCamera;
        }
    }

    void SetCameraPriorities()
    {
        if (playerCamera != null)
        {
            playerCamera.m_Priority = 1;
        }

        if (battleCamera != null)
        {
            battleCamera.m_Priority = 0;
        }
    }

    void ResetEncounterState()
    {
        rotatePlayer = false;
        rotateObjective = false;
        encounterActive = false;
        currentObjective = null;
        currentBoatMover = null;
        showCircle = false;

        SetDebugPointsVisible(false);

        if (virtualJoystick != null)
        {
            virtualJoystick.inputBlocked = false;
        }

        if (GameSettings.Instance != null)
        {
            GameSettings settings = GameSettings.Instance;
            settings.cinematicPause = false;
            settings.interactionBlocked = false;
        }

        if (cannonCanvas != null)
        {
            cannonCanvas.SetActive(false);
        }
    }
    void Update()
    {
        if (player == null || mainCamera == null || virtualJoystick == null)
        {
            RefreshMissingSceneReferences();
        }

        HandleInput();

        if (encounterActive)
        {
            UpdateRotations();
            UpdateBattleCameraFollow();
        }

    }

    // ====================================
    // INPUT
    // ====================================

    void HandleInput()
    {
        // ====================================
        // CINEMATIC PAUSE
        // ====================================

        GameSettings settings = GameSettings.Instance;

        if (settings == null || settings.cinematicPause)
        {
            return;
        }

        //#if UNITY_EDITOR || UNITY_STANDALONE

        if (Input.GetMouseButtonDown(0))
        {
            if (
                EventSystem.current
                .IsPointerOverGameObject()
            )
            {
                return;
            }

            CheckTap(Input.mousePosition);
        }

//#endif
        /*
        // ====================================
        // MOBILE
        // ====================================

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // ====================================
            // IGNORAR TOUCH SOBRE UI
            // ====================================

            if (
                EventSystem.current
                .IsPointerOverGameObject(
                    touch.fingerId
                )
            )
            {
                return;
            }

            // ====================================
            // TAP REAL
            // ====================================

            if (touch.phase == TouchPhase.Began)
            {
                CheckTap(touch.position);
            }
        }*/
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

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

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
        RefreshSceneReferences();
        CreateDebugPoints();

        if (player == null || target == null)
        {
            return;
        }

        if (encounterActive)
        {
            return;
        }

        BoatMover targetBoatMover =
            target.GetComponentInParent<BoatMover>();

        if (targetBoatMover != null)
        {
            currentBoatMover = targetBoatMover;
            currentObjective = targetBoatMover.gameObject;
        }
        else
        {
            currentObjective = target;
            currentBoatMover =
                currentObjective.GetComponent<BoatMover>();
        }

        if (currentObjective == null)
        {
            return;
        }

        // BLOQUEAR JOYSTICK
        if (virtualJoystick != null)
        {
            virtualJoystick.inputBlocked = true;
        }

        encounterActive = true;

        // STOP PLAYER
        if (playerMovement != null)
        {
            playerMovement.StopMovementImmediately();
        }

        // STOP OBJECTIVE
        if (currentBoatMover != null)
        {
            currentBoatMover.StopMovementImmediately();
            currentBoatMover.BeginBattleFacing(player.transform.position);
        }

        battleObjectiveCenter = GetObjectiveCenter(currentObjective);
        SetupRotations(objectiveLooksAtPlayer, battleObjectiveCenter);

        CalculateCircle(battleObjectiveCenter);

        ActivateBattleCamera(battleObjectiveCenter);

        // ACTIVAR UI DE CAÃƒâ€˜Ãƒâ€œN
        if (cannonCanvas != null)
        {
            cannonCanvas.SetActive(true);
        }
    }

    // ====================================
    // ROTATIONS
    // ====================================

    void SetupRotations(
        bool objectiveLooksAtPlayer,
        Vector3 objectiveCenter
    )
    {
        // PLAYER LOOKS AT OBJECTIVE

        Vector3 playerDirection =
            objectiveCenter -
            player.transform.position;

        playerDirection.y = 0;

        if (playerDirection != Vector3.zero)
        {
            playerTargetRotation =
                Quaternion.LookRotation(playerDirection);

            rotatePlayer = true;
        }

        // OBJECTIVE LOOKS AT PLAYER

        if (objectiveLooksAtPlayer && currentBoatMover == null)
        {
            Vector3 objectiveDirection =
                player.transform.position -
                objectiveCenter;

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

    void CalculateCircle(Vector3 objectiveCenter)
    {
        CreateDebugPoints();
        circleCenter =
            (player.transform.position +
             objectiveCenter) / 2f;

        circleRadius =
            Vector3.Distance(
                player.transform.position,
                objectiveCenter
            ) / 2f;

        Vector3 direction =
            (objectiveCenter -
             player.transform.position)
            .normalized;

        Vector3 rightDirection =
            Vector3.Cross(Vector3.up, direction)
            .normalized;

        Vector3 edgePosition =
            circleCenter +
            rightDirection * circleRadius;

        if (edgePoint != null)
        {
            edgePoint.position = edgePosition;
            edgePoint.gameObject.SetActive(true);
        }

        if (centerPoint != null)
        {
            centerPoint.position = circleCenter;
            centerPoint.gameObject.SetActive(true);
        }

        showCircle = true;
    }

    Vector3 GetObjectiveCenter(GameObject objective)
    {
        if (objective == null)
        {
            return Vector3.zero;
        }

        Collider objectiveCollider = objective.GetComponent<Collider>();
        if (objectiveCollider != null)
        {
            return objectiveCollider.bounds.center;
        }

        Collider childCollider = objective.GetComponentInChildren<Collider>();
        if (childCollider != null)
        {
            return childCollider.bounds.center;
        }

        Renderer objectiveRenderer = objective.GetComponent<Renderer>();
        if (objectiveRenderer != null)
        {
            return objectiveRenderer.bounds.center;
        }

        Renderer childRenderer = objective.GetComponentInChildren<Renderer>();
        if (childRenderer != null)
        {
            return childRenderer.bounds.center;
        }

        return objective.transform.position;
    }

    void CreateDebugPoints()
    {
        if (edgePoint == null)
        {
            GameObject edge = new GameObject("EdgePoint");
            edgePoint = edge.transform;
        }

        if (centerPoint == null)
        {
            GameObject center = new GameObject("CenterPoint");
            centerPoint = center.transform;
        }

        SetDebugPointsVisible(false);
    }

    void SetDebugPointsVisible(bool visible)
    {
        if (edgePoint != null)
        {
            edgePoint.gameObject.SetActive(visible);
        }

        if (centerPoint != null)
        {
            centerPoint.gameObject.SetActive(visible);
        }
    }

    // ====================================
    // ====================================
    void CreateBattleCameraFocusPoint()
    {
        if (battleCameraFocusPoint != null)
        {
            return;
        }

        GameObject focusPoint = new GameObject("BattleCameraFocusPoint");
        battleCameraFocusPoint = focusPoint.transform;
    }

    // CAMERA
    // ====================================

    public void ActivateBattleCamera(Vector3 objectiveCenter)
    {
        if (battleCamera == null || player == null || currentObjective == null)
        {
            return;
        }

        playerCamera.m_Priority = 0;
        battleCamera.m_Priority = 1;

        if (battleCameraFocusPoint == null)
        {
            CreateBattleCameraFocusPoint();
        }

        float radius = circleRadius;

        float fov =
            battleCamera.m_Lens.FieldOfView;

        float distance = 0;

        if (GameSettings.Instance.isPortrait)
        {
            distance =
            (radius /
            Mathf.Sin(fov * Mathf.Deg2Rad / 2f))
            * paddingMultiplier;
        }
        else if (GameSettings.Instance.isLandscape)
        {
            distance =
            (radius /
            Mathf.Sin(fov * Mathf.Deg2Rad / 2f));
        }

        Vector3 direction =
            (objectiveCenter -
             player.transform.position)
            .normalized;

        Vector3 cameraDirection =
            Vector3.Cross(Vector3.up, direction)
            .normalized;

        battleCameraDistance = distance;
        UpdateBattleCameraFollow();
    }

    public void ActivateBattleCamera()
    {
        ActivateBattleCamera(battleObjectiveCenter);
    }

    public void UpdateBattleCameraFollow()
    {
        if (battleCamera == null || player == null || currentObjective == null)
        {
            return;
        }

        Vector3 playerPosition = player.transform.position;
        Vector3 objectiveCenter = battleObjectiveCenter;
        Vector3 midpoint = (playerPosition + objectiveCenter) / 2f;

        Vector3 direction = objectiveCenter - playerPosition;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = battleCamera.transform.forward;
            direction.y = 0f;
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.forward;
        }

        direction.Normalize();

        Vector3 cameraDirection =
            Vector3.Cross(Vector3.up, direction).normalized;

        if (cameraDirection.sqrMagnitude < 0.0001f)
        {
            cameraDirection = battleCamera.transform.right;
        }

        if (battleCameraFocusPoint != null)
        {
            battleCameraFocusPoint.position = midpoint;
            battleCamera.m_LookAt = battleCameraFocusPoint;
        }
        else
        {
            battleCamera.m_LookAt = centerPoint;
        }

        battleCamera.transform.position =
            midpoint +
            cameraDirection * battleCameraDistance;
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
            currentBoatMover = null;
        }

        showCircle = false;


        SetDebugPointsVisible(false);
        playerCamera.m_Priority = 1;
        battleCamera.m_Priority = 0;

        /*
        // DESACTIVAR UI DE CAÃƒâ€˜Ãƒâ€œN
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



