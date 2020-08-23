using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Structure : MonoBehaviour
{
    
    public static Queue<WorldVoxelMod> GenerateFlora(int index, Vector3 position, int trunkheightMin, int trunkHeightMax)
    {

        switch(index)
        {
            case 0: 
                return GenerateTree(position, trunkheightMin, trunkHeightMax);

            case 1:
                return GenerateCacti(position, trunkheightMin, trunkHeightMax);
        }

        return new Queue<WorldVoxelMod>();
    }

    public static Queue<WorldVoxelMod> GenerateTree(Vector3 position, int trunkheightMin, int trunkHeightMax)
    {

        Queue<WorldVoxelMod> mods = new Queue<WorldVoxelMod>();

        int height = (int)(trunkHeightMax * Noise.Get2DPerlin(new Vector2(position.x, position.z), 225f, 3f));
        if (height < trunkheightMin)
            height = trunkheightMin;

        for(int i=1; i < height; i++)
        {

            mods.Enqueue(new WorldVoxelMod(new Vector3(position.x, position.y + i, position.z), 6));
            
            for(int x = -3; x<4; x++) {
                for (int y = 0; y < 7; y++) {
                    for (int z = -3; z < 4; z++)
                    {
                        mods.Enqueue(new WorldVoxelMod(new Vector3(position.x + x, position.y + y + height, position.z + z), 11));
                    }
                }
            }
        }

        return mods;
    }

    public static Queue<WorldVoxelMod> GenerateCacti(Vector3 position, int trunkheightMin, int trunkHeightMax)
    {

        Queue<WorldVoxelMod> mods = new Queue<WorldVoxelMod>();

        int height = (int)(trunkHeightMax * Noise.Get2DPerlin(new Vector2(position.x, position.z), 6516f, 1.75f));
        if (height < trunkheightMin)
            height = trunkheightMin;

        for (int i = 1; i < height; i++)
        {  
            mods.Enqueue(new WorldVoxelMod(new Vector3(position.x, position.y + i, position.z), 12));
        }
        mods.Enqueue(new WorldVoxelMod(new Vector3(position.x, position.y + height, position.z), 13));

        return mods;
    }
}
