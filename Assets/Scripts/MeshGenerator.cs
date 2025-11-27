using UnityEngine;
using System;
public class MeshGenerator : MonoBehaviour
{
    // Size of mesh.
    [Header("Mesh Size Settings")]
    [SerializeField, Range(15, 500)] private int width;
    [SerializeField, Range(15, 500)] private int depth;
    [SerializeField, Min(1)] private int meshScale;

    [Header("Height Settings")]
    private AnimationCurve heightCurve;
    [SerializeField, Range(0, 100f)] private float heightMultiplier;

    // Gradient changes based on height.
    [Header("Color Settings")]
    Color[] meshColors;
    Gradient heightGradient;

    [Header("Noise Settings")]
    [SerializeField, Range(0.001f, 1f)] float noiseScale;
    [SerializeField, Range(1, 16)] int octaves;
    [SerializeField, Range(1f, 4f)] float lacunarity;
    [SerializeField, Range(0f, 1f)] float persistence;
    [SerializeField] int seed;

    #region Mesh Data
    // Position of each vertex of mesh.
    Vector3[] vertices;
    // Triangles that connect the mesh.
    int[] triangles;
    private Mesh mesh;
    float minimumTerrainHeight;
    float maximumTerrainHeight;
    #endregion

    private void Awake()
    {
        InitializeDefaultTerrainColor();
        InitializeDefaultTerrainHeight();
    }
    private void Start()
    {
        GenerateTerrain();
    }

    private void InitializeDefaultTerrainColor()
    {
        if (heightGradient == null || heightGradient.colorKeys.Length == 0)
        {
            // Default gradient for terrain.
            heightGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[5];
            colorKeys[0] = new GradientColorKey(new Color(0.2f, 0.5f, 0.8f), 0f); // Water
            colorKeys[1] = new GradientColorKey(new Color(0.3f, 0.5f, 0.2f), 0.4f); // Green
            colorKeys[2] = new GradientColorKey(new Color(0.5f, 0.4f, 0.3f), 0.7f); // Mountain
            colorKeys[4] = new GradientColorKey(new Color(0.9f, 0.9f, 0.9f), 1f); // Snow

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(0.3f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);

            heightGradient.SetKeys(colorKeys, alphaKeys);
        }
    }

    private void InitializeDefaultTerrainHeight()
    {
        if (heightCurve == null || heightCurve.length == 0)
        {
            heightCurve = new AnimationCurve();
            heightCurve.AddKey(0f, 0f);
            heightCurve.AddKey(0.3f, 0.05f);
            heightCurve.AddKey(0.7f, 0.4f);
            heightCurve.AddKey(1f, 1f);

            // Smooth tangetns to transition between curves.
            for (int i = 0; i < heightCurve.length; i++)
            {
                heightCurve.SmoothTangents(i, 0f);
            }
        }
    }

    private void GenerateTerrain()
    {
        GenerateMesh();
        CreateShape();
        #region Determine Min/Max Height
        // Determine min and max height for color heightGradient.
        float actualMinHeight = float.MaxValue;
        float actualMaxHeight = float.MinValue;
        foreach (var v in vertices)
        {
            if (v.y < actualMinHeight)
            {
                actualMinHeight = v.y;
                minimumTerrainHeight = actualMinHeight;
            }
            if (v.y > actualMaxHeight)
            {
                actualMaxHeight = v.y;
                maximumTerrainHeight = actualMaxHeight;
            }
        }
        #endregion
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
        vertices = new Vector3[(width + 1) * (depth + 1)];

        // Assign positions to vertices.
        for (int i = 0, z = 0; z <= depth; z++)
        {
            for (int x = 0; x <= width; x++) 
            {
                // Calculate height of each vertex.
                float vertHeight = CalculateHeight(x, z, octaveOffsets);    
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
        triangles = new int[width * depth * 6];
        int vert = 0;
        int tris = 0;

        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                triangles[tris + 0] = vert + 0; // Bottom left.
                triangles[tris + 1] = vert + width + 1; // Top left.
                triangles[tris + 2] = vert + 1; // Bottom right.
                triangles[tris + 3] = vert + 1; // Bottom right.
                triangles[tris + 4] = vert + width + 1; // Top left.
                triangles[tris + 5] = vert + width + 2; // Top right.
                vert++;
                tris += 6;
            }
            // Incrementing vert here to stop mesh generation on edges.
            vert++;
        }
    }

    private void CreateColors()
    {
        // Evaluate the heightGradient color based on height at a specific vertice.
        meshColors = new Color[vertices.Length];
        for (int z = 0; z < vertices.Length; z++)
        {
            // Get's normalized height between min and max values.
            float height = Mathf.InverseLerp(minimumTerrainHeight, maximumTerrainHeight, vertices[z].y);
            height = Mathf.Clamp01(height);
            // Evaluats heightGradient based on height.
            meshColors[z] = heightGradient.Evaluate(height);
        }
    }

    private float CalculateHeight(int x, int z, Vector2[] octaveOffsets)
    {
        float amplitude = 2f;
        float frequency = 1;
        float height = 0;
        // If at the edges, height should be 0.
        if (x == 0 || x == width || z == 0 || z == depth)
        {
            return height;
        }

        for (int i = 0; i < octaves; i++)
        {
            // Using an octave offset for varying terrain.
            float sampleX = (x / (float)width) / noiseScale * frequency + octaveOffsets[i].x;
            float sampleZ = (z / (float)depth) / noiseScale * frequency + octaveOffsets[i].y;

            float perlinValue = (Mathf.PerlinNoise(sampleX, sampleZ)) * 2 - 1;
            // Normalize perlin value for height curve.
            height += heightCurve.Evaluate((perlinValue + 1f) / 2f) * amplitude;
            // Amplitude decreases each octave.
            amplitude *= persistence;
            // Lacunarity increases frequency each octave.
            frequency *= lacunarity;
        }
        return height * heightMultiplier;
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Regenerates mesh when editor values change.
        if (Application.isPlaying && mesh != null)
        {
            GenerateTerrain();
        }
    }
#endif
}
