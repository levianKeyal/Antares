using Fungus;
using UnityEngine;

public class CannonAimUI : MonoBehaviour
{
    public FormulaSustitution formulaSustitution;
    public System.Action<float> onAngleChanged;
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
        formulaSustitution=GetComponent<FormulaSustitution>();  
        // GUARDAR ROTACIÃ“N INICIAL
        initialRotation =
            cannonPivot.localRotation;

        InitializeAngle();
    }

    void Update()
    {
        // ====================================
        // CINEMATIC PAUSE
        // ====================================

        if (GameSettings.Instance.cinematicPause)
        {
            isDragging = false;

            return;
        }

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
        // UI EN 90Â°
        // = CAÃ‘Ã“N EN 0Â°

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
        // DIRECCIÃ“N ACTUAL
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

        formulaSustitution.UpdateFormulaValues();
        onAngleChanged?.Invoke(Mathf.Abs(targetAngle));
    }

    // ====================================
    // ROTATE CANNON
    // ====================================

    void RotateCannon()
    {
        // RELACIÃ“N:
        //
        // UI 90Â°  = Cannon 0Â°
        // UI 180Â° = Cannon -90Â°
        // UI 270Â° = Cannon -180Â°

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
    // PROGRAMMATIC ANGLE
    // ====================================

    public void SetCurrentAngle(float angle)
    {
        targetAngle =
            Mathf.Clamp(
                angle,
                minAngle,
                maxAngle
            );

        RotateCannon();
        UpdateUIRotation();

        if (formulaSustitution != null)
        {
            formulaSustitution.UpdateFormulaValues();
        }

        onAngleChanged?.Invoke(Mathf.Abs(targetAngle));
    }

    // ====================================
    // GET CURRENT ANGLE
    // ====================================

    public float GetCurrentAngle()
    {
        return targetAngle;
    }
}


