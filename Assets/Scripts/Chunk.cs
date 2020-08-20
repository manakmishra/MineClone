using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Collections.Concurrent;

public class Chunk {

    public ChunkPos pos;

    GameObject chunkObj;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    int vertIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<int> transparentTriangles = new List<int>();
    Material[] materials = new Material[2];
    List<Vector2> uvs = new List<Vector2>();
    List<Color> colors = new List<Color>();

    public Vector3 position;

    public byte[,,] voxelMap = new byte[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];

    World world;

    public Queue<WorldVoxelMod> mods = new Queue<WorldVoxelMod>();

    private bool _isActive;
    private bool isVoxelMapPopulated = false;
    public bool locked = false;

    public Chunk(ChunkPos _pos, World _world, bool generateOnLoad) {

        //references
        pos = _pos;
        world = _world;
        IsActive = true;

        if (generateOnLoad)
            Init();
    }

    public void Init()
    {

        chunkObj = new GameObject();
        meshFilter = chunkObj.AddComponent<MeshFilter>();
        meshRenderer = chunkObj.AddComponent<MeshRenderer>();

        //materials[0] = world.material;
        //materials[1] = world.transparentMaterial;
        meshRenderer.material = world.material;

        chunkObj.transform.SetParent(world.transform);
        chunkObj.transform.position = new Vector3(pos.x * VoxelData.chunkWidth, 0f, pos.z * VoxelData.chunkWidth);
        chunkObj.name = "Chunk " + pos.x + ", " + pos.z;
        position = chunkObj.transform.position;

        Thread InitVoxelMapThread = new Thread(new ThreadStart(InitVoxelMap));
        InitVoxelMapThread.Start();
    }

    void InitVoxelMap() {
        for (int y = 0; y < VoxelData.chunkHeight; y++) {
            for (int x = 0; x < VoxelData.chunkWidth; x++) {
                for (int z = 0; z < VoxelData.chunkWidth; z++) {

                    voxelMap[x, y, z] = world.GetVoxel(new Vector3(x, y, z) + position);
                }
            }
        }

        _updateChunk();
        isVoxelMapPopulated = true;
    }

    public void UpdateChunk()
    {

        Thread updateChunkThread = new Thread(new ThreadStart(_updateChunk));
        updateChunkThread.Start();
    }

    private void _updateChunk() {

        locked = true;

        while(mods.Count > 0)
        {

            WorldVoxelMod mod = mods.Dequeue();
            Vector3 pos = mod.position -= position;
            voxelMap[(int)pos.x, (int)pos.y, (int)pos.z] = mod.id;
        }

        ClearMeshData();

        for(int y=0; y<VoxelData.chunkHeight; y++) {
            for(int x=0; x<VoxelData.chunkWidth; x++) {
                for(int z=0; z<VoxelData.chunkWidth; z++) {

                    if(world.blockTypes[voxelMap[x, y, z]].isSolid)
                        UpdateMesh(new Vector3(x, y, z));

                }
            }
        }

        lock (world.chunksToDraw)
        {
            world.chunksToDraw.Enqueue(this);
        }

        locked = false;
    }

    void ClearMeshData()
    {

        vertIndex = 0;
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
        colors.Clear();
    }

    public bool IsActive {

        get { return _isActive; }
        set {
            _isActive = value;
            if (chunkObj != null)
                chunkObj.SetActive(value);
        }
    }

    public bool IsEditable
    {
        get
        {
            if (!isVoxelMapPopulated || locked)
                return false;
            else return true;
        }
    }

    bool IsVoxelInChunk(int x, int y, int z) {
        if(x<0 || x>VoxelData.chunkWidth-1 || y<0 || y>VoxelData.chunkHeight-1 || z<0 || z>VoxelData.chunkWidth-1)
            return false;
        else
            return true;    
    }

    public void EditVoxelData(Vector3 pos, byte newID)
    {

        int checkX = Mathf.FloorToInt(pos.x);
        int checkY = Mathf.FloorToInt(pos.y);
        int checkZ = Mathf.FloorToInt(pos.z);

        checkX -= Mathf.FloorToInt(chunkObj.transform.position.x);
        checkZ -= Mathf.FloorToInt(chunkObj.transform.position.z);

        voxelMap[checkX, checkY, checkZ] = newID;

        UpdateSurroundingVoxels(checkX, checkY, checkZ);

        _updateChunk();
    }

    void UpdateSurroundingVoxels(int x, int y, int z)
    {
        Vector3 thisVoxel = new Vector3(x, y, z);

        for(int i=0; i<6; i++)
        {

            Vector3 currentVoxel = thisVoxel + VoxelData.adjFaceChecks[i];
            if(!IsVoxelInChunk((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z)) {

                world.GetChunkFromPosition(currentVoxel + position).UpdateChunk();
            }
        }
    }

    bool CheckVoxel(Vector3 pos) {

        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if (!IsVoxelInChunk(x, y, z))
            return world.IfVoxelTransparent(pos + position);
        
        return world.blockTypes[voxelMap[x, y, z]].isTransparent;
    }

    public byte GetVoxelFromGlobalPosition(Vector3 pos)
    {


        int checkX = Mathf.FloorToInt(pos.x);
        int checkY = Mathf.FloorToInt(pos.y);
        int checkZ = Mathf.FloorToInt(pos.z);

        checkX -= Mathf.FloorToInt(position.x);
        checkZ -= Mathf.FloorToInt(position.z);

        return voxelMap[checkX, checkY, checkZ];

    }

    void UpdateMesh(Vector3 pos) {

        byte blockID = voxelMap[(int)pos.x, (int)pos.y, (int)pos.z];
        bool isTransparent = world.blockTypes[blockID].isTransparent;

        for (int i=0; i<6; i++) {

            if(CheckVoxel(pos + VoxelData.adjFaceChecks[i])) {

                vertices.Add(pos + VoxelData.voxelVert[VoxelData.voxelTriangles[i, 0]]);
                vertices.Add(pos + VoxelData.voxelVert[VoxelData.voxelTriangles[i, 1]]);
                vertices.Add(pos + VoxelData.voxelVert[VoxelData.voxelTriangles[i, 2]]);
                vertices.Add(pos + VoxelData.voxelVert[VoxelData.voxelTriangles[i, 3]]);
                
                AddTexture(world.blockTypes[blockID].GetTextureID(i));

                float lightLevel;

                int yPos = (int)pos.y + 1;
                bool shadow = false;
                while(yPos < VoxelData.chunkHeight)
                {
                    if(voxelMap[(int)pos.x, yPos, (int)pos.z] != 0)
                    {
                        shadow = true;
                        break;
                    }

                    yPos++;
                }

                if (shadow)
                    lightLevel = 0.4f;
                else lightLevel = 0f;

                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));

                //if (!isTransparent)
                //{
                triangles.Add(vertIndex);
                    triangles.Add(vertIndex + 1);
                    triangles.Add(vertIndex + 2);
                    triangles.Add(vertIndex + 2);
                    triangles.Add(vertIndex + 1);
                    triangles.Add(vertIndex + 3);
                /*} else
                {
                    transparentTriangles.Add(vertIndex);
                    transparentTriangles.Add(vertIndex + 1);
                    transparentTriangles.Add(vertIndex + 2);
                    transparentTriangles.Add(vertIndex + 2);
                    transparentTriangles.Add(vertIndex + 1);
                    transparentTriangles.Add(vertIndex + 3);
                }*/
                vertIndex+=4;
            }
        }
    }

    public void CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();

        //mesh.subMeshCount = 2;
        //mesh.SetTriangles(triangles.ToArray(), 0);
        //mesh.SetTriangles(transparentTriangles.ToArray(), 1);

        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.colors = colors.ToArray();

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

    public ChunkPos()
    {
        x = 0;
        z = 0;
    }

    public ChunkPos(int _x, int _z) {
        x = _x;
        z = _z;
    }

    public ChunkPos(Vector3 pos)
    {
        int checkX = Mathf.FloorToInt(pos.x);
        int checkZ = Mathf.FloorToInt(pos.z);

        x = checkX / VoxelData.chunkWidth;
        z = checkZ / VoxelData.chunkWidth;
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