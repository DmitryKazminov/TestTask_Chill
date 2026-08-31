using System.Collections;
using UnityEngine;

public class VehicleEnterExit : MonoBehaviour
{
    [Header("References")]
    public SimpleCarController carController;
    public Transform leftExitPoint;
    public Transform rightExitPoint;
    public Transform exitPoint;
    public Transform cameraTarget;
    public GameObject player;
    public UnifiedPlayerEntity playerEntity;
    public MonoBehaviour playerController;
    public ThirdPersonCamera cameraScript;
    public Camera playerCamera;
    public Camera carCamera;

    [Header("Settings")]
    public KeyCode interactKey = KeyCode.E;
    public float interactDistance = 3.5f;
    public float exitHeightOffset = 0.75f;

    private bool isPlayerInside;
    private Transform currentExitPoint;
    private Collider[] playerColliders;
    private Collider[] vehicleColliders;

    private void Awake()
    {
        TryAutoBind();
    }

    private void Start()
    {
        TryAutoBind();
    }

    private void Update()
    {
        if (player == null)
            return;

        if (playerColliders == null)
            playerColliders = player.GetComponentsInChildren<Collider>(true);

        if (vehicleColliders == null)
            vehicleColliders = GetComponentsInChildren<Collider>(true);

        if (Input.GetKeyDown(interactKey))
        {
            if (isPlayerInside)
            {
                ExitVehicle();
            }
            else
            {
                TryEnterVehicle();
            }
        }
    }

    private void TryAutoBind()
    {
        if (playerEntity == null)
            playerEntity = FindFirstObjectByType<UnifiedPlayerEntity>();

        if (playerEntity != null)
        {
            if (player == null && playerEntity.outsidePlayer != null)
                player = playerEntity.outsidePlayer.gameObject;
            if (playerEntity.vehicleTransform == null)
                playerEntity.SetVehicleTransform(transform);
        }

        if (player == null)
        {
            var playerObject = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
            if (playerObject != null)
                player = playerObject;
        }

        if (cameraScript == null)
            cameraScript = FindFirstObjectByType<ThirdPersonCamera>();
    }

    private void TryEnterVehicle()
    {
        if (player == null || carController == null)
            return;

        if (leftExitPoint == null || rightExitPoint == null)
            return;

        float distToLeft = Vector3.Distance(player.transform.position, leftExitPoint.position);
        float distToRight = Vector3.Distance(player.transform.position, rightExitPoint.position);

        if (distToLeft <= interactDistance || distToRight <= interactDistance)
        {
            currentExitPoint = distToLeft < distToRight ? leftExitPoint : rightExitPoint;
            EnterVehicle();
        }
    }

    private void EnterVehicle()
    {
        if (carController == null || player == null)
            return;

        isPlayerInside = true;

        if (playerEntity != null)
        {
            playerEntity.SetVehicleTransform(transform);
            playerEntity.SetActorState(UnifiedPlayerEntity.ActorState.InVehicle);
            playerEntity.SyncToCurrentTarget();
        }

        player.SetActive(false);
        if (playerController != null)
            playerController.enabled = false;

        carController.enabled = true;
        carController.SetPlayerInCar(true);

        if (playerCamera != null)
            playerCamera.gameObject.SetActive(false);
        if (carCamera != null)
            carCamera.gameObject.SetActive(true);

        if (cameraScript != null)
            cameraScript.SetTarget(carController != null ? carController.transform : player.transform, true);

        MinimapFollow minimap = FindFirstObjectByType<MinimapFollow>();
        if (minimap != null)
            minimap.SetTarget(carController != null ? carController.transform : player.transform);
    }

    private void ExitVehicle()
    {
        if (carController == null || player == null)
            return;

        isPlayerInside = false;

        Transform spawnPoint = currentExitPoint != null ? currentExitPoint : (exitPoint != null ? exitPoint : leftExitPoint != null ? leftExitPoint : rightExitPoint);
        if (spawnPoint != null)
        {
            player.transform.position = spawnPoint.position + Vector3.up * exitHeightOffset;
            player.transform.rotation = spawnPoint.rotation;
        }

        if (playerEntity != null)
        {
            playerEntity.SetActorState(UnifiedPlayerEntity.ActorState.OnFoot);
            playerEntity.SetOutsidePlayer(player.transform);
            playerEntity.SyncToCurrentTarget();
        }

        carController.SetPlayerInCar(false);
        carController.enabled = false;

        Rigidbody vehicleRb = GetComponent<Rigidbody>();
        if (vehicleRb != null)
        {
            vehicleRb.linearVelocity = Vector3.zero;
            vehicleRb.angularVelocity = Vector3.zero;
        }

        player.SetActive(true);
        if (playerController != null)
            playerController.enabled = true;

        if (carCamera != null)
            carCamera.gameObject.SetActive(false);
        if (playerCamera != null)
            playerCamera.gameObject.SetActive(true);

        if (cameraScript != null)
            cameraScript.SetTarget(playerEntity != null ? playerEntity.GetActiveTarget() : player.transform, false);

        IgnoreVehicleCollisionForExit();

        MinimapFollow minimap = FindFirstObjectByType<MinimapFollow>();
        if (minimap != null)
            minimap.SetTarget(playerEntity != null ? playerEntity.GetActiveTarget() : player.transform);

        if (cameraScript != null)
        {
            Transform footTarget = playerEntity != null ? playerEntity.GetActiveTarget() : player.transform;
            cameraScript.SetTarget(footTarget, false);
        }
    }

    private void IgnoreVehicleCollisionForExit()
    {
        if (playerColliders == null || vehicleColliders == null)
            return;

        foreach (Collider playerCollider in playerColliders)
        {
            foreach (Collider vehicleCollider in vehicleColliders)
            {
                if (playerCollider != null && vehicleCollider != null)
                    Physics.IgnoreCollision(playerCollider, vehicleCollider, true);
            }
        }

        StartCoroutine(RestoreVehicleCollisionAfterExit());
    }

    private IEnumerator RestoreVehicleCollisionAfterExit()
    {
        yield return new WaitForFixedUpdate();

        if (playerColliders == null || vehicleColliders == null)
            yield break;

        foreach (Collider playerCollider in playerColliders)
        {
            foreach (Collider vehicleCollider in vehicleColliders)
            {
                if (playerCollider != null && vehicleCollider != null)
                    Physics.IgnoreCollision(playerCollider, vehicleCollider, false);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (leftExitPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(leftExitPoint.position, 0.4f);
        }

        if (rightExitPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(rightExitPoint.position, 0.4f);
        }
    }
}