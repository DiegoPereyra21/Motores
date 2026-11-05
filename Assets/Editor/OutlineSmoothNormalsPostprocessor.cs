using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//guarda en el canal UV4 las normales suavizadas de cada malla importada, para que el outline no tenga huecos en bordes duros
public class OutlineSmoothNormalsPostprocessor : AssetPostprocessor
{
    private void OnPostprocessModel(GameObject root)
    {
        foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>())
            Bake(mf.sharedMesh);

        foreach (SkinnedMeshRenderer smr in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            Bake(smr.sharedMesh);
    }

    private static void Bake(Mesh mesh)
    {
        if (mesh == null) return;

        Vector3[] vertices = mesh.vertices;
        Dictionary<Vector3, Vector3> sums = new();

        for (int s = 0; s < mesh.subMeshCount; s++)
        {
            int[] tris = mesh.GetTriangles(s);

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 a = vertices[tris[i]];
                Vector3 b = vertices[tris[i + 1]];
                Vector3 c = vertices[tris[i + 2]];

                Vector3 faceNormal = Vector3.Cross(b - a, c - a);
                if (faceNormal.sqrMagnitude < 1e-12f) continue;
                faceNormal.Normalize();

                //cada esquina aporta segun su angulo, asi las caras muy subdivididas no pesan de mas
                Add(sums, a, faceNormal * Vector3.Angle(b - a, c - a));
                Add(sums, b, faceNormal * Vector3.Angle(c - b, a - b));
                Add(sums, c, faceNormal * Vector3.Angle(a - c, b - c));
            }
        }

        List<Vector3> smooth = new(vertices.Length);
        foreach (Vector3 v in vertices)
            smooth.Add(sums.TryGetValue(v, out Vector3 n) ? n.normalized : Vector3.zero);

        mesh.SetUVs(3, smooth);
    }

    private static void Add(Dictionary<Vector3, Vector3> sums, Vector3 position, Vector3 weightedNormal)
    {
        sums.TryGetValue(position, out Vector3 current);
        sums[position] = current + weightedNormal;
    }
}