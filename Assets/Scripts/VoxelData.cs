using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VoxelData
{
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

    public static readonly int[,] voxelTriangles = new int[6, 6] {
        {0, 3, 1, 1, 3, 2}, //backFace
        {5, 6, 4, 4, 6, 7}, //frontFace
        {3, 7, 2, 2, 7, 6}, //topFace
        {1, 5, 0, 0, 5, 4}, //bottomFace
        {4, 7, 0, 0, 7, 3}, //leftFace
        {1, 2, 5, 5, 2, 6}, //rightFace  
    };

    public static readonly Vector2[] voxeluvs = new Vector2[6] {
        new Vector2(0.0f, 0.0f),
        new Vector2(0.0f, 1.0f),
        new Vector2(1.0f, 0.0f),
        new Vector2(1.0f, 0.0f),
        new Vector2(0.0f, 1.0f),
        new Vector2(1.0f, 1.0f),
    };

}
