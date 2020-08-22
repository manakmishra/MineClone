using System.Collections.Generic;
using UnityEngine;

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
    List<Vector3> normals = new List<Vector3>(); 

    public Vector3 position;

    public VoxelState[,,] voxelMap = new VoxelState[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];

    World world;

    public Queue<WorldVoxelMod> mods = new Queue<WorldVoxelMod>();

    private bool _isActive;
    private bool isVoxelMapPopulated = false;

    public Chunk(ChunkPos _pos, World _world) {

        //references
        pos = _pos;
        world = _world;
    }

    public void Init()
    {

        chunkObj = new GameObject();
        meshFilter = chunkObj.AddComponent<MeshFilter>();
        meshRenderer = chunkObj.AddComponent<MeshRenderer>();

        materials[0] = world.material;
        materials[1] = world.transparentMaterial;
        meshRenderer.materials = materials;  

        chunkObj.transform.SetParent(world.transform);
        chunkObj.transform.position = new Vector3(pos.x * VoxelData.chunkWidth, 0f, pos.z * VoxelData.chunkWidth);
        chunkObj.name = "Chunk " + pos.x + ", " + pos.z;
        position = chunkObj.transform.position;

        InitVoxelMap();
    }

    void InitVoxelMap() {
        for (int y = 0; y < VoxelData.chunkHeight; y++) {
            for (int x = 0; x < VoxelData.chunkWidth; x++) {
                for (int z = 0; z < VoxelData.chunkWidth; z++) {

                    voxelMap[x, y, z] = new VoxelState(world.GetVoxel(new Vector3(x, y, z) + position));
                }
            }
        }

        isVoxelMapPopulated = true;
        
        lock(world._updateThreadLock)
            world.chunksToUpdate.Add(this);
    }

    public void UpdateChunk() 
    {

        while(mods.Count > 0)
        {

            WorldVoxelMod mod = mods.Dequeue();
            Vector3 pos = mod.position -= position;
            voxelMap[(int)pos.x, (int)pos.y, (int)pos.z].id = mod.id;
        }

        ClearMeshData();

        CalculateLightValues();

        for(int y=0; y<VoxelData.chunkHeight; y++) {
            for(int x=0; x<VoxelData.chunkWidth; x++) {
                for(int z=0; z<VoxelData.chunkWidth; z++) {

                    if(world.blockTypes[voxelMap[x, y, z].id].isSolid)
                        UpdateMesh(new Vector3(x, y, z));

                }
            }
        }

        world.chunksToDraw.Enqueue(this);
    }

    void CalculateLightValues()
    {

        Queue<Vector3Int> litVoxels = new Queue<Vector3Int>();

        for (int x = 0; x < VoxelData.chunkWidth; x++) {
            for (int z = 0; z < VoxelData.chunkWidth; z++) {

                float lightAbove = 1f;

                for (int y = VoxelData.chunkHeight - 1; y >= 0; y--) {

                    VoxelState thisVoxel = voxelMap[x, y, z];

                    if (thisVoxel.id > 0 && world.blockTypes[thisVoxel.id].transparency < lightAbove)
                        lightAbove = world.blockTypes[thisVoxel.id].transparency;

                    thisVoxel.globalLightPercentage = lightAbove;
                    voxelMap[x, y, z] = thisVoxel;

                    if(lightAbove > VoxelData.lightFallOff)
                    {
                        litVoxels.Enqueue(new Vector3Int(x, y, z));
                    }
                }
            }
        }

        while(litVoxels.Count > 0)
        {
            Vector3Int subjectVoxel = litVoxels.Dequeue();

            for(int i=0; i<6; i++)
            {
                Vector3 currentVoxel = subjectVoxel + VoxelData.adjFaceChecks[i];
                Vector3Int neighbour = new Vector3Int((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z);
                if(IsVoxelInChunk(neighbour.x, neighbour.y, neighbour.z))
                {
                    if(voxelMap[neighbour.x, neighbour.y, neighbour.z].globalLightPercentage < voxelMap[subjectVoxel.x, subjectVoxel.y, subjectVoxel.z].globalLightPercentage - VoxelData.lightFallOff)
                    {
                        voxelMap[neighbour.x, neighbour.y, neighbour.z].globalLightPercentage = voxelMap[subjectVoxel.x, subjectVoxel.y, subjectVoxel.z].globalLightPercentage - VoxelData.lightFallOff;

                        if (voxelMap[neighbour.x, neighbour.y, neighbour.z].globalLightPercentage > VoxelData.lightFallOff)
                            litVoxels.Enqueue(neighbour);
                    }
                }
            }
        }
    }

    void ClearMeshData()
    {

        vertIndex = 0;
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
        colors.Clear();
        normals.Clear();
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
            if (!isVoxelMapPopulated)
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

        voxelMap[checkX, checkY, checkZ].id = newID;

        lock (world._updateThreadLock)
        {
            world.chunksToUpdate.Insert(0, this);
            UpdateSurroundingVoxels(checkX, checkY, checkZ);
        }
    }

    void UpdateSurroundingVoxels(int x, int y, int z)
    {
        Vector3 thisVoxel = new Vector3(x, y, z);

        for(int i=0; i<6; i++)
        {

            Vector3 currentVoxel = thisVoxel + VoxelData.adjFaceChecks[i];
            if(!IsVoxelInChunk((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z)) {

                world.chunksToUpdate.Insert(0, world.GetChunkFromPosition(currentVoxel + position));
            }
        }
    }

    VoxelState CheckVoxel(Vector3 pos) {

        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if (!IsVoxelInChunk(x, y, z))
            return world.GetVoxelState(pos + position);
        
        return voxelMap[x, y, z];
    }

    public VoxelState GetVoxelFromGlobalPosition(Vector3 pos)
    {


        int checkX = Mathf.FloorToInt(pos.x);
        int checkY = Mathf.FloorToInt(pos.y);
        int checkZ = Mathf.FloorToInt(pos.z);

        checkX -= Mathf.FloorToInt(position.x);
        checkZ -= Mathf.FloorToInt(position.z);

        return voxelMap[checkX, checkY, checkZ];

    }

    void UpdateMesh(Vector3 pos) {

        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        int blockID = voxelMap[x, y, z].id;
        //bool isTransparent = world.blockTypes[blockID].renderNeighbourFaces;

        for (int i=0; i<6; i++) {

            VoxelState neighbour = CheckVoxel(pos + VoxelData.adjFaceChecks[i]); 

            if(neighbour.id != -1 && world.blockTypes[neighbour.id].renderNeighbourFaces) {

                vertices.Add(pos + VoxelData.voxelVert[VoxelData.voxelTriangles[i, 0]]);
                vertices.Add(pos + VoxelData.voxelVert[VoxelData.voxelTriangles[i, 1]]);
                vertices.Add(pos + VoxelData.voxelVert[VoxelData.voxelTriangles[i, 2]]);
                vertices.Add(pos + VoxelData.voxelVert[VoxelData.voxelTriangles[i, 3]]);

                for (int j = 0; j < 4; j++)
                    normals.Add(VoxelData.adjFaceChecks[i]);
                
                AddTexture(world.blockTypes[blockID].GetTextureID(i));

                float lightLevel = neighbour.globalLightPercentage;

                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));

                if (!world.blockTypes[neighbour.id].renderNeighbourFaces)
                {
                    triangles.Add(vertIndex);
                    triangles.Add(vertIndex + 1);
                    triangles.Add(vertIndex + 2);
                    triangles.Add(vertIndex + 2);
                    triangles.Add(vertIndex + 1);
                    triangles.Add(vertIndex + 3);
                } else
                {
                    transparentTriangles.Add(vertIndex);
                    transparentTriangles.Add(vertIndex + 1);
                    transparentTriangles.Add(vertIndex + 2);
                    transparentTriangles.Add(vertIndex + 2);
                    transparentTriangles.Add(vertIndex + 1);
                    transparentTriangles.Add(vertIndex + 3);
                }
                vertIndex+=4;
            }
        }
    }

    public void CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();

        mesh.subMeshCount = 2;
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(transparentTriangles.ToArray(), 1);

        //mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.colors = colors.ToArray();
        mesh.normals = normals.ToArray();

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

public struct VoxelState
{
    public int id;
    public float globalLightPercentage;

    public VoxelState(int _id)
    {
        id = _id;
        globalLightPercentage = 0f;
    }
}