﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.IO;
using UnityEngine.SceneManagement;

public class World : MonoBehaviour
{

    private World world;
    public Settings settings;

    [Header("World Generation Values")] 
    public BiomeAttributes[] biomes;

    [Range(0f, 1f)]
    public float globalLightLevel;
    public Color day;
    public Color night;

    public Transform player;
    public Vector3 spawnPosition;

    public Material material;
    public Material transparentMaterial;
    public BlockType[] blocktypes;

    Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    public ChunkCoord playerChunkCoord;        
    ChunkCoord playerLastChunkCoord;

    List<ChunkCoord> chunksToCreate = new List<ChunkCoord> ();
    public List<Chunk> chunksToUpdate = new List<Chunk>();
    public Queue<Chunk> chunksToDraw = new Queue<Chunk>();

    bool applyingModifications = false;

    Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();

    private bool _inUI = false;

    public GameObject debugScreen;

    public GameObject creativeInventoryWindow;
    public GameObject cursorSlot;

    Thread ChunkUpdateThread;
    public object ChunkUpdateThreadLock = new object();
    public object ChunkListThreadLock = new object();

    private static World _instance;
    public static World Instance { get { return _instance; } }

    public WorldData worldData;

    public string appPath;

    private void Awake()
    {

        //If the instance value is not null and not *this*, we've somehow ended up with more than one World component.
        //Since another one has already been assigned, delete this one.
        if (_instance != null && _instance != this)
            Destroy(this.gameObject);
        //Else set instance to this.
        else
            _instance = this;

        appPath = Application.persistentDataPath;

    }

    private void Start()
    {

        Debug.Log("Generating new world using seed: " + VoxelData.seed);

        worldData = SaveSystem.LoadWorld("New World");

        //string jsonExport = JsonUtility.ToJson(settings);
        //Debug.Log(jsonExport);

        //File.WriteAllText(Application.dataPath + "/settings.cfg", jsonExport);

        string jsonImport = File.ReadAllText(Application.dataPath + "/settings.cfg");
        settings = JsonUtility.FromJson<Settings>(jsonImport);

        Random.InitState(VoxelData.seed);

        Shader.SetGlobalFloat("minGlobalLightLevel", VoxelData.minLightLevel);
        Shader.SetGlobalFloat("maxGlobalLightLevel", VoxelData.maxLightLevel);

        if (settings.enableThreading)
        {
            ChunkUpdateThread = new Thread(new ThreadStart(ThreadedUpdate));
            ChunkUpdateThread.Start();
        }

        SetGlobalLightValue();
        spawnPosition = new Vector3((VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f, VoxelData.ChunkHeight - 50f, (VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f);
        GenerateWorld();
        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);

    }

    public void SetGlobalLightValue ()
    {
        Shader.SetGlobalFloat("GlobalLightLevel", globalLightLevel);
        Camera.main.backgroundColor = Color.Lerp(night, day, globalLightLevel);
    }

    private void Update()
    {

        playerChunkCoord = GetChunkCoordFromVector3(player.position);

        // Only update the chunks if the player has moved from the chunk they were previously on.
        if (!playerChunkCoord.Equals(playerLastChunkCoord))
            CheckViewDistance();

        if (chunksToCreate.Count > 0)
            CreateChunk();

        if (chunksToDraw.Count > 0)
        {

            chunksToDraw.Dequeue().CreateMesh();

        }

        if (!settings.enableThreading)
        {

            if (!applyingModifications)
                ApplyModifications();

            if (chunksToUpdate.Count > 0)
                UpdateChunks();

        }

        if (Input.GetKeyDown(KeyCode.F3))
            debugScreen.SetActive(!debugScreen.activeSelf);

        if (Input.GetKeyDown(KeyCode.F1))
            SaveSystem.SaveWorld(worldData);

    }

    void LoadWorld()
    {

        for (int x = (VoxelData.WorldSizeInChunks / 2) - settings.loadDistance; x < (VoxelData.WorldSizeInChunks / 2) + settings.loadDistance; x++)
        {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - settings.loadDistance; z < (VoxelData.WorldSizeInChunks / 2) + settings.loadDistance; z++)
            {

                worldData.LoadChunk(new Vector2Int(x, z));

            }
        }

    }

    void GenerateWorld()
    {

        for (int x = (VoxelData.WorldSizeInChunks / 2) - settings.viewDistance; x < (VoxelData.WorldSizeInChunks / 2) + settings.viewDistance; x++)
        {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - settings.viewDistance; z < (VoxelData.WorldSizeInChunks / 2) + settings.viewDistance; z++)
            {

                ChunkCoord newChunk = new ChunkCoord(x, z);
                chunks[x, z] = new Chunk(newChunk);
                chunksToCreate.Add(newChunk);

            }
        }

        player.position = spawnPosition;
        CheckViewDistance();

    }

    void CreateChunk ()
    {

        ChunkCoord c = chunksToCreate[0];
        chunksToCreate.RemoveAt(0);
        chunks[c.x, c.z].init();

    }

    void UpdateChunks()
    {
        lock (ChunkUpdateThreadLock)
        {

            chunksToUpdate[0].UpdateChunk();
            if (!activeChunks.Contains(chunksToUpdate[0].coord))
                activeChunks.Add(chunksToUpdate[0].coord);
            chunksToUpdate.RemoveAt(0);

        }

    }

    void ThreadedUpdate()
    {

        while (true)
        {

            if (!applyingModifications)
                ApplyModifications();

            if (chunksToUpdate.Count > 0)
                UpdateChunks();

        }

    }

    private void OnDisable()
    {

        if (settings.enableThreading)
        {
            ChunkUpdateThread.Abort();
        }

    }

    void ApplyModifications ()
    {

        applyingModifications = true;

        while (modifications.Count > 0)
        {
            Queue<VoxelMod> queue = modifications.Dequeue();

            //try
            //{
            while (queue.Count > 0)
            {

                VoxelMod v = queue.Dequeue();

                worldData.SetVoxel(v.position, v.id);
            }
            //}
            //catch (System.NullReferenceException)
            //{
            //    Debug.Log("Nullreference Exception again...");
            //}

        }

        applyingModifications = false;

    }

    ChunkCoord GetChunkCoordFromVector3 (Vector3 pos) {

        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        return new ChunkCoord(x, z);

    }

    public Chunk GetChunkFromVector3 (Vector3 pos) {
         
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        return chunks[x, z];

    }

    void CheckViewDistance()
    {

        ChunkCoord coord = GetChunkCoordFromVector3(player.position);
        playerLastChunkCoord = playerChunkCoord;

        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        activeChunks.Clear();

        //Loop through all chunks currently within view distance of the player.
        for (int x = coord.x - settings.viewDistance; x < coord.x + settings.viewDistance; x++)
        {
            for (int z = coord.z - settings.viewDistance; z < coord.z + settings.viewDistance; z++)
            {

                ChunkCoord thisChunkCoord = new ChunkCoord(x, z);

                //If the current chunk is in the world...
                if (isChunkInWorld(thisChunkCoord))
                {

                    //Check if it active, if not, activate it.
                    if (chunks[x, z] == null)
                    {
                        chunks[x, z] = new Chunk(thisChunkCoord);
                        chunksToCreate.Add(thisChunkCoord);
                    }
                    else if (!chunks[x, z].isActive)
                    {
                        chunks[x, z].isActive = true;
                    }
                    activeChunks.Add(thisChunkCoord);
                }

                //Check through previously active chunks to see if this chunk is there. If it is, remove it from the list.
                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {

                    if (previouslyActiveChunks[i].Equals(thisChunkCoord))
                        previouslyActiveChunks.RemoveAt(i);

                }

            }
        }

        // Any chunks left in the previousActiveChunks list are no longer in the player's view distance, so loop through and disable them.
        foreach (ChunkCoord c in previouslyActiveChunks)
            chunks[c.x, c.z].isActive = false;

    }

    public bool CheckForVoxel (Vector3 pos) {
        try
        {
            VoxelState voxel = worldData.GetVoxel(pos);

            if (blocktypes[voxel.id].isSolid)
                return true;
            else
                return false;
        }
        catch
        {
            SceneManager.LoadScene("Game", LoadSceneMode.Single);
            return true;
        }

    }

    public VoxelState GetVoxelState(Vector3 pos)
    {

        return worldData.GetVoxel(pos);

    }

    public bool inUI
    {
        get
        {
            return _inUI;
        }
        set
        {
            _inUI = value;
            if (_inUI)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                creativeInventoryWindow.SetActive(true);
                cursorSlot.SetActive(true);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                creativeInventoryWindow.SetActive(false);
                cursorSlot.SetActive(false);
            }
        }
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

        //BIOME SELECTION PASS

        int solidGroundHeight = 42;
        float sumOfHeight = 0f;
        int count = 0;
        float strongestWeight = 0f;
        int strongestBiomeIndex = 0;

        for (int i =0; i < biomes.Length; i++)
        {
            float weight = Noise.Get2DPerlin(new Vector2(pos.x, pos.z), biomes[i].offset, biomes[i].scale);

            //Keep track of which weight is strongest
            if (weight > strongestWeight)
            {
                strongestWeight = weight;
                strongestBiomeIndex = i;
            }

            //Get the height of the terrain (for the current biome) and multiply it by its weight
            float height = biomes[i].terrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biomes[i].terrainScale) * weight;

            //If the height value if greater 0 add it to the sum of heights
            if (height > 0)
            {
                sumOfHeight += height;
                count++;
            }

        }

        //Set biome to the one with the strongest weight
        BiomeAttributes biome = biomes[strongestBiomeIndex];

        //Get the average of the heights
        sumOfHeight /= count;

        int terrainHeight = Mathf.FloorToInt(sumOfHeight + solidGroundHeight);

        //BASIC TERRAIN PASS

        byte voxelValue = 6;

        if (yPos == terrainHeight)
            voxelValue = biome.surfaceBlock;
        else if (yPos < terrainHeight && yPos > terrainHeight - 4)
            voxelValue = biome.subSurfaceBlock;
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

        //TREE PASS

        if (yPos == terrainHeight && biome.placeMajorFlora)
        {
            if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.majorFloraZoneScale) > biome.majorFloraZoneThreshold)
            {
                if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.majorFloraPlacementScale) > biome.majorFloraPlacementThreshold)
                {
                    modifications.Enqueue(Structure.GenerateMajorFlora(biome.majorFloraIndex, pos, biome.minHeight, biome.maxHeight));
                }

            } 
        }
        
        return voxelValue;

    }

    bool isChunkInWorld (ChunkCoord coord) {

        if (coord.x > 0 && coord.x < VoxelData.WorldSizeInChunks - 1 && coord.z > 0 && coord.z < VoxelData.WorldSizeInChunks -1)
            return true;
        else
            return false;

    }

    bool IsVoxelInWorld (Vector3 pos) {

        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels  && pos.y >= 0 && pos.y < VoxelData.ChunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels )
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
    public bool renderNeighborFaces;
    public float transparency;
    public Sprite Icon;

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

public class VoxelMod
{

    public Vector3 position;
    public byte id;
    public VoxelMod ()
    {
        position = new Vector3();
        id = 0;
    }
    public VoxelMod (Vector3 _position, byte _id)
    {
        position = _position;
        id = _id;
    }
}

[System.Serializable]
public class Settings
{
    [Header("Game Data")]
    public string version = "0.0.1";

    [Header("Perfomance")]
    public int loadDistance = 12;
    public int viewDistance = 6;
    public bool enableThreading = true;
    public bool enableAnimatedChunks = false;

    [Header("Controls")]
    [Range(0.1f, 10f)]
    public float mouseSensitivity = 2.0f;

}
