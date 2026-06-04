using UnityEngine;
using UnityEditor;

public class TerrainPainter : EditorWindow
{
    private Color brushColor = Color.black;
    private float brushRadius = 3f;
    private bool isPainting = false;

    [MenuItem("Tools/Terrain Painter")]
    public static void showWindow()
    {
        GetWindow<TerrainPainter>("Terrain Painter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Terrain Brush Settings", EditorStyles.boldLabel);
        brushColor = EditorGUILayout.ColorField("Brush Color", brushColor);
        brushRadius = EditorGUILayout.Slider("Brush Size", brushRadius, 0.1f, 50f);

        if (GUILayout.Button(isPainting ? "Stop Painting" : "Start Painting"))
        {
            isPainting = !isPainting;
            if (isPainting)
            {
                SceneView.duringSceneGui += onSceneGUI;
            }
            else
            {
                SceneView.duringSceneGui -= onSceneGUI;
            }
        }

        if (GUILayout.Button("Flood Fill Black (Reset)"))
        {
            floodFillBlack();
        }
    }

    private void floodFillBlack()
    {
        GameObject targetGo = Selection.activeGameObject;
        if (targetGo == null) return;

        MeshFilter targetFilter = targetGo.GetComponent<MeshFilter>();
        if (targetFilter == null || targetFilter.sharedMesh == null) return;

        Mesh currentMesh = targetFilter.sharedMesh;
        
        // Clone the FBX mesh so we can safely modify it
        if (AssetDatabase.Contains(currentMesh))
        {
            Mesh clonedMesh = Instantiate(currentMesh);
            clonedMesh.name = currentMesh.name + "_Painted";
            targetFilter.sharedMesh = clonedMesh;
            currentMesh = clonedMesh;
        }

        Color[] newColors = new Color[currentMesh.vertexCount];
        for (int i = 0; i < newColors.Length; i++)
        {
            newColors[i] = Color.black;
        }
        currentMesh.colors = newColors;
    }

    private void onSceneGUI(SceneView sceneView)
    {
        if (!isPainting) return;

        Event currentEvent = Event.current;
        if (currentEvent.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }

        GameObject targetGo = Selection.activeGameObject;
        if (targetGo == null) return;

        Ray cursorRay = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
        if (Physics.Raycast(cursorRay, out RaycastHit rayHit))
        {
            if (rayHit.collider.gameObject != targetGo) return;

            Handles.color = brushColor;
            Handles.DrawWireDisc(rayHit.point, rayHit.normal, brushRadius);
            sceneView.Repaint();

            if ((currentEvent.type == EventType.MouseDrag || currentEvent.type == EventType.MouseDown) && currentEvent.button == 0)
            {
                paintMesh(targetGo.GetComponent<MeshFilter>(), rayHit.point);
                currentEvent.Use();
            }
        }
    }

    private void paintMesh(MeshFilter targetFilter, Vector3 hitPoint)
    {
        if (targetFilter == null) return;

        Mesh currentMesh = targetFilter.sharedMesh;
        if (AssetDatabase.Contains(currentMesh))
        {
            Mesh clonedMesh = Instantiate(currentMesh);
            clonedMesh.name = currentMesh.name + "_Painted";
            targetFilter.sharedMesh = clonedMesh;
            currentMesh = clonedMesh;
        }

        Vector3[] currentVertices = currentMesh.vertices;
        Color[] currentColors = currentMesh.colors;

        if (currentColors == null || currentColors.Length != currentVertices.Length)
        {
            currentColors = new Color[currentVertices.Length];
            for (int i = 0; i < currentColors.Length; i++) currentColors[i] = Color.black;
        }

        Transform objTransform = targetFilter.transform;
        bool isModified = false;

        for (int i = 0; i < currentVertices.Length; i++)
        {
            Vector3 worldPosition = objTransform.TransformPoint(currentVertices[i]);
            float distanceToCursor = Vector3.Distance(worldPosition, hitPoint);

            if (distanceToCursor <= brushRadius)
            {
                currentColors[i] = brushColor;
                isModified = true;
            }
        }

        if (isModified)
        {
            currentMesh.colors = currentColors;
        }
    }

    private void OnDestroy()
    {
        SceneView.duringSceneGui -= onSceneGUI;
    }
}