using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;

    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();

    bool [,,] voxelMap = new bool[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];

    void Start()
    {

        PopulateVoxelMap();
        CreateMeshData();
        CreateMesh();      

    }


    void PopulateVoxelMap(){

        for(int y = 0; y < VoxelData.ChunkHeight; y++){
            for(int x = 0; x < VoxelData.ChunkWidth; x++){
                for(int z = 0; z < VoxelData.ChunkWidth; z++){

                    voxelMap [x,y,z] = true;

                }
            }
        }
    }


    //Method for changing of voxel data with loops
    void CreateMeshData(){

        for(int y = 0; y < VoxelData.ChunkHeight; y++){
            for(int x = 0; x < VoxelData.ChunkWidth; x++){
                for(int z = 0; z < VoxelData.ChunkWidth; z++){

                    AddVoxelDataToChunk(new Vector3(x,y,z));

                }
            }
        }

    }


    //Method for checking voxels visibility
    bool CheckVoxel(Vector3 pos){

        //FlooToInt - returns the largest integer smaller to or equal to f
        int x = Mathf.FloorToInt(pos.x);  
        int y = Mathf.FloorToInt(pos.y);  
        int z = Mathf.FloorToInt(pos.z);  

        if(x < 0 || x > VoxelData.ChunkWidth - 1 || y < 0 || y > VoxelData.ChunkHeight - 1 || z < 0 || z > VoxelData.ChunkWidth - 1)
            return false;

        return voxelMap [x,y,z];
    }


    //Method for adding voxel data to chunk
    void AddVoxelDataToChunk(Vector3 pos){
        for (int p = 0; p < 6; p++)
        {
            //We only drawing the faces if there is no voxel against that face
            if(!CheckVoxel(pos + VoxelData.faceChecks[p])){
               
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p,0]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p,1]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p,2]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p,3]]);
                uvs.Add(VoxelData.voxelUvs[0]);
                uvs.Add(VoxelData.voxelUvs[1]);
                uvs.Add(VoxelData.voxelUvs[2]);
                uvs.Add(VoxelData.voxelUvs[3]);
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 3);
                vertexIndex += 4;

            }
        }
    }


    //Method for creating mesh
    void CreateMesh(){
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }
}