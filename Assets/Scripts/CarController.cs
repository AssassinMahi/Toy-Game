
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    // All six local directions you can choose from in the Inspector.
    public enum CarAxis
    {
        PositiveX,
        NegativeX,
        PositiveY,
        NegativeY,
        PositiveZ,
        NegativeZ
    }

    [Header("Car Orientation")]

    [Tooltip("The local axis pointing toward the FRONT of the car.")]
    public CarAxis forwardAxis = CarAxis.PositiveZ;

    [Tooltip("The local axis pointing toward the LEFT side of the car.")]
    public CarAxis leftAxis = CarAxis.NegativeX;

    [Header("Movement Settings")]
    public float acceleration = 15f;
    public float maxSpeed = 20f;
    public float turnSpeed = 100f;
    public float brakeForce = 10f;

    private Rigidbody rb;
    private float moveInput;
    private float turnInput;

    // Convert the Inspector axis selection into a Vector3.
    Vector3 GetAxisVector(CarAxis axis)
    {
        switch (axis)
        {
            case CarAxis.PositiveX: return Vector3.right;
            case CarAxis.NegativeX: return Vector3.left;
            case CarAxis.PositiveY: return Vector3.up;
            case CarAxis.NegativeY: return Vector3.down;
            case CarAxis.PositiveZ: return Vector3.forward;
            case CarAxis.NegativeZ: return Vector3.back;

            default: return Vector3.forward;
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // W/S: forward and backward.
        moveInput = Input.GetAxisRaw("Vertical");

        // A/D: left and right.
        turnInput = Input.GetAxisRaw("Horizontal");
    }

    void FixedUpdate()
    {
        // Convert the selected local directions to world directions.
        Vector3 forward = transform.TransformDirection(
            GetAxisVector(forwardAxis)
        );

        Vector3 left = transform.TransformDirection(
            GetAxisVector(leftAxis)
        );

        // Accelerate in the selected forward/backward direction.
        if (rb.linearVelocity.magnitude < maxSpeed)
        {
            rb.AddForce(
                forward * moveInput * acceleration,
                ForceMode.Acceleration
            );
        }

        // Turn around the car's local up axis.
        // Calculate up from the selected forward and left directions.
        Vector3 up = Vector3.Cross(forward, left).normalized;

        if (Mathf.Abs(moveInput) > 0.1f && up.sqrMagnitude > 0.9f)
        {
            // A/D steering; reverse steering direction when reversing.
            float direction = moveInput > 0f ? 1f : -1f;

            float turn = turnInput * turnSpeed
                         * direction * Time.fixedDeltaTime;

            // Positive turn rotates toward the selected left direction.
            rb.MoveRotation(
                rb.rotation *
                Quaternion.AngleAxis(-turn, up)
            );
        }

        // Hold Space to brake.
        if (Input.GetKey(KeyCode.Space))
        {
            rb.linearVelocity = Vector3.Lerp(
                rb.linearVelocity,
                Vector3.zero,
                brakeForce * Time.fixedDeltaTime
            );
        }
    }
}
