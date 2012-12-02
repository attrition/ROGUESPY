using UnityEngine;
using System.Xml;
using System.Collections.Generic;

public enum State
{
    GameStarting,
    PlayerSelectAction,
    PlayerMove,
    PlayerTarget,
    PlayerSelectItem,
    AIActions,
    MissionComplete,
    GameComplete,
    GameOver,
}

public class Game : MonoBehaviour
{
    // visible information
    public TextAsset MissionFile;
    private TextAsset nextMissionFile;
    public int TileSize = 16;
    public float Scale = 2f;

    // mission details
    private int missionNum;
    private bool finalMission;

    // class necessities
    private BackgroundMaker bg;
    private GameObject playerObj;
    private XmlDocument doc;
    private GUILayer gui;
    private Map map;
    private Font font;
    private Overlay overlay;

    // entities
    private List<Entity> entities;
    private Stack<Entity> entsToProcess;
    private Entity player;
    private List<Item> items;
    private List<Exit> exits;
    private int totalObjectives;
    private int objectivesInHand;

    // gui needs
    private int playerMenuItem;
    private int playerTargeting;
    private List<Entity> entitiesInSight;
    private bool itemInRange;
    private bool enemyInSight;
    private bool exitEnabled;

    // turn information
    private Entity turnEnt;
    private State state;

    // buttons
    private KeyCode AButton = KeyCode.Space;
    private KeyCode BButton = KeyCode.LeftControl;

    // ai options
    private float nextAIMove;
    private const float timeBetweenAIMoves = 0.8f;

    // menu keys
    private const int MENU_MOVE = 0;
    private const int MENU_FIRE = 1;
    private const int MENU_WAIT = 2;
    private const int MENU_TAKE = 3;
    private const int MENU_END = 3;

    // audio bits
    AudioClip moveSound;
    AudioClip selectSound;
    AudioClip pickupSound;
    AudioClip gameCompleteSound;
    AudioClip missionStartSound;
    AudioClip missionSuccessSound;
    AudioClip missionFailSound;

    void Awake()
    {
        bg = this.GetComponentInChildren<BackgroundMaker>();
        bg.scale = Scale;

        missionNum = 0;
        finalMission = false;
    }

    // Use this for initialization
    void Start()
    {
        InitSounds();
        InitFont();
        InitGUI();
        InitGame();
    }

    void InitSounds()
    {
        this.gameObject.AddComponent<AudioSource>();
        audio.maxDistance = 100;
        audio.minDistance = 0;
        audio.loop = false;
        audio.volume = 1;
        audio.playOnAwake = false;

        moveSound = Resources.Load("Sounds/move") as AudioClip;
        selectSound = Resources.Load("Sounds/select") as AudioClip;
        pickupSound = Resources.Load("Sounds/pickup") as AudioClip;
        gameCompleteSound = Resources.Load("Sounds/game-complete") as AudioClip;
        missionStartSound = Resources.Load("Sounds/mission-start") as AudioClip;
        missionSuccessSound = Resources.Load("Sounds/mission-success") as AudioClip;
        missionFailSound = Resources.Load("Sounds/mission-fail") as AudioClip;
    }

    void NextTurn(bool firstTurn = false)
    {
        map.UpdateEntities(entities);

        if (entsToProcess.Count == 0)
        {
            entsToProcess = new Stack<Entity>();
            // add non-players first
            foreach (var ent in entities)
                if (ent != player)
                    entsToProcess.Push(ent);

            // player gets first move
            entsToProcess.Push(player);
        }

        turnEnt = entsToProcess.Pop();
        turnEnt.AP = turnEnt.MaxAP;

        if (turnEnt == player)
        {
            state = State.PlayerSelectAction;
            playerMenuItem = 0;

            CheckPlayerHasVisionOnEnemy();
            CheckForItemsInRange();
            CentreCameraOnEntity(player);
            DisplayPlayerTurnGUI();
        }
        else // ai
        {
            if (map.HasVision(turnEnt.Position, player.Position))
                CentreCameraOnEntity(turnEnt);

            nextAIMove = Time.time + timeBetweenAIMoves;
            state = State.AIActions;
            DisplayAITurnGUI();
        }
    }

    void DisplayPlayerTurnGUI()
    {
        gui.RefreshGUI();

        gui.AddWindow(96, 190, 70, 34, Color.gray);
        gui.AddWindow(98, 192, 66, 30, Color.blue);
        gui.AddText("YOUR TURN", 100, 208, Color.white);
        gui.AddText("AP:", 100, 194, Color.white);
        gui.AddText(player.AP.ToString(), 121, 194, Color.green);
        gui.AddText("HP:", 135, 194, Color.white);
        gui.AddText(player.Health.ToString(), 156, 194, Color.green);

        if (state == State.PlayerSelectAction)
        {
            gui.AddWindow(6, 126, 36, 80, Color.gray);
            gui.AddWindow(8, 128, 32, 76, Color.blue);

            var textColors = new Color[4] { Color.white, Color.gray, Color.white, Color.gray };

            if (enemyInSight)
            {
                textColors[MENU_FIRE] = Color.white;
                if (!player.CanFire())
                {
                    textColors[MENU_FIRE] = Color.red;
                    if (playerMenuItem == MENU_FIRE) // if you were on fire but cant fire
                        playerMenuItem = MENU_MOVE;  // go back to move action
                }
            }
            if (itemInRange)
                textColors[MENU_TAKE] = Color.white;
            else
                if (playerMenuItem == MENU_TAKE)
                    playerMenuItem = MENU_MOVE;

            textColors[playerMenuItem] = Color.yellow;

            gui.AddText("MOVE", 10, 190, textColors[MENU_MOVE]);
            gui.AddText("FIRE", 10, 170, textColors[MENU_FIRE]);
            gui.AddText("WAIT", 10, 150, textColors[MENU_WAIT]);
            gui.AddText("TAKE", 10, 130, textColors[MENU_TAKE]);

            var y = 188 - (playerMenuItem * 20);
            gui.AddTexture(Resources.Load("Textures/menu-select-arrow") as Texture2D, 42, y);
        }
        else if (state == State.PlayerMove)
        {
            gui.AddWindow(1, 190, 40, 34, Color.gray);
            gui.AddWindow(3, 192, 36, 30, Color.blue);

            gui.AddText("MOVE:", 5, 208, Color.white);
            gui.AddText(" 1AP ", 5, 194, Color.white);

        }
        else if (state == State.PlayerTarget)
        {
            gui.AddWindow(1, 176, 40, 48, Color.gray);
            gui.AddWindow(3, 178, 36, 44, Color.blue);

            gui.AddText("FIRE:", 5, 208, Color.white);
            gui.AddText(" " + player.Weapon.APCost + "AP ", 5, 194, Color.white);
            gui.AddText(" " + player.ChanceToHit(entitiesInSight[playerTargeting]).ToString() + "%",
                5, 180, Color.white);

            gui.AddText("PLAYER", 5, 44, Color.blue);
            gui.AddText(player.Weapon.Name, 5, 31, Color.white);
            gui.AddText("RNG: " + player.Weapon.MaxRange, 5, 18, Color.white);
            gui.AddText("DMG: " + player.Weapon.Damage, 5, 5, Color.white);

            var targeted = entitiesInSight[playerTargeting];
            gui.AddText(targeted.Name, 150, 31, Color.red);
            gui.AddText(targeted.Weapon.Name, 150, 18, Color.white);
            gui.AddText("HEALTH: " + targeted.Health, 150, 5, Color.white);
        }
        else if (state == State.PlayerSelectItem)
        {
            gui.AddWindow(1, 190, 40, 34, Color.gray);
            gui.AddWindow(3, 192, 36, 30, Color.blue);

            gui.AddText("TAKE:", 5, 208, Color.white);
            gui.AddText(" 1AP ", 5, 194, Color.white);

            var item = ItemUnderCursor();
            if (item != null)
                gui.AddText(item.Name, 5, 5, Color.white);
        }

        if (exitEnabled)
        {
            gui.AddWindow(179, 204, 76, 20, Color.gray);
            gui.AddWindow(181, 206, 72, 16, Color.blue);
            gui.AddText("FIND EXIT!", 183, 208, Color.yellow);
        }

        if (state != State.PlayerTarget && state != State.PlayerSelectItem)
        {
            gui.AddText("PLAYER", 5, 18, Color.white);
            gui.AddText(player.Weapon.Name, 5, 5, Color.white);
        }
    }

    Item ItemUnderCursor()
    {
        Item pickup = null;

        foreach (var item in items)
            if (item.Position == overlay.Position)
                pickup = item;

        return pickup;
    }

    void DisplayMissionGUI()
    {
        gui.RefreshGUI();

        gui.AddWindow(12, 143, 234, 67, Color.gray);
        gui.AddWindow(14, 145, 230, 63, Color.blue);

        gui.AddText(map.Name, 18, 193, Color.green);
        gui.AddText(map.Brief1, 18, 177, Color.white);
        gui.AddText(map.Brief2, 18, 164, Color.white);
        gui.AddText("PRESS SPACE TO BEGIN", 57, 147, Color.white);

        gui.AddWindow(12, 11, 234, 59, Color.gray);
        gui.AddWindow(14, 13, 230, 55, Color.blue);
        gui.AddText("CONTROLS:", 18, 54, Color.white);
        gui.AddText("ARROW KEYS: MOVE/TARGET/SELECT", 18, 41, Color.white);
        gui.AddText("SPACE: ACCEPT", 18, 28, Color.white);
        gui.AddText("LEFT CONTROL: CANCEL", 18, 15, Color.white);
    }

    void DisplayAITurnGUI()
    {
        gui.RefreshGUI();

        gui.AddWindow(99, 205, 63, 20, Color.gray);
        gui.AddWindow(101, 207, 59, 16, Color.blue);
        gui.AddText("CPU TURN", 103, 209, Color.white);

        if (map.HasVision(turnEnt.Position, player.Position))
        {
            gui.AddText(turnEnt.Name, 5, 31, Color.white);
            gui.AddText(turnEnt.Weapon.Name, 5, 18, Color.white);
            gui.AddText("HEALTH: " + turnEnt.Health, 5, 5, Color.white);
        }
    }

    void DisplayGameCompleteGUI()
    {
        gui.RefreshGUI();

        gui.AddWindow(44, 131, 165, 37, Color.black);
        gui.AddWindow(46, 133, 161, 33, Color.white);
        gui.AddWindow(48, 135, 157, 29, Color.blue);

        gui.AddText("YOU WIN!!", 100, 150, Color.green);
        gui.AddText("THANK YOU FOR PLAYING!", 50, 137, Color.white);
    }

    void DisplayGameOverGUI()
    {
        gui.RefreshGUI();

        gui.AddWindow(46, 133, 161, 33, Color.red);
        gui.AddWindow(48, 135, 157, 29, Color.black);

        gui.AddText("GAME OVER", 100, 150, Color.red);
        gui.AddText(" PRESS SPACE TO RETRY", 50, 137, Color.white);
    }

    void InitFont()
    {
        font = new Font();
    }

    void InitGUI()
    {
        var guiObj = new GameObject("GUI Layer");
        guiObj.transform.parent = Camera.main.transform;
        gui = guiObj.AddComponent<GUILayer>();
        gui.Scale = Scale;
        gui.Font = font;

        overlay = new Overlay(TileSize, Scale);
    }

    void InitGame()
    {
        state = State.GameStarting;
        missionNum++;

        doc = new XmlDocument();
        doc.LoadXml(MissionFile.text);

        itemInRange = false;
        enemyInSight = false;
        exitEnabled = false;

        InitMissionDetails();
        InitMissionMap();
        InitEntities();

        CentreCameraOnEntity(player);
        
        if (missionNum == 1)
            audio.PlayOneShot(missionStartSound);
    }

    void InitMissionDetails()
    {
        var next = doc["mission"]["next-mission"];
        if (next != null)
        {
            nextMissionFile =
                Resources.Load("Missions/" + next.InnerText) as TextAsset;
        }
        else
            finalMission = true;
    }

    void InitMissionMap()
    {
        var mapTex = doc["mission"]["map"]["texture"].InnerText;
        var tex = Resources.Load("Textures/" + mapTex) as Texture2D;
        bg.InitMap(tex);

        map = new Map(doc);
    }

    void InitEntities()
    {
        XmlNode entityNodes = doc["mission"]["entities"];

        entities = new List<Entity>();
        items = new List<Item>();
        exits = new List<Exit>();
        entsToProcess = new Stack<Entity>();

        totalObjectives = 0;
        objectivesInHand = 0;

        foreach (XmlNode node in entityNodes)
        {
            var start = node.InnerText.Split(',');
            var loc = new Vector3(float.Parse(start[0]),
                float.Parse(start[1]), -1);

            if (node.Name == "player")
            {
                player = EntityFactory.MakePlayer(this.gameObject, loc, TileSize, Scale);
                entities.Add(player); // copied here for easier searching later
            }
            else if (node.Name == "secguard")
                entities.Add(EntityFactory.MakeSecurity(this.gameObject, loc, TileSize, Scale));
            else if (node.Name == "soldier")
                entities.Add(EntityFactory.MakeSoldier(this.gameObject, loc, TileSize, Scale));
            else if (node.Name == "agent")
                entities.Add(EntityFactory.MakeAgent(this.gameObject, loc, TileSize, Scale));
            else if (node.Name == "documents")
            {
                SpawnItem(ItemType.Document, loc);
                totalObjectives++;
            }
            else if (node.Name == "exit")
            {
                var exitObj = new GameObject("Exit");
                var exit = exitObj.AddComponent<Exit>();
                exit.TileSize = TileSize;
                exit.Scale = Scale;
                exit.SetPosition(loc);
                exits.Add(exit);
            }
        }
    }

    // convenience function for weapons mostly
    void SpawnItem(string type, Vector2 loc)
    {
        ItemType item = ItemType.Document;

        if (type == "Pistol")
            item = ItemType.Pistol;
        else if (type == "Silenced Pistol")
            item = ItemType.SilencedPistol;
        else if (type == "Rifle")
            item = ItemType.Rifle;

        SpawnItem(item, loc);
    }

    void SpawnItem(ItemType type, Vector2 loc)
    {
        var go = new GameObject("Item");
        var item = go.AddComponent<Item>();
        item.Type = type;

        var tileScale = TileSize * Scale;
        item.transform.position = new Vector3(loc.x * tileScale, loc.y * tileScale, -4);
        item.Position = loc;
        items.Add(item);
    }

    void EnableExits()
    {
        exitEnabled = true;
        foreach (var exit in exits)
            exit.EnableExit();
    }

    void EndMission()
    {
        state = State.GameStarting;
        MissionFile = nextMissionFile;
        WipeScene();

        if (finalMission)
        {
            state = State.GameComplete;
            audio.PlayOneShot(gameCompleteSound);
        }
        else
        {
            audio.PlayOneShot(missionSuccessSound);
            InitGame();
        }
    }

    void WipeScene()
    {
        foreach (var e in entities)
            DestroyImmediate(e.gameObject);
        foreach (var e in exits)
            DestroyImmediate(e.gameObject);
        foreach (var i in items)
            DestroyImmediate(i.gameObject);

        entities.Clear();
        exits.Clear();
        items.Clear();

        turnEnt = null;
        player = null;
    }

    // Update is called once per frame
    void Update()
    {
        if (state == State.MissionComplete)
        {
            EndMission();
            return;
        }

        while (GameObject.Find("Bullet") != null)
            return;

        CheckForDeadEntities();

        bool redraw = false;

        if (state == State.PlayerSelectAction && player.AP == 0)
            NextTurn();

        #region Player Input
        if (state == State.GameStarting)
        {
            redraw = true;
            if (Input.GetKeyDown(AButton))
            {
                NextTurn(true);
            }
        }
        else if (state == State.PlayerMove)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                AttemptMove(player, Vector2.right);
                redraw = true;
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                AttemptMove(player, -Vector2.right);
                redraw = true;
            }
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                AttemptMove(player, Vector2.up);
                redraw = true;
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                AttemptMove(player, -Vector2.up);
                redraw = true;
            }

            if (redraw) // moved
                audio.PlayOneShot(moveSound);

            // cancel state
            if (Input.GetKeyDown(BButton))
            {
                state = State.PlayerSelectAction;
                redraw = true;
            }
        }
        else if (state == State.PlayerSelectAction)
        {
            if (Input.GetKeyDown(AButton))
            {
                if (playerMenuItem == MENU_MOVE)
                    state = State.PlayerMove;
                if (playerMenuItem == MENU_FIRE)
                {
                    InitTargeting();
                }
                if (playerMenuItem == MENU_WAIT)
                {
                    NextTurn();
                    return;
                }
                if (playerMenuItem == MENU_TAKE)
                {
                    state = State.PlayerSelectItem;
                    overlay.SetOverlayMode(OverlayMode.Selection);
                    overlay.SetPosition(player.Position);
                    overlay.SetHidden(false);
                }

                redraw = true;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                if (playerMenuItem > 0)
                {
                    playerMenuItem--;

                    if (playerMenuItem == MENU_FIRE && (!enemyInSight || !player.CanFire()))
                        playerMenuItem--;
                }

                redraw = true;
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                if (playerMenuItem < MENU_END)
                {
                    playerMenuItem++;

                    if (playerMenuItem == MENU_FIRE && (!enemyInSight || !player.CanFire()))
                        playerMenuItem++;
                    if (playerMenuItem == MENU_TAKE && !itemInRange)
                        playerMenuItem--;
                }

                redraw = true;
            }
        }
        else if (state == State.PlayerTarget)
        {
            if (Input.GetKeyDown(BButton))
            {
                overlay.SetHidden(true);
                state = State.PlayerSelectAction;
                redraw = true;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                MoveOverlayToNextTarget(1);
                redraw = true;
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                MoveOverlayToNextTarget(-1);
                redraw = true;
            }
            else if (Input.GetKeyDown(AButton))
            {
                var ent = entitiesInSight[playerTargeting];
                player.Attack(ent);

                if (player.AP < 1)
                {
                    overlay.SetHidden(true);
                    NextTurn();
                }

                if (!player.CanFire())
                {
                    overlay.SetHidden(true);
                    state = State.PlayerSelectAction;
                }

                if (ent.State == EntState.Dead)
                    KillEntity(ent);

                // no enemies in sight, leave targeting mode (relevant with rifles)
                if (entitiesInSight == null || entitiesInSight.Count < 1)
                    state = State.PlayerSelectAction;
                redraw = true;
            }
        }
        else if (state == State.PlayerSelectItem)
        {
            if (Input.GetKeyDown(BButton))
            {
                state = State.PlayerSelectAction;
                overlay.SetHidden(true);
                redraw = true;
            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                if (overlay.Position.y == player.Position.y &&
                    overlay.Position.x < player.Position.x + 1)
                    overlay.Move(Vector2.right);
                redraw = true;
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                if (overlay.Position.y == player.Position.y &&
                    overlay.Position.x > player.Position.x - 1)
                    overlay.Move(-Vector2.right);
                redraw = true;
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                if (overlay.Position.x == player.Position.x &&
                    overlay.Position.y > player.Position.y - 1)
                    overlay.Move(-Vector2.up);
                redraw = true;
            }
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                if (overlay.Position.x == player.Position.x &&
                    overlay.Position.y < player.Position.y + 1)
                    overlay.Move(Vector2.up);
                redraw = true;
            }

            if (redraw) // moved
                audio.PlayOneShot(selectSound);

            if (Input.GetKeyDown(AButton))
            {
                var pickup = ItemUnderCursor();

                if (pickup != null)
                {
                    audio.PlayOneShot(pickupSound);

                    if (pickup.Type == ItemType.Document)
                    {
                        objectivesInHand++;
                        if (objectivesInHand >= totalObjectives)
                            EnableExits();
                    }
                    else if (pickup.Type == ItemType.Pistol)
                    {
                        SpawnItem(player.Weapon.Name, player.Position);
                        player.Weapon = new Pistol();
                    }
                    else if (pickup.Type == ItemType.Rifle)
                    {
                        SpawnItem(player.Weapon.Name, player.Position);
                        player.Weapon = new Rifle();
                    }

                    player.TakeItem(pickup);
                    items.Remove(pickup);
                    Destroy(pickup.gameObject);

                    state = State.PlayerSelectAction;
                    redraw = true;
                    overlay.SetHidden(true);
                }
            }
        }
        else if (state == State.GameOver)
        {
            redraw = true;
            if (Input.GetKey(AButton))
            {
                WipeScene();
                InitGame();
            }
        }
        else if (state == State.GameComplete)
        {
            redraw = true;
        }
        #endregion

        #region AI
        if (state == State.AIActions)
        {
            bool doTurn = false;
            if (map.HasVision(player.Position, turnEnt.Position))
            {
                if (Time.time > nextAIMove)
                {
                    nextAIMove = Time.time + timeBetweenAIMoves;
                    doTurn = true;
                }
            }
            else
                doTurn = true;

            if (doTurn)
                turnEnt.DoAITurn(map, player);

            if (turnEnt.AP < Entity.MOVE_AP_COST)
            {
                Debug.Log("turn ends for " + turnEnt.Name);
                NextTurn();
            }
        }
        #endregion

        if (redraw)
        {
            CheckPlayerHasVisionOnEnemy();
            CheckEnemiesHaveVisionOnPlayer();
            CheckForItemsInRange();

            if (state == State.GameStarting)
                DisplayMissionGUI();
            else if (state == State.AIActions)
                DisplayAITurnGUI();
            else if (state == State.GameOver)
                DisplayGameOverGUI();
            else if (state == State.GameComplete)
                DisplayGameCompleteGUI();
            else
                DisplayPlayerTurnGUI();
        }
    }

    void CheckForItemsInRange()
    {
        itemInRange = false;
        foreach (var item in items)
        {
            if (Vector2.Distance(player.Position, item.Position) <= 1.01f)
                itemInRange = true;
        }
    }

    void CheckForDeadEntities()
    {
        var deadEnts = new List<Entity>();
        foreach (var ent in entities)
            if (ent.State == EntState.Dead)
                deadEnts.Add(ent);

        foreach (var dead in deadEnts)
            KillEntity(dead);
    }

    void KillEntity(Entity ent)
    {
        // add blood?
        if (ent != player)
        {
            SpawnItem(ent.Weapon.Name, ent.Position);

            entitiesInSight.Remove(ent);
            entities.Remove(ent);

            // remove ent from turn stack
            // temp push/pop it to another stack
            // don't push the dead ent back
            var tempStack = new Stack<Entity>();

            while (entsToProcess.Count > 0)
            {
                if (entsToProcess.Peek() != ent)
                    tempStack.Push(entsToProcess.Pop());
                else
                {
                    // skipped dead ent, we can push back from temp now
                    entsToProcess.Pop();
                    break;
                }
            }

            while (tempStack.Count > 0)
                entsToProcess.Push(tempStack.Pop());

            Destroy(ent.gameObject);
        }
        else
        {
            state = State.GameOver;
            audio.PlayOneShot(missionFailSound);
        }
    }

    void MoveOverlayToNextTarget(int direction)
    {
        playerTargeting += direction;

        if (playerTargeting < 0)
            playerTargeting = entitiesInSight.Count - 1;
        if (playerTargeting > entitiesInSight.Count - 1)
            playerTargeting = 0;

        overlay.SetPosition(entitiesInSight[playerTargeting].Position);
    }

    void InitTargeting()
    {
        entitiesInSight = new List<Entity>();

        foreach (var e in entities)
        {
            if (e == player)
                continue;

            if (map.HasVision(player.Position, e.Position) && 
                Vector2.Distance(player.Position, e.Position) < player.Weapon.MaxRange)
                entitiesInSight.Add(e);
        }

        if (entitiesInSight.Count > 0)
        {
            playerTargeting = 0;
            state = State.PlayerTarget;
            overlay.SetOverlayMode(OverlayMode.Targeting);
            overlay.SetHidden(false);
            overlay.SetPosition(entitiesInSight[playerTargeting].Position);
        }
        else
            state = State.PlayerSelectAction;
    }

    void AttemptMove(Entity ent, Vector2 move)
    {
        var check = ent.Position + move;
        if (ent.AP >= Entity.MOVE_AP_COST && map.IsInBounds(check) &&
            !map.IsCover(check) && !EntityAt(check))
            ent.Walk(move);

        map.UpdateEntities(entities);

        if (exitEnabled && PlayerOnExit())
            state = State.MissionComplete;
        else
        {
            CentreCameraOnEntity(player);

            if (ent.AP == 0)
                NextTurn();
        }
    }

    bool PlayerOnExit()
    {
        foreach (var exit in exits)
            if (player.Position == exit.Position)
                return true;

        return false;
    }

    bool EntityAt(Vector2 pos)
    {
        foreach (var ent in entities)
            if (ent.Position == pos)
                return true;

        return false;
    }

    void CentreCameraOnEntity(Entity ent)
    {
        var pos = ent.Position;

        Camera.main.transform.position = (new Vector3(pos.x, pos.y, 0) * TileSize * Scale);
        Camera.main.transform.position += new Vector3(0, 0, -10);

        var newGuiPos = Camera.main.transform.position;
        newGuiPos.x -= Screen.width / 2;
        newGuiPos.y -= Screen.height / 2;
        gui.gameObject.transform.position = newGuiPos;
    }

    void CheckEnemiesHaveVisionOnPlayer()
    {
        foreach (var e in entities)
        {
            if (e == player)
                continue;

            if (map.HasVision(e.Position, player.Position))
            {
                if (e.State == EntState.Investigate)
                {
                    e.State = EntState.Active;
                    e.SetEntityStateTex();
                }
                if (e.State == EntState.Idle)
                {
                    e.State = EntState.Awake;
                    e.SetEntityStateTex();
                }

                e.Goal = player.Position;
            }
        }
    }

    bool CheckPlayerHasVisionOnEnemy()
    {
        enemyInSight = false;

        foreach (var e in entities)
        {
            if (e == player)
                continue;

            if (map.HasVision(player.Position, e.Position))
            {
                enemyInSight = true;
                e.InSight = true;
            }
            else
            {
                e.InSight = false;
            }
        }

        return false;
    }

}
