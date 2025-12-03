using UnityEngine;
using System;
public class MeshGenerator : MonoBehaviour
{
    [Header("Mesh Size Settings")]
    [SerializeField, Range(15, 500)] private int width;
    [SerializeField, Range(15, 500)] private int depth;

    [Header("Height Settings")]
    private AnimationCurve heightCurve;
    [SerializeField, Range(0, 100f)] private float heightMultiplier;
    private float[,] heightMap;

    [Header("Water Settings")]
    [SerializeField, Range(0f, 6f)] private float waterLevel;

    [Header("Noise Settings")]
    [SerializeField, Range(0.001f, 1f)] private float noiseScale;
    [SerializeField, Range(1, 16)] private int octaves;
    [SerializeField, Range(1f, 4f)] private float lacunarity;
    [SerializeField, Range(0f, 1f)] private float persistence;
    [SerializeField] int seed;

    [Header("Map Falloff Settings")]
    [SerializeField, Range(0f, 1f)] private float fallOffStrength;
    [SerializeField, Range(0f, 1f)] private float fallOffStart;
    private AnimationCurve fallOffCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);


    #region Mesh Data
    // Position of each vertex of mesh.
    private Vector3[] vertices;
    private Color[] meshColors;
    private Vector2[] uvs;
    // Triangles that connect the mesh.
    private int[] triangles;
    private Mesh mesh;
    private float minimumTerrainHeight;
    private float maximumTerrainHeight;
    private float previousHeightMultiplier;
    private int previousOctaves;
    private float previousNoiseScale;
    private float previousWaterLevelOffset;
    private int previousWidth;
    private int previousDepth;
    private float previousLacunarity;
    private float previousPersistence;
    private float previousFallOffStart;
    private float previousFallOffStrength;
    #endregion

    #region Properties
    // Specifically for access through terrain modifier class for UI sliders.
    public float WaterLevel
    {
        get { return waterLevel; }
        set { waterLevel = value; }
    }
    public float HeightMultiplier
    {
        get { return heightMultiplier; }
        set { heightMultiplier = value; }
    }
    public float Lacunarity
    {
        get { return lacunarity; }
        set { lacunarity = value; }
    }
    public float Persistence
    {
        get { return persistence; }
        set { persistence = value; }
    }
    #endregion

    private void Awake()
    {
        InitializeDefaultTerrainHeight();
    }
    private void Start()
    {
        GenerateTerrain();
    }

    /// <summary>
    /// Initialize a default height curve for terrain.
    /// </summary>
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
                heightCurve.SmoothTangents(i, 1.0f);
            }
        }
    }

    /// <summary>
    /// Handles terrain generation process. Could be seperated into classes, but for simplicity this class isn't that big really.
    /// </summary>
    public void GenerateTerrain()
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
        GenerateColors();
        UpdateMesh();
    }

    /// <summary>
    /// Generates the mesh itself.
    /// </summary>
    private void GenerateMesh()
    {
        // Create mesh with filter.
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    /// <summary>
    /// Creates shape of mesh.
    /// </summary>
    private void CreateShape()
    {
        Vector2[] octaveOffsets = GetOffsetSeed();
        GenerateHeightMap(octaveOffsets);
        CreateMeshData(heightMap);
    }

    /// <summary>
    /// Generates height map based on perlin noise.
    /// </summary>
    /// <param name="octaveOffsets">The offset data passed to height calculation.</param>
    private void GenerateHeightMap(Vector2[] octaveOffsets)
    {
        heightMap = new float[width + 1, depth + 1];

        for (int z = 0; z <= depth; z++)
        {
            for (int x = 0; x <= width; x++)
            {
                float height = CalculateHeight(x, z, octaveOffsets);
                heightMap[x, z] = height;
            }
        }
    }

    /// <summary>
    /// Generates mesh data based on height map.
    /// </summary>
    /// <param name="heightMap">The height map previously calculated, stored in vertices data.</param>
    private void CreateMeshData(float[,] heightMap)
    {
        // Amount of vertices is determined by size.
        vertices = new Vector3[(width + 1) * (depth + 1)];
        uvs = new Vector2[(width + 1) * (depth + 1)];

        // Assign positions to vertices.
        for (int i = 0, z = 0; z <= depth; z++)
        {
            for (int x = 0; x <= width; x++)
            {
                // Calculate normalizedHeight of each vertex.
                float vertHeight = heightMap[x, z];
                vertices[i] = new Vector3(x, vertHeight, z);
                uvs[i] = new Vector2(x / (float)width, z / (float)depth);
                i++;
            }
        }
    }

    /// <summary>
    /// Generates an array of offsets for each octave.
    /// </summary>
    /// <returns>Returns the array of offsets generated.</returns>
    private Vector2[] GetOffsetSeed()
    {
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

    /// <summary>
    /// Creates triangles for mesh based on vertices.
    /// </summary>
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

    /// <summary>
    /// Generate vertex color data.
    /// </summary>
    private void GenerateColors()
    {
        meshColors = new Color[vertices.Length];

        for (int z = 0; z < vertices.Length; z++)
        {
            // Get's normalized normalizedHeight between min and max values.
            float normalizedHeight = Mathf.InverseLerp(minimumTerrainHeight, maximumTerrainHeight, vertices[z].y);
            normalizedHeight = Mathf.Clamp01(normalizedHeight);
            // Get mesh color based on height.
            meshColors[z] = ColorBasedOnHeight(normalizedHeight);
        }
    }

    /// <summary>
    /// Colors the terrain based on height zones.
    /// </summary>
    /// <param name="height">The normalized height of terrain.</param>
    /// <returns></returns>
    private Color ColorBasedOnHeight(float height)
    {
        #region Define Color Zones
        Color snow = new Color(0.95f, 0.95f, 0.95f);
        Color mountain = new Color(0.4f, 0.25f, 0.1f);
        Color grass = new Color(0.15f, 0.8f, 0.15f);
        Color sand = new Color(0.85f, 0.75f, 0.55f);
        Color water = new Color(0.2f, 0.4f, 0.6f);
        Color deepWater = new Color(0.1f, 0.2f, 0.4f);
        #endregion

        float shoreHeight = 2f;
        float normalizedWaterLevel = Mathf.InverseLerp(minimumTerrainHeight, maximumTerrainHeight, waterLevel);
        float normalizedShoreHeight = Mathf.InverseLerp(minimumTerrainHeight, maximumTerrainHeight, waterLevel + shoreHeight);

        if (height < normalizedWaterLevel)
        {
            // Blend from deep water at 0 to shallow water.
            float normalizedWaterDepth = Mathf.InverseLerp(normalizedWaterLevel, 0f, height);
            return Color.Lerp(water, deepWater, normalizedWaterDepth);
        }
        else if (height >= normalizedWaterLevel && height < normalizedShoreHeight)
        {
            // Blend from water to sand at shore.
            float normalizedShoreDepth = Mathf.InverseLerp(normalizedWaterLevel, normalizedShoreHeight, height);
            return Color.Lerp(water, sand, normalizedShoreDepth);
        }
        if (height > 0.7f)
        {
            // Blend from mountain to snow.
            float blendFactor = Mathf.InverseLerp(0.7f, 0.85f, height);
            return Color.Lerp(mountain, snow, blendFactor); 
        }
        else if (height > 0.4f)
        {
            // Blend from grass to mountain.
            float blendFactor = Mathf.InverseLerp(0.4f, 0.65f, height);
            return Color.Lerp(grass, mountain, blendFactor); 
        }
        else if (height > 0.1f)
        {
            // Blend from sand to grass.
            float blendFactor = Mathf.InverseLerp(0.1f, 0.35f, height);
            return Color.Lerp(sand, grass, blendFactor); 
        }
        else
        {
            // Base color for terrain.
            return sand; 
        }
    }

    /// <summary>
    /// Calculates height of a vertex.
    /// </summary>
    /// <param name="x">Coordinate at width of map.</param>
    /// <param name="z">Coordinate at depth of map.</param>
    /// <param name="octaveOffsets">Offset data applied to sample noise.</param>
    /// <returns></returns>
    private float CalculateHeight(int x, int z, Vector2[] octaveOffsets)
    {
        float amplitude = 2f;
        float frequency = 1;
        float height = 0;
        const float RANGE_STRETCH_FACTOR = 2f;
        const float MID_POINT = 2f;
        const float RANGE_CENTER_SHIFT = 1f;
        const float MAX_NORMALIZED_DISTANCE = 1f;

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

            // Generate sample noise and stretch to -1 to 1 range. To account for peaks and valleys.
            float perlinValue = (Mathf.PerlinNoise(sampleX, sampleZ)) * RANGE_STRETCH_FACTOR - RANGE_CENTER_SHIFT;
            // Apply normalized perlin value to height curve.
            height += heightCurve.Evaluate((perlinValue + RANGE_CENTER_SHIFT) / RANGE_STRETCH_FACTOR) * amplitude;
            // Amplitude decreases each octave.
            amplitude *= persistence;
            // Lacunarity increases frequency each octave.
            frequency *= lacunarity;
        }

        float finalHeight = height * heightMultiplier;

        // Calculate distance from center.
        float centerX = width / MID_POINT;
        float centerZ = depth / MID_POINT;
        // Calculate normalized distance from center.
        float distanceX = Mathf.Abs(x - centerX) / centerX;
        float distanceZ = Mathf.Abs(z - centerZ) / centerZ;
        // Square distance for falloff.
        float distanceFromCenter = Mathf.Max(distanceX, distanceZ);

        // Apply falloff at edges of terrain.
        if (distanceFromCenter > fallOffStart)
        {
            // Area of falloff effect.
            float fallOffZone = MAX_NORMALIZED_DISTANCE - fallOffStart;
            // Falloff is calculated based on distance of vertex from falloff start.
            float fallOff = (distanceFromCenter - fallOffStart) / fallOffZone;
            // Normalize for curve evaluation.
            fallOff = Mathf.Clamp01(fallOff);
            // Apply falloff curve and strength to the final height.
            float fallOffValue = MAX_NORMALIZED_DISTANCE - (fallOffCurve.Evaluate(fallOff) * fallOffStrength);
            finalHeight *= fallOffValue;
        }
        return finalHeight;
    }

    
    /// <summary>
    /// Updates mesh with new data.
    /// </summary>
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
                || previousWidth != width || previousDepth != depth || previousLacunarity != lacunarity || previousFallOffStart != fallOffStart
                ||previousFallOffStrength != fallOffStrength || previousPersistence != persistence;
            if (fullRegen)
            {
                GenerateTerrain();
            }

            previousWaterLevelOffset = waterLevel;
            previousHeightMultiplier = heightMultiplier;
            previousOctaves = octaves;
            previousNoiseScale = noiseScale;
            previousDepth = depth;
            previousWidth = width;
            previousPersistence = persistence;
            previousLacunarity = lacunarity;
            previousFallOffStart = fallOffStart;
            previousFallOffStrength = fallOffStrength;
        }
    }
#endif
}
