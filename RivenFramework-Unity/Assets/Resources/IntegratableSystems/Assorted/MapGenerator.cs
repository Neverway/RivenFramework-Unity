//===================== (Neverway 2024) Written by Liz M. =====================
//
// Purpose:
// Notes:
//
//=============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MapGenerator : AutoGUIDObject<MapGenerator.SaveData>
{
    //=-----------------=
    // Public Variables
    //=-----------------=
    [Space]
    public Vector2 miniMapIconOffset = new Vector2(-40, +24 - (16 * 4));
    public float stupidLastMinuteXScaleForPlayerIcon = 1.3f;
    public Vector2Int VillageEntranceForMap;
    public GameObject spriteObjectTemplate;
    public Transform mapSpritesParent;
    public RectTransform mapPlayerIcon;
    public MapNodeSprite[] mapNodeSprites;
    public Sprite fallbackSprite;
    private Vector2 homePlayerIconPos;
    private Vector2 homePlayerPos;
    public bool generateStraightPath;
    public int straightPathColumnIndex;

    //=-----------------=
    // Private Variables
    //=-----------------=



    //Direction notes:
    //I wrote the map gen with +y as South and -y as North for some reason.
    //The debug print of the map is vertically inverted from the map in gameplay at the moment. :U
    //The mapNodes have a Paths array saying which sides of that tile connect to the nearby tile.
    //These are the 4 directions.
    private const int NORTH = 0;
    private const int SOUTH = 1;
    private const int WEST = 2;
    private const int EAST = 3;
    //I did not use these constants consistently throughout the algorithm.
    //Sorry bout that lmao maybe I'll refactor it some time.

    [SerializeField] private int mapWidth = 5; //width of node map
    [SerializeField] private int mapHeight = 5; //height of node map
    [SerializeField] private Vector2Int startPosition;
    private MapNode[,] mapNodes; //Grid of "rooms" (nodes) that the tiles are generated from

    public const int directionCount = 4; //The directions the random walk can go (change if modifying mapgen to use different grid)

    [SerializeField] private int roomWidth = 10; //width of rooms in tiles
    [SerializeField] private int roomHeight = 8; //width of rooms in tiles

    [SerializeField] private int pathWidth = 3; //Width of the path through the rooms
    private int pathRadius; //calculated path radius (saved for optimization)
    [SerializeField] private float pathWidthRandomness = 2; //Randomness amount for the path width (affects visuals only)

    private int branchLength; //ticks up how long the generator has gone without branching.
    [SerializeField] private int maxBranchLength; //at this length, we jump to a new location

    [SerializeField] private int seedToGenerate;

    private bool mapGenerated = false;
    private bool mapIsBeingLoaded;
    private List<GameObject> generatedObjects = new List<GameObject>();

    private int farthestDistance = 0;
    private Vector2Int farthestNode;

    //Used to track how many tiles have been generated, to decide when to place enemy locations.
    private int pathStepCounterForEnemies;
    [Tooltip("Generator will place an enemy spot every X tiles")]
    [SerializeField] private int enemyFrequency = 3;
    [Tooltip ("Minimum number of enemy spawns. If mapgen runs out of enemy spots, it will start adding them to random tiles.")]
    [SerializeField] private int minimumEnemyCount = 5;
    [Tooltip ("Minimum number of POI spawns. If mapgen runs out of standard spots (dead ends, certain intersections), it will start adding them to random tiles.")]
    [SerializeField] private int minimumPOICount = 0;

    private List<Vector2Int> poiLocations = new List<Vector2Int>();
    private List<Vector2Int> enemyLocations = new List<Vector2Int>();

    [SerializeField] private int numberOfLoops = 3;

    //=-----------------=
    // Reference Variables
    //=-----------------=

    [SerializeField] private Tilemap tilemapGround;
    [SerializeField] private RuleTile groundTile;
    [SerializeField] private Tilemap tilemapCollision;
    [SerializeField] private Tile collisionTile;
    [SerializeField] private Tile emptyTile;
    [SerializeField] private GameObject[] propList;
    [SerializeField] private GameObject[] treeList;

    [SerializeField] private GameObject[] poiList; //point of interest list. These get placed on deadends.
    [SerializeField] private List<GameObject> pathObjects; //Objects to generate randomly on the path
    [SerializeField] private List<OverworldEnemySpawner> enemySpawners; //Enemy spawn list. These get scattered at random.

    [SerializeField] private InteractableChestSpawner chestRecreator;

    [Header ("Day 2 specific:")]
    [SerializeField] private bool spawnPathToPatchyBoss = false;


    //=-----------------=
    // Mono Functions
    //=-----------------=
    public IEnumerator Start()
    {
        yield break;
        yield return null;
        homePlayerIconPos = mapPlayerIcon.transform.localPosition;
        homePlayerPos = GameInstance.Playerbody.transform.position;
    }

    public void Update()
    {
        Vector2 playerBodyPos = new(GameInstance.Playerbody.transform.position.x, GameInstance.Playerbody.transform.position.y);
        Vector2 playerPos = (playerBodyPos - homePlayerPos);
        playerPos.x /= roomWidth;
        playerPos.y /= roomHeight;
        playerBodyPos.x *= stupidLastMinuteXScaleForPlayerIcon;

        mapPlayerIcon.transform.localPosition = (playerBodyPos + homePlayerIconPos) + miniMapIconOffset;
    }
    //=-----------------=
    // Internal Functions
    //=-----------------=
    private string storedDebugLog;
    private void NewMapGenDebugLog() => storedDebugLog = "MAP GEN DEBUG:\n";
    private void LogStoredMapGenDebugLog() => Debug.Log(storedDebugLog);
    private void StoreDebugLogMapGenStep(string step) => storedDebugLog += step + "\n";
    private void StoreDebugLogPOILocations() => storedDebugLog += $"POI positions: {string.Join(',', poiLocations)}\n";
    private void StoreDebugLogMapSnapshot ()
    {
        storedDebugLog += "\n";
        Tuple<bool, bool, bool, bool, char>[] mapPathChars = 
            {
            //  NORTH,  SOUTH,  WEST,   EAST
            new(true , false, false, false, '╨'),
            new(false, true , false, false, '╥'),
            new(false, false, true , false, '╡'),
            new(false, false, false, true , '╞'),
            new(true , true , false, false, '║'),
            new(false, false, true , true , '═'),
            new(true , false, true , false, '╝'),
            new(true , false, false, true , '╚'),
            new(false, true , true , false, '╗'),
            new(false, true , false, true , '╔'),
            new(true , true , true , true , '╬'),
            new(false, true , true , true , '╦'),
            new(true , false, true , true , '╩'),
            new(true , true , true , true , '╬'),
            new(true , true , false, true , '╠'),
            new(true , true , true , false, '╣'),
        };
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                char selectedChar = '?';
                MapNode n = mapNodes[x, y];
                if (!mapNodes[x, y].visited)
                    selectedChar = '░';
                else
                {
                    foreach (var mapPathInfo in mapPathChars)
                        if (n.paths[0] == mapPathInfo.Item1 &&
                            n.paths[1] == mapPathInfo.Item2 &&
                            n.paths[2] == mapPathInfo.Item3 &&
                            n.paths[3] == mapPathInfo.Item4)
                        {
                            selectedChar = mapPathInfo.Item5;
                            break;
                        }
                }
                storedDebugLog += selectedChar;
            }
            storedDebugLog += "\n";
        }
    }
    private void CreateForestMiniMap()
    {
        for (int i = 0; i < mapSpritesParent.childCount; i++)
            Destroy(mapSpritesParent.GetChild(i).gameObject);

        for (int y = mapHeight - 1; y >= 0; y--)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                MapNode n = mapNodes[x, y];
                Sprite spriteToUse = fallbackSprite;
                foreach (MapNodeSprite spriteInfo in mapNodeSprites)
                {
                    
                    if (n.paths[1] == spriteInfo.exitNorth &&
                        n.paths[0] == spriteInfo.exitSouth &&
                        n.paths[2] == spriteInfo.exitWest &&
                        n.paths[3] == spriteInfo.exitEast)
                    {
                        spriteToUse = spriteInfo.sprite;
                        break;
                    }
                    
                }
                GameObject obj = Instantiate(spriteObjectTemplate);
                obj.GetComponent<Image>().sprite = spriteToUse;
                obj.transform.SetParent(mapSpritesParent.transform, false);
                obj.SetActive(true);
            }
        }
    }

    /// <param name="isNewMap">Whether or not the generated map will LOAD instances like chests and enemies, or generate new instances</param>
    private void GenerateMap(bool isNewMap = false)
    {
        mapIsBeingLoaded = !isNewMap;
        //Set the seed of the map
        Random.InitState (seedToGenerate);

        //Destroy current map if it is already generated
        if (mapGenerated) DestroyMap();
        NewMapGenDebugLog();

        branchLength = 0;
        mapNodes = new MapNode[mapWidth, mapHeight];
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                mapNodes[x, y] = new MapNode ();
            }
        }

        pathStepCounterForEnemies = 0;
        GenerateFromNode (startPosition.x, startPosition.y, -1);

         StoreDebugLogMapGenStep("Map Nodes Finished");
        if (generateStraightPath)
        {
            AddStraightPath();
        }

        mapNodes[startPosition.x, startPosition.y].paths[NORTH] = true;

        if (spawnPathToPatchyBoss)
        {
            AddPathToPatchyBoss ();
        }
        StoreDebugLogMapGenStep ("Map Nodes Finished");

        AddRandomLoops();
        GenerateTilesFromNodes ();
         StoreDebugLogMapGenStep("Map Tiles Finished");
        ScatterTrees ();
        PrintDistances ();
         StoreDebugLogPOILocations();

        MakeExtraPOIs ();
        MakeExtraEnemySpawns ();

        PlacePOIs ();
        PlaceEnemies ();


        LogStoredMapGenDebugLog();
        CreateForestMiniMap();
        mapGenerated = true;
    }

    /// <summary>
    /// Creates a path at the north end of the map, specifically for pumpkin patch boss.
    /// </summary>
    private void AddPathToPatchyBoss ()
    {
            mapNodes[2, 4].paths[SOUTH] = true;
    }

    /// <summary>
    /// If there's fewer enemyLocations than the required number of enemies, add more at random...
    /// </summary>
    private void MakeExtraEnemySpawns ()
    {
        if (enemyLocations.Count >= minimumEnemyCount)
        {
            return;
        }
        List<Vector2Int> randomTileChoices = new List<Vector2Int> ();
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector2Int location = new Vector2Int (x, y);
                //Avoid adding duplicate locations, also avoid the start tile where Autumn spawns
                if (!enemyLocations.Contains (location) && location != startPosition )
                {
                    randomTileChoices.Add (location);
                }
            }
        }
        RandomBag<Vector2Int> tileBag = new RandomBag<Vector2Int> (randomTileChoices);
        int dif = minimumEnemyCount - enemyLocations.Count;
        for (int i = 0; i < dif; i++)
        {
            enemyLocations.Add (tileBag.Grab ());
        }
    }

    /// <summary>
    /// If there's fewer poiLocations than the required amount, add more at random...
    /// </summary>
    private void MakeExtraPOIs()
    {
        if (poiLocations.Count >= minimumPOICount)
        {
            return;
        }
        List<Vector2Int> randomTileChoices = new List<Vector2Int> ();
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector2Int location = new Vector2Int (x, y);
                //Avoid adding duplicate locations, also avoid the start tile where Autumn spawns
                if (!poiLocations.Contains (location) && location != startPosition)
                {
                    randomTileChoices.Add (location);
                }
            }
        }
        RandomBag<Vector2Int> tileBag = new RandomBag<Vector2Int> (randomTileChoices);
        int dif = minimumPOICount - poiLocations.Count;
        for (int i = 0; i < dif; i++)
        {
            poiLocations.Add (tileBag.Grab ());
        }
    }

    private void AddRandomLoops()
    {
        List<Vector2Int> tiles = new List<Vector2Int> ();
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                tiles.Add (new Vector2Int (x, y));
            }
        }
        RandomBag<Vector2Int> bag = new RandomBag<Vector2Int>(tiles);
        for (int i = 0; i < numberOfLoops; i++)
        {
            while (true)
            {
                //Try adding a loop until it works.
                if (TryAddLoop(bag.Grab())) { break; }
            }
        }
    }

    private void AddStraightPath()
    {
        for (int y = 0; y < mapHeight; y++)
        {
            mapNodes[straightPathColumnIndex, y].paths[NORTH] = true;
            mapNodes[straightPathColumnIndex, y].paths[SOUTH] = true;
        }
    }

    private bool TryAddLoop(Vector2Int _pos)
    {
        List<int> validPaths = new List<int>();
        if (_pos.x < mapWidth - 1 && mapNodes[_pos.x, _pos.y].paths[EAST] == false)
        {
            validPaths.Add(EAST);
        }
        if (_pos.x > 0 && mapNodes[_pos.x, _pos.y].paths[WEST] == false)
        {
            validPaths.Add(WEST);
        }
        if (_pos.y > 0 && mapNodes[_pos.x, _pos.y].paths[NORTH] == false)
        {
            validPaths.Add(NORTH);
        }
        if (_pos.y < mapHeight-1 && mapNodes[_pos.x, _pos.y].paths[SOUTH] == false)
        {
            validPaths.Add(SOUTH);
        }
        if (validPaths.Count == 0)
        {
            return false;
        }
        RandomBag<int> bag = new RandomBag<int>(validPaths);
        AddPath(_pos, bag.Grab());
        StoreDebugLogMapSnapshot();
        return true;
    }
    /// <summary>
    /// Connects the given tile to a tile in the given direction.
    /// </summary>
    /// <param name="tile">The tile to path from</param>
    /// <param name="dir">The direction to make a path in.</param>
    private void AddPath(Vector2Int tile, int dir)
    {
        switch (dir) {
            case NORTH:
                {
                    mapNodes[tile.x, tile.y].paths[NORTH] = true;
                    mapNodes[tile.x, tile.y - 1].paths[SOUTH] = true;
                    break;
                }
            case SOUTH:
                {
                    mapNodes[tile.x, tile.y].paths[SOUTH] = true;
                    mapNodes[tile.x, tile.y + 1].paths[NORTH] = true;
                    break;
                }
            case WEST:
                {
                    mapNodes[tile.x, tile.y].paths[WEST] = true;
                    mapNodes[tile.x-1, tile.y].paths[EAST] = true;
                    break;
                }
            case EAST:
                {
                    mapNodes[tile.x, tile.y].paths[EAST] = true;
                    mapNodes[tile.x+1, tile.y].paths[WEST] = true;
                    break;
                }
        }
    }

    [ContextMenu("Destroy Map")]
    private void DestroyMap()
    {
        Debug.Log("MAP DESTROYED");

        tilemapGround.ClearAllTiles();
        tilemapCollision.ClearAllTiles();

        foreach (GameObject generated in generatedObjects)
        {
            if (generated != null)
                Destroy (generated);
        }
        mapGenerated = false;
        generatedObjects = new List<GameObject>();
    }

    private void PlacePOIs ()
    {
        //=== Create the random gameobject bag to grab from for POIs ========
        List<ICreatesGameObject> gameObjectCreators = new();
        //Add pois
        foreach (var poi in poiList) gameObjectCreators.Add(new BasicGameObjectCreator(poi));
        //Add createable chests (if this is a new map, otherwise use null for no object to be created)
        gameObjectCreators.Add(mapIsBeingLoaded ? new NoObjectCreator() : chestRecreator);
        RandomGameObjectBag gameObjectBag = new RandomGameObjectBag(gameObjectCreators);

        //=== Loop through the POI locations and place random objects at each point ========
        if (poiLocations.Count == 0) return;
        foreach (var loc in poiLocations)
        {
            //Generate the object
            ICreatesGameObject objCreator = gameObjectBag.Grab();
            GameObject poi = objCreator.CreateNew();
            if (poi == null) continue;

            if (poi.GetComponent<GUIDComponent>() == null)
                generatedObjects.Add(poi); //Add to generated objects list in case you need to destroy the map

            //Setup position of object
            poi.transform.position = new Vector3(
                loc.x * roomWidth + (roomWidth / 2),
                loc.y * roomHeight + (roomHeight / 2), 0);
        }
    }
    /// <summary>Places enemies from the enemyList in order until it runs out of enemy locations.</summary>
    private void PlaceEnemies ()
    {
        //=== Create the random gameobject bag to grab from for POIs ========
        List<ICreatesGameObject> gameObjectCreators = new();
        //Add path objects
        foreach (var obj in pathObjects) gameObjectCreators.Add(new BasicGameObjectCreator(obj));
        //Add spawned enemies (if this is a new map, otherwise use null for no object to be created)
        foreach (var enemySpawner in enemySpawners)
            gameObjectCreators.Add(mapIsBeingLoaded ? new NoObjectCreator() : enemySpawner);
        RandomGameObjectBag pathObjectsAndEnemies = new RandomGameObjectBag(gameObjectCreators);

        //=== Loop through the POI locations and place random objects at each point ========
        for (int i = 0; i < enemyLocations.Count; i++) 
        {
            //Generate the object
            ICreatesGameObject objCreator = pathObjectsAndEnemies.Grab();
            if (objCreator == null) continue;
            GameObject enemy = objCreator.CreateNew();
            if (enemy == null) continue;

            if (enemy.GetComponent<GUIDComponent>() == null)
                generatedObjects.Add (enemy); //Add to generated objects list in case you need to destroy the map

            //Setup position of object
            enemy.transform.position = new Vector3 (enemyLocations[i].x * roomWidth + (roomWidth / 2),
                enemyLocations[i].y * roomHeight + (roomHeight / 2), 0);
        }
    }

    private void PrintDistances ()
    {
        string p = "\n";
        for (int y = 0; y < mapHeight;y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                p += mapNodes[x, y].distanceFromStart.ToString ("D2")+",";
            }
            p += "\n";
        }
        StoreDebugLogMapGenStep(p);
    }

    private void ScatterTrees ()
    {
        for (int x = 0; x <mapWidth * roomWidth; x+=2)
        {
            for (int y = 0; y< mapHeight * roomHeight; y+=2)
            {
                if (tilemapCollision.GetTile (new Vector3Int (x, y, 0)) == collisionTile)
                {
                    PlaceProp (x+1, y, treeList);
                    PlaceProp (x+1, y, propList);
                }
            }
        }
    }

    private void PlaceProp (float x, float y, GameObject[] props)
    {
        if (props.Length == 0)
        {
            Debug.LogError ("A props list was empty. Skipping.");
            return;
        }
        GameObject prop = Instantiate (props[Random.Range(0, props.Length)]);
        prop.transform.position = new Vector3(
            x + Random.Range(-1f, 1f),
            y + Random.Range(-1f, 1f) + 1,
            prop.transform.position.z);

        generatedObjects.Add(prop);

        if (Random.Range (0f, 1f) > .5f)
        {
            // 50/50 chance to flip the prop
            prop.transform.localScale = new Vector3 (-prop.transform.localScale.x,prop.transform.localScale.y,prop.transform.localScale.z);
        }
    }

    private bool IsNodeWalkable (int x, int y)
    {
        if (x < 0 || y < 0) return false;
        if (x >= mapWidth || y >= mapHeight) return false;
        if (mapNodes[x, y].visited) return false;
        return true;
    }

    private void GenerateFromNode (int x, int y, int distanceFromStart, bool walking = false)
    {
        StoreDebugLogMapSnapshot ();
        
        branchLength++;
        if (!mapNodes[x, y].visited)
        {
            pathStepCounterForEnemies++;
            if (pathStepCounterForEnemies == enemyFrequency)
            {
                pathStepCounterForEnemies = 0;
                enemyLocations.Add (new Vector2Int(x, y));
            }
        }

        if (distanceFromStart > farthestDistance)
        {
            farthestDistance = distanceFromStart;
            farthestNode = new Vector2Int(x, y);
        }
        mapNodes[x, y].distanceFromStart = distanceFromStart;

        var node = mapNodes[x, y];
        node.visited = true;
        int possibleNodes = 0;
        bool[] foundPaths = new bool[4];
        if (IsNodeWalkable (x, y - 1))
        {
            possibleNodes++;
            foundPaths[0] = true;
        }
        if (IsNodeWalkable (x, y + 1))
        {
            possibleNodes++;
            foundPaths[1] = true;
        }
        if (IsNodeWalkable (x - 1, y))
        {
            possibleNodes++;
            foundPaths[2] = true;
        }
        if (IsNodeWalkable (x + 1, y))
        {
            possibleNodes++;
            foundPaths[3] = true;
        }
        if (possibleNodes == 0)
        {
            //Dead End
            if (walking)
            {
                poiLocations.Add(new Vector2Int(x, y));
                print("POI: " + new Vector2Int (x, y));
            }
            branchLength = 0;
            return;
        }

        if (branchLength > maxBranchLength)
        {
            branchLength = 0;
            PickRandomNode ();
            GenerateFromNode(x, y, distanceFromStart, true);
            return;
        }

        int rand = Random.Range (0, possibleNodes);
        int moveDirection = 0;
        int n = -1;
        //We're going to check foundPaths until
        //"n" is equal to our random number.
        //This selects a path at random.
        for (int i = 0; i < 4; i++)
        {
            if (foundPaths[i])
            {
                n++;
            }
            if (n == rand)
            {
                moveDirection = i;
                break;
            }
        }

        switch (moveDirection)
        {
            case 0:
                {
                    //north
                    node.paths[0] = true;
                    mapNodes[x, y - 1].paths[1] = true;
                    GenerateFromNode (x, y - 1, distanceFromStart+1, true);
                    break;
                }
            case 1:
                {
                    //south
                    node.paths[1] = true;
                    mapNodes[x, y + 1].paths[0] = true;
                    GenerateFromNode (x, y + 1, distanceFromStart+1, true);
                    break;
                }
            case 2:
                {
                    //west
                    node.paths[2] = true;
                    mapNodes[x - 1, y].paths[3] = true;
                    GenerateFromNode (x - 1, y, distanceFromStart+1, true);
                    break;
                }
            case 3:
                {
                    //east
                    node.paths[3] = true;
                    mapNodes[x + 1, y].paths[2] = true;
                    GenerateFromNode (x + 1, y, distanceFromStart+1, true);
                    break;
                }
        }
        GenerateFromNode (x, y, distanceFromStart);
    }

    private void PickRandomNode ()
    {
        List<Vector2Int> nodes = new List<Vector2Int>();
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (mapNodes[x, y].visited)
                {
                    if (
                        (x > 0 && !mapNodes[x - 1, y].visited)
                        || (x < mapWidth-1 && !mapNodes[x+1, y].visited)
                        || (y > 0 && !mapNodes[x, y-1].visited)
                        || (y < mapHeight - 1 && !mapNodes[x, y+1].visited)
                        )
                    {
                        //if the node has visit-able nodes, add it to a list.
                        nodes.Add(new Vector2Int(x, y));
                    }
                }
            }
        }
        if (nodes.Count == 0)
        {
            return;
        }
        int rand = Random.Range (0, nodes.Count);
        StoreDebugLogMapGenStep("Generating from " + nodes[rand].x + "," + nodes[rand].y);
        GenerateFromNode (nodes[rand].x, nodes[rand].y, mapNodes[nodes[rand].x, nodes[rand].y].distanceFromStart) ;
    }

    private void GenerateTilesFromNodes ()
    {
        pathRadius = pathWidth / 2;
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                PlaceNodeTiles (x * roomWidth, y * roomHeight, mapNodes[x, y]);
            }
        }
    }


    private void PlaceNodeTiles (int xoffset, int yoffset, MapNode node)
    {
        for (int x = 0; x < roomWidth; x++)
        {
            for (int y = 0; y < roomHeight; y++)
            {
                tilemapCollision.SetTile(new Vector3Int(x+xoffset, y+yoffset), collisionTile);
            }
        }
        if (node.paths[0])
        {
            //north exit
            for (int y = 0; y < roomHeight / 2; y++)
            {
                SplatGround (roomWidth / 2 + xoffset, y + yoffset);
            }
        }
        if (node.paths[1])
        {
            //south exit
            for (int y = roomHeight / 2; y < roomHeight; y++)
            {
                SplatGround (roomWidth / 2 + xoffset, y + yoffset);
            }
        }
        if (node.paths[2])
        {
            //west exit
            for (int x = 0; x < roomWidth / 2; x++)
            {
                SplatGround (x + xoffset, roomHeight / 2 + yoffset);
            }
        }
        if (node.paths[3])
        {
            //east exit
            for (int x = roomWidth / 2; x < roomWidth; x++)
            {
                SplatGround (x + xoffset, roomHeight / 2 + yoffset);
            }
        }
    }

    private void SplatGround (int _x, int _y)
    {
        int collRadius = pathRadius + (int)(pathWidthRandomness/2);
        for (int x = -collRadius; x < collRadius; x++)
        {
            for (int y = -collRadius; y < collRadius; y++)
            {
                tilemapCollision.SetTile (new Vector3Int (_x + x, _y + y), null);
            }
        }

        float randomRadius = (float)pathRadius + .5f + Random.Range (0f, pathWidthRandomness) - (pathWidthRandomness/2f);
        for (int x = -(int)randomRadius; x < (int)randomRadius; x++)
        {
            for (int y = -(int)randomRadius; y < (int)randomRadius; y++)
            {
                if (new Vector2(x,y).magnitude < randomRadius)
                {
                    tilemapGround.SetTile (new Vector3Int(_x + x, _y + y), groundTile);
                }
            }
        }
    }

    //=-----------------=
    // External Functions
    //=-----------------=



    // SaveData handling ----------------------------------------------------------------------------------------
    public override SaveData OnSaveInstance() => new SaveData
    {
        previouslyGeneratedSeed = mapGenerated ? seedToGenerate : 0
    };

    public override void OnLoadInstance(SaveData data)
    {
        Debug.Log(GI_SaveSystem.CurrentSavingType);
        int newSeed = GameInstance.Gamestate.GetCycleSubSeed(GetGUID());
        if (mapGenerated)
        {
            if (seedToGenerate == newSeed)
                return;
            DestroyMap();
        }
        seedToGenerate = GameInstance.Gamestate.GetCycleSubSeed(GetGUID());
        GenerateMap(isNewMap: seedToGenerate != data.previouslyGeneratedSeed);
    }

    public override void OnNewInstance() 
    {
        seedToGenerate = GameInstance.Gamestate.GetCycleSubSeed(GetGUID());
        GenerateMap(isNewMap: true);
    }

    [Serializable]
    public struct SaveData
    {
        public int previouslyGeneratedSeed;
    }
}

class MapNode
{
    public bool[] paths;
    public bool visited = false;
    public int distanceFromStart;

    public MapNode ()
    {
        paths = new bool[MapGenerator.directionCount];
    }
}

//Setup for RandomBag to work with mixing different methods of generating GameObjects -------------------------------------------------

public class RandomGameObjectBag : RandomBag<ICreatesGameObject>
{
    public RandomGameObjectBag(List<ICreatesGameObject> _sourceList) : base(_sourceList) { }
}

public interface ICreatesGameObject
{
    public GameObject CreateNew()
    {
        //Ensure only ONE random call happens instead of uncertain amounts
        int newSeed = Random.Range(int.MinValue, int.MaxValue);
        var oldSeedState = Random.state;
        Random.InitState(newSeed);
        GameObject created = GetCreatedGameObject();
        Random.state = oldSeedState;
        return created;
    }
    protected GameObject GetCreatedGameObject();
}
public class NoObjectCreator : ICreatesGameObject
{
    GameObject ICreatesGameObject.GetCreatedGameObject() => null;
}
public class BasicGameObjectCreator : ICreatesGameObject
{
    public BasicGameObjectCreator(GameObject toCreate) => this.toCreate = toCreate;
    public GameObject toCreate;
    GameObject ICreatesGameObject.GetCreatedGameObject() => GameObject.Instantiate(toCreate);
}
[Serializable]
public struct MapNodeSprite
{
    public bool exitNorth, exitEast, exitWest, exitSouth;
    public Sprite sprite;
}