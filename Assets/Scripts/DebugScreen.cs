using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugScreen : MonoBehaviour
{

    World world;
    Text text;

    float frameRate;
    float timer;

    int halfWorldSizeInVoxels;
    int halfWorldSizeInChunks;

    public static string consoleText;

    void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();
        text = GetComponent<Text>();

        halfWorldSizeInVoxels = VoxelData.WorldSizeInVoxels / 2;
        halfWorldSizeInChunks = VoxelData.WorldSizeInChunks / 2;
    }

    void Update()
    {
        string debugText = "vlad-beaver' Unity Minecraft clone\n";
        debugText += frameRate + " fps\n";
        //debugText += "XYZ: "+   (Mathf.FloorToInt(world.player.transform.position.x) - halfWorldSizeInVoxels) + " / " + 
        //                        Mathf.FloorToInt(world.player.transform.position.y) + " / " +
        //                        (Mathf.FloorToInt(world.player.transform.position.z) - halfWorldSizeInVoxels) + "\n";
        debugText += "Status: " + consoleText;

        text.text = debugText;

        if (timer > 1f) {
            frameRate = (int)(1f / Time.unscaledDeltaTime);
            timer = 0;
        }
        else    
            timer += Time.deltaTime;
    }
}
