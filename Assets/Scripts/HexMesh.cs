using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct EdgeVertices
{
    public Vector3 v1, v2, v3, v4, v5;
    public EdgeVertices(Vector3 corner1, Vector3 corner2)
    {
        v1 = corner1;
        v2 = Vector3.Lerp(corner1, corner2, 0.25f);
        v3 = Vector3.Lerp(corner1, corner2, 0.5f);
        v4 = Vector3.Lerp(corner1, corner2, 0.75f);
        v5 = corner2;
    }
    public EdgeVertices(Vector3 corner1, Vector3 corner2, float outerStep)
    {
        v1 = corner1;
        v2 = Vector3.Lerp(corner1, corner2, outerStep);
        v3 = Vector3.Lerp(corner1, corner2, 0.5f);
        v4 = Vector3.Lerp(corner1, corner2, 1f - outerStep);
        v5 = corner2;
    }
}

public static class ListPool<T> // 只构造一次的全局类实例
{
    static Stack<List<T>> stack = new Stack<List<T>>();
    public static List<T> Get()
    {
        if (stack.Count > 0) return stack.Pop();
        return new List<T>();
    }
    public static void Add(List<T> list)
    {
        list.Clear();
        stack.Push(list); // 回收利用
    }
}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))] // It requires a mesh filter and renderer
public class HexMesh : MonoBehaviour
{
    Mesh hexMesh;
    //使Unity就不会在重编译时保存它们
    [System.NonSerialized] List<Vector3> vertices, terrainTypes;
    [System.NonSerialized] List<Color> colors;
    [System.NonSerialized] List<int> triangles;

    MeshCollider meshCollider;
    public bool useCollider, useColor; // terrain必须为true
    public bool useUVCoordinates; // 水流必须为true
    public bool useUV2Coordinates; // 瀑布河口需要设为true，需要；两个uv集混合效果
    public bool useTerrainTypes;
    [System.NonSerialized] List<Vector2> uvs, uv2s;

    void showList()
    {
        for(int i = 0; i < vertices.Count; ++i)
        {
            Debug.Log("vertice[" + i.ToString() + "] = " + vertices[i].ToString());
        }
        for (int i = 0; i < triangles.Count; ++i)
        {
            Debug.Log("triangle[" + i.ToString() + "] = " + triangles[i].ToString());
        }
        for (int i = 0; i < colors.Count; ++i)
        {
            Debug.Log("colors[" + i.ToString() + "] = " + colors[i].ToString());
        }
    }

    private void Awake()
    {
        GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
        if (useCollider) {
            meshCollider = gameObject.AddComponent<MeshCollider>(); // 加入MeshCollider组件，用于收集接收到的碰撞信息
        }
        hexMesh.name = "Hex Mesh";
    }

    public void Clear()
    {
        hexMesh.Clear();
        vertices = ListPool<Vector3>.Get();
        if (useColor) {
            colors = ListPool<Color>.Get();
        }
        if (useUVCoordinates) {
            uvs = ListPool<Vector2>.Get();
        }
        if (useUV2Coordinates) {
            uv2s = ListPool<Vector2>.Get();
        }
        if (useTerrainTypes) {
            terrainTypes = ListPool<Vector3>.Get();
        }
        triangles = ListPool<int>.Get();
    }
    public void Apply()
    {
        hexMesh.SetVertices(vertices);
        ListPool<Vector3>.Add(vertices);
        if (useColor) {
            hexMesh.SetColors(colors);
            ListPool<Color>.Add(colors);
        }
        if (useUVCoordinates) {
            hexMesh.SetUVs(0, uvs); // mesh支持set多个uv集，这里存的是0号UV集
            ListPool<Vector2>.Add(uvs); 
        }
        if (useUV2Coordinates) {
            hexMesh.SetUVs(1, uv2s); // 1号UV集
            ListPool<Vector2>.Add(uv2s);
        }
        if (useTerrainTypes) {
            hexMesh.SetUVs(2, terrainTypes);
            ListPool<Vector3>.Add(terrainTypes);
        }
        hexMesh.SetTriangles(triangles, 0);
        ListPool<int>.Add(triangles);

        hexMesh.RecalculateNormals();
        if (useCollider) {
            meshCollider.sharedMesh = hexMesh;
        }
    }

    public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(HexMetrics.Perturb(v1));
        vertices.Add(HexMetrics.Perturb(v2));
        vertices.Add(HexMetrics.Perturb(v3));
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }
    public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }
    public void AddTriangleColor(Color color)
    {
        AddTriangleColor(color, color, color);
    }
    public void AddTriangleColor(Color c1, Color c2, Color c3)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
    }

    public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) // (v1, v2)是上底，(v3, v4)是下底
    {
        int vertexIndex = vertices.Count;
        vertices.Add(HexMetrics.Perturb(v1));
        vertices.Add(HexMetrics.Perturb(v2));
        vertices.Add(HexMetrics.Perturb(v3));
        vertices.Add(HexMetrics.Perturb(v4));
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }
    public void AddQuadUnperturbed(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4
    )
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        vertices.Add(v4);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }
    public void AddQuadColor(Color color)
    {
        AddQuadColor(color, color, color, color);
    }
    public void AddQuadColor(Color c1, Color c2)
    {
        AddQuadColor(c1, c1, c2, c2);
    }
    public void AddQuadColor(Color c1, Color c2, Color c3, Color c4)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
        colors.Add(c4);
    }

    public void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
    }
    public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
    {
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
        uvs.Add(uv4);
    }
    public void AddQuadUV(float uMin, float uMax, float vMin, float vMax)
    {
        uvs.Add(new Vector2(uMin, vMin));
        uvs.Add(new Vector2(uMax, vMin));
        uvs.Add(new Vector2(uMin, vMax));
        uvs.Add(new Vector2(uMax, vMax));
    }

    public void AddTriangleUV2(Vector2 uv1, Vector2 uv2, Vector3 uv3)
    {
        uv2s.Add(uv1);
        uv2s.Add(uv2);
        uv2s.Add(uv3);
    }
    public void AddQuadUV2(Vector2 uv1, Vector2 uv2, Vector3 uv3, Vector3 uv4)
    {
        uv2s.Add(uv1);
        uv2s.Add(uv2);
        uv2s.Add(uv3);
        uv2s.Add(uv4);
    }
    public void AddQuadUV2(float uMin, float uMax, float vMin, float vMax)
    {
        uv2s.Add(new Vector2(uMin, vMin));
        uv2s.Add(new Vector2(uMax, vMin));
        uv2s.Add(new Vector2(uMin, vMax));
        uv2s.Add(new Vector2(uMax, vMax));
    }

    public void AddTriangleTerrainTypes(Vector3 types)
    {
        terrainTypes.Add(types);
        terrainTypes.Add(types);
        terrainTypes.Add(types);
    }
    public void AddQuadTerrainTypes(Vector3 types)
    {
        terrainTypes.Add(types);
        terrainTypes.Add(types);
        terrainTypes.Add(types);
        terrainTypes.Add(types);
    }
}
