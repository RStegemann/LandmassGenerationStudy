using UnityEngine;

[CreateAssetMenu(menuName = "Settings/TextureSettings")]
public class TextureSettings : UpdatableScriptable
{
    private float savedMinHeight;
    private float savedMaxHeight;
    public Color[] baseColours;
    [Range(0, 1)]
    public float[] baseStartHeights;
    
    public void ApplyToMaterial(Material mat)
    {
        mat.SetInt("base_colour_count", baseColours.Length);
        mat.SetColorArray("base_colours", baseColours);
        mat.SetFloatArray("base_start_heights", baseStartHeights);
        UpdateMeshHeights(mat, savedMinHeight, savedMaxHeight);
    }

    public void UpdateMeshHeights(Material mat, float minHeight, float maxHeight)
    {
        savedMaxHeight = maxHeight;
        savedMinHeight = minHeight;
        mat.SetFloat("min_height", minHeight);
        mat.SetFloat("max_height", maxHeight);
    }
}
