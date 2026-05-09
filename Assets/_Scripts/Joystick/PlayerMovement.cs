using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public VirtualJoystick joystick;

    [Header("Movement")]
    public float moveSpeed = 4f;

    [Header("Rotation")]
    public float rotationSpeed = 120f;

    Rigidbody rb;

    float moveInput;
    float rotationInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        Vector2 input = joystick.InputDirection;

        moveInput = input.y;
        rotationInput = input.x;
    }

    void FixedUpdate()
    {
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
        // MOVIMIENTO
        // =========================

        Vector3 velocity =
            transform.forward *
            moveInput *
            moveSpeed;

        velocity.y = rb.linearVelocity.y;

        rb.linearVelocity = velocity;
    }    
}
