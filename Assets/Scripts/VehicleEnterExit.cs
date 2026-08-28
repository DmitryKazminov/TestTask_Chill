using UnityEngine;
using System.Collections;

public class VehicleEnterExit : MonoBehaviour
{
    [Header("References")]
    public SimpleCarController carController;
    public Transform leftExitPoint;
    public Transform rightExitPoint;
    public Transform cameraTarget;
    public GameObject player;
    public MonoBehaviour playerController;
    public ThirdPersonCamera cameraScript;

    [Header("Settings")]
    public KeyCode interactKey = KeyCode.E;
    public float interactDistance = 3.5f;
    public float exitHeightOffset = 0.75f;

    private bool isPlayerInside = false;
    private Transform currentExitPoint;
    private Collider[] playerColliders;
    private Collider[] vehicleColliders;

    void Update()
    {
        if (player == null) return;

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

    void TryEnterVehicle()
    {
        if (leftExitPoint == null || rightExitPoint == null || carController == null) return;

        float distToLeft = Vector3.Distance(player.transform.position, leftExitPoint.position);
        float distToRight = Vector3.Distance(player.transform.position, rightExitPoint.position);

        if (distToLeft <= interactDistance || distToRight <= interactDistance)
        {
            currentExitPoint = (distToLeft < distToRight) ? leftExitPoint : rightExitPoint;
            EnterVehicle();
        }
    }

    void EnterVehicle()
    {
        isPlayerInside = true;

        player.SetActive(false);
        if (playerController != null)
            playerController.enabled = false;

        carController.SetPlayerInCar(true);

        if (cameraScript != null)
            cameraScript.SetTarget(cameraTarget, true);
    }

    void ExitVehicle()
    {
        isPlayerInside = false;

        carController.SetPlayerInCar(false);

        if (currentExitPoint == null)
            currentExitPoint = rightExitPoint != null ? rightExitPoint : leftExitPoint;

        player.transform.position = currentExitPoint.position + Vector3.up * exitHeightOffset;
        player.transform.rotation = currentExitPoint.rotation;

        player.SetActive(true);
        if (playerController != null)
            playerController.enabled = true;

        if (cameraScript != null)
            cameraScript.SetTarget(player.transform, false);

        IgnoreVehicleCollisionForExit();
    }

    void IgnoreVehicleCollisionForExit()
    {
        if (playerColliders == null || vehicleColliders == null) return;

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

    IEnumerator RestoreVehicleCollisionAfterExit()
    {
        yield return new WaitForFixedUpdate();

        if (playerColliders == null || vehicleColliders == null) yield break;

        foreach (Collider playerCollider in playerColliders)
        {
            foreach (Collider vehicleCollider in vehicleColliders)
            {
                if (playerCollider != null && vehicleCollider != null)
                    Physics.IgnoreCollision(playerCollider, vehicleCollider, false);
            }
        }
    }

    void OnDrawGizmosSelected()
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