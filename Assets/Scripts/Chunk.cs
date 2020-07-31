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

    bool [,,] voxelMap = new bool[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];

    void Start() {
        initVoxelMap();
        genMeshData();
        CreateMesh();
    }

    void initVoxelMap() {
        for(int y=0; y<VoxelData.chunkHeight; y++) {
            for(int x=0; x<VoxelData.chunkWidth; x++) {
                for(int z=0; z<VoxelData.chunkWidth; z++) {

                    voxelMap[x, y, z] = true;

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
        
        return voxelMap[x, y, z];

    }

    void AddtoChunk(Vector3 pos) {
        
        for(int i=0; i<6; i++) {

            if(!checkVoxel(pos + VoxelData.adjFaceChecks[i])) {

                vertices.Add(pos + VoxelData.voxelVert[VoxelData.voxelTriangles[i, 0]]);
                vertices.Add(pos + VoxelData.voxelVert[VoxelData.voxelTriangles[i, 1]]);
                vertices.Add(pos + VoxelData.voxelVert[VoxelData.voxelTriangles[i, 2]]);
                vertices.Add(pos + VoxelData.voxelVert[VoxelData.voxelTriangles[i, 3]]);
                uvs.Add(VoxelData.voxeluvs[0]);
                uvs.Add(VoxelData.voxeluvs[1]);
                uvs.Add(VoxelData.voxeluvs[2]);
                uvs.Add(VoxelData.voxeluvs[3]);
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

}
