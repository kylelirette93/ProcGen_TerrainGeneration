using UnityEngine;

public class TerrainModifier : MonoBehaviour
{
    [SerializeField] MeshGenerator meshGenerator;
    private Rect windowRect = new Rect(20, 20, 300, 400);
    bool showWindow = false;

    float heightMultiplier = 1; // Default height multiplier.
    float waterLevel = 0.001f; // Default water level
    float lacunarity = 0.001f; // Default lacunarity
    float persistence = 0.001f; // Default persistence

    private float prevHeightMultiplier, prevWaterLevel, prevLacunarity, prevPersistence;

    bool valuesDirty = false;


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            showWindow = !showWindow;
        }
    }
    void OnGUI()
    {
        if (showWindow)
        {
            windowRect = GUI.Window(0, windowRect, TerrainWindow, "Terrain Modifier");
            if (valuesDirty)
            {
                ApplyTerrainModifications();
                valuesDirty = false;
            }
        }
    }

    void TerrainWindow(int windowID)
    {
        float yPos = 30;
        float spacing = 60;

        GUI.Label(new Rect(10, yPos, 280, 20), "Terrain Height: " + heightMultiplier.ToString("F2"));
        heightMultiplier = GUI.HorizontalSlider(new Rect(10, yPos + 20, 200, 30), heightMultiplier, 0f, 100f);
        if (heightMultiplier != prevHeightMultiplier)
        {
            valuesDirty = true;
            prevHeightMultiplier = heightMultiplier;
        }
        yPos += spacing;

        GUI.Label(new Rect(10, yPos, 280, 20), "Water Level: " + waterLevel.ToString("F2"));
        waterLevel = GUI.HorizontalSlider(new Rect(10, yPos + 20, 200, 30), waterLevel, 0f, 6f);
        if (waterLevel != prevWaterLevel)
        {
            valuesDirty = true;
            prevWaterLevel = waterLevel;
        }
        yPos += spacing;

        GUI.Label(new Rect(10, yPos, 280, 20), "Irregularity: " + lacunarity.ToString("F2"));
        lacunarity = GUI.HorizontalSlider(new Rect(10, yPos + 20, 200, 30), lacunarity, 0f, 4f);
        if (lacunarity != prevLacunarity)
        {
            valuesDirty = true;
            prevLacunarity = lacunarity;
        }
        yPos += spacing;

        GUI.Label(new Rect(10, yPos, 280, 20), "Roughness: " + persistence.ToString("F2"));
        persistence = GUI.HorizontalSlider(new Rect(10, yPos + 20, 200, 30), persistence, 0f, 1f);
        if (persistence != prevPersistence)
        {
            valuesDirty = true;
            prevPersistence = persistence;
        }
        yPos += spacing;
    }

    void ApplyTerrainModifications()
    {
        meshGenerator.HeightMultiplier = heightMultiplier;
        meshGenerator.WaterLevel = waterLevel;
        meshGenerator.Lacunarity = lacunarity;
        meshGenerator.Persistence = persistence;
        meshGenerator.GenerateTerrain();
    }
}