using System;
using System.Collections.Generic;

using UnityEngine;

public class EntityFactory : MonoBehaviour
{
    private static Entity SpawnEntity(GameObject parent, string name, Vector3 loc)
    {
        var go = new GameObject(name);
        go.transform.position = loc;
        go.transform.parent = parent.transform;
        return go.AddComponent<Entity>();
    }

    private static void StandardInit(Entity ent, string name, int tileSize, float scale, Vector2 pos)
    {
        ent.Name = name;
        ent.Scale = scale;
        ent.TileSize = tileSize;
        ent.SetPosition(pos);
    }

    public static Entity MakePlayer(GameObject parent, Vector3 pos, int tileSize, float scale)
    {
        var name = "Player";
        var ent = EntityFactory.SpawnEntity(parent, name, pos);
        ent.EntityTex = Instantiate(Resources.Load("Textures/Player-k")) as Texture2D;
        
        ent.MaxAP = 5;
        ent.Health = 4;
        ent.MaxHealth = 4;
        ent.AccuracyMod = 1.0f;
        ent.Weapon = new SilencedPistol();
        ent.State = EntState.Player;
        EntityFactory.StandardInit(ent, name, tileSize, scale, pos);
        return ent;
    }

    public static Entity MakeSecurity(GameObject parent, Vector3 pos, int tileSize, float scale)
    {
        var name = "Security Guard";
        var ent = EntityFactory.SpawnEntity(parent, name, pos);
        ent.EntityTex = Instantiate(Resources.Load("Textures/Security-k")) as Texture2D;

        ent.MaxAP = 3;
        ent.Health = 1;
        ent.MaxHealth = 1;
        ent.Weapon = new Pistol();
        ent.AccuracyMod = 0.66f;
        ent.State = EntState.Idle;
        ent.InSight = false;
        EntityFactory.StandardInit(ent, name, tileSize, scale, pos);

        return ent;
    }

    public static Entity MakeSoldier(GameObject parent, Vector3 pos, int tileSize, float scale)
    {
        var name = "Soldier";
        var ent = EntityFactory.SpawnEntity(parent, name, pos);
        ent.EntityTex = Instantiate(Resources.Load("Textures/Soldier-k")) as Texture2D;

        ent.MaxAP = 4;
        ent.Health = 2;
        ent.MaxHealth = 2;
        ent.Weapon = new Rifle();
        ent.AccuracyMod = 0.9f;
        ent.State = EntState.Idle;
        ent.InSight = false;
        EntityFactory.StandardInit(ent, name, tileSize, scale, pos);

        return ent;
    }

    public static Entity MakeAgent(GameObject parent, Vector3 pos, int tileSize, float scale)
    {
        var name = "Agent";
        var ent = EntityFactory.SpawnEntity(parent, name, pos);
        ent.EntityTex = Instantiate(Resources.Load("Textures/Agent-k")) as Texture2D;

        ent.MaxAP = 8;
        ent.Health = 2;
        ent.MaxHealth = 3;
        ent.Weapon = new Pistol();
        ent.AccuracyMod = 1f;
        ent.State = EntState.Idle;
        ent.InSight = false;
        EntityFactory.StandardInit(ent, name, tileSize, scale, pos);

        return ent;
    }
}
