using UnityEngine;

public class UnifiedPlayerEntity : MonoBehaviour
{
    public enum ActorState
    {
        OnFoot,
        InVehicle
    }

    [Header("Target references")]
    public Transform outsidePlayer;
    public Transform vehicleTransform;

    [Header("Actor state")]
    [SerializeField] private ActorState actorState = ActorState.OnFoot;

    public ActorState State => actorState;
    public bool IsInVehicle => actorState == ActorState.InVehicle;

    private void Awake()
    {
        AutoBindReferences();
    }

    private void OnValidate()
    {
        AutoBindReferences();
    }

    private void LateUpdate()
    {
        SyncToCurrentTarget();
    }

    public Transform GetActiveTarget()
    {
        if (actorState == ActorState.InVehicle)
        {
            if (vehicleTransform != null)
                return vehicleTransform;
        }
    
        if (outsidePlayer != null && outsidePlayer.gameObject.activeInHierarchy)
            return outsidePlayer;
    
        // запасной вариант
        return transform;
    }

    public void SetOutsidePlayer(Transform player)
    {
        outsidePlayer = player;
    }

    public void SetVehicleTransform(Transform vehicle)
    {
        vehicleTransform = vehicle;
    }

    public void SetActorState(ActorState value)
    {
        actorState = value;
        SyncToCurrentTarget();
    }

    public void SetInVehicle(bool value)
    {
        actorState = value ? ActorState.InVehicle : ActorState.OnFoot;
        SyncToCurrentTarget();
    }

    public void SyncToCurrentTarget()
    {
        Transform currentTarget = GetActiveTarget();
        if (currentTarget == null)
            return;

        transform.position = currentTarget.position;
        transform.rotation = currentTarget.rotation;
    }

    private void AutoBindReferences()
    {
        if (outsidePlayer == null)
        {
            var playerObject = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
            if (playerObject != null)
                outsidePlayer = playerObject.transform;
        }

        if (vehicleTransform == null)
        {
            var carController = FindFirstObjectByType<SimpleCarController>();
            if (carController != null)
                vehicleTransform = carController.transform;
            else
            {
                var vehicleEnterExit = FindFirstObjectByType<VehicleEnterExit>();
                if (vehicleEnterExit != null)
                    vehicleTransform = vehicleEnterExit.transform;
            }
        }
    }
}
