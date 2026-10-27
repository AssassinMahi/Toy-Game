using UnityEngine;

/// <summary>
/// Orbits a target with mouse movement while keeping the camera aimed at it.
/// Attach this component to the Main Camera and assign a target, or leave the
/// target empty to find the GameObject named "Pickup-Truck" at runtime.
/// </summary>
public class OrbitFollowCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetObjectName = "Pickup-Truck";
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Orbit")]
    [SerializeField] private float distance = 6f;
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float minPitch = -20f;
    [SerializeField] private float maxPitch = 75f;
    [SerializeField] private bool requireRightMouseButton = true;

    [Header("Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.08f;

    private float yaw;
    private float pitch = 20f;
    private Vector3 positionVelocity;

    private void Start()
    {
        if (target == null)
        {
            GameObject targetObject = GameObject.Find(targetObjectName);
            if (targetObject != null)
                target = targetObject.transform;
        }

        if (target == null)
        {
            Debug.LogWarning($"{nameof(OrbitFollowCamera)} could not find a target. " +
                             $"Assign one in the Inspector or create an object named '{targetObjectName}'.", this);
            enabled = false;
            return;
        }

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = NormalizePitch(angles.x);
    }

    private void LateUpdate()
    {
        bool canOrbit = !requireRightMouseButton || Input.GetMouseButton(1);
        if (canOrbit)
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focusPoint = target.position + targetOffset;
        Vector3 desiredPosition = focusPoint - orbitRotation * Vector3.forward * distance;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref positionVelocity,
            positionSmoothTime
        );
        transform.rotation = Quaternion.LookRotation(focusPoint - transform.position, Vector3.up);
    }

    private static float NormalizePitch(float angle)
    {
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }
}
