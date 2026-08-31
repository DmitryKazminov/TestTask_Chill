using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BezierAnchor
{
    public Vector3 position;
    public Vector3 controlIn;
    public Vector3 controlOut;
    public bool smooth = true;

    public BezierAnchor()
    {
    }

    public BezierAnchor(Vector3 point)
    {
        position = point;
        controlIn = point - new Vector3(2f, 0f, 1f);
        controlOut = point + new Vector3(2f, 0f, 1f);
        smooth = true;
    }
}

[System.Serializable]
public class BezierPath
{
    public List<BezierAnchor> anchors = new List<BezierAnchor>();
    public List<BezierSegment> segments = new List<BezierSegment>();

    public int samplingPoints = 30;
    public float totalLength;
    public List<float> segmentStartDistances = new List<float>();
    public List<float> segmentEndDistances = new List<float>();

    public void Reset()
    {
        anchors.Clear();

        BezierAnchor start = new BezierAnchor(new Vector3(0f, 0f, 0f));
        start.controlIn = new Vector3(-2f, 0f, -1f);
        start.controlOut = new Vector3(2f, 0f, 1f);
        start.smooth = true;

        BezierAnchor end = new BezierAnchor(new Vector3(10f, 0f, 0f));
        end.controlIn = new Vector3(8f, 0f, -1f);
        end.controlOut = new Vector3(12f, 0f, 1f);
        end.smooth = true;

        anchors.Add(start);
        anchors.Add(end);

        RecalculateLength();
    }

    public void AddAnchor()
    {
        if (anchors.Count == 0)
        {
            Reset();
            return;
        }

        BezierAnchor last = anchors[anchors.Count - 1];
        Vector3 direction = Vector3.forward;

        if (anchors.Count > 1)
        {
            BezierAnchor previous = anchors[anchors.Count - 2];
            direction = (last.position - previous.position);
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.forward;
            }
            else
            {
                direction = direction.normalized;
            }
        }

        Vector3 newPosition = last.position + direction * 8f;
        BezierAnchor anchor = new BezierAnchor(newPosition);

        anchor.controlIn = newPosition - direction * 4f;
        anchor.controlOut = newPosition + direction * 4f;
        anchor.smooth = true;

        anchors.Add(anchor);
        RecalculateLength();
    }

    public void RemoveAnchor(int index)
    {
        if (index < 0 || index >= anchors.Count)
        {
            return;
        }

        if (anchors.Count <= 2)
        {
            Reset();
            return;
        }

        anchors.RemoveAt(index);
        RecalculateLength();
    }

    public void RecalculateLength()
    {
        if (anchors == null)
        {
            anchors = new List<BezierAnchor>();
        }

        segments.Clear();
        segmentStartDistances.Clear();
        segmentEndDistances.Clear();

        if (anchors.Count < 2)
        {
            totalLength = 0f;
            return;
        }

        float runningLength = 0f;

        for (int i = 0; i < anchors.Count - 1; i++)
        {
            BezierAnchor current = anchors[i];
            BezierAnchor next = anchors[i + 1];

            BezierSegment segment = new BezierSegment
            {
                anchorStart = current.position,
                controlStart = current.controlOut,
                controlEnd = next.controlIn,
                anchorEnd = next.position,
                samplingPoints = Mathf.Max(2, samplingPoints)
            };

            segment.RecalculateLength();
            segments.Add(segment);

            segmentStartDistances.Add(runningLength);
            runningLength += segment.length;
            segmentEndDistances.Add(runningLength);
        }

        totalLength = runningLength;
    }

    public float GetLength()
    {
        return totalLength;
    }

    public Vector3 GetPoint(float t)
    {
        if (segments.Count == 0)
        {
            return Vector3.zero;
        }

        if (segments.Count == 1)
        {
            return segments[0].GetPoint(Mathf.Clamp01(t));
        }

        float targetDistance = Mathf.Clamp01(t) * totalLength;
        return GetPointAtDistance(targetDistance);
    }

    public Vector3 GetDirection(float t)
    {
        if (segments.Count == 0)
        {
            return Vector3.forward;
        }

        if (segments.Count == 1)
        {
            return segments[0].GetDirection(Mathf.Clamp01(t));
        }

        float targetDistance = Mathf.Clamp01(t) * totalLength;
        return GetDirectionAtDistance(targetDistance);
    }

    public Vector3 GetPointAtDistance(float distance)
    {
        if (segments.Count == 0)
        {
            return Vector3.zero;
        }

        if (distance <= 0f)
        {
            return segments[0].GetPoint(0f);
        }

        if (distance >= totalLength)
        {
            return segments[segments.Count - 1].GetPoint(1f);
        }

        for (int i = 0; i < segments.Count; i++)
        {
            float segmentStart = segmentStartDistances[i];
            float segmentEnd = segmentEndDistances[i];

            if (distance >= segmentStart && distance <= segmentEnd)
            {
                float localDistance = distance - segmentStart;
                return segments[i].GetPointAtDistance(localDistance);
            }
        }

        return segments[segments.Count - 1].GetPoint(1f);
    }

    public Vector3 GetDirectionAtDistance(float distance)
    {
        if (segments.Count == 0)
        {
            return Vector3.forward;
        }

        if (distance <= 0f)
        {
            return segments[0].GetDirection(0f);
        }

        if (distance >= totalLength)
        {
            return segments[segments.Count - 1].GetDirection(1f);
        }

        for (int i = 0; i < segments.Count; i++)
        {
            float segmentStart = segmentStartDistances[i];
            float segmentEnd = segmentEndDistances[i];

            if (distance >= segmentStart && distance <= segmentEnd)
            {
                float localDistance = distance - segmentStart;
                return segments[i].GetDirectionAtDistance(localDistance);
            }
        }

        return segments[segments.Count - 1].GetDirection(1f);
    }
}
