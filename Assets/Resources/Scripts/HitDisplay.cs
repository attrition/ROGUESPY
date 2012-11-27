using System;
using System.Collections.Generic;

using UnityEngine;

class HitDisplay : MonoBehaviour
{
    private int width = 64;
    private int height = 32;

    private float endTime = 0f;
    private const float maxTime = 1f;
    private const float riseAmount = 20f;

    public int TileSize = 16;
    public float Scale = 2f;

    void Awake()
    {
        CreateMesh();
    }

    void Start()
    {
    }

    void Update()
    {
        if (Time.time > endTime)
            Destroy(this.gameObject);
        else
        {
            var pos = this.gameObject.transform.position;
            pos.y += (riseAmount * Time.deltaTime);
            this.gameObject.transform.position = pos;
        }

    }

    private void CreateMesh()
    {
        var mesh = new Mesh();
        mesh.name = "HitResult";

        mesh.vertices = new Vector3[4] { new Vector3(0, 0, 0), new Vector3(0, height, 0),
            new Vector3(width, height, 0), new Vector3(width, 0, 0) };
        mesh.triangles = new int[6] { 0, 1, 2, 2, 3, 0 };
        mesh.uv = new Vector2[4] { new Vector2(0, 0), new Vector2(0, 1),
            new Vector2(1, 1), new Vector2(1, 0) };

        mesh.RecalculateNormals();

        var rend = this.gameObject.AddComponent<MeshRenderer>();
        var filt = this.gameObject.AddComponent<MeshFilter>();

        filt.mesh = mesh;
        rend.material = Resources.Load("Materials/HitText") as Material;

        endTime = Time.time + maxTime;
    }

    public void SetPosition(Vector2 pos)
    {
        pos *= TileSize * Scale;
        pos.y += 28;
        pos.x -= 7;
        
        this.gameObject.transform.position = pos;
    }

    public void SetResult(bool hit)
    {
        Texture2D tex;

        if (hit)
            tex = Resources.Load("Textures/Hit") as Texture2D;
        else
            tex = Resources.Load("Textures/Miss") as Texture2D;

        this.gameObject.renderer.material.SetTexture("_MainTex", tex);
    }
}
