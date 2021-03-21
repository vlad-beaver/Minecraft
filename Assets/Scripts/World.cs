using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public int seed;
    public BiomeAttributes biome;

    public Transform player;
    public Vector3 spawnPosition;

    public Material material;
    public BlockType[] blocktypes;

    Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    ChunkCoord playerChunkCoord;
    ChunkCoord playerLastChunkCoord;

    public void Start(){

        Random.InitState(seed);

        spawnPosition = new Vector3((VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f, VoxelData.ChunkHeight + 2f, (VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f);
        GenerateWorld();
        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);

    }

    private void Update () {

        playerChunkCoord = GetChunkCoordFromVector3(player.position);
        
        if (!playerChunkCoord.Equals(playerLastChunkCoord))
            CheckViewDistance();

    }

    void GenerateWorld () {

        for(int x = (VoxelData.WorldSizeInChunks / 2) - VoxelData.ViewDistanceInChunks; x < (VoxelData.WorldSizeInChunks / 2) + VoxelData.ViewDistanceInChunks; x++){
            for(int z = (VoxelData.WorldSizeInChunks / 2) - VoxelData.ViewDistanceInChunks; z < (VoxelData.WorldSizeInChunks / 2) + VoxelData.ViewDistanceInChunks; z++){
                
                CreateNewChunk(x, z);

            }
        }

        player.position = spawnPosition;
    }

    ChunkCoord GetChunkCoordFromVector3 (Vector3 pos) {

        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        return new ChunkCoord(x, z);

    }

    void CheckViewDistance () {

        ChunkCoord coord = GetChunkCoordFromVector3(player.position);

        List<ChunkCoord>previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        for (int x = coord.x - VoxelData.ViewDistanceInChunks; x< coord.x + VoxelData.ViewDistanceInChunks; x++){
            for (int z = coord.z - VoxelData.ViewDistanceInChunks; z < coord.z + VoxelData.ViewDistanceInChunks; z++){
            
                if (isChunkInWorld (new ChunkCoord (x,z))) {

                    if(chunks[x, z] == null)
                        CreateNewChunk(x, z);
                    else if (!chunks[x, z].isActive){
                        chunks[x, z].isActive = true;
                        activeChunks.Add(new ChunkCoord(x, z));
                    }

                }

                for (int i = 0; i < previouslyActiveChunks.Count; i++) {

                    if (previouslyActiveChunks[i].Equals (new ChunkCoord(x, z)))
                        previouslyActiveChunks.RemoveAt(i);
                }
            }
        }

        foreach (ChunkCoord c in previouslyActiveChunks)
            chunks[c.x, c.z].isActive = false;
    }

    //World Algorithm
    public byte GetVoxel (Vector3 pos) {

        int yPos = Mathf.FloorToInt(pos.y);

        //IMMUTABLE PASS

        //If outside world, return air
        if (!IsVoxelInWorld(pos))    //Making Chunk empty
            return 0;

        //If bottom block of chunk, return bedrock
        if (yPos == 0)
            return 1;

        //BASIC TERRAIN PASS

        int terrainHeight = Mathf.FloorToInt(biome.terrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.terrainScale)) + biome.solidGroundHeight;
        byte voxelValue = 6;

        if (yPos == terrainHeight)
            voxelValue = 3;
        else if (yPos < terrainHeight && yPos > terrainHeight - 4)
            voxelValue = 5;
        else if (yPos > terrainHeight)
            return 0;
        else    
            voxelValue = 2;

        //SECOND PASS

        if (voxelValue == 2) {

            foreach (Lode lode in biome.lodes) {
                if (yPos > lode.minHeight && yPos < lode.maxHeight)
                    if (Noise.Get3DPerlin(pos, lode.noiseOffset, lode.scale, lode.threshold))
                    voxelValue = lode.blockID;
            }

        }
        
        return voxelValue;

    }

    void CreateNewChunk (int x, int z) {

        chunks [x,z] = new Chunk (new ChunkCoord(x, z), this);
        activeChunks.Add(new ChunkCoord(x, z));

    }

    bool isChunkInWorld (ChunkCoord coord) {

        if (coord.x > 0 && coord.x < VoxelData.WorldSizeInChunks - 1 && coord.z > 0 && coord.z < VoxelData.WorldSizeInChunks -1)
            return true;
        else
            return false;

    }

    bool IsVoxelInWorld (Vector3 pos) {

        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels  && pos.y >= 0 && pos.y < VoxelData.ChunkHeight   && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels )
            return true;
        else 
            return false;

    }

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
