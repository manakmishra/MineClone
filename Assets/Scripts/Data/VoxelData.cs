using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VoxelData
{

    public static readonly int chunkWidth = 16;
    public static readonly int chunkHeight = 128;
    public static readonly int worldSizeInChunks = 100;

    //lighting
    public static float minLightLevel = 0.15f;
    public static float maxLightLevel = 0.8f;

    public static float lightUnit
    {
        get { return 0.0625f; }
    }

    public static int seed;

    public static int worldCentre
    {
        get { return (worldSizeInChunks) * chunkWidth / 2; }
    }

    public static int worldSizeInVoxels {

        get { return worldSizeInChunks * chunkWidth; }
    } 

    public static readonly int textureAtlasSizeInBlocks = 16;
    
    public static float NormalizedBlockTextureSize {

        get{return 1f /(float)textureAtlasSizeInBlocks;}
    }

    public static readonly Vector3[] voxelVert = new Vector3[8] {
        
        new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 1.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 1.0f),
    };

    public static readonly Vector3Int[] adjFaceChecks = new Vector3Int[6] {

        new Vector3Int(0, 0, -1), //backFace
        new Vector3Int(0, 0, 1), //frontFace
        new Vector3Int(0, 1, 0), //topFace
        new Vector3Int(0, -1, 0), //bottomFace
        new Vector3Int(-1, 0, 0), //leftFace
        new Vector3Int(1, 0, 0), //rightFace
    };

    public static readonly int[] revfaceCheckIndex = new int[6] { 1, 0, 3, 2, 5, 4 };

    public static readonly int[,] voxelTriangles = new int[6, 4] {

        {0, 3, 1, 2}, //backFace
        {5, 6, 4, 7}, //frontFace
        {3, 7, 2, 6}, //topFace
        {1, 5, 0, 4}, //bottomFace
        {4, 7, 0, 3}, //leftFace
        {1, 2, 5, 6}, //rightFace  
    };

    public static readonly Vector2[] voxeluvs = new Vector2[4] {
        
        new Vector2(0.0f, 0.0f),
        new Vector2(0.0f, 1.0f),
        new Vector2(1.0f, 0.0f),
        new Vector2(1.0f, 1.0f),
    };

}
