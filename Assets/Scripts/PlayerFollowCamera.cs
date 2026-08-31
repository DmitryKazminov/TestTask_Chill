using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public UnifiedPlayerEntity playerEntity;

    [Header("Settings")]
    public float distance = 5f;
    public float height = 2f;
    public float mouseSensitivity = 3f;
    public float minVerticalAngle = -30f;
    public float maxVerticalAngle = 60f;
    public float smoothSpeed = 10f;
    public float autoAlignAfterSeconds = 2.5f;
    public float autoAlignSpeed = 5f;
    public float movementThreshold = 0.1f;

    [Header("Vehicle Camera")]
    public float vehicleDistance = 7f;
    public float vehicleHeight = 2.8f;
    public float vehicleDefaultPitch = 12f;

    private float currentX = 0f;
    private float currentY = 20f;
    private float timeSinceMouseInput;
    private Rigidbody targetRigidbody;
    private bool isFollowingVehicle;

    void Start()
    {
        if (playerEntity == null)
            playerEntity = FindFirstObjectByType<UnifiedPlayerEntity>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        UpdateTargetRigidbody();
    }

    void LateUpdate()
    {
        if (target == null && playerEntity != null)
            target = playerEntity.GetActiveTarget();

        if (playerEntity != null && target == playerEntity.transform)
            target = playerEntity.GetActiveTarget();

        if (target == null) return;

        float currentDistance = isFollowingVehicle ? vehicleDistance : distance;
        float currentHeight = isFollowingVehicle ? vehicleHeight : height;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        currentX += mouseX * mouseSensitivity;
        currentY -= mouseY * mouseSensitivity;
        currentY = Mathf.Clamp(currentY, minVerticalAngle, maxVerticalAngle);

        if (Mathf.Abs(mouseX) > 0.001f || Mathf.Abs(mouseY) > 0.001f)
        {
            timeSinceMouseInput = 0f;
        }
        else
        {
            timeSinceMouseInput += Time.deltaTime;
        }

        if (timeSinceMouseInput >= autoAlignAfterSeconds)
            AlignWithMovement();

        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 direction = rotation * Vector3.back * currentDistance;
        Vector3 desiredPosition = target.position + Vector3.up * currentHeight + direction;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        transform.LookAt(target.position + Vector3.up * currentHeight * 0.6f);
    }

    public void SetTarget(Transform newTarget, bool vehicle)
    {
        target = newTarget;
        isFollowingVehicle = vehicle;
        UpdateTargetRigidbody();

        if (vehicle)
            currentY = vehicleDefaultPitch;
    }

    void UpdateTargetRigidbody()
    {
        targetRigidbody = target != null ? target.GetComponentInParent<Rigidbody>() : null;
    }

    void AlignWithMovement()
    {
        Vector3 movementDirection = target.forward;

        if (targetRigidbody != null && targetRigidbody.linearVelocity.sqrMagnitude > movementThreshold * movementThreshold)
            movementDirection = targetRigidbody.linearVelocity;

        movementDirection.y = 0f;
        if (movementDirection.sqrMagnitude < 0.0001f) return;

        float targetAngle = Mathf.Atan2(movementDirection.x, movementDirection.z) * Mathf.Rad2Deg;
        currentX = Mathf.LerpAngle(currentX, targetAngle, autoAlignSpeed * Time.deltaTime);
    }
}