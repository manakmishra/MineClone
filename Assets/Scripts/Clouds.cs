using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clouds : MonoBehaviour
{
    public int cloudHeight = 128;
    public int cloudDepth = 4;

    [SerializeField] private Texture2D noisemap = null;
    [SerializeField] private Material material = null;
    [SerializeField] private World world = null;
    bool[,] cloudData;

    int cloudTextureWidth;

    int cloudTileSize;
    Vector3Int offset;

    Dictionary<Vector2Int, GameObject> clouds = new Dictionary<Vector2Int, GameObject>();

    private void Start()
    {
        cloudTextureWidth = noisemap.width;
        cloudTileSize = VoxelData.chunkWidth;
        offset = new Vector3Int(-(cloudTextureWidth/2), 0, -(cloudTextureWidth/2));

        transform.position = new Vector3(VoxelData.worldCentre, cloudHeight, VoxelData.worldCentre);
        
        LoadCloudData();
        GenerateClouds();
    }

    private void LoadCloudData()
    {
        cloudData = new bool[cloudTextureWidth, cloudTextureWidth];
        Color[] cloudPattern = noisemap.GetPixels();

        for(int i=0; i<cloudTextureWidth; i++)
        {
            for (int j = 0; j < cloudTextureWidth; j++)
            {
                cloudData[i, j] = (cloudPattern[j * cloudTextureWidth + i].a > 0);
            }
        }
    }

    private void GenerateClouds()
    {
        if (world.settings.clouds == CloudStyle.Off)
            return;

        for (int i = 0; i < cloudTextureWidth; i+=cloudTileSize)
        {
            for (int j = 0; j < cloudTextureWidth; j+=cloudTileSize)
            {
                Mesh cloudMesh;
                if (world.settings.clouds == CloudStyle._2D)
                    cloudMesh = Create2DCloudMesh(i, j);
                else
                    cloudMesh = Create3DCloudMesh(i, j);

                Vector3 position = new Vector3(i, cloudHeight, j);
                position += transform.position - new Vector3(cloudTextureWidth / 2f, 0f, cloudTextureWidth / 2f);
                position.y = cloudHeight;
                clouds.Add(CloudTile2DPosition(position), CreateCloudTile(cloudMesh, position));

            }
        }
    }

    public void UpdateClouds()
    {
        if (world.settings.clouds == CloudStyle.Off)
            return;

        for (int i = 0; i < cloudTextureWidth; i += cloudTileSize)
        {
            for (int j = 0; j < cloudTextureWidth; j += cloudTileSize)
            {
                Vector3 position = world.player.position + new Vector3(i, 0, j) + offset;
                position = new Vector3(RoundToCloud(position.x), cloudHeight, RoundToCloud(position.z));
                Vector2Int cloudPosition = CloudTile2DPosition(position);

                clouds[cloudPosition].transform.position = position;
            }
        }
    }

    private int RoundToCloud(float value)
    {
        return Mathf.FloorToInt(value / cloudTileSize) * cloudTileSize;
    }

    private Mesh Create3DCloudMesh(int x, int z)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        int vertCount = 0;

        for (int i = 0; i < cloudTileSize; i++)
        {
            for (int j = 0; j < cloudTileSize; j++)
            {
                int xVal = x + i;
                int zVal = z + j;

                if (cloudData[xVal, zVal])
                {
                    for(int k=0; k<6; k++)
                    {
                        if(!CheckCloudData(new Vector3Int(xVal, 0, zVal) + VoxelData.adjFaceChecks[k]))
                        {
                            for(int m=0; m<4; m++)
                            {
                                Vector3 vert = new Vector3Int(i, 0, j);
                                vert += VoxelData.voxelVert[VoxelData.voxelTriangles[k, m]];
                                vert.y *= cloudDepth;
                                vertices.Add(vert);
                            }

                            for (int n = 0; n < 4; n++)
                                normals.Add(VoxelData.adjFaceChecks[n]);

                            triangles.Add(vertCount);
                            triangles.Add(vertCount + 1);
                            triangles.Add(vertCount + 2);
                            triangles.Add(vertCount + 2);
                            triangles.Add(vertCount + 1);
                            triangles.Add(vertCount + 3);

                            vertCount += 4;
                        }
                    }
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        return mesh;
    }

    private Mesh Create2DCloudMesh(int x, int z)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        int vertCount = 0;

        for (int i = 0; i < cloudTileSize; i++)
        {
            for (int j = 0; j < cloudTileSize; j++)
            {
                int xVal = x + i;
                int zVal = z + j;

                if (cloudData[xVal, zVal])
                {
                    //four vertices of cloud face
                    vertices.Add(new Vector3(i, 0, j));
                    vertices.Add(new Vector3(i, 0, j + 1));
                    vertices.Add(new Vector3(i + 1, 0, j + 1));
                    vertices.Add(new Vector3(i + 1, 0, j));

                    //downward facing normals
                    for (int k = 0; k < 4; k++)
                        normals.Add(Vector3.down);

                    //first triangle
                    triangles.Add(vertCount + 1);
                    triangles.Add(vertCount);
                    triangles.Add(vertCount + 2);

                    //second triangle
                    triangles.Add(vertCount + 2);
                    triangles.Add(vertCount);
                    triangles.Add(vertCount + 3);

                    vertCount += 4;
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        return mesh;
    }

    private bool CheckCloudData(Vector3Int check)
    {
        //return false if y coord is not 0
        if (check.y != 0)
            return false;

        int x = check.x;
        int z = check.z;

        if (x < 0) x = cloudTextureWidth - 1;
        if (x > cloudTextureWidth - 1) x = 0;
        if (z < 0) z = cloudTextureWidth - 1;
        if (z > cloudTextureWidth - 1) z = 0;

        return cloudData[x, z];
    }

    private GameObject CreateCloudTile(Mesh mesh, Vector3 position)
    { 
        GameObject newCloudTile = new GameObject();
        newCloudTile.transform.position = position;
        newCloudTile.transform.parent = transform;
        newCloudTile.name = "Cloud: " + position.x + ", " + position.z;
        MeshFilter meshFilter = newCloudTile.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = newCloudTile.AddComponent<MeshRenderer>();

        meshRenderer.material = material;
        meshFilter.mesh = mesh;

        return newCloudTile;
    }

    private Vector2Int CloudTile2DPosition(Vector3 pos)
    {
        return new Vector2Int(CloudTileFromFloat(pos.x), CloudTileFromFloat(pos.z));
    }

    private int CloudTileFromFloat(float value)
    {
        float tempA = value / (float)cloudTextureWidth;
        tempA -= Mathf.FloorToInt(tempA);
        int tempB = Mathf.FloorToInt((float)cloudTextureWidth * tempA);

        return tempB;
    }
}

public enum CloudStyle
{
    Off,
    _2D,
    _3D
}
