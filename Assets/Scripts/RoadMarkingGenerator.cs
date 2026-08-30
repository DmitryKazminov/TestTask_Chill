using System.Collections.Generic;
using UnityEngine;

public enum RoadMarkingPatternType
{
    Solid,
    Dashed,
    Double,
    CenterLine,
    ShoulderMarking
}

[System.Serializable]
public class SerializableRoadMarkingPoint
{
    public Vector3 position;
    public Vector3 inHandle;
    public Vector3 outHandle;
    public bool isBezier;
}

public class RoadMarkingControlPoint
{
    public Vector3 position;
    public Vector3 inHandle;
    public Vector3 outHandle;
    public bool isBezier;

    public RoadMarkingControlPoint(Vector3 position)
    {
        this.position = position;
        inHandle = position;
        outHandle = position;
        isBezier = false;
    }

    public SerializableRoadMarkingPoint ToSerializable()
    {
        return new SerializableRoadMarkingPoint
        {
            position = position,
            inHandle = inHandle,
            outHandle = outHandle,
            isBezier = isBezier
        };
    }

    public static RoadMarkingControlPoint FromSerializable(SerializableRoadMarkingPoint point)
    {
        if (point == null)
            return new RoadMarkingControlPoint(Vector3.zero);

        var result = new RoadMarkingControlPoint(point.position)
        {
            inHandle = point.inHandle,
            outHandle = point.outHandle,
            isBezier = point.isBezier
        };
        return result;
    }
}

public class RoadMarkingDefinition : MonoBehaviour
{
    [SerializeField] private List<SerializableRoadMarkingPoint> controlPoints = new List<SerializableRoadMarkingPoint>();
    [SerializeField] private RoadMarkingPatternType patternType = RoadMarkingPatternType.Solid;
    [SerializeField] private float roadWidth = 4f;
    [SerializeField] private float lineThickness = 0.12f;
    [SerializeField] private int lineCount = 1;
    [SerializeField] private float lineSpacing = 1.5f;
    [SerializeField] private float minLineSpacing = 0.5f;
    [SerializeField] private float maxLinesPerMeter = 3f;
    [SerializeField] private float elevationOffset = 0.01f;
    [SerializeField] private int curveSamples = 36;
    [SerializeField] private float maxSegmentLength = 1.5f;
    [SerializeField] private bool useBezier = true;
    [SerializeField] private bool autoRaise = true;

    public List<RoadMarkingControlPoint> GetControlPoints()
    {
        List<RoadMarkingControlPoint> result = new List<RoadMarkingControlPoint>();
        if (controlPoints == null)
            return result;

        for (int i = 0; i < controlPoints.Count; i++)
        {
            result.Add(RoadMarkingControlPoint.FromSerializable(controlPoints[i]));
        }

        return result;
    }

    public void SetControlPoints(List<RoadMarkingControlPoint> points)
    {
        controlPoints = new List<SerializableRoadMarkingPoint>();
        if (points == null)
            return;

        for (int i = 0; i < points.Count; i++)
        {
            controlPoints.Add(points[i].ToSerializable());
        }
    }

    public RoadMarkingPatternType PatternType
    {
        get => patternType;
        set => patternType = value;
    }

    public float RoadWidth
    {
        get => roadWidth;
        set => roadWidth = Mathf.Max(0.5f, value);
    }

    public float LineThickness
    {
        get => lineThickness;
        set => lineThickness = Mathf.Max(0.02f, value);
    }

    public int LineCount
    {
        get => lineCount;
        set => lineCount = Mathf.Max(1, value);
    }

    public float LineSpacing
    {
        get => lineSpacing;
        set => lineSpacing = Mathf.Max(0.05f, value);
    }

    public float MinLineSpacing
    {
        get => minLineSpacing;
        set => minLineSpacing = Mathf.Max(0.1f, value);
    }

    public float MaxLinesPerMeter
    {
        get => maxLinesPerMeter;
        set => maxLinesPerMeter = Mathf.Clamp(value, 0.5f, 20f);
    }

    public float ElevationOffset
    {
        get => elevationOffset;
        set => elevationOffset = Mathf.Max(0f, value);
    }

    public int CurveSamples
    {
        get => curveSamples;
        set => curveSamples = Mathf.Max(8, value);
    }

    public float MaxSegmentLength
    {
        get => maxSegmentLength;
        set => maxSegmentLength = Mathf.Max(0.15f, value);
    }

    public bool UseBezier
    {
        get => useBezier;
        set => useBezier = value;
    }

    public bool AutoRaise
    {
        get => autoRaise;
        set => autoRaise = value;
    }
}

public class RoadMarkingGenerator : MonoBehaviour
{
    [Header("Road marking settings")]
    [SerializeField] private Material markingMaterial;
    [SerializeField] private RoadMarkingPatternType patternType = RoadMarkingPatternType.Solid;
    [SerializeField] private float roadWidth = 4f;
    [SerializeField] private float lineThickness = 0.12f;
    [SerializeField] private int lineCount = 1;
    [SerializeField] private float lineSpacing = 1.5f;
    [SerializeField] private float minLineSpacing = 0.5f;
    [SerializeField] private float maxLinesPerMeter = 3f;
    [SerializeField] private float elevationOffset = 0.01f;
    [SerializeField] private int curveSamples = 36;
    [SerializeField] private float maxSegmentLength = 1.5f;
    [SerializeField] private bool useBezier = true;
    [SerializeField] private bool autoRaise = true;

    public Material MarkingMaterial
    {
        get => markingMaterial != null ? markingMaterial : CreateDefaultMaterial();
        set => markingMaterial = value;
    }

    public RoadMarkingPatternType PatternType
    {
        get => patternType;
        set => patternType = value;
    }

    public float RoadWidth
    {
        get => roadWidth;
        set => roadWidth = Mathf.Max(0.5f, value);
    }

    public float LineThickness
    {
        get => lineThickness;
        set => lineThickness = Mathf.Max(0.02f, value);
    }

    public int LineCount
    {
        get => lineCount;
        set => lineCount = Mathf.Max(1, value);
    }

    public float LineSpacing
    {
        get => lineSpacing;
        set => lineSpacing = Mathf.Max(0.05f, value);
    }

    public float MinLineSpacing
    {
        get => minLineSpacing;
        set => minLineSpacing = Mathf.Max(0.1f, value);
    }

    public float MaxLinesPerMeter
    {
        get => maxLinesPerMeter;
        set => maxLinesPerMeter = Mathf.Clamp(value, 0.5f, 20f);
    }

    public float ElevationOffset
    {
        get => elevationOffset;
        set => elevationOffset = Mathf.Max(0f, value);
    }

    public int CurveSamples
    {
        get => curveSamples;
        set => curveSamples = Mathf.Max(8, value);
    }

    public float MaxSegmentLength
    {
        get => maxSegmentLength;
        set => maxSegmentLength = Mathf.Max(0.15f, value);
    }

    public bool UseBezier
    {
        get => useBezier;
        set => useBezier = value;
    }

    public bool AutoRaise
    {
        get => autoRaise;
        set => autoRaise = value;
    }

    public void GenerateFromPoints(List<Vector3> worldPoints)
    {
        GenerateFromPoints(worldPoints, transform);
    }

    public void GenerateFromPoints(List<Vector3> worldPoints, Transform bindingPlane)
    {
        if (worldPoints == null || worldPoints.Count < 2)
        {
            Debug.LogWarning("RoadMarkingGenerator: need at least two points.");
            return;
        }

        if (bindingPlane == null)
        {
            bindingPlane = transform;
        }

        Vector3 planeUp = bindingPlane.up;
        Material material = MarkingMaterial;
        float length = GetCurveLength(worldPoints, useBezier);
        int safeLineCount = GetAllowedLineCount(length, lineCount, maxLinesPerMeter, minLineSpacing);
        CreateMarkingGroup(worldPoints, bindingPlane, planeUp, material, lineThickness, safeLineCount, lineSpacing, curveSamples, maxSegmentLength, useBezier, autoRaise ? elevationOffset : 0f, patternType, roadWidth);
    }

    public static float GetRoadWidth(GameObject plane)
    {
        if (plane == null)
            return 4f;

        Renderer renderer = plane.GetComponent<Renderer>();
        if (renderer == null)
            renderer = plane.GetComponentInChildren<Renderer>();

        if (renderer != null)
        {
            Vector3 size = renderer.bounds.size;
            return Mathf.Max(1f, Mathf.Max(size.x, size.z));
        }

        Collider collider = plane.GetComponent<Collider>();
        if (collider == null)
            collider = plane.GetComponentInChildren<Collider>();

        if (collider != null)
        {
            Vector3 size = collider.bounds.size;
            return Mathf.Max(1f, Mathf.Max(size.x, size.z));
        }

        return 4f;
    }

    public static GameObject CreateMarkingGroup(
        List<Vector3> worldPoints,
        Transform bindingPlane,
        Vector3 planeUp,
        Material material,
        float lineThickness,
        int lineCount,
        float lineSpacing,
        int curveSamples,
        float maxSegmentLength,
        bool useBezier,
        float elevationOffset)
    {
        return CreateMarkingGroup(worldPoints, bindingPlane, planeUp, material, lineThickness, lineCount, lineSpacing, curveSamples, maxSegmentLength, useBezier, elevationOffset, RoadMarkingPatternType.Solid, GetRoadWidth(bindingPlane != null ? bindingPlane.gameObject : null));
    }

    public static GameObject CreateMarkingGroup(
        List<Vector3> worldPoints,
        Transform bindingPlane,
        Vector3 planeUp,
        Material material,
        float lineThickness,
        int lineCount,
        float lineSpacing,
        int curveSamples,
        float maxSegmentLength,
        bool useBezier,
        float elevationOffset,
        RoadMarkingPatternType patternType,
        float roadWidth)
    {
        if (worldPoints == null || worldPoints.Count < 2)
        {
            return null;
        }

        GameObject root = new GameObject("RoadMarking");
        root.transform.SetParent(bindingPlane, false);
        root.transform.localPosition = Vector3.zero;

        var definition = root.AddComponent<RoadMarkingDefinition>();
        definition.PatternType = patternType;
        definition.RoadWidth = roadWidth;
        definition.LineThickness = lineThickness;
        definition.LineCount = lineCount;
        definition.LineSpacing = lineSpacing;
        definition.ElevationOffset = elevationOffset;
        definition.CurveSamples = curveSamples;
        definition.MaxSegmentLength = maxSegmentLength;
        definition.UseBezier = useBezier;
        definition.AutoRaise = elevationOffset > 0f;

        GameObject layer = new GameObject("MarkingLayer");
        layer.transform.SetParent(root.transform, false);
        layer.transform.localPosition = new Vector3(0f, elevationOffset, 0f);

        BuildStripSegments(worldPoints, layer.transform, planeUp, material, lineThickness, lineCount, lineSpacing, curveSamples, maxSegmentLength, useBezier, elevationOffset, patternType, roadWidth, definition);
        return root;
    }

    public static int GetAllowedLineCount(float totalCurveLength, int requestedCount, float maxLinesPerMeter, float minLineSpacing)
    {
        int baseCount = Mathf.Max(1, requestedCount);
        if (totalCurveLength <= 0.0001f)
            return baseCount;

        float maxAllowedByDensity = Mathf.Floor(totalCurveLength * Mathf.Clamp(maxLinesPerMeter, 0.5f, 20f));
        float maxAllowedBySpacing = Mathf.Max(1f, totalCurveLength / Mathf.Max(0.15f, minLineSpacing));
        int limit = Mathf.FloorToInt(Mathf.Min(maxAllowedByDensity, maxAllowedBySpacing));
        return Mathf.Clamp(baseCount, 1, Mathf.Max(1, limit));
    }

    public static List<float> GetPatternOffsets(RoadMarkingPatternType patternType, float roadWidth, int lineCount, float lineSpacing)
    {
        List<float> offsets = new List<float>();
        float halfRoadWidth = Mathf.Max(0.35f, roadWidth * 0.5f);
        int safeLineCount = Mathf.Max(1, lineCount);

        switch (patternType)
        {
            case RoadMarkingPatternType.Solid:
            case RoadMarkingPatternType.Dashed:
            case RoadMarkingPatternType.CenterLine:
                for (int i = 0; i < safeLineCount; i++)
                {
                    float spread = (safeLineCount > 1) ? Mathf.Max(0.15f, lineSpacing) * (i - (safeLineCount - 1) * 0.5f) : 0f;
                    offsets.Add(Mathf.Clamp(spread, -halfRoadWidth * 0.8f, halfRoadWidth * 0.8f));
                }
                break;
            case RoadMarkingPatternType.Double:
                float doubleStep = Mathf.Max(0.25f, Mathf.Min(halfRoadWidth * 0.35f, roadWidth * 0.16f));
                for (int i = 0; i < safeLineCount; i++)
                {
                    offsets.Add(-doubleStep * (i + 1));
                    offsets.Add(doubleStep * (i + 1));
                }
                break;
            case RoadMarkingPatternType.ShoulderMarking:
                float shoulderMargin = Mathf.Max(0.25f, roadWidth * 0.12f);
                offsets.Add(-halfRoadWidth + shoulderMargin);
                offsets.Add(halfRoadWidth - shoulderMargin);
                break;
        }

        if (offsets.Count == 0)
            offsets.Add(0f);

        return offsets;
    }

    public static float GetCurveLength(List<Vector3> points, bool useBezier)
    {
        if (points == null || points.Count < 2)
            return 0f;

        Vector3[] sampled = SampleCurve(points, 120, useBezier);
        float length = 0f;
        for (int i = 1; i < sampled.Length; i++)
        {
            length += Vector3.Distance(sampled[i - 1], sampled[i]);
        }

        return length;
    }

    public static void BuildStripSegments(
        List<Vector3> worldPoints,
        Transform parent,
        Vector3 planeUp,
        Material material,
        float lineThickness,
        int lineCount,
        float lineSpacing,
        int curveSamples,
        float maxSegmentLength,
        bool useBezier,
        float elevationOffset)
    {
        BuildStripSegments(worldPoints, parent, planeUp, material, lineThickness, lineCount, lineSpacing, curveSamples, maxSegmentLength, useBezier, elevationOffset, RoadMarkingPatternType.Solid, GetRoadWidth(parent != null ? parent.parent != null ? parent.parent.gameObject : null : null));
    }

    public static void BuildStripSegments(
        List<Vector3> worldPoints,
        Transform parent,
        Vector3 planeUp,
        Material material,
        float lineThickness,
        int lineCount,
        float lineSpacing,
        int curveSamples,
        float maxSegmentLength,
        bool useBezier,
        float elevationOffset,
        RoadMarkingPatternType patternType,
        float roadWidth,
        RoadMarkingDefinition definition = null)
    {
        var sampled = SampleCurve(worldPoints, curveSamples, useBezier);
        if (sampled == null || sampled.Length < 2)
            return;

        Vector3 normal = planeUp.sqrMagnitude > 0.0001f ? planeUp.normalized : Vector3.up;
        var offsets = GetPatternOffsets(patternType, roadWidth, lineCount, lineSpacing);
        float clampedSegmentLength = Mathf.Max(0.15f, maxSegmentLength);

        int laneIndex = 0;
        for (int offsetIndex = 0; offsetIndex < offsets.Count; offsetIndex++)
        {
            float lateralOffset = offsets[offsetIndex];
            int segmentIndex = 0;

            for (int i = 0; i < sampled.Length - 1; i++)
            {
                Vector3 start = sampled[i];
                Vector3 end = sampled[i + 1];
                Vector3 delta = end - start;
                float segmentLength = delta.magnitude;
                if (segmentLength < 0.0001f)
                    continue;

                if (patternType == RoadMarkingPatternType.Dashed || patternType == RoadMarkingPatternType.CenterLine)
                {
                    float dashLength = 0.9f;
                    float gapLength = 0.6f;
                    float patternLength = dashLength + gapLength;
                    float remaining = segmentLength;
                    float traveled = 0f;

                    while (remaining > 0.0001f)
                    {
                        float localDashLength = Mathf.Min(remaining, dashLength);
                        float localDashProgress = traveled / segmentLength;
                        Vector3 p0 = Vector3.Lerp(start, end, localDashProgress);
                        Vector3 p1 = Vector3.Lerp(start, end, Mathf.Min(1f, (traveled + localDashLength) / segmentLength));
                        Vector3 mid = (p0 + p1) * 0.5f;
                        Vector3 tangent = (p1 - p0).normalized;
                        if (tangent.sqrMagnitude < 0.0001f)
                            tangent = delta.normalized;

                        Vector3 lateral = Vector3.Cross(normal, tangent).normalized;
                        Vector3 center = mid + lateral * lateralOffset;
                        float lengthToDraw = Vector3.Distance(p0, p1);

                        GameObject segment = CreateMarkingCube("RoadMarkingLine_" + laneIndex + "_Seg_" + segmentIndex, center + normal * elevationOffset, Quaternion.LookRotation(tangent, normal), Mathf.Max(0.05f, lengthToDraw), Mathf.Max(0.02f, lineThickness), parent, material);

                        segmentIndex++;
                        traveled += patternLength;
                        remaining -= patternLength;
                        if (remaining < 0.0001f)
                            break;
                    }
                }
                else
                {
                    int subdivisions = Mathf.Max(1, Mathf.CeilToInt(segmentLength / clampedSegmentLength));
                    for (int s = 0; s < subdivisions; s++)
                    {
                        float t0 = s / (float)subdivisions;
                        float t1 = (s + 1f) / subdivisions;
                        Vector3 p0 = Vector3.Lerp(start, end, t0);
                        Vector3 p1 = Vector3.Lerp(start, end, t1);
                        Vector3 segmentDelta = p1 - p0;
                        float currentLength = segmentDelta.magnitude;
                        if (currentLength < 0.0001f)
                            continue;

                        Vector3 tangent = segmentDelta.normalized;
                        Vector3 lateral = Vector3.Cross(normal, tangent).normalized;
                        Vector3 center = p0 + segmentDelta * 0.5f + lateral * lateralOffset;

                        GameObject segment = CreateMarkingCube("RoadMarkingLine_" + laneIndex + "_Seg_" + segmentIndex, center + normal * elevationOffset, Quaternion.LookRotation(tangent, normal), Mathf.Max(0.05f, currentLength), Mathf.Max(0.02f, lineThickness), parent, material);

                        segmentIndex++;
                    }
                }
            }

            laneIndex++;
        }
    }

    public static List<Vector3> SampleControlPoints(List<RoadMarkingControlPoint> points, int sampleCount, bool useBezier)
    {
        if (points == null || points.Count < 2)
            return new List<Vector3>();

        if (!useBezier || points.Count < 2)
        {
            List<Vector3> result = new List<Vector3>();
            foreach (var point in points)
            {
                result.Add(point.position);
            }
            return result;
        }

        List<Vector3> sampled = new List<Vector3>();
        int perSegmentSamples = Mathf.Max(8, sampleCount / Mathf.Max(1, points.Count - 1));

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 p0 = points[i].position;
            Vector3 p1 = points[i].isBezier ? points[i].outHandle : points[i].position;
            Vector3 p2 = points[i + 1].isBezier ? points[i + 1].inHandle : points[i + 1].position;
            Vector3 p3 = points[i + 1].position;

            for (int s = 0; s <= perSegmentSamples; s++)
            {
                float t = s / (float)perSegmentSamples;
                sampled.Add(CubicBezier(p0, p1, p2, p3, t));
            }
        }

        return sampled;
    }

    public static float GetCurveLength(List<RoadMarkingControlPoint> points, bool useBezier)
    {
        if (points == null || points.Count < 2)
            return 0f;

        List<Vector3> sampled = SampleControlPoints(points, 120, useBezier);
        float length = 0f;
        for (int i = 1; i < sampled.Count; i++)
        {
            length += Vector3.Distance(sampled[i - 1], sampled[i]);
        }
        return length;
    }

    public static Vector3[] SampleCurve(List<Vector3> points, int sampleCount, bool useBezier)
    {
        if (points == null || points.Count < 2)
            return new Vector3[0];

        int count = Mathf.Max(8, sampleCount);

        if (!useBezier || points.Count < 4)
        {
            List<Vector3> result = new List<Vector3>();
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                result.Add(GetCatmullRomPoint(points, t));
            }
            return result.ToArray();
        }

        List<Vector3> bezierPoints = new List<Vector3>();
        int segmentCount = Mathf.Max(1, (points.Count - 1) / 3);
        int samplesPerSegment = Mathf.Max(4, count / Mathf.Max(1, segmentCount));

        for (int segment = 0; segment < segmentCount; segment++)
        {
            int start = segment * 3;
            if (start + 3 >= points.Count)
                break;

            Vector3 p0 = points[start];
            Vector3 p1 = points[start + 1];
            Vector3 p2 = points[start + 2];
            Vector3 p3 = points[Mathf.Min(start + 3, points.Count - 1)];

            for (int i = 0; i <= samplesPerSegment; i++)
            {
                float t = i / (float)samplesPerSegment;
                bezierPoints.Add(CubicBezier(p0, p1, p2, p3, t));
            }
        }

        if (bezierPoints.Count == 0)
            return SampleCurve(points, count, false);

        return bezierPoints.ToArray();
    }

    private static Vector3 GetCatmullRomPoint(List<Vector3> points, float t)
    {
        float scaledTime = t * (points.Count - 1);
        int index = Mathf.FloorToInt(scaledTime);
        int nextIndex = Mathf.Min(index + 1, points.Count - 1);
        float localT = scaledTime - index;

        Vector3 p0 = index > 0 ? points[index - 1] : points[index];
        Vector3 p1 = points[index];
        Vector3 p2 = points[nextIndex];
        Vector3 p3 = nextIndex + 1 < points.Count ? points[nextIndex + 1] : points[nextIndex];

        float t2 = localT * localT;
        float t3 = t2 * localT;

        return 0.5f * (
            (2f * p1)
            + (-p0 + p2) * localT
            + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
            + (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float mt = 1f - t;
        float mt2 = mt * mt;
        float t2 = t * t;

        return mt2 * mt * p0
             + 3f * mt2 * t * p1
             + 3f * mt * t2 * p2
             + t2 * t * p3;
    }

    private static GameObject CreateMarkingCube(string name, Vector3 position, Quaternion rotation, float length, float width, Transform parent, Material material)
    {
        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        segment.name = name;
        segment.transform.SetParent(parent, false);
        segment.transform.position = position;
        segment.transform.rotation = rotation;
        segment.transform.localScale = new Vector3(Mathf.Max(0.05f, width), 0.06f, Mathf.Max(0.05f, length));

        Collider collider = segment.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        Renderer renderer = segment.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material != null ? material : CreateDefaultMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        return segment;
    }

    public static Material CreateDefaultMaterial()
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
            return null;

        Material material = new Material(shader);
        material.color = new Color(1f, 0.8f, 0.2f, 1f);
        return material;
    }
}
