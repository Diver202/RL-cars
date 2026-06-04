using UnityEngine;
using UnityEditor;

public class grassPainter : EditorWindow
{
    private grassManager targetManager;
    private Material targetMaterial;
    
    private float brushSize = 3f;
    private int paintDensity = 5;
    private float sizeMultiplier = 1f;
    private bool isPainting = false;

    private enum paintMode { paint, erase }
    private paintMode currentMode = paintMode.paint;

    [MenuItem("Tools/GPU Grass Painter")]
    public static void showWindow()
    {
        GetWindow<grassPainter>("GPU Grass Painter");
    }

    private void OnEnable()
    {
        Undo.undoRedoPerformed += onUndoRedo;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= onUndoRedo;
        SceneView.duringSceneGui -= onSceneGUI;
    }

    private void onUndoRedo()
    {
        if (targetManager != null)
        {
            targetManager.buildBatches();
            SceneView.RepaintAll();
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Instanced Grass Brush", EditorStyles.boldLabel);
        
        targetManager = (grassManager)EditorGUILayout.ObjectField("Grass Manager", targetManager, typeof(grassManager), true);
        targetMaterial = (Material)EditorGUILayout.ObjectField("Paintable Material", targetMaterial, typeof(Material), false);
        
        if (targetManager == null)
        {
            EditorGUILayout.HelpBox("assign the grassManager component from your terrain.", MessageType.Warning);
            return;
        }

        if (targetMaterial == null)
        {
            EditorGUILayout.HelpBox("assign terrainMat to restrict painting only to that material.", MessageType.Info);
        }

        brushSize = EditorGUILayout.Slider("Brush Size", brushSize, 1f, 20f);
        paintDensity = EditorGUILayout.IntSlider("Density (per click)", paintDensity, 1, 20);
        sizeMultiplier = EditorGUILayout.Slider("Size Multiplier", sizeMultiplier, 0.1f, 10f);

        GUILayout.Space(10);
        currentMode = (paintMode)GUILayout.Toolbar((int)currentMode, new string[] { "Paint", "Erase" });
        GUILayout.Space(10);

        if (GUILayout.Button(isPainting ? "Stop Painting" : "Start Painting"))
        {
            isPainting = !isPainting;
            if (isPainting) SceneView.duringSceneGui += onSceneGUI;
            else SceneView.duringSceneGui -= onSceneGUI;
        }

        if (GUILayout.Button("Clear All Painted Grass"))
        {
            Undo.RecordObject(targetManager, "Clear Grass");
            targetManager.paintedGrass.Clear();
            targetManager.buildBatches();
            EditorUtility.SetDirty(targetManager);
        }
    }

    private void onSceneGUI(SceneView sceneView)
    {
        if (!isPainting || targetManager == null) return;

        Event currentEvent = Event.current;
        if (currentEvent.type == EventType.Layout) HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Ray mouseRay = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
        if (Physics.Raycast(mouseRay, out RaycastHit centerHit))
        {
            Handles.color = currentMode == paintMode.paint ? Color.green : Color.red;
            Handles.DrawWireDisc(centerHit.point, centerHit.normal, brushSize);
            sceneView.Repaint();

            if ((currentEvent.type == EventType.MouseDrag || currentEvent.type == EventType.MouseDown) && currentEvent.button == 0)
            {
                if (currentMode == paintMode.paint)
                {
                    paintGrassData(centerHit);
                }
                else
                {
                    eraseGrassData(centerHit);
                }
                currentEvent.Use();
            }
        }
    }

    private void eraseGrassData(RaycastHit centerHit)
    {
        Undo.RecordObject(targetManager, "Erase Grass");
        bool hasChanged = false;

        for (int i = targetManager.paintedGrass.Count - 1; i >= 0; i--)
        {
            if (Vector3.Distance(targetManager.paintedGrass[i].position, centerHit.point) <= brushSize)
            {
                targetManager.paintedGrass.RemoveAt(i);
                hasChanged = true;
            }
        }

        if (hasChanged)
        {
            targetManager.buildBatches();
            EditorUtility.SetDirty(targetManager);
        }
    }

    private void paintGrassData(RaycastHit centerHit)
    {
        if (targetManager.grassPrefabs == null || targetManager.grassPrefabs.Length == 0) return;

        Undo.RecordObject(targetManager, "Paint Grass");
        bool hasChanged = false;

        for (int i = 0; i < paintDensity; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * brushSize;
            Vector3 rayStartPos = new Vector3(centerHit.point.x + randomOffset.x, centerHit.point.y + 1000f, centerHit.point.z + randomOffset.y);
            
            if (Physics.Raycast(new Ray(rayStartPos, Vector3.down), out RaycastHit surfaceHit, 2000f))
            {
                bool isCorrectMaterial = true;
                
                // Calculates the specific material on the exact polygon that was hit
                if (targetMaterial != null)
                {
                    Material hitMat = getMaterialFromHit(surfaceHit);
                    isCorrectMaterial = (hitMat == targetMaterial);
                }

                if (surfaceHit.collider.gameObject == targetManager.gameObject && isCorrectMaterial)
                {
                    grassManager.grassData newData = new grassManager.grassData();
                    newData.position = surfaceHit.point;
                    newData.rotation = Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f);
                    newData.scale = Vector3.one * 100f * sizeMultiplier * Random.Range(0.85f, 1.15f);
                    newData.varietyIndex = Random.Range(0, targetManager.grassPrefabs.Length);
                    
                    targetManager.paintedGrass.Add(newData);
                    hasChanged = true;
                }
            }
        }
        
        if (hasChanged)
        {
            targetManager.buildBatches();
            EditorUtility.SetDirty(targetManager);
        }
    }

    private Material getMaterialFromHit(RaycastHit hit)
    {
        MeshCollider meshCollider = hit.collider as MeshCollider;
        Renderer meshRenderer = hit.collider.GetComponent<Renderer>();
        
        if (meshCollider == null || meshCollider.sharedMesh == null || meshRenderer == null) 
            return null;

        Mesh mesh = meshCollider.sharedMesh;
        
        // Multiply by 3 because each triangle consists of 3 index points in the array
        int limit = hit.triangleIndex * 3;
        int submesh = 0;
        
        // Subtract the triangle count of each submesh until we drop below zero
        while (submesh < mesh.subMeshCount)
        {
            int indexCount = (int)mesh.GetIndexCount(submesh);
            limit -= indexCount;
            if (limit < 0) break;
            submesh++;
        }
        
        if (submesh >= 0 && submesh < meshRenderer.sharedMaterials.Length)
            return meshRenderer.sharedMaterials[submesh];
            
        return meshRenderer.sharedMaterial;
    }
}