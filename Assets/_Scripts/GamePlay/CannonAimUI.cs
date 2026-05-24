using UnityEngine;

public class CannonAimUI : MonoBehaviour
{
    [Header("UI")]
    public RectTransform aimImage;

    [Header("Cannon")]
    public Transform cannonPivot;

    [Header("Angle Limits")]
    public float minAngle = -180f;

    public float maxAngle = 0f;

    [Header("Debug")]
    [SerializeField]
    float targetAngle;

    Quaternion initialRotation;

    bool isDragging;
    bool dragInitialized;

    Vector2 lastDirection;

    [HideInInspector]
    public bool inputBlocked;

    // ====================================
    // UNITY
    // ====================================

    void Start()
    {
        // GUARDAR ROTACIÓN INICIAL
        initialRotation =
            cannonPivot.localRotation;

        InitializeAngle();
    }

    void Update()
    {
        // ====================================
        // INPUT BLOCKED
        // ====================================

        if (
            inputBlocked
            || !aimImage.gameObject.activeInHierarchy
        )
        {
            isDragging = false;

            return;
        }

        HandleMouseInput();

        HandleTouchInput();
    }

    // ====================================
    // INITIALIZE
    // ====================================

    void InitializeAngle()
    {
        // UI EN 90°
        // = CAÑÓN EN 0°

        targetAngle = 0f;

        UpdateUIRotation();
    }

    // ====================================
    // TOUCH INPUT
    // ====================================

    void HandleTouchInput()
    {
        if (Input.touchCount <= 0)
            return;

        Touch touch =
            Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:

                StartDrag(touch.position);

                break;

            case TouchPhase.Moved:
            case TouchPhase.Stationary:

                if (isDragging)
                {
                    UpdateAim(touch.position);
                }

                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:

                isDragging = false;

                break;
        }
    }

    // ====================================
    // MOUSE INPUT
    // ====================================

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartDrag(Input.mousePosition);
        }

        if (
            Input.GetMouseButton(0)
            && isDragging
        )
        {
            UpdateAim(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    // ====================================
    // START DRAG
    // ====================================

    void StartDrag(Vector2 screenPosition)
    {
        // SOLO SI TOCAMOS
        // LA IMAGEN

        if (
            !RectTransformUtility
            .RectangleContainsScreenPoint(
                aimImage,
                screenPosition,
                null
            )
        )
        {
            return;
        }

        Vector2 center =
            aimImage.position;

        Vector2 direction =
            (
                screenPosition -
                center
            ).normalized;

        lastDirection =
            direction;

        isDragging = true;

        dragInitialized = false;
    }

    // ====================================
    // AIM
    // ====================================

    void UpdateAim(
        Vector2 screenPosition
    )
    {
        if (!isDragging)
            return;

        // ====================================
        // IGNORAR PRIMER FRAME
        // ====================================

        if (!dragInitialized)
        {
            Vector2 initialCenter =
                aimImage.position;

            lastDirection =
                (
                    screenPosition -
                    initialCenter
                ).normalized;

            dragInitialized = true;

            return;
        }

        // ====================================
        // CENTRO
        // ====================================

        Vector2 center =
            aimImage.position;

        // ====================================
        // DIRECCIÓN ACTUAL
        // ====================================

        Vector2 currentDirection =
            (
                screenPosition -
                center
            ).normalized;

        // ====================================
        // DELTA ANGLE
        // ====================================

        float deltaAngle =
            Vector2.SignedAngle(
                lastDirection,
                currentDirection
            );

        // ====================================
        // ACUMULAR
        // ====================================

        targetAngle -= deltaAngle;

        // ====================================
        // LIMITAR
        // ====================================

        targetAngle =
            Mathf.Clamp(
                targetAngle,
                minAngle,
                maxAngle
            );

        // ====================================
        // ACTUALIZAR
        // ====================================

        RotateCannon();

        UpdateUIRotation();

        lastDirection =
            currentDirection;
    }

    // ====================================
    // ROTATE CANNON
    // ====================================

    void RotateCannon()
    {
        // RELACIÓN:
        //
        // UI 90°  = Cannon 0°
        // UI 180° = Cannon -90°
        // UI 270° = Cannon -180°

        cannonPivot.localRotation =
            initialRotation *
            Quaternion.Euler(
                targetAngle,
                0,
                0
            );
    }

    // ====================================
    // UI ROTATION
    // ====================================

    void UpdateUIRotation()
    {
        float uiRotation =
            90f - targetAngle;

        aimImage.rotation =
            Quaternion.Euler(
                0,
                0,
                uiRotation
            );
    }

    // ====================================
    // GET CURRENT ANGLE
    // ====================================

    public float GetCurrentAngle()
    {
        return targetAngle;
    }
}