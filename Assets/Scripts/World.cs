using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{

    public int seed;
    public BiomeAttributes biome;

    public Transform player;
    public Vector3 spawnPosition;

    public Material material;
    public BlockType[] blockTypes;

    Chunk[,] chunks = new Chunk[VoxelData.worldSizeInChunks, VoxelData.worldSizeInChunks];

    List<ChunkPos> renderedChunks = new List<ChunkPos>();
    public ChunkPos playerChunkPos;
    ChunkPos playerLastChunkPos;

    List<ChunkPos> chunksToBeCreated = new List<ChunkPos>();
    private bool isCreatingChunks;

    public GameObject debug;

    private void Start()
    {

        Random.InitState(seed);
        debug.SetActive(false);
        spawnPosition = new Vector3((VoxelData.worldSizeInChunks * VoxelData.chunkWidth) / 2f, VoxelData.chunkHeight - 50f, (VoxelData.worldSizeInChunks * VoxelData.chunkWidth) / 2f);
        GenerateWorld();
        playerLastChunkPos = GetChunkFromPosition(player.position);
    }

    private void Update()
    {

        playerChunkPos = GetChunkFromPosition(player.position);

        if (!playerChunkPos.Equals(playerLastChunkPos))
            CheckViewDistance();

        if (chunksToBeCreated.Count > 0 && !isCreatingChunks)
            StartCoroutine("CreateChunks");

        if (Input.GetKeyDown(KeyCode.F3))
            debug.SetActive(!debug.activeSelf);
    }

    void GenerateWorld()
    {

        for (int x = (VoxelData.worldSizeInChunks / 2) - VoxelData.viewDistanceInChunks; x < (VoxelData.worldSizeInChunks / 2) + VoxelData.viewDistanceInChunks; x++)
        {
            for (int z = (VoxelData.worldSizeInChunks / 2) - VoxelData.viewDistanceInChunks; z < (VoxelData.worldSizeInChunks / 2) + VoxelData.viewDistanceInChunks; z++)
            {
                chunks[x, z] = new Chunk(new ChunkPos(x, z), this, true);
                renderedChunks.Add(new ChunkPos(x, z));
            }
        }

        player.position = spawnPosition;
    }

    IEnumerator CreateChunks()
    {

        isCreatingChunks = true;

        while (chunksToBeCreated.Count > 0)
        {

            chunks[chunksToBeCreated[0].x, chunksToBeCreated[0].z].Init();
            chunksToBeCreated.RemoveAt(0);
            yield return null;
        }
        isCreatingChunks = false;
    }

    ChunkPos GetChunkFromPosition(Vector3 pos)
    {

        int x = Mathf.FloorToInt(pos.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.chunkWidth);
        return new ChunkPos(x, z);
    }

    void CheckViewDistance()
    {

        ChunkPos pos = GetChunkFromPosition(player.position);
        playerLastChunkPos = playerChunkPos;

        List<ChunkPos> previouslyActiveChunks = new List<ChunkPos>(renderedChunks);

        for (int x = pos.x - VoxelData.viewDistanceInChunks; x < pos.x + VoxelData.viewDistanceInChunks; x++)
        {
            for (int z = pos.z - VoxelData.viewDistanceInChunks; z < pos.z + VoxelData.viewDistanceInChunks; z++)
            {
                if (IsChunkInWorld(new ChunkPos(x, z)))
                {

                    if (chunks[x, z] == null)
                    {
                        chunks[x, z] = new Chunk(new ChunkPos(x, z), this, false);
                        chunksToBeCreated.Add(new ChunkPos(x, z));
                    }
                    else if (!chunks[x, z].isActive)
                    {
                        chunks[x, z].isActive = true;
                    }
                    renderedChunks.Add(new ChunkPos(x, z));

                }

                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {

                    if (previouslyActiveChunks[i].Equals(new ChunkPos(x, z)))
                        previouslyActiveChunks.RemoveAt(i);
                }
            }
        }

        foreach (ChunkPos c in previouslyActiveChunks)
            chunks[c.x, c.z].isActive = false;
    }

    public bool CheckVoxelCollider(Vector3 pos)
    {

        ChunkPos thisChunk = new ChunkPos(pos);

        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y > VoxelData.chunkHeight)
            return false;

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isVoxelMapPopulated)
            return blockTypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalPosition(pos)].isSolid;

        return blockTypes[GetVoxel(pos)].isSolid;
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
        int terrainHeight = Mathf.FloorToInt(biome.terrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.terrainScale)) + biome.groundHeight;
        byte voxelValue = 0;

        if (yPos == terrainHeight)
            voxelValue = 3;
        else if (yPos < terrainHeight && yPos > terrainHeight - 4)
            voxelValue = 5;
        else if (yPos > terrainHeight)
            return 0;
        else
            voxelValue = 2;

        /* Second Pass for detail */
        if (voxelValue == 2)
        {

            foreach (Lode lode in biome.lodes)
            {

                if (yPos > lode.minHeight && yPos < lode.maxHeight)
                    if (Noise.Get3DPerlin(pos, lode.noiseOffset, lode.scale, lode.threshold))
                        voxelValue = lode.blockID;
            }
        }
        return voxelValue;
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