using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    [Header("Targets")]
    public Transform player;
    public Transform vehicle;
    public UnifiedPlayerEntity playerEntity;

    [Header("Settings")]
    public float height = 80f;

    private Transform currentTarget;

    void Start()
    {
        if (playerEntity == null)
            playerEntity = FindFirstObjectByType<UnifiedPlayerEntity>();

        if (playerEntity != null)
        {
            if (player == null && playerEntity.outsidePlayer != null)
                player = playerEntity.outsidePlayer;
            if (vehicle == null && playerEntity.vehicleTransform != null)
                vehicle = playerEntity.vehicleTransform;
        }

        currentTarget = playerEntity != null ? playerEntity.GetActiveTarget() : player;
    }

    void LateUpdate()
    {
        if (currentTarget == null) return;

        Vector3 pos = currentTarget.position;
        pos.y = height;
        transform.position = pos;
    }

    /// <summary>
    /// Переключить слежение на игрока
    /// </summary>
    public void FollowPlayer()
    {
        currentTarget = playerEntity != null ? playerEntity.GetActiveTarget() : player;
    }

    /// <summary>
    /// Переключить слежение на машину
    /// </summary>
    public void FollowVehicle()
    {
        currentTarget = vehicle != null ? vehicle : (playerEntity != null ? playerEntity.GetActiveTarget() : player);
    }

    /// <summary>
    /// Универсальный метод — передать любой Transform
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        currentTarget = newTarget != null ? newTarget : (playerEntity != null ? playerEntity.GetActiveTarget() : player);
    }
}