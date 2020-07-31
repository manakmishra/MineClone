using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    int vertIndex = 0;
    List<Vector3> vertices = new List<Vector3> ();
    List<int> triangles = new List<int> ();
    List<Vector2> uvs = new List<Vector2>();

    byte[,,] voxelMap = new byte[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];

    World world;

    void Start() {

        world = GameObject.Find("World").GetComponent<World>();

        initVoxelMap();
        genMeshData();
        CreateMesh();
    }

    void initVoxelMap() {
        for(int y=0; y<VoxelData.chunkHeight; y++) {
            for(int x=0; x<VoxelData.chunkWidth; x++) {
                for(int z=0; z<VoxelData.chunkWidth; z++) {
                    
                    if(y<1)
                        voxelMap[x, y, z] = 0;
                    else if(y == VoxelData.chunkHeight-1)
                        voxelMap[x, y, z] = 2;
                    else
                        voxelMap[x, y, z] = 1;

                }
            }
        }
    }

    void genMeshData() {
        for(int y=0; y<VoxelData.chunkHeight; y++) {
            for(int x=0; x<VoxelData.chunkWidth; x++) {
                for(int z=0; z<VoxelData.chunkWidth; z++) {

                    AddtoChunk(new Vector3(x, y, z));

                }
            }
        }
    }

    bool checkVoxel(Vector3 pos) {

        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if(x<0 || x>VoxelData.chunkWidth-1 || y<0 || y>VoxelData.chunkHeight-1 || z<0 || z>VoxelData.chunkWidth-1)
            return false;
        
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
