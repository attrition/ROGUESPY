using System;
using System.Collections.Generic;

using UnityEngine;

public enum OverlayMode
{
    Targeting,
    Selection,
}

// mesh for 32x32 overlays (target and selection)
class Overlay
{
    private Texture2D TargetTex;
    private Texture2D SelectionTex;

    private GameObject go;
    public Vector2 Position;
    private bool hidden;

    private float scale;
    private int tileSize;

    public Overlay(int tileSize, float scale)
    {
        this.tileSize = tileSize;
        this.scale = scale;

        CreateOverlayMesh();
        SetHidden(true);
    }

    private void CreateOverlayMesh()
    {
        var mesh = new Mesh();
        mesh.name = "Overlay Mesh";

        mesh.vertices = new Vector3[4] { new Vector3(0, 0, 0), new Vector3(0, tileSize * 4, 0),
            new Vector3(tileSize * 4, tileSize * 4, 0), new Vector3(tileSize * 4, 0, 0) };
        mesh.triangles = new int[6] { 0, 1, 2, 2, 3, 0 };
        mesh.uv = new Vector2[4] { new Vector2(0, 0), new Vector2(0, 1),
            new Vector2(1, 1), new Vector2(1, 0) };

        mesh.RecalculateNormals();

        go = new GameObject("Overlay");
        var rend = go.AddComponent<MeshRenderer>();
        var filt = go.AddComponent<MeshFilter>();

        filt.mesh = mesh;
        rend.material = Resources.Load("Materials/Overlay") as Material;

        TargetTex = Resources.Load("Textures/Crosshair32") as Texture2D;
        SelectionTex = Resources.Load("Textures/Selection32") as Texture2D;
    }

    public void SetHidden(bool hide)
    {
        hidden = hide;
        var pos = go.transform.position;

        if (hidden)
            pos.z = -20;
        else
            pos.z = -5;

        go.transform.position = pos;
    }

    public void SetOverlayMode(OverlayMode mode)
    {
        Texture2D tex;
        if (mode == OverlayMode.Targeting)
            tex = TargetTex;
        else
            tex = SelectionTex;

        go.renderer.material.SetTexture("_MainTex", tex);
    }

    public void SetPosition(Vector2 newPos)
    {
        var pos = go.transform.position;

        pos.x = (newPos.x * tileSize * scale) - (tileSize);
        pos.y = (newPos.y * tileSize * scale) - (tileSize);

        go.transform.position = pos;
        Position = newPos;
    }

    public void Move(Vector2 dir)
    {
        Vector2 move = Position;

        if (dir.x != 0)
        {
            move.x = Position.x + dir.x;
            SetPosition(move);
        }
        else if (dir.y != 0)
        {
            move.y = Position.y + dir.y;
            SetPosition(move);
        }
    }
}
