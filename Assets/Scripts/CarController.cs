using UnityEngine;

public class SimpleCarController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontLeft;
    public WheelCollider frontRight;
    public WheelCollider rearLeft;
    public WheelCollider rearRight;

    [Header("Wheel Meshes")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    [Header("Settings")]
    public float maxMotorTorque = 2500f;
    public float maxSteerAngle = 28f;
    public float brakeForce = 4000f;
    public float handBrakeForce = 8000f;
    public float maxSpeedKmh = 100f;
    public float speedLimitRangeKmh = 5f;
    public float steeringDeadZone = 0.05f;

    [Header("Recovery")]
    public float flipRecoveryUpDotThreshold = 0.25f;
    public float flipRecoveryMinSpeed = 5f;
    public float flipRecoveryTorque = 30f;
    public float flipRecoveryLiftForce = 12f;
    public float roadRespawnHeightOffset = 1.25f;
    public string roadObjectName = "Road";

    private Rigidbody rb;
    private Transform roadTransform;
    private bool isPlayerInCar = false;

    private float motorInput;
    private float steerInput;
    private bool isHandBraking;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("SimpleCarController requires a Rigidbody.", this);
            enabled = false;
            return;
        }

        FindRoadTransform();

        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.centerOfMass = new Vector3(0f, -0.8f, 0f);
    }

    void Update()
    {
        if (!isPlayerInCar) return;

        motorInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");
        if (Mathf.Abs(steerInput) < steeringDeadZone)
            steerInput = 0f;
        isHandBraking = Input.GetKey(KeyCode.Space);
    }

    void FixedUpdate()
    {
        if (rb == null || frontLeft == null || frontRight == null || rearLeft == null || rearRight == null)
            return;

        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward) * 3.6f;

        if (IsFlipped())
        {
            if (TryTeleportToRoad())
                return;

            if (Mathf.Abs(forwardSpeed) < flipRecoveryMinSpeed)
            {
                Vector3 recoveryAxis = Vector3.Cross(transform.up, Vector3.up);
                if (recoveryAxis.sqrMagnitude > 0.0001f)
                {
                    rb.AddTorque(recoveryAxis.normalized * flipRecoveryTorque, ForceMode.Acceleration);
                }

                rb.AddForce(Vector3.up * flipRecoveryLiftForce, ForceMode.Acceleration);
            }
        }

        if (!isPlayerInCar)
        {
            UpdateWheel(frontLeft, frontLeftMesh);
            UpdateWheel(frontRight, frontRightMesh);
            UpdateWheel(rearLeft, rearLeftMesh);
            UpdateWheel(rearRight, rearRightMesh);
            return;
        }

        float motor = maxMotorTorque * motorInput;
        if (motorInput > 0f && forwardSpeed > maxSpeedKmh)
        {
            float remainingSpeed = Mathf.Clamp01((maxSpeedKmh + speedLimitRangeKmh - forwardSpeed) / speedLimitRangeKmh);
            motor *= remainingSpeed;
        }

        rearLeft.motorTorque = motor;
        rearRight.motorTorque = motor;

        float steer = maxSteerAngle * steerInput;
        frontLeft.steerAngle = steer;
        frontRight.steerAngle = steer;

        float brake = isHandBraking ? handBrakeForce : (motorInput == 0 ? brakeForce * 0.3f : 0f);

        ApplyBrake(frontLeft, brake);
        ApplyBrake(frontRight, brake);
        ApplyBrake(rearLeft, brake);
        ApplyBrake(rearRight, brake);

        UpdateWheel(frontLeft, frontLeftMesh);
        UpdateWheel(frontRight, frontRightMesh);
        UpdateWheel(rearLeft, rearLeftMesh);
        UpdateWheel(rearRight, rearRightMesh);
    }

    private bool IsFlipped()
    {
        return Vector3.Dot(transform.up, Vector3.up) < flipRecoveryUpDotThreshold;
    }

    private void FindRoadTransform()
    {
        GameObject roadObject = GameObject.Find(roadObjectName);
        roadTransform = roadObject != null ? roadObject.transform : null;
    }

    private bool TryTeleportToRoad()
    {
        if (roadTransform == null)
            FindRoadTransform();

        if (roadTransform == null)
            return false;

        Collider roadCollider = roadTransform.GetComponent<Collider>();
        if (roadCollider == null)
            roadCollider = roadTransform.GetComponentInChildren<Collider>();

        if (roadCollider == null)
            return false;

        Vector3 targetPosition = roadCollider.bounds.center + Vector3.up * (roadRespawnHeightOffset + 0.5f);
        rb.position = targetPosition;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 currentEuler = transform.eulerAngles;
        currentEuler.x = 0f;
        currentEuler.z = 0f;
        transform.rotation = Quaternion.Euler(currentEuler);

        return true;
    }

    void ApplyBrake(WheelCollider col, float force)
    {
        col.brakeTorque = force;
    }

    void UpdateWheel(WheelCollider col, Transform mesh)
    {
        if (mesh == null || col == null) return;

        Vector3 pos;
        Quaternion rot;
        col.GetWorldPose(out pos, out rot);

        mesh.position = pos;

        mesh.rotation = rot * Quaternion.Euler(0f, 0f, 90f);
    }

    public void SetPlayerInCar(bool value)
    {
        isPlayerInCar = value;

        if (!value)
        {
            rearLeft.motorTorque = 0;
            rearRight.motorTorque = 0;
            frontLeft.brakeTorque = 0;
            frontRight.brakeTorque = 0;
            rearLeft.brakeTorque = 0;
            rearRight.brakeTorque = 0;
            frontLeft.steerAngle = 0;
            frontRight.steerAngle = 0;
        }
    }
}