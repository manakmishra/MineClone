using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AtlasPacker : EditorWindow
{

    int blockSize = 32;
    int atlasSizeInBlocks = 16;
    int atlasSize;

    Object[] rawTextures = new Object[256];
    List<Texture2D> textures = new List<Texture2D>();
    Texture2D atlas;
    
    [MenuItem("MineClone/AtlasPacker")]
    public static void ShowWindow()
    {

        EditorWindow.GetWindow(typeof(AtlasPacker));
    }

    private void OnGUI()
    {
        atlasSize = blockSize * atlasSizeInBlocks;
        
        GUILayout.Label("MineClone Texture Atlas Packer", EditorStyles.boldLabel);

        blockSize = EditorGUILayout.IntField("Block Size", blockSize);
        atlasSizeInBlocks = EditorGUILayout.IntField("Atlas Size in Blocks", atlasSizeInBlocks);

        GUILayout.Label(atlas);

        if(GUILayout.Button("Load Textures"))
        {
            LoadTextures();
            CreateTextureAtlas();
        }

        if(GUILayout.Button("Clear Textures"))
        {
            atlas = new Texture2D(atlasSize, atlasSize);
        }

        if (GUILayout.Button("Create Atlas"))
        {
            byte[] bytes = atlas.EncodeToPNG();

            try
            {
                File.WriteAllBytes(Application.dataPath + "/Textures/Blocks.png", bytes);
            } catch
            {
                Debug.Log("Coudn't save atlas to Blocks.png");
            }
        }
    }

    void LoadTextures()
    {

        textures.Clear();

        rawTextures = Resources.LoadAll("AtlasPacker", typeof(Texture2D));

        int index = 0;
        foreach(Object t in rawTextures)
        {
            Texture2D tex = (Texture2D)t;

            if (tex.width == blockSize && tex.height == blockSize)
                textures.Add(tex);
            else Debug.Log("AtlasPacker: " + tex.name + " (incorrect dimensions)");

            index++;
        }
        Debug.Log("AtlasPacker: " + textures.Count + " successfully loaded.");
    }

    void CreateTextureAtlas()
    {
        atlas = new Texture2D(atlasSize, atlasSize);
        Color[] pixels = new Color[atlasSize * atlasSize];

        for(int i=0; i < atlasSize; i++)
        {
            for(int j=0; j < atlasSize; j++)
            {
                int currentBlockX = i / blockSize;
                int currentBlockY = j / blockSize;

                int index = currentBlockY * atlasSizeInBlocks + currentBlockX;

                int currentPixelX = i - (currentBlockX * blockSize);
                int currentPixelY = j - (currentBlockY * blockSize);

                if (index < textures.Count)
                    pixels[(atlasSize - j - 1) * atlasSize + i] = textures[index].GetPixel(i, blockSize - j - 1);
                else
                    pixels[(atlasSize - j - 1) * atlasSize + i] = new Color(0f, 0f, 0f, 0f);
            }
        }

        atlas.SetPixels(pixels);
        atlas.Apply();
    }
}
