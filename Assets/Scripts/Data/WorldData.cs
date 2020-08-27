using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

[System.Serializable]
public class WorldData
{
    public string worldName = "Prototype";
    public int seed;
    
    [System.NonSerialized]
    public Dictionary<Vector2Int, ChunkData> chunks = new Dictionary<Vector2Int, ChunkData>();

    [System.NonSerialized]
    public List<ChunkData> modifiedChunks = new List<ChunkData>(); 

    public WorldData(string _worldName, int _seed)
    {
        worldName = _worldName;
        seed = _seed;
    }

    public WorldData(WorldData world)
    {
        worldName = world.worldName;
        seed = world.seed;
    }

    public void AddModifiedChunk(ChunkData chunk)
    {
        if (!modifiedChunks.Contains(chunk))
            modifiedChunks.Add(chunk);
    }

    public ChunkData RequestChunk(Vector2Int pos, bool create)
    {
        ChunkData temp;

        lock (World.Instance._listThreadLock)
        {
            if (chunks.ContainsKey(pos))
                temp = chunks[pos];
            else if (!create)
                temp = null;
            else
            {
                LoadChunk(pos);
                temp = chunks[pos];
            }
        }
        return temp;
    }

    public void LoadChunk(Vector2Int pos)
    {
        if (chunks.ContainsKey(pos))
            return;

        ChunkData chunk = WorldSaveSystem.LoadChunk(worldName, pos);
        if(chunk != null)
        {
            chunks.Add(pos, chunk);
            return;
        }

        chunks.Add(pos, new ChunkData(pos));
        chunks[pos].Populate();
    }

    bool IsVoxelInWorld(Vector3 pos)
    {
        if (pos.x >= 0 && pos.x < VoxelData.worldSizeInVoxels && pos.y >= 0 && pos.y < VoxelData.chunkHeight && pos.z >= 0 && pos.z < VoxelData.worldSizeInVoxels)
            return true;
        else
            return false;
    }

    public void SetVoxel(Vector3 pos, byte value)
    {
        if (!IsVoxelInWorld(pos))
            return;

        //get the whole number coordinates from floor Vector3 pos
        int x = Mathf.FloorToInt(pos.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z   / VoxelData.chunkWidth);
        x *= VoxelData.chunkWidth;
        z *= VoxelData.chunkWidth;

        ChunkData chunk = RequestChunk(new Vector2Int(x, z), true);

        Vector3Int voxel = new Vector3Int((int)(pos.x - x), (int)pos.y, (int)(pos.z - z));

        chunk.ModifyVoxel(voxel, value);
    }

    public VoxelState GetVoxel (Vector3 pos)
    {
        if (!IsVoxelInWorld(pos))
            return null;

        //get the whole number coordinates from floor Vector3 pos
        int x = Mathf.FloorToInt(pos.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.chunkWidth);
        x *= VoxelData.chunkWidth;
        z *= VoxelData.chunkWidth;

        ChunkData chunk = RequestChunk(new Vector2Int(x, z), false);

        if (chunk == null)
            return null;

        Vector3Int voxel = new Vector3Int((int)(pos.x - x), (int)pos.y, (int)(pos.z - z));

        return chunk.map[voxel.x, voxel.y, voxel.z];
    }
}
