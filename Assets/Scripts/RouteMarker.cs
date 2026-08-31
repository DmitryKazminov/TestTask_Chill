using UnityEngine;
using UnityEngine.Events;

public class RouteMarker : MonoBehaviour
{
    public enum MarkerType { Start, Checkpoint, Finish }

    [Header("Settings")]
    public MarkerType type = MarkerType.Checkpoint;
    public PathDrawer path;
    public float triggerRadius = 4f;

    [Header("Visual")]
    public GameObject visualPrefab;
    public GameObject visualObject;
    public bool hideWhenReached = true;

    [Header("Events")]
    public UnityEvent onReached;

    [HideInInspector] public bool isReached = false;

    private RouteNavigationController navigation;

    private void Awake()
    {
        if (visualObject == null)
            TryBindVisual();
    }

    private void Start()
    {
        navigation = FindFirstObjectByType<RouteNavigationController>();

        if (visualObject == null)
            TryBindVisual();

        if (visualObject != null)
            SetVisualActive(true);
    }

    private void Update()
    {
        if (isReached) return;
        if (navigation == null || navigation.player == null) return;

        float dist = Vector3.Distance(transform.position, navigation.player.position);
        if (dist <= triggerRadius)
        {
            Reach();
        }
    }

    public void TryBindVisual()
    {
        if (visualObject != null)
            return;

        if (transform.childCount > 0)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (child.GetComponent<Renderer>() != null || child.GetComponentsInChildren<Renderer>(true).Length > 0)
                {
                    visualObject = child;
                    break;
                }
            }
        }

        if (visualObject == null && visualPrefab != null)
        {
            visualObject = Instantiate(visualPrefab, transform);
            visualObject.name = "MarkerVisual";
            visualObject.transform.localPosition = Vector3.zero;
            visualObject.transform.localRotation = Quaternion.identity;
        }

        if (visualObject == null)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "MarkerVisual";
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale = new Vector3(1f, 0.5f, 1f);
            visualObject = marker;
        }
    }

    public void Reach()
    {
        if (isReached) return;
        isReached = true;

        onReached?.Invoke();

        switch (type)
        {
            case MarkerType.Start:
                Debug.Log($"🟢 Start '{name}' достигнут");
                break;

            case MarkerType.Finish:
                if (navigation != null)
                    navigation.CompleteCurrentRoute();
                Debug.Log($"🏁 Finish '{name}' достигнут — миссия завершена");
                break;

            case MarkerType.Checkpoint:
                Debug.Log($"🟡 Checkpoint '{name}' достигнут");
                break;
        }

        if (hideWhenReached)
            SetVisualActive(false);
    }

    public void ResetMarker()
    {
        isReached = false;
        if (visualObject != null)
            SetVisualActive(true);
    }

    public void MarkAsActive()
    {
        SetVisualActive(true);
        Debug.Log($"[RouteMarker] '{name}' → Active");
    }

    public void MarkAsInactive()
    {
        SetVisualActive(false);
        Debug.Log($"[RouteMarker] '{name}' → Inactive");
    }

    public void SetActiveMarker(bool active)
    {
        SetVisualActive(active);
    }

    private void SetVisualActive(bool active)
    {
        if (visualObject == null)
            TryBindVisual();

        if (visualObject == null)
            return;

        visualObject.SetActive(active);

        Renderer[] renderers = visualObject.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
            renderer.enabled = active;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = type == MarkerType.Finish ? Color.red :
                       type == MarkerType.Start  ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = type == MarkerType.Finish ? new Color(1, 0, 0, 0.4f) :
                       type == MarkerType.Start  ? new Color(0, 1, 0, 0.4f) : new Color(1, 1, 0, 0.4f);
        Gizmos.DrawSphere(transform.position, 0.35f);
    }
}