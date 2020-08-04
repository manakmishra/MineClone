using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    
    public static float Get2DPerlin (Vector2 position, float offset, float scale) {

        return Mathf.PerlinNoise((position.x + 0.1f) / VoxelData.chunkWidth * scale + offset, (position.y + 0.1f) / VoxelData.chunkWidth * scale + offset); 
    }

    public static bool Get3DPerlin (Vector3 position, float offset, float scale, float threshold) {

        float x = (position.x + 0.1f + offset) * scale;
        float y = (position.y + 0.1f + offset) * scale;
        float z = (position.z + 0.1f + offset) * scale;

        float AB = Mathf.PerlinNoise(x, y);
        float BC = Mathf.PerlinNoise(y, z);
        float AC = Mathf.PerlinNoise(x, z);
        float BA = Mathf.PerlinNoise(y, x);
        float CB = Mathf.PerlinNoise(z, y);
        float CA = Mathf.PerlinNoise(z, x);

        if((AB+BC+AC+BA+CB+CA) / 6f > threshold)
            return true;
        else return false;
    }
}
