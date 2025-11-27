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

    [Header("Water Settings")]
    [SerializeField, Range(0f, 0.2f)] private float waterLevelOffset;

    [Header("Noise Settings")]
    [SerializeField, Range(0.001f, 1f)] float noiseScale;
    [SerializeField, Range(1, 16)] int octaves;
    [SerializeField, Range(1f, 4f)] float lacunarity;
    [SerializeField, Range(0f, 1f)] float persistence;
    [SerializeField] int seed;

    #region Mesh Data
    // Position of each vertex of mesh.
    Vector3[] vertices;
    Color[] meshColors;
    Vector2[] uvs;
    // Triangles that connect the mesh.
    int[] triangles;
    private Mesh mesh;
    float minimumTerrainHeight;
    float maximumTerrainHeight;
    float previousHeightMultiplier;
    int previousOctaves;
    float previousNoiseScale;
    float previousWaterLevelOffset;
    int previousWidth;
    int previousDepth;
    int previousMeshScale;
    float previousLacunarity;
    float previousPersistence;
    #endregion

    private void Awake()
    {
        InitializeDefaultTerrainHeight();
    }
    private void Start()
    {
        GenerateTerrain();
    }

    private void InitializeDefaultTerrainHeight()
    {
        if (heightCurve == null || heightCurve.length == 0)
        {
            heightCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.3f, 0.15f),
                new Keyframe(0.6f, 0.5f),
                new Keyframe(1f, 1f)
            );  


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
        // Determine min and max normalizedHeight for color heightGradient.
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
        uvs = new Vector2[(width + 1) * (depth + 1)];

        // Assign positions to vertices.
        for (int i = 0, z = 0; z <= depth; z++)
        {
            for (int x = 0; x <= width; x++) 
            {
                // Calculate normalizedHeight of each vertex.
                float vertHeight = CalculateHeight(x, z, octaveOffsets);    
                vertices[i] = new Vector3(x, vertHeight, z);
                uvs[i] = new Vector2(x / (float)width, z / (float)depth);
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
        // Evaluate the heightGradient color based on normalizedHeight at a specific vertice.
        meshColors = new Color[vertices.Length];

        for (int z = 0; z < vertices.Length; z++)
        {
            // Get's normalized normalizedHeight between min and max values.
            float normalizedHeight = Mathf.InverseLerp(minimumTerrainHeight, maximumTerrainHeight, vertices[z].y);
            normalizedHeight = Mathf.Clamp01(normalizedHeight);
            // Evaluats heightGradient based on normalizedHeight.
            meshColors[z] = ColorBasedOnHeight(normalizedHeight);
        }
    }

    private Color ColorBasedOnHeight(float height)
    {
        Color snow = new Color(0.95f, 0.95f, 0.95f);
        Color mountain = new Color(0.62f, 0.42f, 0.26f);
        Color grass = new Color(0.2f, 0.6f, 0.1f);
        Color sand = new Color(0.85f, 0.75f, 0.55f);
        Color water = new Color(0.2f, 0.4f, 0.6f);
        if (height > 0.8f)
        {
            return Color.Lerp(mountain, snow, (height - 0.8f) / 0.2f); // Blend to white for snow.
        }
        else if (height > 0.65f)
        {
            return Color.Lerp(grass, mountain, (height - 0.65f) / 0.15f); // Blend to brown for mountains.
        }
        else if (height > 0.4f)
        {
            return Color.Lerp(sand, grass, (height - 0.4f) / 0.25f); // Blend to green for grass.
        }
        else if (height > 0.2f)
        {
            return Color.Lerp(water, sand, (height - 0.2f) / 0.2f); // Blend to yellow for sand.
        }
        else
        {
            return Color.Lerp(new Color(0f, 0f, 0.5f), water, height / 0.2f); // Blend to dark blue for deep water.
        }
    }

    private float CalculateHeight(int x, int z, Vector2[] octaveOffsets)
    {
        float amplitude = 2f;
        float frequency = 1;
        float height = 0;
        
        // If x or z is 0 it's at edge of mesh.
        bool isEdge = (x == 0 || x == width || z == 0 || z == depth);

        // Max dimension for consistent terrain scaling.
        float maxDimension = Mathf.Max(width, depth);
        for (int i = 0; i < octaves; i++)
        {
            // Normalize coordinates.
            float normalizedX = x / (float)width;
            float normalizedZ = z / (float)depth;

            // Using an octave offset for varying terrain.
            float sampleX = normalizedX / noiseScale * frequency + octaveOffsets[i].x;
            float sampleZ = normalizedZ / noiseScale * frequency + octaveOffsets[i].y;

            float perlinValue = (Mathf.PerlinNoise(sampleX, sampleZ)) * 2 - 1;
            // Normalize perlin value for normalizedHeight curve.
            height += heightCurve.Evaluate((perlinValue + 1f) / 2f) * amplitude;
            // Amplitude decreases each octave.
            amplitude *= persistence;
            // Lacunarity increases frequency each octave.
            frequency *= lacunarity;
        }

        float finalHeight = height * heightMultiplier;

        if (isEdge)
        {
            // Offset edge heights to create water level.
            finalHeight *= 0.2f;
        }
        finalHeight -= waterLevelOffset;
        return finalHeight;
    }

  
    private void UpdateMesh()
    {
        // Clear old mesh data and update.
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.colors = meshColors;
        mesh.RecalculateNormals();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Regenerates mesh when editor values change.
        if (Application.isPlaying && mesh != null)
        {
            bool fullRegen = previousHeightMultiplier != heightMultiplier || previousOctaves != octaves || noiseScale != previousNoiseScale
                || previousWidth != width || previousDepth != depth || previousLacunarity != lacunarity || previousMeshScale != meshScale;
            bool needsColor = previousWaterLevelOffset != waterLevelOffset;
            if (fullRegen)
            {
                GenerateTerrain();
            }
            else if (needsColor)
            {
                CreateColors();
                mesh.colors = meshColors;
            }

            previousWaterLevelOffset = waterLevelOffset;
            previousHeightMultiplier = heightMultiplier;
            previousOctaves = octaves;
            previousNoiseScale = noiseScale;
            previousDepth = depth;
            previousWidth = width;
        }
    }
#endif
}
