using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Settings/TerrainSettings")]
public class MeshSettings : UpdatableScriptable
{
    public const int NumSupportedLoDs = 5;
    public const int NumSupportedChunkSizes = 9;
    public const int NumSupportedFlatShadedChunkSizes = 3;
    public static readonly int[] SupportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };
    
    [Range(0, NumSupportedChunkSizes - 1)]
    public int chunkSizeIndex;
    [Range(0, NumSupportedFlatShadedChunkSizes - 1)]
    public int flatShadedChunkSizeIndex;
    
    [FormerlySerializedAs("uniformScale")] public float meshScale = 5f;
    public bool useFlatShading;

    /* num verts per line of mesh rendered at LOD = 0. Includes the 2 extra verts that are excluded from final mesh, 
     but used for calculating normals as well as 2 extra verts for high res borders to make chunks of all LODs connect
     without gaps*/
    public int NumVertsPerLine => SupportedChunkSizes[useFlatShading ? flatShadedChunkSizeIndex : chunkSizeIndex] + 5;

    public float MeshWorldSize => (NumVertsPerLine - 3) * meshScale;
}
