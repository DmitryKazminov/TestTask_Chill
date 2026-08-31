using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BezierSegment
{
    public Vector3 anchorStart;
    public Vector3 controlStart;
    public Vector3 controlEnd;
    public Vector3 anchorEnd;

    public int samplingPoints = 30;
    public float length;
    public float cumulativeLength;
    public List<BezierSample> samples = new List<BezierSample>();

    public BezierSegment()
    {
    }

    public BezierSegment(Vector3 start, Vector3 startControl, Vector3 endControl, Vector3 end)
    {
        anchorStart = start;
        controlStart = startControl;
        controlEnd = endControl;
        anchorEnd = end;
        samplingPoints = 30;
        RecalculateLength();
    }

    public Vector3 GetPoint(float t)
    {
        t = Mathf.Clamp01(t);

        float oneMinusT = 1f - t;
        float oneMinusT2 = oneMinusT * oneMinusT;
        float t2 = t * t;

        return
            oneMinusT2 * oneMinusT * anchorStart +
            3f * oneMinusT2 * t * controlStart +
            3f * oneMinusT * t2 * controlEnd +
            t2 * t * anchorEnd;
    }

    public Vector3 GetDirection(float t)
    {
        t = Mathf.Clamp01(t);

        Vector3 derivative =
            3f * (1f - t) * (1f - t) * (controlStart - anchorStart) +
            6f * (1f - t) * t * (controlEnd - controlStart) +
            3f * t * t * (anchorEnd - controlEnd);

        if (derivative.sqrMagnitude < 0.0001f)
        {
            Vector3 direct = anchorEnd - anchorStart;
            if (direct.sqrMagnitude < 0.0001f)
            {
                return Vector3.forward;
            }

            return direct.normalized;
        }

        return derivative.normalized;
    }

    public void RecalculateLength()
    {
        if (samplingPoints < 2)
        {
            samplingPoints = 2;
        }

        samples.Clear();

        for (int i = 0; i < samplingPoints; i++)
        {
            float t = samplingPoints == 1 ? 0f : i / (float)(samplingPoints - 1);
            BezierSample sample = new BezierSample
            {
                t = t,
                distance = 0f,
                position = GetPoint(t)
            };

            samples.Add(sample);
        }

        length = 0f;
        for (int i = 1; i < samples.Count; i++)
        {
            Vector3 previous = samples[i - 1].position;
            Vector3 current = samples[i].position;

            float segmentDist = Vector3.Distance(previous, current);
            length += segmentDist;

            BezierSample updated = samples[i];
            updated.distance = length;
            samples[i] = updated;
        }

        if (samples.Count > 0)
        {
            BezierSample first = samples[0];
            first.distance = 0f;
            samples[0] = first;
        }

        cumulativeLength = length;
    }

    public BezierSample GetSampleAtDistance(float distance)
    {
        if (samples.Count == 0)
        {
            RecalculateLength();
        }

        if (distance <= 0f)
        {
            return samples[0];
        }

        if (distance >= length)
        {
            return samples[samples.Count - 1];
        }

        for (int i = 0; i < samples.Count - 1; i++)
        {
            BezierSample current = samples[i];
            BezierSample next = samples[i + 1];

            if (distance >= current.distance && distance <= next.distance)
            {
                float span = next.distance - current.distance;
                if (span <= 0.0001f)
                {
                    return current;
                }

                float ratio = (distance - current.distance) / span;
                float t = Mathf.Lerp(current.t, next.t, ratio);

                return new BezierSample
                {
                    t = t,
                    distance = distance,
                    position = GetPoint(t)
                };
            }
        }

        return samples[samples.Count - 1];
    }

    public Vector3 GetPointAtDistance(float distance)
    {
        return GetSampleAtDistance(distance).position;
    }

    public Vector3 GetDirectionAtDistance(float distance)
    {
        BezierSample sample = GetSampleAtDistance(distance);
        return GetDirection(sample.t);
    }
}
