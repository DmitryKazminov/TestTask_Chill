using UnityEngine;

[ExecuteAlways]
public class PathDrawer : MonoBehaviour
{
    [SerializeField] private BezierPath path = new BezierPath();

    [Header("Curve Settings")]
    public int samplingPoints = 30;
    public bool allowVerticalMovement = false;
    public bool drawSamplePoints = false;
    public int selectedAnchorIndex = -1;

    [Header("Visual Settings")]
    public Color anchorColor = new Color(0.18f, 0.85f, 1f, 1f);
    public Color controlColor = new Color(1f, 0.7f, 0.2f, 1f);
    public Color curveColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    public float handleSize = 0.25f;

    public BezierPath Path
    {
        get
        {
            if (path == null)
            {
                path = new BezierPath();
            }

            return path;
        }
    }

    private void Reset()
    {
        samplingPoints = Mathf.Max(2, samplingPoints);
        path = new BezierPath();
        path.samplingPoints = samplingPoints;
        path.Reset();
        selectedAnchorIndex = -1;
    }

    private void OnValidate()
    {
        if (path == null)
        {
            path = new BezierPath();
        }

        path.samplingPoints = Mathf.Max(2, samplingPoints);
        path.RecalculateLength();

        if (path.anchors != null && path.anchors.Count > 0)
        {
            selectedAnchorIndex = Mathf.Clamp(selectedAnchorIndex, -1, path.anchors.Count - 1);
        }
        else
        {
            selectedAnchorIndex = -1;
        }
    }

    private void Awake()
    {
        if (path == null)
        {
            path = new BezierPath();
        }

        if (path.anchors == null || path.anchors.Count == 0)
        {
            path.Reset();
        }

        path.samplingPoints = Mathf.Max(2, samplingPoints);
        path.RecalculateLength();
    }

    public void AddAnchor()
    {
        if (path == null)
        {
            path = new BezierPath();
        }

        path.AddAnchor();
        selectedAnchorIndex = path.anchors.Count - 1;
        path.RecalculateLength();
    }

    public void RemoveSelectedAnchor()
    {
        if (path == null || path.anchors == null || path.anchors.Count == 0)
        {
            return;
        }

        if (selectedAnchorIndex < 0)
        {
            return;
        }

        path.RemoveAnchor(selectedAnchorIndex);
        selectedAnchorIndex = Mathf.Clamp(selectedAnchorIndex, -1, Mathf.Max(0, path.anchors.Count - 1));
        path.RecalculateLength();
    }

    public void ResetPath()
    {
        path = new BezierPath();
        path.samplingPoints = Mathf.Max(2, samplingPoints);
        path.Reset();
        selectedAnchorIndex = -1;
    }

    public void RecalculateLength()
    {
        if (path == null)
        {
            return;
        }

        path.samplingPoints = Mathf.Max(2, samplingPoints);
        path.RecalculateLength();
    }

    public Vector3 LocalToWorld(Vector3 localPosition)
    {
        return transform.TransformPoint(localPosition);
    }

    public Vector3 WorldToLocal(Vector3 worldPosition)
    {
        return transform.InverseTransformPoint(worldPosition);
    }

    private void OnDrawGizmosSelected()
    {
        if (path == null || path.segments == null || path.segments.Count == 0)
        {
            return;
        }

        Gizmos.color = curveColor;

        for (int i = 0; i < path.segments.Count; i++)
        {
            BezierSegment segment = path.segments[i];
            Vector3 start = LocalToWorld(segment.anchorStart);
            Vector3 controlStart = LocalToWorld(segment.controlStart);
            Vector3 controlEnd = LocalToWorld(segment.controlEnd);
            Vector3 end = LocalToWorld(segment.anchorEnd);

            Gizmos.DrawLine(start, controlStart);
            Gizmos.DrawLine(end, controlEnd);

            Vector3 previous = start;
            int sampleCount = Mathf.Max(2, segment.samples.Count);

            for (int j = 1; j <= sampleCount; j++)
            {
                float t = j / (float)sampleCount;
                Vector3 point = LocalToWorld(segment.GetPoint(t));
                Gizmos.DrawLine(previous, point);
                previous = point;
            }

            if (drawSamplePoints)
            {
                for (int s = 0; s < segment.samples.Count; s++)
                {
                    Gizmos.color = controlColor;
                    Gizmos.DrawSphere(LocalToWorld(segment.samples[s].position), 0.08f);
                }
            }
        }
    }
}
