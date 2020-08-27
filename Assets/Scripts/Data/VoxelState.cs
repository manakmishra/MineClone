using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VoxelState
{
    public int id;

    [NonSerialized] private byte _light;
    [NonSerialized] public ChunkData chunkData;
    [NonSerialized] public VoxelNeighbours neighbours;
    [NonSerialized] public Vector3Int position;

    public byte light
    {
        get { return _light; }
        set 
        {
            if (value != _light)
            {
                byte oldLightValue = _light;
                byte oldLightCast = lightCast;

                _light = value;

                if (_light < oldLightValue)
                {
                    List<int> neighboursToDarken = new List<int>();

                    for(int i=0; i<6; i++)
                    {
                        if (neighbours[i] != null)
                        {
                            if (neighbours[i].light <= oldLightCast)
                                neighboursToDarken.Add(i);
                            else
                            {
                                neighbours[i].PropagateLight();
                            }
                        }
                    }

                    foreach(int i in neighboursToDarken)
                    {
                        neighbours[i].light = 0;
                    }

                    if (chunkData.chunk != null)
                        World.Instance.AddChunkToUpdate(chunkData.chunk);
                }
                else if (_light > 1)
                    PropagateLight();
            }
        }
    }

    public VoxelState(int _id, ChunkData _chunkData, Vector3Int _position)
    {
        id = _id;
        chunkData = _chunkData;
        neighbours = new VoxelNeighbours(this);
        position = _position;
        light = 0;
    }

    public Vector3Int GlobalPosition
    {
        get { return new Vector3Int(position.x + chunkData.position.x, position.y, position.z + chunkData.position.y); }
    }

    public float lightAsFloat
    {
        get { return (float)light * VoxelData.lightUnit; }
    }

    public byte lightCast
    {
        get
        {
            int lightLevel = _light - properties.opacity - 1;
            if (lightLevel < 0) lightLevel = 0;
            return (byte)lightLevel;
        }
    }

    public void PropagateLight()
    {
        //check for dark voxel
        if (light < 2)
            return;

        //loop through neighbouring voxel faces to check light and proceed accordingly
        for (int i = 0; i < 6; i++)
        {
            if (neighbours[i] != null)
            {
                if (neighbours[i].light < lightCast)
                    neighbours[i].light = lightCast;
            }
            if (chunkData.chunk != null)
                World.Instance.AddChunkToUpdate(chunkData.chunk);
        }
    }

    public BlockType properties
    {
        get { return World.Instance.blockTypes[id]; }
    }
}

public class VoxelNeighbours
{
    public readonly VoxelState parent;

    public VoxelNeighbours(VoxelState voxel) 
    { 
        parent = voxel; 
    }

    private VoxelState[] neighbours = new VoxelState[6];    

    public int Length
    {
        get { return neighbours.Length; }
    }

    public VoxelState this[int index]
    {
        get 
        {
            if(neighbours[index] == null)
            {
                neighbours[index] = World.Instance.worldData.GetVoxel(parent.GlobalPosition + VoxelData.adjFaceChecks[index]);
                ReturnNeighbour(index);
            }
            return neighbours[index];
        }
        set 
        { 
            neighbours[index] = value;
            ReturnNeighbour(index);
        }
    }

    void ReturnNeighbour(int index)
    {
        //neighbour can't be set if neighbour is null
        if (neighbours[index] == null)
            return;

        if (neighbours[index].neighbours[VoxelData.revfaceCheckIndex[index]] != parent)
            neighbours[index].neighbours[VoxelData.revfaceCheckIndex[index]] = parent;
    }
}
