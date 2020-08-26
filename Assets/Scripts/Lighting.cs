using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Lighting
{
    public static void RecalculateNaturalLight(ChunkData chunk)
    {
        for(int i =0; i<VoxelData.chunkWidth; i++)
        {
            for(int j =0; j<VoxelData.chunkWidth; j++)
            {
                NaturalLight(chunk, x, z, VoxelData.chunkHeight - 1);
            }
        }
    }

    public static void NaturalLight(ChunkData chunk, int x, int z, int startY)
    {
        if(startY > VoxelData.chunkHeight - 1)
        {
            startY = VoxelData.chunkHeight - 1;
        }

        //check if light has hit an opaque or translucent block
        bool lightObstructed = false;

        for(int y = startY; y > -1; y--)
        {
            VoxelState voxel = chunk.map[x, y, z];

            if (lightObstructed)
                voxel.light = 0;
            else if (voxel.properties.opacity > 0)
            {
                voxel.light = 0;
                lightObstructed = true;
            }
            else voxel.light = 15;
        }
    }
}
