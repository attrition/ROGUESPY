using System;
using System.Collections.Generic;

using UnityEngine;

class Exit : MonoBehaviour
{
    public Vector2 Position;
    public int TileSize = 16;
    public float Scale = 2f;

    void Start()
    {
    }
    
    public void EnableExit()
    {
        CreateExitMesh();
    }
    
    private void CreateExitMesh()
    {
        var mesh = new Mesh();
        mesh.name = "Exit Mesh";

        var tileScale = TileSize * Scale;
        mesh.vertices = new Vector3[4] { new Vector3(0, 0, 0), new Vector3(0, tileScale, 0),
            new Vector3(tileScale, tileScale, 0), new Vector3(tileScale, 0, 0) };
        mesh.triangles = new int[6] { 0, 1, 2, 2, 3, 0 };
        mesh.uv = new Vector2[4] { new Vector2(0, 0), new Vector2(0, 1),
            new Vector2(1, 1), new Vector2(1, 0) };

        mesh.RecalculateNormals();

        var rend = this.gameObject.AddComponent<MeshRenderer>();
        var filt = this.gameObject.AddComponent<MeshFilter>();

        filt.mesh = mesh;
        rend.material = Resources.Load("Materials/Exit") as Material;

        var pos = this.gameObject.transform.position;
        pos.z = -0.5f;
        this.gameObject.transform.position = pos;
    }

    public void SetPosition(Vector2 pos)
    {
        Position = pos;

        pos *= TileSize * Scale;
        this.gameObject.transform.position = pos;
    }
}