using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour
{
    private Vector2 start;
    private Vector2 end;

    private const int bulletSize = 4;
    private const float bulletSpeed = 7f;

    public int TileSize;
    public float Scale;
    public float targetDistance;

    // Use this for initialization
    void Start()
    {
        CreateBulletMesh();
    }

    // Update is called once per frame
    void Update()
    {
        var pos = this.gameObject.transform.position;
        var line = end - start;
        var dt = Time.deltaTime * bulletSpeed;
        pos += new Vector3(line.x * dt, line.y * dt, 0);
        this.gameObject.transform.position = pos;

        if (Vector2.Distance(pos, start) > targetDistance)
        {
            Destroy(this.gameObject);
        }
    }

    private void CreateBulletMesh()
    {
        var mesh = new Mesh();
        mesh.name = "Bullet Mesh";

        mesh.vertices = new Vector3[4] { new Vector3(0, 0, 0), new Vector3(0, bulletSize, 0),
            new Vector3(bulletSize, bulletSize, 0), new Vector3(bulletSize, 0, 0) };
        mesh.triangles = new int[6] { 0, 1, 2, 2, 3, 0 };
        mesh.uv = new Vector2[4] { new Vector2(0, 0), new Vector2(0, 1),
            new Vector2(1, 1), new Vector2(1, 0) };

        mesh.RecalculateNormals();

        var rend = this.gameObject.AddComponent<MeshRenderer>();
        var filt = this.gameObject.AddComponent<MeshFilter>();

        filt.mesh = mesh;
        rend.material = Resources.Load("Materials/Bullet") as Material;
    }

    public void SetPath(Vector2 start, Vector2 end)
    {
        // scale and centre positions
        Vector3 bulletStart = (start * TileSize * Scale) + (Vector2.one * (TileSize / 2));
        Vector3 bulletEnd = (end * TileSize * Scale) + (Vector2.one * (TileSize / 2));
        
        // bullet plane is -5
        bulletStart.z = -5;
        bulletEnd.z = -5;

        this.transform.position = bulletStart;
        this.start = bulletStart;
        this.end = bulletEnd;
        this.targetDistance = Vector2.Distance(bulletStart, bulletEnd);
    }
}
