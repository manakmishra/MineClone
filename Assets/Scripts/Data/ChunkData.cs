using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChunkData
{
    int x;
    int y;
    public Vector2Int position
    {
        get { return new Vector2Int(x, y); }
        set
        {
            x = value.x;
            y = value.y;
        }
    }

    public ChunkData(Vector2Int pos)
    {
        position = pos;
    }

    public ChunkData(int _x, int _y)
    {
        x = _x;
        y = _y;
    }

    [NonSerialized] public Chunk chunk;

    [HideInInspector]
    public VoxelState[,,] map = new VoxelState[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];

    public void Populate()
    {
        for (int y = 0; y < VoxelData.chunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.chunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.chunkWidth; z++)
                {
                    Vector3 globalVoxelPos = new Vector3(x + position.x, y, z + position.y);
                    map[x, y, z] = new VoxelState(World.Instance.GetVoxel(globalVoxelPos), this, new Vector3Int(x, y, z));
                     
                    //loop through voxel's neighbour and try to set them
                    for(int i = 0; i<6; i++)
                    {
                        Vector3Int neighbourVector = new Vector3Int(x, y, z) + VoxelData.adjFaceChecks[i];
                        if (IsVoxelInChunk(neighbourVector))
                            map[x, y, z].neighbours[i] = VoxelFromPosition(neighbourVector);
                        else
                            map[x, y, z].neighbours[i] = World.Instance.worldData.GetVoxel(globalVoxelPos + VoxelData.adjFaceChecks[i]);
                    }
                }
            }
        }
        Lighting.RecalculateNaturalLight(this);

        World.Instance.worldData.AddModifiedChunk(this);
    }

    public void ModifyVoxel(Vector3Int pos, byte _id)
    {
        if (map[pos.x, pos.y, pos.z].id == _id)
            return;

        VoxelState voxel = map[pos.x, pos.y, pos.z];
        BlockType newVoxel = World.Instance.blockTypes[_id];

        //old opacity value
        byte oldOpacity = voxel.properties.opacity;
        voxel.id = _id;

        if(voxel.properties.opacity != oldOpacity && 
            (pos.y == VoxelData.chunkHeight - 1 || map[pos.x, pos.y + 1, pos.z].light == 15))
        {
            Lighting.NaturalLight(this, pos.x, pos.z, pos.y + 1);
        }

        World.Instance.worldData.AddModifiedChunk(this);

        if (chunk != null)
            World.Instance.AddChunkToUpdate(chunk);
    }

    public bool IsVoxelInChunk(int x, int y, int z)
    {
        if (x < 0 || x > VoxelData.chunkWidth - 1 || y < 0 || y > VoxelData.chunkHeight - 1 || z < 0 || z > VoxelData.chunkWidth - 1)
            return false;
        else
            return true;
    }

    public bool IsVoxelInChunk(Vector3Int pos)
    {
        return IsVoxelInChunk(pos.x, pos.y, pos.z);
    }

    public VoxelState VoxelFromPosition(Vector3Int pos)
    {
        return map[pos.x, pos.y, pos.z];
    }
}
