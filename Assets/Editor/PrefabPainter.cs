using UnityEngine;
using UnityEditor;

public class SimplePrefabPainter : EditorWindow
{
    public GameObject[] prefabsToPaint; 
    
    private float brushSize = 5f;
    private float spacingAmount = 2f;
    private bool isPainting = false;
    private Vector3 lastSpawnPos;
    private Transform parentFolder;

    private SerializedObject serializedObj;
    private SerializedProperty prefabsProperty;

    [MenuItem("Tools/Simple Prefab Painter")]
    public static void showWindow()
    {
        GetWindow<SimplePrefabPainter>("Simple Prefab Painter");
    }

    private void OnEnable()
    {
        serializedObj = new SerializedObject(this);
        prefabsProperty = serializedObj.FindProperty("prefabsToPaint");
    }

    private void OnGUI()
    {
        GUILayout.Label("Simple Painter Settings", EditorStyles.boldLabel);
        
        serializedObj.Update();
        EditorGUILayout.PropertyField(prefabsProperty, true);
        serializedObj.ApplyModifiedProperties();

        brushSize = EditorGUILayout.Slider("Brush Size", brushSize, 1f, 50f);
        spacingAmount = EditorGUILayout.Slider("Tree Spacing", spacingAmount, 0.5f, 20f);

        if (GUILayout.Button(isPainting ? "Stop Painting" : "Start Painting"))
        {
            isPainting = !isPainting;
            if (isPainting)
            {
                SceneView.duringSceneGui += onSceneGUI;
                setupParentFolder();
            }
            else
            {
                SceneView.duringSceneGui -= onSceneGUI;
            }
        }
    }

    private void setupParentFolder()
    {
        if (parentFolder == null)
        {
            GameObject folderObj = GameObject.Find("PaintedFoliage");
            if (folderObj == null)
            {
                folderObj = new GameObject("PaintedFoliage");
            }
            parentFolder = folderObj.transform;
        }
    }

    private void onSceneGUI(SceneView sceneView)
    {
        if (!isPainting || prefabsToPaint == null || prefabsToPaint.Length == 0) return;

        Event currentEvent = Event.current;
        
        if (currentEvent.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }

        Ray mouseRay = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
        if (Physics.Raycast(mouseRay, out RaycastHit centerHit))
        {
            Handles.color = Color.green;
            Handles.DrawWireDisc(centerHit.point, centerHit.normal, brushSize);
            sceneView.Repaint();

            if ((currentEvent.type == EventType.MouseDrag || currentEvent.type == EventType.MouseDown) && currentEvent.button == 0)
            {
                if (Vector3.Distance(centerHit.point, lastSpawnPos) > spacingAmount)
                {
                    spawnRandomPrefab(centerHit, centerHit.collider);
                    lastSpawnPos = centerHit.point;
                    currentEvent.Use(); 
                }
            }
        }
    }

    private void spawnRandomPrefab(RaycastHit centerHit, Collider terrainCollider)
    {
        if (prefabsToPaint.Length == 0) return;

        int randomIndex = Random.Range(0, prefabsToPaint.Length);
        GameObject selectedPrefab = prefabsToPaint[randomIndex];
        
        if (selectedPrefab == null) return;

        Vector2 randomOffset = Random.insideUnitCircle * brushSize;
        
        Vector3 rayStartPos = new Vector3(centerHit.point.x + randomOffset.x, centerHit.point.y + 1000f, centerHit.point.z + randomOffset.y);
        Ray verticalRay = new Ray(rayStartPos, Vector3.down);

        if (terrainCollider.Raycast(verticalRay, out RaycastHit surfaceHit, 2000f))
        {
            GameObject newTree = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
            if (newTree == null) return;

            // Direct placement with a simple Y-axis random spin and slight natural scaling
            newTree.transform.position = surfaceHit.point;
            newTree.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            
            float randomScale = Random.Range(0.85f, 1.15f);
            newTree.transform.localScale = Vector3.one * randomScale; 

            newTree.transform.parent = parentFolder;
            Undo.RegisterCreatedObjectUndo(newTree, "Paint Tree");
        }
    }

    private void OnDestroy()
    {
        SceneView.duringSceneGui -= onSceneGUI;
    }
}