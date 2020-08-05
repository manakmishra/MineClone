using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk {

    public ChunkPos pos;

    GameObject chunkObj;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    int vertIndex = 0;
    List<Vector3> vertices = new List<Vector3> ();
    List<int> triangles = new List<int> ();
    List<Vector2> uvs = new List<Vector2>();

    public byte[,,] voxelMap = new byte[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];

    World world;

    public Chunk(ChunkPos _pos, World _world) {

        //references
        pos = _pos;
        world = _world;
        chunkObj = new GameObject();
        meshFilter = chunkObj.AddComponent<MeshFilter>();
        meshRenderer = chunkObj.AddComponent<MeshRenderer>();

        meshRenderer.material = world.material;
        chunkObj.transform.SetParent(world.transform);
        chunkObj.transform.position = new Vector3(pos.x * VoxelData.chunkWidth, 0f, pos.z * VoxelData.chunkWidth);
        chunkObj.name = "Chunk " + pos.x + ", " + pos.z;

        initVoxelMap();
        genMeshData();
        CreateMesh();
    }

    void initVoxelMap() {
        for(int y=0; y<VoxelData.chunkHeight; y++) {
            for(int x=0; x<VoxelData.chunkWidth; x++) {
                for(int z=0; z<VoxelData.chunkWidth; z++) {
                    
                    voxelMap[x, y, z] = world.GetVoxel(new Vector3(x, y, z) + position);
                }
            }
        }
    }

    void genMeshData() {
        for(int y=0; y<VoxelData.chunkHeight; y++) {
            for(int x=0; x<VoxelData.chunkWidth; x++) {
                for(int z=0; z<VoxelData.chunkWidth; z++) {

                    if(world.blockTypes[voxelMap[x, y, z]].isSolid)
                        AddtoChunk(new Vector3(x, y, z));

                }
            }
        }
    }

    public bool isActive {

        get { return chunkObj.activeSelf; }
        set { chunkObj.SetActive(value); }
    }

    public Vector3 position {
        get { return chunkObj.transform.position; }
    }

    bool isVoxelInChunk(int x, int y, int z) {
        if(x<0 || x>VoxelData.chunkWidth-1 || y<0 || y>VoxelData.chunkHeight-1 || z<0 || z>VoxelData.chunkWidth-1)
            return false;
        else
            return true;    
    }

    bool checkVoxel(Vector3 pos) {

        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if(!isVoxelInChunk(x, y, z))
            return world.blockTypes[world.GetVoxel(pos + position)].isSolid;
        
        return world.blockTypes[voxelMap[x, y, z]].isSolid;

    }

    void AddtoChunk(Vector3 pos) {
        
        for(int i=0; i<6; i++) {

            if(!checkVoxel(pos + VoxelData.adjFaceChecks[i])) {

                byte blockID = voxelMap[(int)pos.x, (int)pos.y, (int)pos.z];

                vertices.Add(pos + VoxelData.voxelVert[VoxelData.voxelTriangles[i, 0]]);
                vertices.Add(pos + VoxelData.voxelVert[VoxelData.voxelTriangles[i, 1]]);
                vertices.Add(pos + VoxelData.voxelVert[VoxelData.voxelTriangles[i, 2]]);
                vertices.Add(pos + VoxelData.voxelVert[VoxelData.voxelTriangles[i, 3]]);
                
                AddTexture(world.blockTypes[blockID].GetTextureID(i));

                triangles.Add(vertIndex);
                triangles.Add(vertIndex+1);
                triangles.Add(vertIndex+2);
                triangles.Add(vertIndex+2);
                triangles.Add(vertIndex+1);
                triangles.Add(vertIndex+3);
                vertIndex+=4;
            }
        }
    }

    void CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    void AddTexture(int textureID) {

        float y = textureID/VoxelData.textureAtlasSizeInBlocks;
        float x = textureID - (y * VoxelData.textureAtlasSizeInBlocks);

        x *= VoxelData.NormalizedBlockTextureSize;
        y *= VoxelData.NormalizedBlockTextureSize;

        y = 1f - y - VoxelData.NormalizedBlockTextureSize;

        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y + VoxelData.NormalizedBlockTextureSize));
        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y));
        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y + VoxelData.NormalizedBlockTextureSize));
    }

}

public class ChunkPos {

    public int x;
    public int z;

    public ChunkPos(int x, int z) {
        this.x = x;
        this.z = z;
    }

    public bool Equals (ChunkPos other) {

        if(other == null)
            return false;
        else if(other.x == x && other.z == z)
            return true;
        else    
            return false;
    }
}