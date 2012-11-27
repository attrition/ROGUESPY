using UnityEngine;
using System.Xml;
using System.Collections.Generic;

public class BackgroundMaker : MonoBehaviour
{
    public Material BackgroundMaterial;
    public Texture2D MapTexture;
    private GameObject BGObj;

    private int mapTexHeight = 448;
    private int mapTexWidth = 512;
    public float scale = 2f;

    void Awake()
    {
        CreateBG();
    }

    // Use this for initialization
    void Start()
    {
    }

    void CreateBG()
    {
        var mesh = new Mesh();
        mesh.name = "BG Mesh";

        mesh.vertices = new Vector3[4] { new Vector3(0, 0, 0), new Vector3(0, mapTexHeight * scale, 0),
            new Vector3(mapTexWidth * scale, mapTexHeight * scale, 0), new Vector3(mapTexWidth * scale, 0, 0) };
        mesh.triangles = new int[6] { 0, 1, 2, 2, 3, 0 };
        mesh.uv = new Vector2[4] { new Vector2(0, 0), new Vector2(0, 0.875f),
            new Vector2(1, 0.875f), new Vector2(1, 0) };

        mesh.RecalculateNormals();

        BGObj = new GameObject("BG Object");
        var rend = BGObj.AddComponent<MeshRenderer>();
        var filt = BGObj.AddComponent<MeshFilter>();

        filt.mesh = mesh;
        rend.material = BackgroundMaterial;

        BGObj.transform.parent = this.transform;
    }

    public void InitMap(Texture2D tex)
    {
        MapTexture = tex;
        InitMapTexture();
    }

    Texture2D InitMapTexture()
    {
        // texture colors
        var texCols = MapTexture.GetPixels();
        var height = MapTexture.height;
        var width = MapTexture.width;

        // find next largest power of 2 from width
        int pot = 1;
        while (pot < width)
            pot <<= 1;
        
        // fill texture
        var blacks = new Color[pot * pot];
        for (int i = 0; i < pot * pot; i++)
            blacks[i] = Color.black;

        var bgTex = new Texture2D(pot, pot, TextureFormat.RGBA32, false); // must be power of 2
        bgTex.filterMode = FilterMode.Point;
        bgTex.wrapMode = TextureWrapMode.Clamp;
        bgTex.SetPixels(blacks);
        bgTex.SetPixels(0, 0, width, height, texCols);
        bgTex.Apply();

        // set material texture
        var rend = BGObj.GetComponent<MeshRenderer>();
        rend.material.SetTexture("_MainTex", bgTex);

        return bgTex;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
