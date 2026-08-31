using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PathDrawer))]
public class PathDrawerEditor : Editor
{
    private PathDrawer targetDrawer;

    private void OnEnable()
    {
        targetDrawer = (PathDrawer)target;

        if (targetDrawer.Path == null || targetDrawer.Path.anchors == null || targetDrawer.Path.anchors.Count == 0)
        {
            targetDrawer.ResetPath();
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (targetDrawer.Path != null && targetDrawer.Path.anchors != null && targetDrawer.Path.anchors.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Anchor Editing", EditorStyles.boldLabel);

            if (targetDrawer.selectedAnchorIndex >= 0 && targetDrawer.selectedAnchorIndex < targetDrawer.Path.anchors.Count)
            {
                BezierAnchor selectedAnchor = targetDrawer.Path.anchors[targetDrawer.selectedAnchorIndex];
                EditorGUI.BeginChangeCheck();
                bool smooth = EditorGUILayout.Toggle("Smooth / Continuous", selectedAnchor.smooth);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetDrawer, "Toggle Smooth");
                    selectedAnchor.smooth = smooth;
                    if (smooth)
                    {
                        Vector3 anchorPosition = selectedAnchor.position;
                        Vector3 inDir = selectedAnchor.controlIn - anchorPosition;
                        Vector3 outDir = selectedAnchor.controlOut - anchorPosition;
                        if (inDir.sqrMagnitude > 0.0001f && outDir.sqrMagnitude > 0.0001f)
                        {
                            Vector3 mirrorIn = anchorPosition - inDir;
                            Vector3 mirrorOut = anchorPosition - outDir;
                            selectedAnchor.controlIn = mirrorIn;
                            selectedAnchor.controlOut = mirrorOut;
                        }
                    }

                    targetDrawer.Path.RecalculateLength();
                    EditorUtility.SetDirty(targetDrawer);
                    Repaint();
                }
            }
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Add Anchor"))
        {
            Undo.RecordObject(targetDrawer, "Add Anchor");
            targetDrawer.AddAnchor();
            EditorUtility.SetDirty(targetDrawer);
        }

        if (GUILayout.Button("Remove Selected Anchor"))
        {
            Undo.RecordObject(targetDrawer, "Remove Selected Anchor");
            targetDrawer.RemoveSelectedAnchor();
            EditorUtility.SetDirty(targetDrawer);
        }

        if (GUILayout.Button("Reset Path"))
        {
            Undo.RecordObject(targetDrawer, "Reset Path");
            targetDrawer.ResetPath();
            EditorUtility.SetDirty(targetDrawer);
        }

        if (GUILayout.Button("Recalculate Length"))
        {
            Undo.RecordObject(targetDrawer, "Recalculate Length");
            targetDrawer.RecalculateLength();
            EditorUtility.SetDirty(targetDrawer);
        }
    }

    private void OnSceneGUI()
    {
        if (targetDrawer == null)
        {
            return;
        }

        HandleKeyboardShortcuts();

        BezierPath path = targetDrawer.Path;
        if (path == null || path.anchors == null || path.segments == null)
        {
            return;
        }

        for (int i = 0; i < path.segments.Count; i++)
        {
            BezierSegment segment = path.segments[i];
            Vector3 worldStart = targetDrawer.LocalToWorld(segment.anchorStart);
            Vector3 worldControlStart = targetDrawer.LocalToWorld(segment.controlStart);
            Vector3 worldControlEnd = targetDrawer.LocalToWorld(segment.controlEnd);
            Vector3 worldEnd = targetDrawer.LocalToWorld(segment.anchorEnd);

            Handles.color = targetDrawer.controlColor;
            Handles.DrawLine(worldStart, worldControlStart);
            Handles.DrawLine(worldEnd, worldControlEnd);

            Handles.color = targetDrawer.curveColor;
            Handles.DrawBezier(worldStart, worldEnd, worldControlStart, worldControlEnd, targetDrawer.curveColor, null, 2f);
        }

        for (int i = 0; i < path.anchors.Count; i++)
        {
            BezierAnchor anchor = path.anchors[i];
            Vector3 worldAnchor = targetDrawer.LocalToWorld(anchor.position);
            Vector3 worldControlIn = targetDrawer.LocalToWorld(anchor.controlIn);
            Vector3 worldControlOut = targetDrawer.LocalToWorld(anchor.controlOut);

            Handles.color = targetDrawer.anchorColor;
            float anchorSize = targetDrawer.handleSize * 1.2f;
            if (Handles.Button(worldAnchor, Quaternion.identity, anchorSize, anchorSize * 1.5f, Handles.CubeHandleCap))
            {
                targetDrawer.selectedAnchorIndex = i;
            }

            if (targetDrawer.selectedAnchorIndex == i)
            {
                Handles.DrawWireCube(worldAnchor, Vector3.one * anchorSize * 1.8f);
            }

            Handles.color = targetDrawer.controlColor;
            Handles.SphereHandleCap(0, worldControlIn, Quaternion.identity, targetDrawer.handleSize * 0.75f, EventType.Repaint);
            Handles.SphereHandleCap(0, worldControlOut, Quaternion.identity, targetDrawer.handleSize * 0.75f, EventType.Repaint);

            EditorGUI.BeginChangeCheck();
            Vector3 newWorldAnchor = Handles.PositionHandle(worldAnchor, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetDrawer, "Move Anchor");

                Vector3 newLocalAnchor = targetDrawer.WorldToLocal(newWorldAnchor);
                if (!targetDrawer.allowVerticalMovement)
                {
                    newLocalAnchor.y = anchor.position.y;
                }

                Vector3 delta = newLocalAnchor - anchor.position;
                anchor.position = newLocalAnchor;
                anchor.controlIn += delta;
                anchor.controlOut += delta;

                targetDrawer.Path.RecalculateLength();
                EditorUtility.SetDirty(targetDrawer);
                Repaint();
            }

            EditorGUI.BeginChangeCheck();
            Vector3 newWorldControlIn = Handles.PositionHandle(worldControlIn, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetDrawer, "Move Control In");

                Vector3 newLocalControlIn = targetDrawer.WorldToLocal(newWorldControlIn);
                anchor.controlIn = newLocalControlIn;

                if (anchor.smooth)
                {
                    Vector3 anchorLocal = anchor.position;
                    Vector3 direction = newLocalControlIn - anchorLocal;
                    anchor.controlOut = anchorLocal - direction;
                }

                targetDrawer.Path.RecalculateLength();
                EditorUtility.SetDirty(targetDrawer);
                Repaint();
            }

            EditorGUI.BeginChangeCheck();
            Vector3 newWorldControlOut = Handles.PositionHandle(worldControlOut, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetDrawer, "Move Control Out");

                Vector3 newLocalControlOut = targetDrawer.WorldToLocal(newWorldControlOut);
                anchor.controlOut = newLocalControlOut;

                if (anchor.smooth)
                {
                    Vector3 anchorLocal = anchor.position;
                    Vector3 direction = newLocalControlOut - anchorLocal;
                    anchor.controlIn = anchorLocal - direction;
                }

                targetDrawer.Path.RecalculateLength();
                EditorUtility.SetDirty(targetDrawer);
                Repaint();
            }
        }

        if (targetDrawer.drawSamplePoints)
        {
            Handles.color = Color.magenta;
            for (int i = 0; i < path.segments.Count; i++)
            {
                BezierSegment segment = path.segments[i];

                for (int s = 0; s < segment.samples.Count; s++)
                {
                    Vector3 sampleWorld = targetDrawer.LocalToWorld(segment.samples[s].position);
                    Handles.SphereHandleCap(0, sampleWorld, Quaternion.identity, 0.06f, EventType.Repaint);
                }
            }
        }
    }

    private void HandleKeyboardShortcuts()
    {
        Event currentEvent = Event.current;
        if (currentEvent.type != EventType.KeyDown || currentEvent.control || currentEvent.command || currentEvent.alt)
        {
            return;
        }

        if (currentEvent.keyCode == KeyCode.A)
        {
            Undo.RecordObject(targetDrawer, "Add Anchor");
            targetDrawer.AddAnchor();
            EditorUtility.SetDirty(targetDrawer);
            currentEvent.Use();
        }
        else if (currentEvent.keyCode == KeyCode.Delete || currentEvent.keyCode == KeyCode.Backspace)
        {
            Undo.RecordObject(targetDrawer, "Remove Selected Anchor");
            targetDrawer.RemoveSelectedAnchor();
            EditorUtility.SetDirty(targetDrawer);
            currentEvent.Use();
        }
        else if (currentEvent.keyCode == KeyCode.S)
        {
            ToggleSelectedAnchorSmooth();
            currentEvent.Use();
        }
    }

    private void ToggleSelectedAnchorSmooth()
    {
        int index = targetDrawer.selectedAnchorIndex;
        if (index < 0 || index >= targetDrawer.Path.anchors.Count)
        {
            return;
        }

        BezierAnchor anchor = targetDrawer.Path.anchors[index];
        Undo.RecordObject(targetDrawer, "Toggle Smooth");
        anchor.smooth = !anchor.smooth;
        if (anchor.smooth)
        {
            Vector3 direction = anchor.controlOut - anchor.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                anchor.controlIn = anchor.position - direction;
            }
        }

        targetDrawer.Path.RecalculateLength();
        EditorUtility.SetDirty(targetDrawer);
        Repaint();
    }
}
