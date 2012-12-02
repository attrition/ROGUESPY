using UnityEngine;
using System.Collections.Generic;

public enum EntState
{
    Player,
    Idle,
    Awake,
    Investigate,
    Active,
    Dead
}

public class Entity : MonoBehaviour
{
    public string Name;
    public float AP; // action points
    public float MaxAP; // max action points
    public int Health;
    public int MaxHealth;
    public float AccuracyMod; // acc * mod
    public IWeapon Weapon;
    public EntState State;
    public bool InSight;
    public Vector2 Goal;

    // graphics
    public Texture2D EntityTex;
    public Texture2D EntityStateTex;

    public int TileSize = 16;
    public float Scale = 2f;
    public Vector2 Position;

    public const int MOVE_AP_COST = 1;
    public const int TAKE_AP_COST = 1;

    private AudioClip silentShootSound;
    private AudioClip shootSound;
    private AudioClip hitSound;

    void Awake()
    {
        Position = Vector2.zero;
        InSight = false;
    }

    // Use this for initialization
    void Start()
    {
        InitMesh();

        this.gameObject.AddComponent<AudioSource>();
        audio.maxDistance = 100;
        audio.minDistance = 0;
        audio.loop = false;
        audio.volume = 1;

        silentShootSound = Resources.Load("Sounds/silencedShoot") as AudioClip;
        shootSound = Resources.Load("Sounds/shoot") as AudioClip;
        hitSound = Resources.Load("Sounds/hit") as AudioClip;
    }

    public void SetEntityStateTex()
    {
        var entTex = "";

        if (State == EntState.Awake)
            entTex = "ai-suspicious";
        else if (State == EntState.Investigate)
            entTex = "ai-investigate";
        else if (State == EntState.Active)
            entTex = "ai-active";

        if (entTex != "")
        {
            EntityStateTex = Instantiate(EntityTex) as Texture2D;
            var stateTex = Resources.Load("Textures/" + entTex) as Texture2D;

            for (int y = 0; y < TileSize; y++)
                for (int x = 0; x < TileSize; x++)
                    if (stateTex.GetPixel(x, y).a > 0)
                        EntityStateTex.SetPixel(x, y, stateTex.GetPixel(x, y));

            EntityStateTex.Apply();
            this.renderer.material.SetTexture("_MainTex", EntityStateTex);
        }
        else
            this.renderer.material.SetTexture("_MainTex", EntityTex);

    }

    public void DoAITurn(Map map, Entity player)
    {
        if (State == EntState.Player)
            return;

        SetEntityStateTex();

        bool vision = map.HasVision(Position, player.Position);

        if (vision)
        {
            State = EntState.Active;
            Goal = player.Position;
        }

        if (State == EntState.Idle)
            AP = 0;

        if (State == EntState.Awake)      // awake only switches to investigate on turn start
            State = EntState.Investigate; // so you can't double-trigger investigate

        if (State == EntState.Investigate)
        {
            WalkTowardsGoal(map);
            if (map.HasVision(Position, player.Position))
                State = EntState.Active;
        }
        else if (State == EntState.Active) //investigate or active not both, can't walk twice
        {
            var dist = (int)Vector2.Distance(Position, player.Position);

            // if can't see player, move towards goal
            if (!vision)
            {
                WalkTowardsGoal(map);
                Debug.Log(Name + ":no vision:1");
            }
            else
            {
                // close to mark range
                if (dist > Weapon.Mark && AP >= MOVE_AP_COST)
                {
                    WalkTowardsGoal(map);
                    Debug.Log(Name + ":walk:2");
                }
                else
                {
                    if (dist <= Weapon.Mark && AP >= Weapon.APCost)
                    {
                        Attack(player);
                        Debug.Log(Name + ":attack:3");
                    }
                    else
                    {
                        if (dist > 1 && AP >= MOVE_AP_COST)
                        {
                            WalkTowardsGoal(map);
                            Debug.Log(Name + ":walk:4");
                        }
                        else
                        {
                            AP = 0;
                            Debug.Log(Name + ":no move:5");
                        }
                    }
                }
            }

            if (AP < MOVE_AP_COST)
                AP = 0;
        }

        if (Position == Goal) // reached goal
        {
            // check vision again
            if (!map.HasVision(Position, player.Position))
                State = EntState.Idle;
        }

        SetEntityStateTex();
    }

    public bool CanFire()
    {
        return (AP >= Weapon.APCost);
    }

    public bool CanTake()
    {
        return (AP >= TAKE_AP_COST);
    }

    public int ChanceToHit(Entity ent)
    {
        var dist = (int)Vector2.Distance(Position, ent.Position);
        if (dist > Weapon.MaxRange)
            return 0;

        return Weapon.AccuracyChart[dist];
    }

    public void TakeItem(Item taken)
    {
        AP -= TAKE_AP_COST;
    }

    public void Attacked()
    {
        if (State != EntState.Player)
            State = EntState.Active;
    }

    public void Attack(Entity ent)
    {
        var dist = (int)Vector2.Distance(Position, ent.Position);
        var hit = Accuracy.AttemptHit(Weapon, dist, AccuracyMod);

        if (Weapon.Silenced)
            audio.PlayOneShot(silentShootSound);
        else
            audio.PlayOneShot(shootSound);

        SpawnBullet(Position, ent.Position);
        SpawnHitResult(ent.Position, hit);

        ent.Attacked();

        if (hit)
        {
            ent.Health -= Weapon.Damage;
            audio.PlayOneShot(hitSound);
        }

        if (ent.Health <= 0)
            ent.State = EntState.Dead;

        AP -= Weapon.APCost;
    }

    void SpawnHitResult(Vector2 pos, bool hit)
    {
        var go = new GameObject("HitResult");
        var hitResult = go.AddComponent<HitDisplay>();

        hitResult.TileSize = TileSize;
        hitResult.Scale = Scale;

        hitResult.SetPosition(pos);
        hitResult.SetResult(hit);
    }

    void SpawnBullet(Vector2 start, Vector2 end)
    {
        var bulletObj = new GameObject("Bullet");
        var bullet = bulletObj.AddComponent<Bullet>();

        bullet.TileSize = TileSize;
        bullet.Scale = Scale;
        bullet.SetPath(start, end);
    }

    public void WalkTowardsGoal(Map map)
    {
        var path = map.GetPath(Position, Goal);
        var next = path[0];

        Walk(next, false);
    }

    public void SetPosition(Vector2 newPos)
    {
        var tileScale = TileSize * Scale;
        this.gameObject.transform.position =
            new Vector3(newPos.x * tileScale, newPos.y * tileScale, -1);
        Position = newPos;
    }

    public void Walk(Vector2 pos, bool relative = true)
    {
        if (AP >= MOVE_AP_COST)
        {
            Vector2 newPos;
            if (relative)
                newPos = Position + pos;
            else
                newPos = pos;

            SetPosition(newPos);
            AP -= MOVE_AP_COST;
        }
    }

    void InitMesh()
    {
        var mesh = new Mesh();
        mesh.name = "Entity Mesh: " + Name;

        var tileScale = TileSize * Scale;
        mesh.vertices = new Vector3[4] { new Vector3(0, 0, 0), new Vector3(0, tileScale, 0),
            new Vector3(tileScale, tileScale, 0), new Vector3(tileScale, 0, 0) };
        mesh.triangles = new int[6] { 0, 1, 2, 2, 3, 0 };
        mesh.uv = new Vector2[4] { new Vector2(0, 0), new Vector2(0, 1), 
            new Vector2(1, 1), new Vector2(1, 0) };

        mesh.RecalculateNormals();

        var rend = this.gameObject.AddComponent<MeshRenderer>();
        var filt = this.gameObject.AddComponent<MeshFilter>();

        rend.material = Resources.Load("Materials/Entity") as Material;
        rend.material.SetTexture("_MainTex", EntityTex);

        filt.mesh = mesh;
    }

    void Update()
    {
        if (Name == "Player")
            InSight = true;

        var pos = this.gameObject.transform.position;

        if (InSight)
            pos.z = -2;
        else
            pos.z = -20; // behind camera

        this.gameObject.transform.position = pos;
    }
}
