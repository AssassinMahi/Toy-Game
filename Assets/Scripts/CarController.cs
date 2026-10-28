
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

    [Header("Wheel Animation")]
    [Tooltip("If empty, child transforms named Wheel_E, Wheel_E001, etc. are found automatically.")]
    [SerializeField] private Transform[] wheels;
    [Tooltip("Local axis each wheel rotates around. X is typical for wheels whose axles run left to right.")]
    [SerializeField] private Vector3 wheelSpinAxis = Vector3.right;
    [Tooltip("Approximate wheel radius in Unity units. Used to match wheel spin to car speed.")]
    [SerializeField] private float wheelRadius = 0.2f;
    [Tooltip("Reverse this if the wheel meshes spin in the wrong direction.")]
    [SerializeField] private bool invertWheelSpin;
    [Tooltip("Maximum angle the two wheels nearest the front of the car steer.")]
    [SerializeField] private float maxSteeringAngle = 30f;
    [Tooltip("Reverse this if A/D steers the front wheels the wrong way.")]
    [SerializeField] private bool invertWheelSteering;
    [Tooltip("How quickly the car rotates upright while holding A or S after rolling over.")]
    [SerializeField] private float recoverySpeed = 120f;
    [Tooltip("Begin rollover recovery when the car's up direction has less alignment with world up than this value.")]
    [SerializeField, Range(0f, 1f)] private float recoveryTiltThreshold = 0.65f;

    [Header("Motor Sound")]
    [SerializeField] private AudioClip motorSound;
    [SerializeField] private AudioSource motorAudioSource;

    private Rigidbody rb;
    private float moveInput;
    private float turnInput;
    private Quaternion[] wheelBaseLocalRotations;
    private float[] wheelSpinAngles;
    private bool[] frontWheels;
    private PauseMenuUI pauseMenu;

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
        pauseMenu = FindFirstObjectByType<PauseMenuUI>();
        SetupMotorAudio();
        FindWheelsIfNeeded();
        InitializeWheelPose();
    }

    void Update()
    {
        // W/S: forward and backward.
        moveInput = Input.GetAxisRaw("Vertical");

        // A/D: left and right.
        turnInput = Input.GetAxisRaw("Horizontal");

        UpdateMotorAudio();
    }

    private void SetupMotorAudio()
    {
        if (motorSound == null)
            return;

        if (motorAudioSource == null)
            motorAudioSource = GetComponent<AudioSource>();
        if (motorAudioSource == null)
            motorAudioSource = gameObject.AddComponent<AudioSource>();

        motorAudioSource.clip = motorSound;
        motorAudioSource.loop = true;
        motorAudioSource.playOnAwake = false;
    }

    private void UpdateMotorAudio()
    {
        if (motorAudioSource == null || motorSound == null)
            return;

        bool movementKeyHeld = Mathf.Abs(moveInput) > 0.01f;
        bool isUpright = Vector3.Dot(transform.up, Vector3.up) >= recoveryTiltThreshold;
        bool shouldPlay = movementKeyHeld && Time.timeScale > 0f && isUpright;

        if (shouldPlay && !motorAudioSource.isPlaying)
            motorAudioSource.Play();
        else if (!shouldPlay && motorAudioSource.isPlaying)
            motorAudioSource.Stop();
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

        // Turn around the car's local up axis.
        // Calculate up from the selected forward and left directions.
        Vector3 up = Vector3.Cross(forward, left).normalized;

        bool isTippedOver = Vector3.Dot(transform.up, Vector3.up) < recoveryTiltThreshold;
        if (isTippedOver)
        {
            // If the car is on its side or roof, hold A or S to right it.
            // Normal drive and steering are suspended until it is upright.
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S))
                RecoverUpright(forward);

            return;
        }

        // Accelerate in the selected forward/backward direction.
        if (rb.linearVelocity.magnitude < maxSpeed)
        {
            rb.AddForce(
                forward * moveInput * acceleration,
                ForceMode.Acceleration
            );
        }

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

        AnimateWheels(forward, up);
    }

    private void RecoverUpright(Vector3 forward)
    {
        Vector3 flatForward = Vector3.ProjectOnPlane(forward, Vector3.up);
        if (flatForward.sqrMagnitude < 0.001f)
            flatForward = Vector3.ProjectOnPlane(transform.right, Vector3.up);
        if (flatForward.sqrMagnitude < 0.001f)
            flatForward = Vector3.forward;

        Quaternion uprightRotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.MoveRotation(Quaternion.RotateTowards(
            rb.rotation,
            uprightRotation,
            recoverySpeed * Time.fixedDeltaTime
        ));
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (HasGameOverTag(collision.collider.transform))
            pauseMenu?.TriggerGameOver();

        NotifyCheckpoint(collision.collider.transform);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (HasGameOverTag(other.transform))
            pauseMenu?.TriggerGameOver();

        NotifyCheckpoint(other.transform);
    }

    private static void NotifyCheckpoint(Transform touchedTransform)
    {
        CheckpointSequence sequence = touchedTransform.GetComponentInParent<CheckpointSequence>();
        sequence?.TryReachCheckpoint(touchedTransform);
    }

    private static bool HasGameOverTag(Transform hitTransform)
    {
        // Check ancestors too, so a collider on a child object still works
        // when the Game Over Tag is set on its parent.
        for (Transform current = hitTransform; current != null; current = current.parent)
        {
            if (current.CompareTag("Game Over Tag"))
                return true;
        }

        return false;
    }

    void FindWheelsIfNeeded()
    {
        if (wheels != null && wheels.Length > 0)
            return;

        Transform[] childTransforms = GetComponentsInChildren<Transform>(true);
        System.Collections.Generic.List<Transform> foundWheels =
            new System.Collections.Generic.List<Transform>();

        foreach (Transform child in childTransforms)
        {
            if (child == transform)
                continue;

            if (child.name == "Wheel_E" || child.name.StartsWith("Wheel_E"))
                foundWheels.Add(child);
        }

        wheels = foundWheels.ToArray();
        if (wheels.Length == 0)
            Debug.LogWarning("CarController did not find child wheels named Wheel_E, Wheel_E001, etc. Assign wheel transforms in the Inspector.", this);
    }

    void InitializeWheelPose()
    {
        if (wheels == null || wheels.Length == 0)
            return;

        wheelBaseLocalRotations = new Quaternion[wheels.Length];
        wheelSpinAngles = new float[wheels.Length];
        frontWheels = new bool[wheels.Length];

        Vector3 localForward = GetAxisVector(forwardAxis).normalized;
        float firstFrontScore = float.NegativeInfinity;
        float secondFrontScore = float.NegativeInfinity;
        int firstFrontIndex = -1;
        int secondFrontIndex = -1;

        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] == null)
                continue;

            wheelBaseLocalRotations[i] = wheels[i].localRotation;
            float forwardPosition = Vector3.Dot(
                transform.InverseTransformPoint(wheels[i].position),
                localForward
            );

            if (forwardPosition > firstFrontScore)
            {
                secondFrontScore = firstFrontScore;
                secondFrontIndex = firstFrontIndex;
                firstFrontScore = forwardPosition;
                firstFrontIndex = i;
            }
            else if (forwardPosition > secondFrontScore)
            {
                secondFrontScore = forwardPosition;
                secondFrontIndex = i;
            }
        }

        if (firstFrontIndex >= 0)
            frontWheels[firstFrontIndex] = true;
        if (secondFrontIndex >= 0)
            frontWheels[secondFrontIndex] = true;
    }

    void AnimateWheels(Vector3 forward, Vector3 carUp)
    {
        if (wheels == null || wheels.Length == 0 || wheelRadius <= 0f || wheelSpinAngles == null)
            return;

        // Use actual forward/reverse speed so wheel rotation follows the car's
        // motion and naturally slows when braking or coasting.
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, forward);
        float spinDirection = invertWheelSpin ? -1f : 1f;
        float spinDegrees = (forwardSpeed / wheelRadius) * Mathf.Rad2Deg
                            * Time.fixedDeltaTime * spinDirection;
        float steeringDirection = invertWheelSteering ? 1f : -1f;
        float steeringAngle = turnInput * maxSteeringAngle * steeringDirection;
        Vector3 localSpinAxis = wheelSpinAxis.sqrMagnitude > 0f
            ? wheelSpinAxis.normalized
            : Vector3.right;

        for (int i = 0; i < wheels.Length; i++)
        {
            Transform wheel = wheels[i];
            if (wheel != null)
            {
                wheelSpinAngles[i] += spinDegrees;

                Quaternion parentRotation = wheel.parent != null
                    ? wheel.parent.rotation
                    : Quaternion.identity;
                Quaternion baseWorldRotation = parentRotation * wheelBaseLocalRotations[i];
                Quaternion rollingRotation = baseWorldRotation
                    * Quaternion.AngleAxis(wheelSpinAngles[i], localSpinAxis);

                if (frontWheels[i])
                {
                    Quaternion steeringRotation = Quaternion.AngleAxis(
                        steeringAngle,
                        carUp
                    );
                    wheel.rotation = steeringRotation * rollingRotation;
                }
                else
                {
                    wheel.rotation = rollingRotation;
                }
            }
        }
    }
}
