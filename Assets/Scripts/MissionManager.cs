using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class MissionManager : MonoBehaviour
{
    [Header("References")]
    public RouteNavigationController routeNavigationController;
    public Transform playerTransform;

    [Header("Debug")]
    public bool debugMode = true;

    [Header("Events")]
    public UnityEvent onMissionStart;
    public UnityEvent onMissionComplete;
    public UnityEvent<int> onCheckpointReached;

    [Header("Markers (задай вручную в порядке прохождения)")]
    public List<RouteMarker> markersInOrder = new List<RouteMarker>();

    private List<RouteMarker> sortedMarkers = new List<RouteMarker>();
    private RouteMarker startMarker;
    private RouteMarker finishMarker;
    private int currentMarkerIndex = -1;
    private bool missionStarted = false;

    private void Start()
    {
        InitializeMission();
    }

    private void InitializeMission()
    {
        if (routeNavigationController == null)
            routeNavigationController = FindFirstObjectByType<RouteNavigationController>();

        if (routeNavigationController == null)
        {
            LogError("RouteNavigationController не найден!");
            return;
        }

        UnifiedPlayerEntity unifiedPlayer = FindFirstObjectByType<UnifiedPlayerEntity>();

        if (unifiedPlayer != null)
        {
            routeNavigationController.SetPlayerEntity(unifiedPlayer);
            playerTransform = unifiedPlayer.GetActiveTarget() != null ? unifiedPlayer.GetActiveTarget() : (unifiedPlayer.outsidePlayer != null ? unifiedPlayer.outsidePlayer : unifiedPlayer.transform);
        }
        else if (playerTransform == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        if (playerTransform == null)
        {
            LogError("Player не найден!");
            return;
        }

        if (routeNavigationController != null)
            routeNavigationController.SetPlayer(playerTransform);

        if (markersInOrder != null && markersInOrder.Count > 0)
        {
            sortedMarkers = markersInOrder.Where(m => m != null).ToList();
        }

        if (sortedMarkers.Count < 2)
        {
            LogError("Нужно минимум Start + Finish");
            return;
        }

        startMarker = sortedMarkers.FirstOrDefault(m => m.type == RouteMarker.MarkerType.Start);
        finishMarker = sortedMarkers.FirstOrDefault(m => m.type == RouteMarker.MarkerType.Finish);

        foreach (var marker in sortedMarkers)
        {
            if (marker == null) continue;
            marker.onReached.RemoveAllListeners();
            marker.onReached.AddListener(() => OnMarkerReached(marker));
            marker.MarkAsInactive();
        }

        ActivateMarker(0);

        missionStarted = true;
        onMissionStart?.Invoke();
        LogDebug($"Миссия готова. Маркеров: {sortedMarkers.Count}");
    }
    private void SortMarkers(RouteMarker[] allMarkers)
    {
        sortedMarkers.Clear();

        List<RouteMarker> checkpoints = new List<RouteMarker>();

        foreach (var marker in allMarkers)
        {
            switch (marker.type)
            {
                case RouteMarker.MarkerType.Start:
                    startMarker = marker;
                    break;

                case RouteMarker.MarkerType.Checkpoint:
                    checkpoints.Add(marker);
                    break;

                case RouteMarker.MarkerType.Finish:
                    finishMarker = marker;
                    break;
            }
        }

        if (startMarker != null)
            sortedMarkers.Add(startMarker);

        sortedMarkers.AddRange(checkpoints);

        if (finishMarker != null)
            sortedMarkers.Add(finishMarker);

        LogDebug($"Маркеры отсортированы: Start → {checkpoints.Count} Checkpoints → Finish");
    }

    private void ActivateMarker(int markerIndex)
    {
        if (markerIndex < 0 || markerIndex >= sortedMarkers.Count)
        {
            LogError($"Неверный индекс маркера: {markerIndex}");
            return;
        }

        RouteMarker marker = sortedMarkers[markerIndex];
        if (marker == null)
        {
            LogError($"Маркер [{markerIndex}] is null!");
            return;
        }

        foreach (var otherMarker in sortedMarkers)
        {
            if (otherMarker == null || otherMarker == marker)
                continue;

            otherMarker.MarkAsInactive();
        }

        currentMarkerIndex = markerIndex;

        marker.MarkAsActive();

        if (marker.path != null && routeNavigationController != null)
        {
            routeNavigationController.SetRoute(marker, marker.path);
            LogDebug($"Активирован маркер [{markerIndex}]: {marker.type}");
        }
        else
        {
            routeNavigationController?.CompleteCurrentRoute();
            LogError($"Маркер [{markerIndex}] не имеет пути!");
        }
    }

    private void OnMarkerReached(RouteMarker reachedMarker)
    {
        int reachedIndex = sortedMarkers.IndexOf(reachedMarker);

        if (reachedIndex == -1)
        {
            LogError($"Маркер {reachedMarker.name} не найден в списке!");
            return;
        }

        LogDebug($"Маркер [{reachedIndex}] достигнут: {reachedMarker.type}");

        switch (reachedMarker.type)
        {
            case RouteMarker.MarkerType.Start:
                LogDebug("Миссия начинается! Активируем следующий маршрут.");
                reachedMarker.MarkAsInactive();

                if (routeNavigationController != null)
                    routeNavigationController.CompleteCurrentRoute();

                if (reachedIndex + 1 < sortedMarkers.Count)
                    ActivateMarker(reachedIndex + 1);
                break;

            case RouteMarker.MarkerType.Checkpoint:
                OnCheckpointReached(reachedIndex);
                break;

            case RouteMarker.MarkerType.Finish:
                OnMissionComplete(reachedIndex);
                break;
        }
    }

    private void OnCheckpointReached(int checkpointIndex)
    {
        onCheckpointReached?.Invoke(checkpointIndex);

        if (checkpointIndex >= 0 && checkpointIndex < sortedMarkers.Count)
            sortedMarkers[checkpointIndex].MarkAsInactive();

        int nextIndex = checkpointIndex + 1;
        if (nextIndex < sortedMarkers.Count)
        {
            if (routeNavigationController != null)
                routeNavigationController.CompleteCurrentRoute();

            ActivateMarker(nextIndex);
            LogDebug($"→ Переключились на маркер [{nextIndex}]");
        }
    }

    private void OnMissionComplete(int finishIndex)
    {
        if (finishIndex >= 0 && finishIndex < sortedMarkers.Count && sortedMarkers[finishIndex] != null)
            sortedMarkers[finishIndex].MarkAsInactive();

        if (routeNavigationController != null)
            routeNavigationController.CompleteCurrentRoute();

        LogDebug("🏁 МИССИЯ ЗАВЕРШЕНА!");
        missionStarted = false;
        onMissionComplete?.Invoke();
    }

    public void ResetMission()
    {
        LogDebug("Миссия сброшена.");
        
        foreach (var marker in sortedMarkers)
        {
            if (marker != null)
            {
                marker.ResetMarker();
                marker.MarkAsInactive();
            }
        }

        currentMarkerIndex = -1;
        missionStarted = false;

        if (routeNavigationController != null)
            routeNavigationController.CompleteCurrentRoute();
    }

    public void RestartMission()
    {
        ResetMission();
        InitializeMission();
    }

    public int GetCurrentMarkerIndex() => currentMarkerIndex;

    public bool IsMissionStarted() => missionStarted;

    public RouteMarker GetCurrentMarker()
    {
        if (currentMarkerIndex >= 0 && currentMarkerIndex < sortedMarkers.Count)
            return sortedMarkers[currentMarkerIndex];
        return null;
    }

    private void LogDebug(string message)
    {
        if (debugMode)
            Debug.Log($"[MissionManager] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[MissionManager] ❌ {message}");
    }
}
