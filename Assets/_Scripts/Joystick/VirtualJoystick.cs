using UnityEngine;

public class VirtualJoystick : MonoBehaviour
{
    [Header("References")]
    public RectTransform background;
    public RectTransform handle;
    public Canvas canvas;

    [Header("Joystick Settings")]
    public float handleRange = 80f;

    [Header("Screen Margins")]
    public float screenMargin = 120f;

    public Vector2 InputDirection { get; private set; }

    bool isDragging = false;
    int activeFingerId = -1;

    Camera uiCamera;

    void Start()
    {
        uiCamera = canvas.worldCamera;

        // Ocultar joystick al iniciar
        background.gameObject.SetActive(false);
    }

    void Update()
    {
        HandleMouseInput();
        HandleTouchInput();
    }

    // =====================================================
    // TOUCH INPUT
    // =====================================================

    void HandleTouchInput()
    {
        foreach (Touch touch in Input.touches)
        {
            switch (touch.phase)
            {
                case TouchPhase.Began:

                    if (IsInsideSafeArea(touch.position))
                    {
                        activeFingerId = touch.fingerId;
                        isDragging = true;

                        ShowJoystick(touch.position);
                        UpdateJoystick(touch.position);
                    }

                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:

                    if (touch.fingerId == activeFingerId)
                    {
                        UpdateJoystick(touch.position);
                    }

                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:

                    if (touch.fingerId == activeFingerId)
                    {
                        HideJoystick();
                    }

                    break;
            }
        }
    }

    // =====================================================
    // MOUSE INPUT (EDITOR)
    // =====================================================

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsInsideSafeArea(Input.mousePosition))
            {
                isDragging = true;

                ShowJoystick(Input.mousePosition);
                UpdateJoystick(Input.mousePosition);
            }
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            UpdateJoystick(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            HideJoystick();
        }
    }

    // =====================================================
    // SAFE AREA
    // =====================================================

    bool IsInsideSafeArea(Vector2 position)
    {
        // Solo tercio inferior
        bool insideBottomThird =
            position.y <= Screen.height / 3f;

        // Márgenes laterales
        bool insideHorizontalMargins =
            position.x > screenMargin &&
            position.x < Screen.width - screenMargin;

        // Margen inferior
        bool insideVerticalMargins =
            position.y > screenMargin;

        return
            insideBottomThird &&
            insideHorizontalMargins &&
            insideVerticalMargins;
    }

    // =====================================================
    // JOYSTICK VISUALS
    // =====================================================

    void ShowJoystick(Vector2 screenPosition)
    {
        background.gameObject.SetActive(true);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPosition,
            uiCamera,
            out Vector2 localPoint
        );

        background.localPosition = localPoint;

        handle.localPosition = Vector2.zero;
    }

    void UpdateJoystick(Vector2 screenPosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            screenPosition,
            uiCamera,
            out Vector2 position
        );

        // Limitar rango del stick
        position = Vector2.ClampMagnitude(
            position,
            handleRange
        );

        // Mover visualmente el handle
        handle.localPosition = position;

        // Dirección normalizada
        InputDirection = position / handleRange;
    }

    void HideJoystick()
    {
        isDragging = false;
        activeFingerId = -1;

        InputDirection = Vector2.zero;

        handle.localPosition = Vector2.zero;

        background.gameObject.SetActive(false);
    }
}