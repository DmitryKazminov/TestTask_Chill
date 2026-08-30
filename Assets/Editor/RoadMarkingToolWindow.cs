using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RoadMarkingToolWindow : EditorWindow
{
    private enum EditMode
    {
        AddPoint,
        DeletePoint,
        MovePoint,
        ToggleBezier,
        EditHandles
    }

    private const string WindowTitle = "Road Marking Tool";

    private GameObject selectedPlane;
    private Material markingMaterial;
    private RoadMarkingPatternType patternType = RoadMarkingPatternType.Solid;
    private float roadWidth = 4f;
    private int lineCount = 1;
    private float lineThickness = 0.12f;
    private float lineSpacing = 1.5f;
    private float minLineSpacing = 0.5f;
    private float maxLinesPerMeter = 3f;
    private float elevationOffset = 0.01f;
    private int curveSamples = 36;
    private float maxSegmentLength = 1.5f;
    private bool useBezier = true;
    private bool autoRaise = true;
    private bool penToolActive;
    private EditMode currentMode = EditMode.AddPoint;
    private readonly List<RoadMarkingControlPoint> controlPoints = new List<RoadMarkingControlPoint>();
    private GameObject previewRoot;
    private int selectedPointIndex = -1;
    private int selectedHandleIndex = -1;

    [MenuItem("Tools/Road Marking Tool")]
    public static void OpenWindow()
    {
        var window = GetWindow<RoadMarkingToolWindow>(WindowTitle);
        window.minSize = new Vector2(380f, 560f);
        window.Show();
    }

    private void OnEnable()
    {
        selectedPlane = Selection.activeGameObject;
        UpdateRoadWidthFromSelection();
        if (markingMaterial == null)
        {
            markingMaterial = RoadMarkingGenerator.CreateDefaultMaterial();
        }

        SceneView.duringSceneGui += OnSceneGUI;
        Selection.selectionChanged += OnSelectionChanged;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        Selection.selectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged()
    {
        if (Selection.activeGameObject == null)
            return;

        var definition = Selection.activeGameObject.GetComponent<RoadMarkingDefinition>();
        if (definition == null)
            definition = Selection.activeGameObject.GetComponentInParent<RoadMarkingDefinition>();

        if (definition != null)
        {
            selectedPlane = definition.transform.parent != null ? definition.transform.parent.gameObject : Selection.activeGameObject;
            LoadFromDefinition(definition);
            Repaint();
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Road Marking Tool", EditorStyles.boldLabel);
        GUILayout.Space(6f);

        selectedPlane = (GameObject)EditorGUILayout.ObjectField("Plane / parent", selectedPlane, typeof(GameObject), true);
        markingMaterial = (Material)EditorGUILayout.ObjectField("Material", markingMaterial, typeof(Material), true);

        GUILayout.BeginVertical("box");
        GUILayout.Label("Editor activation", EditorStyles.miniBoldLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Off", penToolActive ? EditorStyles.miniButtonLeft : EditorStyles.toolbarButton))
        {
            penToolActive = false;
            Tools.current = Tool.Move;
        }
        if (GUILayout.Button("Pen", !penToolActive ? EditorStyles.miniButtonMid : EditorStyles.toolbarButton))
        {
            penToolActive = true;
            Tools.current = Tool.None;
        }
        if (GUILayout.Button("Scene", EditorStyles.miniButtonRight))
        {
            penToolActive = false;
            Tools.current = Tool.View;
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.BeginVertical("box");
        GUILayout.Label("Editing mode", EditorStyles.miniBoldLabel);
        currentMode = (EditMode)GUILayout.Toolbar((int)currentMode, new[] { "Add", "Delete", "Move", "Bezier", "Handles" });
        GUILayout.EndVertical();

        patternType = (RoadMarkingPatternType)EditorGUILayout.EnumPopup("Marking type", patternType);
        roadWidth = EditorGUILayout.FloatField("Road width", roadWidth);
        lineThickness = EditorGUILayout.FloatField("Line thickness", lineThickness);
        lineCount = Mathf.Max(1, EditorGUILayout.IntField("Line count", lineCount));
        lineSpacing = EditorGUILayout.FloatField("Line spacing", lineSpacing);
        minLineSpacing = EditorGUILayout.FloatField("Min line spacing", minLineSpacing);
        maxLinesPerMeter = EditorGUILayout.FloatField("Max lines / meter", maxLinesPerMeter);
        elevationOffset = EditorGUILayout.FloatField("Elevation offset", elevationOffset);
        curveSamples = Mathf.Max(8, EditorGUILayout.IntField("Curve samples", curveSamples));
        maxSegmentLength = Mathf.Max(0.15f, EditorGUILayout.FloatField("Max segment length", maxSegmentLength));
        useBezier = EditorGUILayout.Toggle("Smooth with Bézier", useBezier);
        autoRaise = EditorGUILayout.Toggle("Create raised layer", autoRaise);

        if (GUILayout.Button("Use selected object"))
        {
            selectedPlane = Selection.activeGameObject;
            if (selectedPlane != null)
            {
                selectedPlane = Selection.activeGameObject;
            }
        }

        if (GUILayout.Button("Use plate width"))
        {
            UpdateRoadWidthFromSelection();
        }

        if (GUILayout.Button("Load selected marking"))
        {
            TryLoadSelectedMarking();
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Preview"))
        {
            PreviewMarking();
        }
        if (GUILayout.Button("Create"))
        {
            CreateMarking();
        }
        if (GUILayout.Button("Cancel"))
        {
            CancelPreview();
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Clear current curve"))
        {
            ClearCurve();
        }

        GUILayout.Space(8f);
        EditorGUILayout.HelpBox(
            "Draw a path in the Scene view. The tool supports solid, dashed, double, center and shoulder lane styles, auto-calculates road width from the selected plane, and can reload a previously created marking for further editing.",
            MessageType.Info);
        GUILayout.Space(4f);
        EditorGUILayout.LabelField("Control points", controlPoints.Count.ToString());
    }

    private void UpdateRoadWidthFromSelection()
    {
        if (selectedPlane == null)
            return;

        float detected = RoadMarkingGenerator.GetRoadWidth(selectedPlane);
        roadWidth = Mathf.Max(0.5f, detected);
    }

    private void TryLoadSelectedMarking()
    {
        GameObject current = Selection.activeGameObject;
        if (current == null)
            return;

        RoadMarkingDefinition definition = current.GetComponent<RoadMarkingDefinition>();
        if (definition == null)
            definition = current.GetComponentInParent<RoadMarkingDefinition>();

        if (definition == null)
        {
            Debug.LogWarning("RoadMarkingToolWindow: selected object is not a road marking.");
            return;
        }

        selectedPlane = definition.transform.parent != null ? definition.transform.parent.gameObject : current;
        LoadFromDefinition(definition);
    }

    private void LoadFromDefinition(RoadMarkingDefinition definition)
    {
        if (definition == null)
            return;

        patternType = definition.PatternType;
        roadWidth = definition.RoadWidth;
        lineThickness = definition.LineThickness;
        lineCount = definition.LineCount;
        lineSpacing = definition.LineSpacing;
        minLineSpacing = definition.MinLineSpacing;
        maxLinesPerMeter = definition.MaxLinesPerMeter;
        elevationOffset = definition.ElevationOffset;
        curveSamples = definition.CurveSamples;
        maxSegmentLength = definition.MaxSegmentLength;
        useBezier = definition.UseBezier;
        autoRaise = definition.AutoRaise;

        controlPoints.Clear();
        var points = definition.GetControlPoints();
        for (int i = 0; i < points.Count; i++)
        {
            controlPoints.Add(points[i]);
        }

        if (previewRoot != null)
        {
            DestroyImmediate(previewRoot);
            previewRoot = null;
        }
    }

    private bool IsDrawingAllowed()
    {
        if (!penToolActive)
            return false;

        return Tools.current == Tool.None || Tools.current == Tool.View;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (selectedPlane == null)
            return;

        Event e = Event.current;
        if (e.button == 2)
            return;

        if (!IsDrawingAllowed())
            return;

        Plane plane = new Plane(selectedPlane.transform.up, selectedPlane.transform.position + selectedPlane.transform.up * 0.01f);

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            if (currentMode == EditMode.AddPoint)
            {
                if (plane.Raycast(ray, out float enter))
                {
                    Vector3 point = ray.GetPoint(enter);
                    if (controlPoints.Count == 0 || Vector3.Distance(controlPoints[controlPoints.Count - 1].position, point) > 0.03f)
                    {
                        controlPoints.Add(new RoadMarkingControlPoint(point));
                        selectedPointIndex = controlPoints.Count - 1;
                        e.Use();
                        Repaint();
                        RefreshPreview();
                    }
                }
                return;
            }

            if (currentMode == EditMode.DeletePoint)
            {
                int index = GetNearestPointIndex(ray, plane);
                if (index >= 0)
                {
                    controlPoints.RemoveAt(index);
                    selectedPointIndex = -1;
                    selectedHandleIndex = -1;
                    e.Use();
                    RefreshPreview();
                }
                return;
            }

            if (currentMode == EditMode.ToggleBezier)
            {
                int index = GetNearestPointIndex(ray, plane);
                if (index >= 0)
                {
                    var point = controlPoints[index];
                    point.isBezier = !point.isBezier;
                    if (point.isBezier)
                    {
                        Vector3 tangent = GetTangentForPoint(index);
                        Vector3 offset = tangent.normalized * 0.6f;
                        point.inHandle = point.position - offset;
                        point.outHandle = point.position + offset;
                    }
                    controlPoints[index] = point;
                    selectedPointIndex = index;
                    e.Use();
                    RefreshPreview();
                }
                return;
            }

            if (currentMode == EditMode.MovePoint || currentMode == EditMode.EditHandles)
            {
                int pointIndex = GetNearestPointIndex(ray, plane);
                selectedPointIndex = pointIndex;
                if (pointIndex >= 0)
                {
                    if (currentMode == EditMode.EditHandles)
                    {
                        int handleIndex = GetNearestHandleIndex(pointIndex, ray);
                        selectedHandleIndex = handleIndex;
                    }
                    else
                    {
                        selectedHandleIndex = -1;
                    }

                    e.Use();
                    Repaint();
                }
            }
        }

        if (e.type == EventType.MouseDrag && selectedPointIndex >= 0)
        {
            Ray dragRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (plane.Raycast(dragRay, out float enter))
            {
                Vector3 dragPoint = dragRay.GetPoint(enter);
                if (currentMode == EditMode.EditHandles && selectedHandleIndex >= 0)
                {
                    var point = controlPoints[selectedPointIndex];
                    if (selectedHandleIndex == 0)
                    {
                        point.inHandle = dragPoint;
                    }
                    else if (selectedHandleIndex == 1)
                    {
                        point.outHandle = dragPoint;
                    }
                    controlPoints[selectedPointIndex] = point;
                }
                else
                {
                    var point = controlPoints[selectedPointIndex];
                    point.position = dragPoint;
                    if (point.isBezier)
                    {
                        Vector3 delta = dragPoint - point.position;
                        point.inHandle += delta;
                        point.outHandle += delta;
                    }
                    controlPoints[selectedPointIndex] = point;
                }

                e.Use();
                Repaint();
                RefreshPreview();
            }
        }

        if (e.type == EventType.MouseUp)
        {
            selectedHandleIndex = -1;
        }

        DrawCurvePreview();
    }

    private void DrawCurvePreview()
    {
        if (controlPoints.Count < 2)
            return;

        List<Vector3> previewPath = RoadMarkingGenerator.SampleControlPoints(controlPoints, curveSamples, useBezier);
        if (previewPath.Count > 1)
        {
            Handles.color = new Color(0.2f, 0.9f, 1f, 1f);
            Handles.DrawAAPolyLine(previewPath.ToArray());
        }

        for (int i = 0; i < controlPoints.Count; i++)
        {
            var point = controlPoints[i];
            Handles.color = i == selectedPointIndex ? Color.yellow : new Color(1f, 0.6f, 0.2f, 1f);
            Handles.SphereHandleCap(i, point.position, Quaternion.identity, 0.12f, EventType.Repaint);

            if (point.isBezier)
            {
                Handles.color = Color.magenta;
                Handles.DrawDottedLine(point.position, point.inHandle, 2f);
                Handles.DrawDottedLine(point.position, point.outHandle, 2f);
                Handles.SphereHandleCap(i + 1000, point.inHandle, Quaternion.identity, 0.08f, EventType.Repaint);
                Handles.SphereHandleCap(i + 2000, point.outHandle, Quaternion.identity, 0.08f, EventType.Repaint);
            }
        }
    }

    private int GetNearestPointIndex(Ray ray, Plane plane)
    {
        int nearest = -1;
        float nearestDistance = 0.3f;

        for (int i = 0; i < controlPoints.Count; i++)
        {
            Vector3 point = controlPoints[i].position;
            Vector3 projected = GetClosestPointOnRay(ray, point);
            float distance = Vector3.Distance(projected, point);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = i;
            }
        }

        return nearest;
    }

    private int GetNearestHandleIndex(int pointIndex, Ray ray)
    {
        var point = controlPoints[pointIndex];
        int nearest = -1;
        float nearestDistance = 0.5f;

        float inDist = Vector3.Distance(GetClosestPointOnRay(ray, point.inHandle), point.inHandle);
        float outDist = Vector3.Distance(GetClosestPointOnRay(ray, point.outHandle), point.outHandle);

        if (point.isBezier)
        {
            if (inDist < nearestDistance)
            {
                nearestDistance = inDist;
                nearest = 0;
            }
            if (outDist < nearestDistance)
            {
                nearestDistance = outDist;
                nearest = 1;
            }
        }

        return nearest;
    }

    private Vector3 GetClosestPointOnRay(Ray ray, Vector3 point)
    {
        Plane tempPlane = new Plane(Vector3.up, point);
        if (tempPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return point;
    }

    private Vector3 GetTangentForPoint(int index)
    {
        if (controlPoints.Count <= 1)
            return Vector3.right;

        if (index == 0)
        {
            return (controlPoints[1].position - controlPoints[0].position).normalized;
        }

        if (index == controlPoints.Count - 1)
        {
            return (controlPoints[index].position - controlPoints[index - 1].position).normalized;
        }

        return (controlPoints[index + 1].position - controlPoints[index - 1].position).normalized;
    }

    private void PreviewMarking()
    {
        if (selectedPlane == null)
        {
            Debug.LogWarning("RoadMarkingToolWindow: select a road/plane first.");
            return;
        }

        if (controlPoints.Count < 2)
        {
            Debug.LogWarning("RoadMarkingToolWindow: create at least two points before previewing the marking.");
            return;
        }

        RefreshPreview();
    }

    private void CreateMarking()
    {
        if (selectedPlane == null)
        {
            Debug.LogWarning("RoadMarkingToolWindow: select a road/plane first.");
            return;
        }

        if (controlPoints.Count < 2)
        {
            Debug.LogWarning("RoadMarkingToolWindow: create at least two points before generating the marking.");
            return;
        }

        if (previewRoot != null)
        {
            DestroyImmediate(previewRoot);
            previewRoot = null;
        }

        Material material = markingMaterial != null ? markingMaterial : RoadMarkingGenerator.CreateDefaultMaterial();
        List<Vector3> sampledCurve = RoadMarkingGenerator.SampleControlPoints(controlPoints, curveSamples, useBezier);
        float length = RoadMarkingGenerator.GetCurveLength(controlPoints, useBezier);
        int safeLineCount = RoadMarkingGenerator.GetAllowedLineCount(length, lineCount, maxLinesPerMeter, minLineSpacing);

        GameObject root = RoadMarkingGenerator.CreateMarkingGroup(
            sampledCurve,
            selectedPlane.transform,
            selectedPlane.transform.up,
            material,
            lineThickness,
            safeLineCount,
            lineSpacing,
            curveSamples,
            maxSegmentLength,
            useBezier,
            autoRaise ? elevationOffset : 0f,
            patternType,
            roadWidth);

        if (root == null)
        {
            Debug.LogWarning("RoadMarkingToolWindow: failed to build the road marking.");
            return;
        }

        var definition = root.GetComponent<RoadMarkingDefinition>();
        definition.SetControlPoints(controlPoints);
        definition.PatternType = patternType;
        definition.RoadWidth = roadWidth;
        definition.LineThickness = lineThickness;
        definition.LineCount = safeLineCount;
        definition.LineSpacing = lineSpacing;
        definition.MinLineSpacing = minLineSpacing;
        definition.MaxLinesPerMeter = maxLinesPerMeter;
        definition.ElevationOffset = elevationOffset;
        definition.CurveSamples = curveSamples;
        definition.MaxSegmentLength = maxSegmentLength;
        definition.UseBezier = useBezier;
        definition.AutoRaise = autoRaise;

        root.transform.SetParent(selectedPlane.transform, false);
        root.name = "RoadMarking_" + selectedPlane.name;
        Selection.activeGameObject = root;
        Debug.Log("Road marking created: " + root.name + " with " + safeLineCount + " lane lines and pattern: " + patternType.ToString());
    }

    private void CancelPreview()
    {
        if (previewRoot != null)
        {
            DestroyImmediate(previewRoot);
            previewRoot = null;
        }

        Repaint();
    }

    private void ClearCurve()
    {
        controlPoints.Clear();
        selectedPointIndex = -1;
        selectedHandleIndex = -1;
        CancelPreview();
    }

    private void RefreshPreview()
    {
        if (selectedPlane == null || controlPoints.Count < 2)
            return;

        if (previewRoot != null)
        {
            DestroyImmediate(previewRoot);
            previewRoot = null;
        }

        Material material = markingMaterial != null ? markingMaterial : RoadMarkingGenerator.CreateDefaultMaterial();
        List<Vector3> sampledCurve = RoadMarkingGenerator.SampleControlPoints(controlPoints, curveSamples, useBezier);
        float length = RoadMarkingGenerator.GetCurveLength(controlPoints, useBezier);
        int safeLineCount = RoadMarkingGenerator.GetAllowedLineCount(length, lineCount, maxLinesPerMeter, minLineSpacing);

        GameObject root = RoadMarkingGenerator.CreateMarkingGroup(
            sampledCurve,
            selectedPlane.transform,
            selectedPlane.transform.up,
            material,
            lineThickness,
            safeLineCount,
            lineSpacing,
            curveSamples,
            maxSegmentLength,
            useBezier,
            autoRaise ? elevationOffset : 0f,
            patternType,
            Mathf.Max(roadWidth, RoadMarkingGenerator.GetRoadWidth(selectedPlane)));

        if (root == null)
            return;

        root.transform.SetParent(selectedPlane.transform, false);
        root.name = "RoadMarking_Preview";
        previewRoot = root;
    }
}
