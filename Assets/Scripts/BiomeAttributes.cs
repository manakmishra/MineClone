using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "BiomeAttributes", menuName = "MineCraftClone/BiomeAttributes")]
public class BiomeAttributes : ScriptableObject {
    
    public string biomeName;

    public int groundHeight;
    public int terrainHeight;
    public float terrainScale;

    [Header("Trees")]
    public float treeZoneScale = 1.3f;
    [Range(0.1f, 1f)]
    public float treeThreshhold = 0.7f;
    public float treePlacementScale = 30f;
    [Range(0.1f, 1f)]
    public float treePlacementThreshold = 0.8f;

    public int treeHeightMax = 12;
    public int treeHeightMin = 4;

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
