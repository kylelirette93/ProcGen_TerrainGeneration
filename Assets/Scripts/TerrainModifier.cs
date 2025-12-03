using UnityEngine;

/// <summary>
/// Class responsible for modifying terrain via sliders. I simply like the look of old GUI, it's clean.
/// </summary>
public class TerrainModifier : MonoBehaviour
{
    [SerializeField] MeshGenerator meshGenerator;

    [Header("UI Settings")]
    [SerializeField] private Rect terrainWindowRect = new Rect(20, 20, 300, 400);
    [SerializeField] private Rect cameraWindowRect = new Rect(20, 500, 300, 200);

    #region Default Terrain Modifier Values
    private float heightMultiplier = 1; // Default height multiplier.
    private float waterLevel = 0.001f; // Default water level
    private float lacunarity = 0.001f; // Default lacunarity
    private float persistence = 0.001f; // Default persistence
    #endregion

    #region Label and Slider Values
    // Label values.
    private float labelXPos = 10;
    private float labelYPos = 30;
    private float labelWidth = 280;
    private float labelHeight = 20;
    private float labelSpacing = 60;

    // Slider values.
    private float sliderXPos = 10;
    private float sliderWidth = 200;
    private float sliderHeight = 30;
    private float sliderSpacing = 20;
    #endregion

    // Dirty flag values.
    private float prevHeightMultiplier, prevWaterLevel, prevLacunarity, prevPersistence;
    private bool valuesDirty = false;

    private bool showTerrainWindow = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            ToggleTerrainWindow();
        }
    }

    /// <summary>
    /// Toggles terrain modification window on and off.
    /// </summary>
    private void ToggleTerrainWindow()
    {
        showTerrainWindow = !showTerrainWindow;
    }
    private void OnGUI()
    {
        if (showTerrainWindow)
        {
            terrainWindowRect = GUI.Window(0, terrainWindowRect, TerrainWindow, "Terrain Modifier");
            // Dirty flag to check if values changed.
            if (valuesDirty)
            {
                ApplyTerrainModifications();
                valuesDirty = false;
            }
        }
        cameraWindowRect = GUI.Window(1, cameraWindowRect, CameraWindow, "Camera Controls");      
    }

    private void TerrainWindow(int windowID)
    {
        labelYPos = 30;
        DrawSlider("Terrain Height", ref heightMultiplier, ref prevHeightMultiplier, 0f, 100f);

        DrawSlider("Water Level", ref waterLevel, ref prevWaterLevel, 0f, 6f);

        DrawSlider("Irregularity", ref lacunarity, ref prevLacunarity, 0f, 4f);

        DrawSlider("Roughness", ref persistence, ref prevPersistence, 0f, 1f);
    }

    private void CameraWindow(int windowID)
    {
        GUI.Label(new Rect(10, 30, 280, 20), "WASD: Move Camera");
        GUI.Label(new Rect(10, 60, 280, 20), "Mouse: Look Around");
        GUI.Label(new Rect(10, 90, 280, 20), "Scroll Wheel: Zoom In/Out");
        GUI.Label(new Rect(10, 120, 280, 20), "Space: Lock Camera");
    }

    private void DrawSlider(string labelText, ref float currentValue, ref float previousValue, float minValue, float maxValue)
    {
        // Labels the slider.
        GUI.Label(new Rect(labelXPos, labelYPos, labelWidth, labelHeight), labelText + ": " + currentValue.ToString("F2"));
        // Draw the slider.
        float sliderY = labelYPos + sliderSpacing;
        float newValue = GUI.HorizontalSlider(new Rect(sliderXPos, sliderY, sliderWidth, sliderHeight), currentValue, minValue, maxValue);
        CheckDirty(ref newValue, ref previousValue);
        currentValue = newValue;
        labelYPos += labelSpacing;
    }

    private void CheckDirty(ref float value, ref float previousValue)
    {
        // If the values changed, I mark the dirty flag.
        if (value != previousValue)
        {
            valuesDirty = true;
            previousValue = value;
        }
    }

    /// <summary>
    /// Applies actual modifications to terrain based on slider values.
    /// </summary>
    private void ApplyTerrainModifications()
    {
        meshGenerator.HeightMultiplier = heightMultiplier;
        meshGenerator.WaterLevel = waterLevel;
        meshGenerator.Lacunarity = lacunarity;
        meshGenerator.Persistence = persistence;
        meshGenerator.GenerateTerrain();
    }
}