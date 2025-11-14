using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    // Size of mesh.
    [SerializeField] int xSize;
    [SerializeField] int zSize;

    // Position of each vertex of mesh.
    Vector3[] vertices;

    // Triangles that connect the mesh.
    int[] triangles;

    private Mesh mesh;

    private void Start()
    {
        GenerateMesh();
        CreateShape();
        UpdateMesh();
    }

    private void GenerateMesh()
    {
        // Create mesh with filter.
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    private void CreateShape()
    {
        // Amount of vertices is determined by size.
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        // Assign positions to vertices.
        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++) 
            {
                // Calculate height of each vertex.
                float vertHeight = CalculateHeight(x, z);           
                vertices[i] = new Vector3(x, vertHeight, z);
                i++;
            }
        }

        // Each triangle has 3 vertices and each square has 2 triangles.
        triangles = new int[xSize * zSize * 6];
        int vert = 0;
        int tris = 0;

        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                triangles[tris + 0] = vert + 0; // Bottom left.
                triangles[tris + 1] = vert + xSize + 1; // Top left.
                triangles[tris + 2] = vert + 1; // Bottom right.
                triangles[tris + 3] = vert + 1; // Bottom right.
                triangles[tris + 4] = vert + xSize + 1; // Top left.
                triangles[tris + 5] = vert + xSize + 2; // Top right.
                vert++;
                tris += 6;
            }
            // Incrementing vert here to stop mesh generation on edges.
            vert++;
        }       
    }

    private float CalculateHeight(int x, int z)
    {
        float height = 0;
        // If at the edges, height should be 0.
        if (x == 0 || x == xSize || z == 0 || z == zSize)
        {
            return height;
        }
        return Mathf.PerlinNoise(x * .3f, z * .3f) * 2f;
    }

    private void UpdateMesh()
    {
        // Clear old mesh data and update.
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}
