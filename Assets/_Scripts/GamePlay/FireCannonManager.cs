using Fungus;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FireCanonManager : MonoBehaviour
{
    public FormulaSustitution formulaSustition;

    [Header("UI")]
    public TMP_Text angleText;
    public Slider velocitySlider;
    public TMP_Text velocityValue;

    [Header("UI Rotation")]
    public RectTransform angleTextTransform;

    [Header("References")]
    public Transform cannonMuzzle;

    public GameObject cannonBallPrefab;

    [Header("Cannon")]
    public CannonAimUI cannonAimUI;
    public GameObject cannonFireFx;
    public GameObject cannonballFiredFx;

    [Header("Physics")]
    
    [Header("Velocity Range")]
    public float minInitialVelocity = 5f;

    public float maxInitialVelocity = 50f;

    public float initialVelocity = 20f;

    public float gravity = 9.81f;

    public float currentAngle;

    public float currentRange;

    [Header("Trajectory")]
    public bool showTrajectory = true;

    public GameObject trajectoryDotPrefab;

    public int maxDots = 100;

    public float dotSpacing = 0.15f;

    public float trajectoryTimeStep = 0.1f;

    [Header("Dot Animation")]
    public bool enableDotPulse = true;

    public float dotPulseSpeed = 6f;

    public float dotMinScale = 0.8f;

    public float dotMaxScale = 1.2f;

    [Space]

    public bool enableDotFade = true;

    public float dotFadeSpeed = 4f;

    public float dotFadeOffset = 0.15f;

    public float dotMinAlpha = 0.1f;

    public float dotMaxAlpha = 1f;

    [Header("Ground")]
    public LayerMask groundLayer;    

    List<GameObject> trajectoryDots =
        new List<GameObject>();

    // ====================================
    // UNITY
    // ====================================

    void Start()
    {
        formulaSustition = GetComponent<FormulaSustitution>();
        CreateTrajectoryPool();
        InitializeVelocitySlider();
    }

    void Update()
    {
        // ====================================
        // SLIDER INTERACTION
        // ====================================

        if (velocitySlider != null)
        {
            velocitySlider.interactable =
                !GameSettings.Instance.cinematicPause;
        }

        // ====================================
        // CINEMATIC PAUSE
        // ====================================

        if (GameSettings.Instance.cinematicPause)
        {
            return;
        }

        CalculateRange();

        UpdateTrajectory();

        // TEST FIRE

        bool encounterActive =
            StartGamePlay.Instance != null
            &&
            StartGamePlay.Instance.encounterActive;

        if (
            encounterActive
            &&
            Input.GetKeyDown(KeyCode.Space)
            )
        {
            Fire();
            Instantiate(cannonballFiredFx);

            Instantiate(cannonFireFx, cannonMuzzle.position, Quaternion.identity);
        }
    }

    public void FireButton()
    {
        // ====================================
        // CINEMATIC PAUSE
        // ====================================

        if (GameSettings.Instance.cinematicPause)
            return;

        bool encounterActive =
           StartGamePlay.Instance != null
           &&
           StartGamePlay.Instance.encounterActive;

        if (encounterActive)
        {
            Fire();
            Instantiate(cannonballFiredFx);

            Instantiate(cannonFireFx, cannonMuzzle.position, Quaternion.identity);
        }
    }

    // ====================================
    // CALCULATE RANGE
    // ====================================

    void CalculateRange()
    {
        // ====================================
        // VISUAL ANGLE
        // ====================================

        float visualAngle =
            cannonAimUI.GetCurrentAngle();

        // ====================================
        // PHYSICAL ANGLE
        // ====================================

        currentAngle =
            Mathf.Abs(visualAngle);

        // ====================================
        // UPDATE UI
        // ====================================

        if (angleText != null)
        {
            angleText.text =
                currentAngle
                .ToString("F1") + "°";
        }

        // ====================================
        // KEEP TEXT READABLE
        // ====================================

        if (angleTextTransform != null)
        {
            angleTextTransform.rotation =
                Quaternion.identity;
        }

        // ====================================
        // CALCULATE RANGE
        // ====================================

        float radians =
            currentAngle *
            Mathf.Deg2Rad;

        currentRange =
            (
                initialVelocity *
                initialVelocity *
                Mathf.Sin(2f * radians)
            ) / gravity;
    }

    // ====================================
    // CREATE DOT POOL
    // ====================================

    void CreateTrajectoryPool()
    {
        // ====================================
        // EVITAR DUPLICADOS
        // ====================================

        if (trajectoryDots.Count > 0)
            return;

        // ====================================
        // CREAR DOTS
        // ====================================

        for (int i = 0; i < maxDots; i++)
        {
            GameObject dot =
                Instantiate(
                    trajectoryDotPrefab,
                    transform
                );

            dot.SetActive(false);

            // ====================================
            // GUARDAR EN LISTA
            // ====================================

            trajectoryDots.Add(dot);

            // ====================================
            // ASIGNAR MANAGER
            // ====================================

            TrajectoryDot trajectoryDot =
                dot.GetComponent<TrajectoryDot>();

            if (trajectoryDot != null)
            {
                trajectoryDot.manager = this;
            }
        }
    }

    void ExpandDotPool(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            GameObject dot =
                Instantiate(
                    trajectoryDotPrefab,
                    transform
                );

            dot.SetActive(false);

            trajectoryDots.Add(dot);

            TrajectoryDot trajectoryDot =
                dot.GetComponent<TrajectoryDot>();

            if (trajectoryDot != null)
            {
                trajectoryDot.manager = this;
            }
        }
    }

    void InitializeVelocitySlider()
    {
        if (velocitySlider == null)
            return;

        // ====================================
        // CONFIGURAR RANGO
        // ====================================

        velocitySlider.minValue =
            minInitialVelocity;

        velocitySlider.maxValue =
            maxInitialVelocity;

        // ====================================
        // VALOR ACTUAL
        // ====================================

        velocitySlider.value =
            initialVelocity;

        // ====================================
        // LISTENER
        // ====================================

        velocitySlider.onValueChanged
            .AddListener(UpdateVelocityFromSlider);

        velocityValue.text = initialVelocity.ToString("f2") + (" m/s");
    }

    void UpdateVelocityFromSlider(float value)
    {
        initialVelocity =
            Mathf.Clamp(
                value,
                minInitialVelocity,
                maxInitialVelocity
            );
        velocityValue.text = initialVelocity.ToString("f2") + (" m/s");

        formulaSustition.UpdateFormulaValues();
    }

    public void RefreshVelocitySlider()
    {
        if (velocitySlider == null)
            return;

        velocitySlider.minValue =
            minInitialVelocity;

        velocitySlider.maxValue =
            maxInitialVelocity;

        velocitySlider.value =
            initialVelocity;
    }

    // ====================================
    // UPDATE TRAJECTORY
    // ====================================

    void UpdateTrajectory()
    {
        // ====================================
        // ENCOUNTER STATE
        // ====================================

        bool encounterActive =
            StartGamePlay.Instance != null
            &&
            StartGamePlay.Instance.encounterActive;

        bool visible =
            showTrajectory &&
            encounterActive;

        // ====================================
        // HIDE ALL
        // ====================================

        if (!visible)
        {
            for (int i = 0; i < trajectoryDots.Count; i++)
            {
                trajectoryDots[i].SetActive(false);
            }

            return;
        }

        // ====================================
        // INITIAL DATA
        // ====================================

        Vector3 currentPosition =
            cannonMuzzle.position;

        Vector3 currentVelocity =
            cannonMuzzle.forward.normalized *
            initialVelocity;

        int dotIndex = 0;

        float accumulatedDistance = 0f;

        Vector3 lastDotPosition =
            currentPosition;

        // ====================================
        // SIMULATION
        // ====================================

        while (true)
        {
            Vector3 previousPosition =
                currentPosition;

            // ====================================
            // APPLY GRAVITY
            // ====================================

            currentVelocity +=
                Vector3.down *
                gravity *
                trajectoryTimeStep;

            // ====================================
            // MOVE
            // ====================================

            currentPosition +=
                currentVelocity *
                trajectoryTimeStep;

            // ====================================
            // DISTANCE
            // ====================================

            accumulatedDistance +=
                Vector3.Distance(
                    previousPosition,
                    currentPosition
                );

            // ====================================
            // PLACE DOT
            // ====================================

            if (accumulatedDistance >= dotSpacing)
            {
                accumulatedDistance = 0f;

                // ====================================
                // EXPAND POOL
                // ====================================

                if (dotIndex >= trajectoryDots.Count)
                {
                    ExpandDotPool(20);
                }

                GameObject dot =
                    trajectoryDots[dotIndex];
                if (dot == null)
                {
                    continue;
                }

                dot.SetActive(true);

                dot.transform.position =
                    currentPosition;

                lastDotPosition =
                    currentPosition;

                dotIndex++;
            }

            // ====================================
            // HIT GROUND
            // ====================================

            if (
                Physics.Linecast(
                    previousPosition,
                    currentPosition,
                    groundLayer
                )
            )
            {
                break;
            }

            // ====================================
            // SAFETY
            // ====================================

            if (dotIndex > 1000)
            {
                break;
            }
        }

        // ====================================
        // HIDE UNUSED
        // ====================================

        for (
            int i = dotIndex;
            i < trajectoryDots.Count;
            i++
        )
        {
            if (trajectoryDots[i] != null)
            {
                trajectoryDots[i].SetActive(false);
            }
        }
    }

    // ====================================
    // FIRE
    // ====================================

    public void Fire()
    {
        if (cannonBallPrefab == null)
            return;

        // ====================================
        // CREATE BALL
        // ====================================

        GameObject ball =
            Instantiate(
                cannonBallPrefab,
                cannonMuzzle.position,
                Quaternion.identity
            );

        // ====================================
        // GET SCRIPT
        // ====================================

        CannonBall cannonBall =
            ball.GetComponent<CannonBall>();

        if (cannonBall == null)
            return;

        // ====================================
        // FORWARD DIRECTION
        // ====================================

        Vector3 forward =
            cannonMuzzle.forward.normalized;

        // ====================================
        // INITIAL VELOCITY
        // ====================================

        Vector3 velocity =
            forward *
            initialVelocity;

        // ====================================
        // INITIALIZE BALL
        // ====================================

        cannonBall.Initialize(
            velocity,
            gravity
        );
    }
}