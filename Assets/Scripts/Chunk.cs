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
 
    private bool _isActive;

    ChunkData chunkData;

    public Chunk(ChunkPos _pos) {
        //references
        pos = _pos;
    }

    public void Init()
    {

        chunkObj = new GameObject();
        meshFilter = chunkObj.AddComponent<MeshFilter>();
        meshRenderer = chunkObj.AddComponent<MeshRenderer>();

        materials[0] = World.Instance.material;
        materials[1] = World.Instance.transparentMaterial;
        meshRenderer.materials = materials;  

        chunkObj.transform.SetParent(World.Instance.transform);
        chunkObj.transform.position = new Vector3(pos.x * VoxelData.chunkWidth, 0f, pos.z * VoxelData.chunkWidth);
        chunkObj.name = "Chunk " + pos.x + ", " + pos.z;
        position = chunkObj.transform.position;

        chunkData = World.Instance.worldData.RequestChunk(new Vector2Int((int)position.x, (int)position.z), true);

        lock (World.Instance._updateThreadLock)
            World.Instance.chunksToUpdate.Add(this);

        if (World.Instance.settings.enableAnimatedChunkLoading)
            chunkObj.AddComponent<AnimateChunkLoading>();
    }

    public void UpdateChunk() 
    {
        ClearMeshData();

        CalculateLightValues();

        for(int y=0; y<VoxelData.chunkHeight; y++) {
            for(int x=0; x<VoxelData.chunkWidth; x++) {
                for(int z=0; z<VoxelData.chunkWidth; z++) {

                    if(World.Instance.blockTypes[chunkData.map[x, y, z].id].isSolid)
                        UpdateMesh(new Vector3(x, y, z));

                }
            }
        }

        World.Instance.chunksToDraw.Enqueue(this);
    }

    void CalculateLightValues()
    {

        Queue<Vector3Int> litVoxels = new Queue<Vector3Int>();

        for (int x = 0; x < VoxelData.chunkWidth; x++) {
            for (int z = 0; z < VoxelData.chunkWidth; z++) {

                float lightAbove = 1f;

                for (int y = VoxelData.chunkHeight - 1; y >= 0; y--) {

                    VoxelState thisVoxel = chunkData.map[x, y, z];

                    if (thisVoxel.id > 0 && World.Instance.blockTypes[thisVoxel.id].transparency < lightAbove)
                        lightAbove = World.Instance.blockTypes[thisVoxel.id].transparency;

                    thisVoxel.globalLightPercentage = lightAbove;
                    chunkData.map[x, y, z] = thisVoxel;

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
                    if(chunkData.map[neighbour.x, neighbour.y, neighbour.z].globalLightPercentage < chunkData.map[subjectVoxel.x, subjectVoxel.y, subjectVoxel.z].globalLightPercentage - VoxelData.lightFallOff)
                    {
                        chunkData.map[neighbour.x, neighbour.y, neighbour.z].globalLightPercentage = chunkData.map[subjectVoxel.x, subjectVoxel.y, subjectVoxel.z].globalLightPercentage - VoxelData.lightFallOff;

                        if (chunkData.map[neighbour.x, neighbour.y, neighbour.z].globalLightPercentage > VoxelData.lightFallOff)
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

        chunkData.map[checkX, checkY, checkZ].id = newID;
        World.Instance.worldData.AddModifiedChunk(chunkData);

        lock (World.Instance._updateThreadLock)
        {
            World.Instance.chunksToUpdate.Insert(0, this);
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

                World.Instance.chunksToUpdate.Insert(0, World.Instance.GetChunkFromPosition(currentVoxel + position));
            }
        }
    }

    VoxelState CheckVoxel(Vector3 pos) {

        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if (!IsVoxelInChunk(x, y, z))
            return World.Instance.GetVoxelState(pos + position);
        
        return chunkData.map[x, y, z];
    }

    public VoxelState GetVoxelFromGlobalPosition(Vector3 pos)
    {


        int checkX = Mathf.FloorToInt(pos.x);
        int checkY = Mathf.FloorToInt(pos.y);
        int checkZ = Mathf.FloorToInt(pos.z);

        checkX -= Mathf.FloorToInt(position.x);
        checkZ -= Mathf.FloorToInt(position.z);

        return chunkData.map[checkX, checkY, checkZ];

    }

    void UpdateMesh(Vector3 pos) {

        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        int blockID = chunkData.map[x, y, z].id;
        //bool isTransparent = World.Instance.blockTypes[blockID].renderNeighbourFaces;

        for (int i=0; i<6; i++) {

            VoxelState neighbour = CheckVoxel(pos + VoxelData.adjFaceChecks[i]); 

            if(neighbour != null && World.Instance.blockTypes[neighbour.id].renderNeighbourFaces) {

                vertices.Add(pos + VoxelData.voxelVert[VoxelData.voxelTriangles[i, 0]]);
                vertices.Add(pos + VoxelData.voxelVert[VoxelData.voxelTriangles[i, 1]]);
                vertices.Add(pos + VoxelData.voxelVert[VoxelData.voxelTriangles[i, 2]]);
                vertices.Add(pos + VoxelData.voxelVert[VoxelData.voxelTriangles[i, 3]]);

                for (int j = 0; j < 4; j++)
                    normals.Add(VoxelData.adjFaceChecks[i]);
                
                AddTexture(World.Instance.blockTypes[blockID].GetTextureID(i));

                float lightLevel = neighbour.globalLightPercentage;

                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));

                if (!World.Instance.blockTypes[neighbour.id].renderNeighbourFaces)
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

[System.Serializable]
public class VoxelState
{
    public int id;
    public float globalLightPercentage;

    public VoxelState()
    {
        id = 0;
        globalLightPercentage = 0f;
    }

    public VoxelState(int _id)
    {
        id = _id;
        globalLightPercentage = 0f;
    }
}