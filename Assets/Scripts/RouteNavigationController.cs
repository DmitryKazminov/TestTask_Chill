using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RouteNavigationController : MonoBehaviour
{
    [Header("Player / Route")]
    public Transform player;
    public UnifiedPlayerEntity playerEntity;
    public RouteMarker currentMarker;
    public PathDrawer currentPath;

    [Header("Arrow Prefab")]
    public GameObject arrowPrefab;

    [Header("Arrow Layout")]
    [Range(0.5f, 10f)]
    public float arrowSpacing = 2.0f;

    [Range(0.1f, 10f)]
    public float fadeDistance = 1.5f;

    [Range(0.1f, 10f)]
    public float fadeInDistance = 1.5f;

    [Range(1, 10)]
    public int maxVisibleArrows = 4;

    [Header("Visual")]
    public float heightOffset = 0.5f;

    private readonly List<GameObject> pooledArrows = new List<GameObject>();
    private readonly List<Renderer[]> pooledRenderers = new List<Renderer[]>();
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        EnsureArrowPool();
    }

    private void OnValidate()
    {
        maxVisibleArrows = Mathf.Clamp(maxVisibleArrows, 1, 10);
        arrowSpacing = Mathf.Max(0.1f, arrowSpacing);
        fadeDistance = Mathf.Max(0.1f, fadeDistance);
        fadeInDistance = Mathf.Max(0.1f, fadeInDistance);

    }

    private void Update()
    {
        Transform activePlayer = ResolvePlayerTransform();
        if (activePlayer == null || currentPath == null || currentPath.Path == null ||
            currentPath.Path.anchors == null || currentPath.Path.anchors.Count < 2)
        {
            HideAllArrows();
            return;
        }

        player = activePlayer;
        UpdateVisibleArrows();
    }

    public void SetRoute(RouteMarker routeMarker, PathDrawer routePath)
    {
        HideAllArrows();
        currentMarker = null;
        currentPath = null;

        currentMarker = routeMarker;
        currentPath = routePath;

        if (routePath != null)
        {
            routePath.RecalculateLength();
        }
        else
        {
            HideAllArrows();
        }

        if (Application.isPlaying)
        {
            EnsureArrowPool();
            UpdateVisibleArrows();
        }
    }

    public void SetPlayer(Transform targetPlayer)
    {
        player = targetPlayer;
        if (playerEntity != null && targetPlayer != null)
            playerEntity.SetOutsidePlayer(targetPlayer);
    }

    public void SetPlayerEntity(UnifiedPlayerEntity targetEntity)
    {
        playerEntity = targetEntity;
        if (targetEntity == null)
        {
            player = null;
            return;
        }

        Transform active = targetEntity.GetActiveTarget();
        player = active != null ? active : (targetEntity.outsidePlayer != null ? targetEntity.outsidePlayer : targetEntity.transform);
    }

    private Transform ResolvePlayerTransform()
    {
        if (playerEntity != null)
        {
            Transform activeTarget = playerEntity.GetActiveTarget();
            if (activeTarget != null)
            {
                player = activeTarget;
                return activeTarget;
            }
        }

        return player;
    }

    public void CompleteCurrentRoute()
    {
        HideAllArrows();
        currentMarker = null;
        currentPath = null;
    }

    private void EnsureArrowPool()
    {
        if (arrowPrefab == null) return;

        // Создаём недостающие
        while (pooledArrows.Count < maxVisibleArrows)
        {
            GameObject instance = Instantiate(arrowPrefab, transform);
            instance.name = "ArrowPool_" + pooledArrows.Count;
            instance.SetActive(false);

            pooledArrows.Add(instance);

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.sortingOrder = 100;

                if (r.material != null)
                {
                    r.material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                }
            }

            pooledRenderers.Add(renderers);
        }

        while (pooledArrows.Count > maxVisibleArrows)
        {
            int last = pooledArrows.Count - 1;
            if (pooledArrows[last] != null)
            {
                if (Application.isPlaying)
                    Destroy(pooledArrows[last]);
                else
                    DestroyImmediate(pooledArrows[last]);
            }
            pooledArrows.RemoveAt(last);
            pooledRenderers.RemoveAt(last);
        }
    }

    private void UpdateVisibleArrows()
    {
        Transform activePlayer = ResolvePlayerTransform();
        if (arrowPrefab == null || activePlayer == null || currentPath == null || currentPath.Path == null)
        {
            HideAllArrows();
            return;
        }

        player = activePlayer;

        // На всякий случай
        if (pooledArrows.Count < maxVisibleArrows)
            EnsureArrowPool();

        float routeLength = currentPath.Path.GetLength();
        if (routeLength <= 0.0001f)
        {
            HideAllArrows();
            return;
        }

        float playerDistance = GetNearestDistanceOnPath(player.position, currentPath);
        float visibleStartDistance = playerDistance + (arrowSpacing * 0.5f);

        for (int i = 0; i < pooledArrows.Count; i++)
        {
            float targetDistance = visibleStartDistance - i * arrowSpacing;
            GameObject arrow = pooledArrows[i];

            if (arrow == null) continue;

            if (targetDistance > routeLength)
            {
                arrow.SetActive(false);
                continue;
            }

            Vector3 localPoint = currentPath.Path.GetPointAtDistance(targetDistance);
            Vector3 localDirection = currentPath.Path.GetDirectionAtDistance(targetDistance);

            Vector3 point = currentPath.LocalToWorld(localPoint);
            Vector3 direction = -currentPath.transform.TransformDirection(localDirection);

            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector3.back;

            arrow.transform.position = point + Vector3.up * heightOffset;
            Quaternion pathRotation = Quaternion.LookRotation(direction, Vector3.up);
            arrow.transform.rotation = pathRotation * Quaternion.Euler(-90f, 0f, 0f);

            float distanceAhead = Mathf.Max(0f, targetDistance - playerDistance);
            float alpha = GetArrowAlpha(distanceAhead);

            SetArrowAlpha(i, alpha);
            arrow.SetActive(true);
        }
    }

    private float GetArrowAlpha(float distanceAhead)
    {
        const float minVisibleAlpha = 0.35f;

        if (distanceAhead <= fadeDistance)
        {
            float alpha = Mathf.Clamp01(distanceAhead / fadeDistance);
            return Mathf.Max(alpha, minVisibleAlpha);
        }

        float maxWindow = (maxVisibleArrows - 1) * arrowSpacing;
        float fadeInStart = maxWindow - fadeInDistance;

        if (distanceAhead >= fadeInStart)
        {
            float alpha = Mathf.Clamp01(1f - (distanceAhead - fadeInStart) / fadeInDistance);
            return Mathf.Max(alpha, minVisibleAlpha);
        }

        return 1f;
    }

    private void SetArrowAlpha(int index, float alpha)
    {
        if (index < 0 || index >= pooledRenderers.Count) return;

        Renderer[] renderers = pooledRenderers[index];
        if (renderers == null) return;

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        float finalAlpha = Mathf.Clamp01(alpha);
        finalAlpha = Mathf.Max(finalAlpha, 0.35f);

        Color displayColor = new Color(1f, 1f, 1f, finalAlpha);
        Color emissionColor = Color.cyan * finalAlpha;

        foreach (var r in renderers)
        {
            if (r == null) continue;

            r.enabled = true;

            Material mat = r.material;
            if (mat != null)
            {
                mat.color = displayColor;

                if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", displayColor);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", displayColor);
                if (mat.HasProperty("_EmissionColor"))
                    mat.SetColor("_EmissionColor", emissionColor);
                if (mat.HasProperty("_Emission"))
                    mat.SetColor("_Emission", emissionColor);
            }

            if (r.sharedMaterial == null)
                continue;

            r.GetPropertyBlock(propertyBlock);

            if (r.sharedMaterial.HasProperty("_Color"))
                propertyBlock.SetColor("_Color", displayColor);
            if (r.sharedMaterial.HasProperty("_BaseColor"))
                propertyBlock.SetColor("_BaseColor", displayColor);
            if (r.sharedMaterial.HasProperty("_EmissionColor"))
                propertyBlock.SetColor("_EmissionColor", emissionColor);
            if (r.sharedMaterial.HasProperty("_Emission"))
                propertyBlock.SetColor("_Emission", emissionColor);

            r.SetPropertyBlock(propertyBlock);
        }
    }

    private void HideAllArrows()
    {
        for (int i = 0; i < pooledArrows.Count; i++)
        {
            if (pooledArrows[i] != null)
                pooledArrows[i].SetActive(false);
        }
    }

    private float GetNearestDistanceOnPath(Vector3 worldPosition, PathDrawer pathDrawer)
    {
        if (pathDrawer == null || pathDrawer.Path == null || pathDrawer.Path.segments == null || pathDrawer.Path.segments.Count == 0)
            return 0f;

        BezierPath path = pathDrawer.Path;
        float routeLength = path.GetLength();
        if (routeLength <= 0.0001f) return 0f;

        Vector3 localPlayer = pathDrawer.WorldToLocal(worldPosition);

        int sampleCount = Mathf.Max(80, path.segments.Count * 40);
        float bestDistance = 0f;
        float bestSqr = float.MaxValue;

        for (int i = 0; i <= sampleCount; i++)
        {
            float d = (i / (float)sampleCount) * routeLength;
            Vector3 point = path.GetPointAtDistance(d);
            float sqr = (point - localPlayer).sqrMagnitude;

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                bestDistance = d;
            }
        }

        return bestDistance;
    }
}