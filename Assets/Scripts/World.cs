using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public Material material;
    public BlockType[] blocktypes;


}

//System.Serializable tells the system that a class should try to serialize. 
//Without this, the object in this class will not retain its state when entering/leaving the game.
[System.Serializable]
public class BlockType {
    public string blockName;
    public bool isSolid;

    [Header ("Texture Values")]
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;

    //Back, Front, Top, Bottom, Left, Right

    public int GetTextureID(int faceIndex){
        switch(faceIndex){
            case 0: 
                return backFaceTexture;
            case 1: 
                return frontFaceTexture;
            case 2: 
                return topFaceTexture;
            case 3: 
                return bottomFaceTexture;
            case 4: 
                return leftFaceTexture;
            case 5: 
                return rightFaceTexture;
            default:
                Debug.Log("Error in GerTextureID; invalid face index");
                return 0;


        }

    }

}
