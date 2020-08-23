using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "BiomeAttributes", menuName = "MineCraftClone/BiomeAttributes")]
public class BiomeAttributes : ScriptableObject {
    
    [Header("General Biome Values")]
    public string biomeName;
    public float offset;
    public float scale;

    public int terrainHeight;
    public float terrainScale;

    public byte surfaceBlock;
    public byte subSurfaceBlock;

    [Header("Flora")]
    public int floraIndex;
    public float floraZoneScale = 1.3f;
    [Range(0.1f, 1f)]
    public float floraThreshhold = 0.7f;
    public float floraPlacementScale = 30f;
    [Range(0.1f, 1f)]
    public float floraPlacementThreshold = 0.8f;
    public bool placeFlora = true;

    public int HeightMax = 12;
    public int HeightMin = 4;

    public Lode[] lodes;
}

[System.Serializable]
public class Lode {

    public string nodeName;
    public byte blockID;
    public int minHeight;
    public int maxHeight;
    public float scale;
    public float threshold;
    public float noiseOffset;
}
