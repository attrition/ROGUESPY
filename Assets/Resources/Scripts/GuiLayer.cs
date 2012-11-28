using System;
using System.Collections.Generic;
using UnityEngine;

public class GUILayer : MonoBehaviour
{
    public int Width = 512;
    public int Height = 448;
    public float Scale = 2f;

    public Font Font;
    private Texture2D guiTex;
    private Color[] guiColors;

    void Awake()
    {
        InitGuiColors();
        InitGuiTex();

        CreateGUILayer();
    }

    void Start()
    {
    }

    void InitGuiColors()
    {
        var halfW = Width / 2;
        guiColors = new Color[halfW * halfW];
        for (int i = 0; i < halfW * halfW; i++)
            guiColors[i] = new Color(0, 0, 0, 0);
    }

    void InitGuiTex()
    {
        Destroy(guiTex);
        guiTex = new Texture2D(Width / 2, Width / 2, TextureFormat.ARGB32, false);
        guiTex.wrapMode = TextureWrapMode.Clamp;
        guiTex.filterMode = FilterMode.Point;

        guiTex.SetPixels(guiColors);
    }

    void CreateGUILayer()
    {
        var mesh = new Mesh();
        mesh.name = "GUI Layer";

        mesh.vertices = new Vector3[4] { new Vector3(0, Height, 2), new Vector3(Width, Height, 2),
            new Vector3(Width, 0, 2), new Vector3(0, 0, 2) };
        mesh.triangles = new int[6] { 0, 1, 2, 2, 3, 0 };
        mesh.uv = new Vector2[4] { new Vector2(0, 0.875f), new Vector2(1, 0.875f),
            new Vector2(1, 0), new Vector2(0, 0) };

        mesh.RecalculateNormals();

        var rend = this.gameObject.AddComponent<MeshRenderer>();
        var filt = this.gameObject.AddComponent<MeshFilter>();

        filt.mesh = mesh;
        rend.material = Resources.Load("Materials/GUI") as Material;
        rend.material.SetTexture("_MainTex", guiTex);
    }

    public void RefreshGUI()
    {
        InitGuiColors();
        InitGuiTex();
    }

    public void AddWindow(int startx, int starty, int width, int height, Color color)
    {
        var window = new Color[width * height];
        for (int i = 0; i < width * height; i++)
            window[i] = color;

        guiTex.SetPixels(startx, starty, width, height, window);
    }

    public void AddText(string text, int x, int y, Color tint)
    {
        var block = Font.GetString(text, tint);
        var width = Font.GetWidth(text);
        var height = Font.GetHeight(text);

        for (int yy = 0; yy < height; yy++)
            for (int xx = 0; xx < width; xx++)
                if (block[yy * width + xx].a > 0f)
                    guiTex.SetPixel(x + xx, y + yy, block[yy * width + xx]);
        
        guiTex.Apply();
        renderer.material.SetTexture("_MainTex", guiTex);
    }

    public void AddTexture(Texture2D tex, int x, int y)
    {
        guiTex.SetPixels(x, y, tex.width, tex.height, tex.GetPixels());
        guiTex.Apply();
        renderer.material.SetTexture("_MainTex", guiTex);
    }

    void Update()
    {
    }
}
