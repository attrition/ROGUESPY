using System;
using System.Collections.Generic;

using UnityEngine;

public enum ItemType
{
    Document,
    SilencedPistol,
    Pistol,
    Rifle,
}

public class Item : MonoBehaviour
{
    public string Name = "";

    public Vector2 Position;
    public ItemType Type;
    public int TileSize = 16;
    public float Scale = 2f;

    private const float blinkTime = 0.5f;
    private float nextBlink = -1f;
    private bool blinking = false;

    void Start()
    {
        CreateItemMesh();
    }

    void Update()
    {
        if (Name == "")
        {
            if (Type == ItemType.Document)
                Name = "Documents";
            else if (Type == ItemType.Pistol)
                Name = "Pistol";
            else if (Type == ItemType.SilencedPistol)
                Name = "Silenced Pistol";
            else if (Type == ItemType.Rifle)
                Name = "Rifle";
        }

        if (Type == ItemType.Document && Time.time > nextBlink)
        {
            nextBlink = Time.time + blinkTime;
            blinking = !blinking;

            var tileScale = TileSize * (int)Scale;
            Texture2D tex = new Texture2D(tileScale, tileScale);
            
            if (blinking)
            {
                //tex = Resources.Load("Textures/obj-overlay-off") as Texture2D;
                var cols = new Color[tileScale * tileScale];
                for (int i = 0; i < cols.Length; i++)
                    cols[i] = Color.clear;
                tex.SetPixels(cols);
                tex.Apply();
            }
            else
                tex = Resources.Load("Textures/obj-overlay-on") as Texture2D;

            this.renderer.material.SetTexture("_MainTex", tex);
        }
    }

    private void CreateItemMesh()
    {
        var mesh = new Mesh();
        mesh.name = "Item Mesh";

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
        rend.material = Resources.Load("Materials/Item") as Material;

        Texture2D tex;
        if (Type == ItemType.Document)
            tex = Resources.Load("Textures/obj-overlay-on") as Texture2D;
        else if (Type == ItemType.Pistol || Type == ItemType.SilencedPistol)
            tex = Resources.Load("Textures/weapon-pistol") as Texture2D;
        else if (Type == ItemType.Rifle)
            tex = Resources.Load("Textures/weapon-rifle") as Texture2D;
        else
            tex = new Texture2D((int)tileScale, (int)tileScale);

        rend.material.SetTexture("_MainTex", tex);
        nextBlink = Time.time + blinkTime;

        var pos = this.gameObject.transform.position;
        pos.z = -1;
        this.gameObject.transform.position = pos;
    }
}
