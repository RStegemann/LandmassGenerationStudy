using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Settings/TextureSettings")]
public class TextureSettings : UpdatableScriptable
{
    private const int TextureSize = 512;
    private const TextureFormat TextureFormat = UnityEngine.TextureFormat.RGB565;
    private float savedMinHeight;
    private float savedMaxHeight;
    public Layer[] layers;
    
    public void ApplyToMaterial(Material mat)
    {
        mat.SetInt("layer_count", layers.Length);
        mat.SetColorArray("base_colours", layers.Select(layer => layer.tint).ToArray());
        mat.SetFloatArray("base_start_heights", layers.Select(layer => layer.startHeight).ToArray());
        mat.SetFloatArray("base_blends", layers.Select((layer) => layer.blendStrength).ToArray());
        mat.SetFloatArray("base_colour_strengths", layers.Select((layer) => layer.tintStrength).ToArray());
        mat.SetFloatArray("base_texture_scales", layers.Select((layer) => layer.textureScale).ToArray());
        Texture2DArray texturesArray = GenerateTextureArray(layers.Select((layer) => layer.texture).ToArray());
        mat.SetTexture("base_textures", texturesArray);
        UpdateMeshHeights(mat, savedMinHeight, savedMaxHeight);
    }

    public void UpdateMeshHeights(Material mat, float minHeight, float maxHeight)
    {
        savedMaxHeight = maxHeight;
        savedMinHeight = minHeight;
        mat.SetFloat("min_height", minHeight);
        mat.SetFloat("max_height", maxHeight);
    }

    private Texture2DArray GenerateTextureArray(Texture2D[] textures)
    {
        Texture2DArray textureArray = new Texture2DArray(
            TextureSize,
            TextureSize,
            textures.Length,
            TextureFormat,
            true
        );
        for (int i = 0; i < textures.Length; i++)
        {
            textureArray.SetPixels(textures[i].GetPixels(), i);
        }
        textureArray.Apply();
        return textureArray;
    }

    [System.Serializable]
    public class Layer
    {
        public Texture2D texture;
        public Color tint;
        [Range(0, 1)]
        public float tintStrength;
        [Range(0, 1)]
        public float startHeight;
        [Range(0, 1)]
        public float blendStrength;
        public float textureScale;
    }
}
