using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugScreen : MonoBehaviour
{

    World world;
    TextMeshProUGUI debugText;

    float frameRate;
    float timer;

    int halfWorldSizeInVoxels;
    int halfWorldSizeInChunks;

    void Start()
    {

        world = GameObject.Find("World").GetComponent<World>();
        debugText = GetComponent<TextMeshProUGUI>();
        halfWorldSizeInChunks = VoxelData.worldSizeInChunks / 2;
        halfWorldSizeInVoxels = VoxelData.worldSizeInVoxels / 2;
    }

    // Update is called once per frame
    void Update()
    {

        string debugMessage = frameRate + "fps\n";
        debugMessage += "XYZ: " + (Mathf.FloorToInt(world.player.transform.position.x)-halfWorldSizeInVoxels) + "," + Mathf.FloorToInt(world.player.transform.position.y) + "," + (Mathf.FloorToInt(world.player.transform.position.z)-halfWorldSizeInVoxels) + "\n";
        debugMessage += "Chunk: " + (world.playerChunkPos.x - halfWorldSizeInChunks) + "," + (world.playerChunkPos.z - halfWorldSizeInChunks);

        debugText.SetText(debugMessage, true);

        if (timer > 1f)
        {
            frameRate = (int)(1f / Time.unscaledDeltaTime);
            timer = 0;
        }
        else
            timer += Time.deltaTime;
    }
}
