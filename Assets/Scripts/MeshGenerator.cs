using UnityEngine;
using System;
public class MeshGenerator : MonoBehaviour
{
    // Size of mesh.
    [Header("Mesh Size Settings")]
    [SerializeField] int xSize;
    [SerializeField] int zSize;
    [SerializeField] int meshScale;

    [Header("Terrain Settings")]
    float minimumTerrainHeight;
    float maximumTerrainHeight;
    [SerializeField] AnimationCurve heightCurve;

    // Gradient changes based on height.
    [Header("Color Settings")]
    Color[] meshColors;
    [SerializeField] Gradient gradient;

    [Header("Noise Settings")]
    [SerializeField] float noiseScale;
    [SerializeField] int octaves;
    [SerializeField] float lacunarity;
    [SerializeField] int seed;

    #region Mesh Data
    // Position of each vertex of mesh.
    Vector3[] vertices;
    // Triangles that connect the mesh.
    int[] triangles;
    private Mesh mesh;
    #endregion

    private void Start()
    {
        GenerateMesh();
        CreateShape();
        CreateTriangles();
        CreateColors();
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
        Vector2[] octaveOffsets = GetOffsetSeed();
        // Amount of vertices is determined by size.
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        // Assign positions to vertices.
        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++) 
            {
                // Calculate height of each vertex.
                float vertHeight = CalculateHeight(x, z, octaveOffsets);    
                SetTerrainHeights(vertHeight);
                vertices[i] = new Vector3(x, vertHeight, z);
                i++;
            }
        }
    }

    private Vector2[] GetOffsetSeed()
    {
        // Get random seed for variation.
        seed = UnityEngine.Random.Range(0, 1000);

        System.Random prng = new System.Random(seed);
        // Creates an offset for each octave to be applied to noise.
        Vector2[] octaveOffsets = new Vector2[octaves];

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000);
            float offsetY = prng.Next(-100000, 100000);
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }
        return octaveOffsets;
    }

    private void CreateTriangles()
    {
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

    private void CreateColors()
    {
        // Evaluate the gradient color based on height at a specific vertice.
        meshColors = new Color[vertices.Length];
        for (int z = 0; z < vertices.Length; z++)
        {
            // Get's normalized height between min and max values.
            float height = Mathf.InverseLerp(minimumTerrainHeight, maximumTerrainHeight, vertices[z].y);
            // Evaluats gradient based on height.
            meshColors[z] = gradient.Evaluate(height);
        }
    }

    private float CalculateHeight(int x, int z, Vector2[] octaveOffsets)
    {
        float amplitude = 20f;
        float frequency = 2;
        float persistence = 0.5f;
        float height = 0;
        // If at the edges, height should be 0.
        if (x == 0 || x == xSize || z == 0 || z == zSize)
        {
            return height;
        }

        for (int i = 0; i < octaves; i++)
        {
            // Using an octave offset for varying terrain.
            float mapZ = z / noiseScale * frequency * octaveOffsets[i].y;
            float mapX = x / noiseScale * frequency * octaveOffsets[i].x;

            float perlinValue = (Mathf.PerlinNoise(mapZ, mapX)) * 2 - 1;
            height += heightCurve.Evaluate(perlinValue) * amplitude;
            // Lacunarity increases frequency each octave.
            frequency *= lacunarity;
            // Amplitude decreases each octave.
            amplitude *= persistence;
        }
        return height;
    }

    private void SetTerrainHeights(float height)
    {
        // Set the min and max height for color gradient.
        if (height > maximumTerrainHeight)
        {
            maximumTerrainHeight = height;
        }
        if (height < minimumTerrainHeight)
        {
            minimumTerrainHeight = height;
        }
    }

  
    private void UpdateMesh()
    {
        // Clear old mesh data and update.
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = meshColors;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        gameObject.transform.localScale = new Vector3(meshScale, meshScale, meshScale);
    }
}
