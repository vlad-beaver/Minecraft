using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttributes", menuName = "Minecraft/Biome Attribute")]
public class BiomeAttributes : ScriptableObject
{

    public string biomeName;

    public int solidGroundHeight;
    public int terrainHeight;
    public float terrainScale;

    public Lode[] lodes;

}

[System.Serializable]
public class Lode {

    public string nodeName;
    public byte blockID;
    public byte minHeight;
    public byte maxHeight;
    public float scale;
    public float threshold;
    public float noiseOffset;

}
