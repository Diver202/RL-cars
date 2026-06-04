using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class grassManager : MonoBehaviour
{
    [System.Serializable]
    public class grassData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public int varietyIndex;
    }

    public GameObject[] grassPrefabs;
    
    [HideInInspector] 
    public List<grassData> paintedGrass = new List<grassData>();

    private class grassBatch
    {
        public Mesh mesh;
        public Material material;
        public List<Matrix4x4[]> matrices = new List<Matrix4x4[]>();
    }

    private List<grassBatch> runtimeBatches = new List<grassBatch>();

    public void buildBatches()
    {
        runtimeBatches.Clear();
        if (grassPrefabs == null || grassPrefabs.Length == 0) return;

        List<Matrix4x4>[] workingLists = new List<Matrix4x4>[grassPrefabs.Length];
        for (int i = 0; i < grassPrefabs.Length; i++) 
        {
            workingLists[i] = new List<Matrix4x4>();
        }

        foreach (grassData data in paintedGrass)
        {
            if (data.varietyIndex >= 0 && data.varietyIndex < grassPrefabs.Length)
            {
                Matrix4x4 mat = Matrix4x4.TRS(data.position, data.rotation, data.scale);
                workingLists[data.varietyIndex].Add(mat);
            }
        }

        for (int v = 0; v < grassPrefabs.Length; v++)
        {
            if (workingLists[v].Count == 0 || grassPrefabs[v] == null) continue;
            
            MeshFilter meshFilter = grassPrefabs[v].GetComponentInChildren<MeshFilter>();
            MeshRenderer meshRenderer = grassPrefabs[v].GetComponentInChildren<MeshRenderer>();
            
            if (meshFilter == null || meshRenderer == null)
            {
                Debug.LogWarning("grass prefab missing meshFilter or meshRenderer at index: " + v);
                continue;
            }

            grassBatch batch = new grassBatch { 
                mesh = meshFilter.sharedMesh, 
                material = meshRenderer.sharedMaterial 
            };
            
            List<Matrix4x4> currentChunk = new List<Matrix4x4>();
            for (int i = 0; i < workingLists[v].Count; i++)
            {
                currentChunk.Add(workingLists[v][i]);
                if (currentChunk.Count == 1023)
                {
                    batch.matrices.Add(currentChunk.ToArray());
                    currentChunk.Clear();
                }
            }
            if (currentChunk.Count > 0) batch.matrices.Add(currentChunk.ToArray());
            runtimeBatches.Add(batch);
        }
    }

    private void Start()
    {
        buildBatches();
    }

    private void Update()
    {
        if (runtimeBatches.Count == 0) return;
        
        foreach (grassBatch batch in runtimeBatches)
        {
            if (batch.mesh == null || batch.material == null) continue;
            foreach (Matrix4x4[] matArray in batch.matrices)
            {
                Graphics.DrawMeshInstanced(batch.mesh, 0, batch.material, matArray);
            }
        }
    }
}