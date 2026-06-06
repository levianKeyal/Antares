using Fungus;
using UnityEngine;

public enum MovementState
{
    Idle,
    Forward,
    Backward,
    Turning
}

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public VirtualJoystick joystick;

    [Header("Movement")]
    public float moveSpeed = 6f;

    [Header("Rotation")]
    public float rotationSpeed = 120f;

    [Header("Boat Movement")]
    public float acceleration = 2f;
    public float deceleration = 1f;

    Rigidbody rb;

    float moveInput;
    float rotationInput;

    float currentSpeed;

    bool isActuallyMoving;

    [Header("Particles & FX")]
    [SerializeField]
    private ParticleSystem _foam;

    [SerializeField]
    private TrailRenderer _waterTrail;

    // =========================
    // STATES
    // =========================

    MovementState currentState;
    MovementState previousState;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // =========================
        // CINEMATIC PAUSE
        // =========================

        if (GameSettings.Instance.cinematicPause)
        {
            moveInput = 0;
            rotationInput = 0;

            return;
        }

        // =========================
        // INPUT
        // =========================

        Vector2 input = joystick.InputDirection;

        moveInput = input.y;
        rotationInput = input.x;

        // =========================
        // DETECTAR ESTADO
        // =========================

        if (moveInput > 0.1f)
        {
            currentState = MovementState.Forward;
        }
        else if (moveInput < -0.1f)
        {
            currentState = MovementState.Backward;
        }
        else if (Mathf.Abs(rotationInput) > 0.1f)
        {
            currentState = MovementState.Turning;
        }
        else
        {
            currentState = MovementState.Idle;
        }

        // =========================
        // CAMBIO DE ESTADO
        // =========================

        if (currentState != previousState)
        {
            switch (currentState)
            {
                case MovementState.Idle:

                    Debug.Log("Player Idle");
                    _foam.Stop();


                    break;

                case MovementState.Forward:

                    Debug.Log("Player Moving Forward");
                    _foam.Play();
                    _waterTrail.emitting = true;

                    break;

                case MovementState.Backward:

                    Debug.Log("Player Moving Backward");

                    break;

                case MovementState.Turning:

                    Debug.Log("Player Turning");

                    break;
            }

            previousState = currentState;
        }
    }
    void FixedUpdate()
    {
        // =========================
        // CINEMATIC PAUSE
        // =========================

        if (GameSettings.Instance.cinematicPause)
        {
            StopMovementImmediately();
            return;
        }
        // =========================
        // ROTACIÓN
        // =========================

        float rotationAmount =
            rotationInput *
            rotationSpeed *
            Time.fixedDeltaTime;

        Quaternion rotation =
            Quaternion.Euler(0, rotationAmount, 0);

        rb.MoveRotation(rb.rotation * rotation);

        // =========================
        // VELOCIDAD OBJETIVO
        // =========================

        float targetSpeed =
            moveInput * moveSpeed;

        // =========================
        // ACELERACIÓN
        // =========================

        if (Mathf.Abs(moveInput) > 0.1f)
        {
            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                targetSpeed,
                acceleration * Time.fixedDeltaTime
            );
        }
        // =========================
        // DESACELERACIÓN
        // =========================
        else
        {
            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                0,
                deceleration * Time.fixedDeltaTime
            );
        }

        // =========================
        // VELOCIDAD FINAL
        // =========================

        Vector3 velocity =
            transform.forward *
            currentSpeed;

        velocity.y = rb.linearVelocity.y;

        rb.linearVelocity = velocity;

        isActuallyMoving = rb.linearVelocity.magnitude > 0.1f;

        _waterTrail.emitting = isActuallyMoving;
    }
    public void StopMovementImmediately()
    {
        currentSpeed = 0;

        rb.linearVelocity = Vector3.zero;

        rb.angularVelocity = Vector3.zero;
    }
}