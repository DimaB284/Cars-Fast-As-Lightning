using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[System.Serializable]
public struct BorderZone
{
    [Range(0f, 1f)] public float start; // Початок зони (від 0 до 1)
    [Range(0f, 1f)] public float end;   // Кінець зони (від 0 до 1)
}

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FlatRoadBuilder : MonoBehaviour
{
    [Header("Dependencies")]
    public SplineContainer splineContainer;

    [Header("Road Settings")]
    [Range(10, 1000)] public int resolution = 200; 
    public float roadWidth = 6f; 

    [Header("Border Settings")]
    public float borderHeight = 0.5f; 
    public float borderWidth = 0.5f; 
    public List<BorderZone> leftBorderZones;  // Спити зон для лівого бортика
    public List<BorderZone> rightBorderZones; // Списки зон для правого бортика

    private MeshFilter meshFilter;

    void Update()
    {
        if (splineContainer != null) BuildRoad();
    }

    void BuildRoad()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        
        Mesh mesh = new Mesh();
        mesh.name = "ProceduralTrack";

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        Spline spline = splineContainer.Splines[0];
        
        for (int i = 0; i <= resolution; i++)
        {
            float t = (float)i / resolution;
            
            splineContainer.Evaluate(t, out float3 pos, out float3 tangent, out float3 up);
            
            Vector3 position = pos;
            Vector3 forward = math.normalize(tangent);
            Vector3 normal = math.normalize(up);
            Vector3 right = Vector3.Cross(normal, forward).normalized;

            // Перевіряємо, чи має бути бортик у цій точці t
            float currentLeftHeight = IsInZone(t, leftBorderZones) ? borderHeight : 0f;
            float currentRightHeight = IsInZone(t, rightBorderZones) ? borderHeight : 0f;

            // Вершини профілю
            // Якщо висота 0, бортик просто "згортається" в лінію краю дороги
            Vector3 p0 = position - right * (roadWidth / 2f + borderWidth) + normal * currentLeftHeight; 
            Vector3 p1 = position - right * (roadWidth / 2f); 
            Vector3 p2 = position + right * (roadWidth / 2f); 
            Vector3 p3 = position + right * (roadWidth / 2f + borderWidth) + normal * currentRightHeight;

            verts.Add(p0);
            verts.Add(p1);
            verts.Add(p2);
            verts.Add(p3);

            if (i < resolution)
            {
                int root = i * 4;
                AddQuad(tris, root, root + 4, root + 1, root + 5);
                AddQuad(tris, root + 1, root + 5, root + 2, root + 6);
                AddQuad(tris, root + 2, root + 6, root + 3, root + 7);
            }
        }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        meshFilter.sharedMesh = mesh;
    }

    bool IsInZone(float t, List<BorderZone> zones)
    {
        if (zones == null) return false;
        foreach (var zone in zones)
        {
            if (t >= zone.start && t <= zone.end) return true;
        }
        return false;
    }

    void AddQuad(List<int> tris, int v0, int v1, int v2, int v3)
    {
        tris.Add(v0); tris.Add(v1); tris.Add(v2);
        tris.Add(v2); tris.Add(v1); tris.Add(v3);
    }
}