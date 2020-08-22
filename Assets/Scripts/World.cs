﻿using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.IO;

public class World : MonoBehaviour
{
    public UserSettings settings;

    public BiomeAttributes biome;

    [Range(0f, 1f)]
    public float globalLightLevel;
    public Color day;
    public Color night; 

    public Transform player;
    public Vector3 spawnPosition;

    public Material material;
    public Material transparentMaterial;
    public BlockType[] blockTypes;

    Chunk[,] chunks = new Chunk[VoxelData.worldSizeInChunks, VoxelData.worldSizeInChunks];

    List<ChunkPos> activeChunks = new List<ChunkPos>();
    public ChunkPos playerChunkPos;
    ChunkPos playerLastChunkPos;

    List<ChunkPos> chunksToBeCreated = new List<ChunkPos>();
    public List<Chunk> chunksToUpdate = new List<Chunk>();
    public Queue<Chunk> chunksToDraw = new Queue<Chunk>();

    bool modsApplying = false;

    Queue<Queue<WorldVoxelMod>> mods = new Queue<Queue<WorldVoxelMod>>();

    private bool _uiActive = false;

    public GameObject debug;

    public GameObject inventoryWindow;
    public GameObject cursorSlot;

    Thread _chunkUpdateThread;
    public object _updateThreadLock = new object();

    private void Start()
    {

        //string settingsExport = JsonUtility.ToJson(settings);
        //File.WriteAllText(Application.dataPath + "/game.cfg", settingsExport);

        string settingsImport = File.ReadAllText(Application.dataPath + "/game.cfg");
        settings = JsonUtility.FromJson<UserSettings>(settingsImport);

        Random.InitState(settings.seed);

        Shader.SetGlobalFloat("MinGlobalLightLevel", VoxelData.minLightLevel);
        Shader.SetGlobalFloat("MaxGlobalLightLevel", VoxelData.maxLightLevel);

        if (settings.enableMultiThreading)
        {
            _chunkUpdateThread = new Thread(new ThreadStart(ThreadedUpdate));
            _chunkUpdateThread.Start();
        }

        SetGlobalLightValue();
        spawnPosition = new Vector3((VoxelData.worldSizeInChunks * VoxelData.chunkWidth) / 2f, VoxelData.chunkHeight - 50f, (VoxelData.worldSizeInChunks * VoxelData.chunkWidth) / 2f);
        GenerateWorld();
        playerLastChunkPos = GetChunkCoordFromPosition(player.position); 
    }

    private void Update()
    {

        playerChunkPos = GetChunkCoordFromPosition(player.position);

        if (!playerChunkPos.Equals(playerLastChunkPos))
            CheckViewDistance();

        if (chunksToBeCreated.Count > 0)
            CreateChunk();

        if (chunksToDraw.Count > 0)
        {

            if (chunksToDraw.Peek().IsEditable)
                chunksToDraw.Dequeue().CreateMesh();
        }

        if (!settings.enableMultiThreading)
        {
            if (!modsApplying)
                ApplyModifications();

            if (chunksToUpdate.Count > 0)
                UpdateChunks();
        }

        if (Input.GetKeyDown(KeyCode.F3))
            debug.SetActive(!debug.activeSelf);
    }

    void GenerateWorld()
    {

        for (int x = (VoxelData.worldSizeInChunks / 2) - settings.viewDistanceInChunks; x < (VoxelData.worldSizeInChunks / 2) + settings.viewDistanceInChunks; x++)
        {
            for (int z = (VoxelData.worldSizeInChunks / 2) - settings.viewDistanceInChunks; z < (VoxelData.worldSizeInChunks / 2) + settings.viewDistanceInChunks; z++)
            {
                ChunkPos newChunk = new ChunkPos(x, z);
                chunks[x, z] = new Chunk(newChunk, this);
                chunksToBeCreated.Add(newChunk);
            }
        }

        player.position = spawnPosition;
        CheckViewDistance();
    }

    public void SetGlobalLightValue()
    {
        Shader.SetGlobalFloat("GlobalLightLevel", globalLightLevel);
        Camera.main.backgroundColor = Color.Lerp(night, day, globalLightLevel);
    }

    void CreateChunk()
    {

        ChunkPos p = chunksToBeCreated[0];
        chunksToBeCreated.RemoveAt(0);
        chunks[p.x, p.z].Init();
    }

    void UpdateChunks()
    {

        bool updated = false;
        int index = 0;

        lock (_updateThreadLock)
        {
            while (!updated && index < chunksToUpdate.Count - 1)
            {

                if (chunksToUpdate[index].IsEditable)
                {
                    chunksToUpdate[index].UpdateChunk();

                    if(!activeChunks.Contains(chunksToUpdate[index].pos))
                        activeChunks.Add(chunksToUpdate[index].pos);

                    chunksToUpdate.RemoveAt(index);
                    updated = true;
                }
                else index++;
            }
        }
    }

    void ThreadedUpdate()
    {
        while(true)
        {
            if (!modsApplying)
                ApplyModifications();

            if (chunksToUpdate.Count > 0)
                UpdateChunks();
        }
    }

    private void OnDisable()
    {
        if (settings.enableMultiThreading)
            _chunkUpdateThread.Abort();
    }

    void ApplyModifications()
    {

        modsApplying = true;

        while (mods.Count > 0)
        {

            Queue<WorldVoxelMod> queue = mods.Dequeue();

            while (queue.Count > 0)
            {
                WorldVoxelMod mod = queue.Dequeue(); 
                ChunkPos p = GetChunkCoordFromPosition(mod.position);

                if (chunks[p.x, p.z] == null)
                {
                    chunks[p.x, p.z] = new Chunk(p, this);
                    chunksToBeCreated.Add(p);
                }

                chunks[p.x, p.z].mods.Enqueue(mod);
            }
        }
        modsApplying = false;
    }

    ChunkPos GetChunkCoordFromPosition(Vector3 pos)
    {

        int x = Mathf.FloorToInt(pos.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.chunkWidth);
        return new ChunkPos(x, z);
    }

    public Chunk GetChunkFromPosition(Vector3 pos)
    {

        int x = Mathf.FloorToInt(pos.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.chunkWidth);
        return chunks[x, z];
    }

    void CheckViewDistance()
    {

        ChunkPos pos = GetChunkCoordFromPosition(player.position);
        playerLastChunkPos = playerChunkPos;

        List<ChunkPos> previouslyActiveChunks = new List<ChunkPos>(activeChunks);
        activeChunks.Clear();

        for (int x = pos.x - settings.viewDistanceInChunks; x < pos.x + settings.viewDistanceInChunks; x++)
        {
            for (int z = pos.z - settings.viewDistanceInChunks; z < pos.z + settings.viewDistanceInChunks; z++)
            {

                ChunkPos thisChunkPos = new ChunkPos(x, z);

                if (IsChunkInWorld(thisChunkPos))
                {

                    if (chunks[x, z] == null)
                    {
                        chunks[x, z] = new Chunk(thisChunkPos, this);
                        chunksToBeCreated.Add(thisChunkPos);
                    }
                    else if (!chunks[x, z].IsActive)
                    {
                        chunks[x, z].IsActive = true;
                    }
                    activeChunks.Add(thisChunkPos);

                }

                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {

                    if (previouslyActiveChunks[i].Equals(thisChunkPos))
                        previouslyActiveChunks.RemoveAt(i);
                }
            }
        }

        foreach (ChunkPos c in previouslyActiveChunks)
            chunks[c.x, c.z].IsActive = false;
    }

    public bool CheckVoxelCollider(Vector3 pos)
    {

        ChunkPos thisChunk = new ChunkPos(pos);

        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y > VoxelData.chunkHeight)
            return false;

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].IsEditable)
            return blockTypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalPosition(pos).id].isSolid;

        return blockTypes[GetVoxel(pos)].isSolid;
    }

    public VoxelState GetVoxelState(Vector3 pos)
    {

        ChunkPos thisChunk = new ChunkPos(pos);

        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y > VoxelData.chunkHeight)
            return new VoxelState(-1);

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].IsEditable)
            return chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalPosition(pos);

        return new VoxelState(GetVoxel(pos));
    }

    public byte GetVoxel(Vector3 pos)
    {

        int yPos = Mathf.FloorToInt(pos.y);

        /* Boundary conditions*/
        //if outside world
        if (!IsVoxelInWorld(pos))
            return 0;
        //if bottom block then return bedrock
        if (yPos == 0)
            return 1;

        /*Basic terrain conditions*/
        int terrainHeight = Mathf.FloorToInt(biome.terrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 2.5f, biome.terrainScale)) + biome.groundHeight;
        byte voxelValue = 0;

        if (yPos == terrainHeight)
            voxelValue = 3;
        else if (yPos < terrainHeight && yPos > terrainHeight - 4)
            voxelValue = 5;
        else if (yPos > terrainHeight)
            return 0;
        else
            voxelValue = 2;

        /* Second Pass for dirt and ores */
        if (voxelValue == 2)
        {
            foreach (Lode lode in biome.lodes)
            {
                if (yPos > lode.minHeight && yPos < lode.maxHeight)
                    if (Noise.Get3DPerlin(pos, lode.noiseOffset, lode.scale, lode.threshold))
                        voxelValue = lode.blockID;
            }
        }

        /* Third pass for trees */
        if (yPos == terrainHeight)
        {
            if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 2.5f, biome.treeZoneScale) > biome.treeThreshhold)
            {
                if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 2.5f, biome.treePlacementScale) > biome.treePlacementThreshold)
                {
                    mods.Enqueue(Structure.GenerateTree(pos, biome.treeHeightMin, biome.treeHeightMax)); 
                }
            }
        }

        return voxelValue;
    }

    public bool uiActive
    {
        get { return _uiActive; }
        set
        {
            _uiActive = value;
            if (_uiActive)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                inventoryWindow.SetActive(true);
                cursorSlot.SetActive(true);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                inventoryWindow.SetActive(false);
                cursorSlot.SetActive(false);
            }
        }
    }

    bool IsChunkInWorld(ChunkPos pos)
    {
        if (pos.x > 0 && pos.x < VoxelData.worldSizeInChunks - 1 && pos.z > 0 && pos.z < VoxelData.worldSizeInChunks - 1)
            return true;
        else
            return false;
    }

    bool IsVoxelInWorld(Vector3 pos)
    {

        if (pos.x >= 0 && pos.x < VoxelData.worldSizeInVoxels && pos.y >= 0 && pos.y < VoxelData.chunkHeight && pos.z >= 0 && pos.z < VoxelData.worldSizeInVoxels)
            return true;
        else
            return false;
    }
}

[System.Serializable]
public class BlockType
{

    public string blockName;
    public bool isSolid;
    public bool renderNeighbourFaces;
    public float transparency;
    public Sprite icon;

    [Header("TextureValues")]
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;

    public int GetTextureID(int faceIndex)
    {

        switch (faceIndex)
        {
            case 0: return backFaceTexture;
            case 1: return frontFaceTexture;
            case 2: return topFaceTexture;
            case 3: return bottomFaceTexture;
            case 4: return leftFaceTexture;
            case 5: return rightFaceTexture;
            default:
                Debug.Log("Error in GetTextureID");
                return 0;
        }
    }

}

public class WorldVoxelMod
{

    public Vector3 position;
    public byte id;

    public WorldVoxelMod()
    {
        id = 0;
        position = new Vector3();
    }

    public WorldVoxelMod(Vector3 _position, byte _id)
    {

        position = _position;
        id = _id;
    }
}

[System.Serializable]
public class UserSettings
{
    [Header("Game Data")]
    public string version;

    [Header("General Game Settings")]
    public int seed;

    [Header("Performance")]
    public bool enableMultiThreading;
    public int viewDistanceInChunks;

    [Header("Controls")]
    [Range(0.15f, 15f)]
    public float mouseSensitivity;
}