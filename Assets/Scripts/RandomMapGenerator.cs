using System.Collections.Generic;
using UnityEngine;
using System;

/*
 * Code in this class was in part found from videos in the following tutorial series
 * https://unity3d.com/learn/tutorials/s/procedural-cave-generation-tutorial
 * https://unity3d.com/learn/tutorials/s/2d-roguelike-tutorial
 */
public class RandomMapGenerator : MonoBehaviour {

    // this struct is used to help store the information of coordinates without creating a whole class to handle it
    struct Coord {
        public int x, y;

        public Coord(int x, int y) {
            this.x = x;
            this.y = y;
        }
    }

    public int width, height, smoothLevel, wallClusterThresholdSize, roomThresholdSize;
    public string seed;
    public bool useRandomSeed;
    [Range(40, 60)] // numbers outside of this range produce completely unusable maps
    public int randomFillPercent;

    private int[,] map;
    private int[,] map2; // used for cellular automota smoothing, to remove "corruption" from the alogorithm by modifying the original
    private Transform boardHolder;
    
    public GameObject[] floorTiles;     //Array of floor prefabs.
    public GameObject[] wallTiles;      //Array of wall prefabs.
    public GameObject[] outerWallTiles; //Array of outer tile prefabs.

    // Use this for initialization
    void Start() {
        GenerateMap();
    }

    private void Update() {
        // generate new map on left mouse button click
        if (Input.GetMouseButtonDown(0)) {
            GenerateMap();
        }
    }

    void GenerateMap() {
        // clearing the map arrays for new data
        map = new int[width, height];
        map2 = new int[width, height];
        if (boardHolder != null) {
            Destroy(boardHolder.gameObject); // clearing the map game object, otherwise the map starts to clear out
        }
        RandomFillMap();

        for (int i = 0; i < smoothLevel; i++) {
            SmoothMap();
            map = map2;
        }

        ProcessMap();
        BoardSetup();
    }

    void RandomFillMap() {
        if (useRandomSeed) {
            seed = DateTime.Now.Ticks.ToString(); // get random seed from current date and time of the system. Time.time was always 0
        }

        // pseudo random number generator (17% is just a little Dota 2 joke)
        System.Random seventeenPercent = new System.Random(seed.GetHashCode());

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                // if the spot is along the edge of the map
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1) {
                    map[x, y] = 1;
                } else {
                    // space is a wall (1) if the next number generated is less than the fill percent
                    map[x, y] = seventeenPercent.Next(0, 100) < randomFillPercent ? 1 : 0;
                }
            }
        }
    }

    // this is the meat of the cellular automota, where it looks at its neighbors to determine if it should change
    void SmoothMap() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                int wallCount = GetSurroundingWallCount(x, y);
                if (wallCount > 4) {
                    map2[x, y] = 1;
                } else if (wallCount < 4) {
                    map2[x, y] = 0;
                }
            }
        }
    }

    int GetSurroundingWallCount(int x, int y) {
        int wallCount = 0;

        for (int neighbourX = x - 1; neighbourX <= x + 1; neighbourX++) {
            for (int neighbourY = y - 1; neighbourY <= y + 1; neighbourY++) {
                // if indices are inside the grid and we're not looking at currently selected tile
                if (IsInMapRange(neighbourX, neighbourY)) {
                    // if we are not looking at the currently selected cell
                    if (neighbourX != x || neighbourY != y) {
                        wallCount += map[neighbourX, neighbourY]; // walls are 1, floors are 0
                    }
                } else {
                    wallCount++;
                }
            }
        }

        return wallCount;
    }

    // this function is simply used to place real objects on the map instead of just having gizmos drawn only visible in the scene editor
    void BoardSetup() {
        boardHolder = new GameObject("Map").transform;

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                GameObject toInstantiate = floorTiles[UnityEngine.Random.Range(0, floorTiles.Length)]; // assume floor to start

                if (x == 0 || x == width - 1 || y == 0 || y == height - 1) { // tile is outer wall
                    toInstantiate = outerWallTiles[UnityEngine.Random.Range(0, outerWallTiles.Length)];
                } else if (map[x, y] == 1) { // tile is inner wall
                    toInstantiate = wallTiles[UnityEngine.Random.Range(0, wallTiles.Length)];
                }

                GameObject instance = Instantiate(toInstantiate, new Vector3(x, 0f, y), Quaternion.identity) as GameObject;

                instance.transform.SetParent(boardHolder);
                instance.transform.Rotate(new Vector3(90, 0, 0)); // rotate the tile to face the camera
            }
        }
    }

    // refactored line that was used in multiple places
    bool IsInMapRange(int x, int y) {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    // performs the flood fill algorithm to find a region
    List<Coord> GetRegionTiles(int startX, int startY) {
        List<Coord> tiles = new List<Coord>(); // list of all tiles in the current region
        int[,] mapFlags = new int[width, height];
        int tileType = map[startX, startY];

        // add first coordinate to the queue and mark it looked at
        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY));
        mapFlags[startX, startY] = 1;

        while (queue.Count > 0) {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

            for (int x = tile.x - 1; x <= tile.x + 1; x++) {
                for (int y = tile.y - 1; y <= tile.y + 1; y++) {
                    // if the tile is in the map and in a cardinal direction of current tile
                    if (IsInMapRange(x, y) && (y == tile.y || x == tile.x)) {
                        // if we haven't looked at this tile yet and it's of the tile type we are checking
                        if (mapFlags[x, y] == 0 && map[x, y] == tileType) {
                            // add the tile to the queue (for checking around it) and mark it as looked at
                            mapFlags[x, y] = 1;
                            queue.Enqueue(new Coord(x, y)); 
                        }
                    }
                }
            }
        }

        return tiles;
    }

    List<List<Coord>> GetRegions(int tileType) {
        List<List<Coord>> regions = new List<List<Coord>>(); // list of all regions of given type
        int[,] mapFlags = new int[width, height];

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                // if the tile hasn't been looked at and is of the type we are looking for
                if (mapFlags[x, y] == 0 && map[x, y] == tileType) {
                    List<Coord> newRegion = GetRegionTiles(x, y); // get the region this tile is in
                    regions.Add(newRegion);

                    foreach (Coord tile in newRegion) {
                        mapFlags[tile.x, tile.y] = 1; // mark all tiles in the newly found region as looked at
                    }
                }
            }
        }

        return regions;
    }

    // this function goes through all of the regions and clears out any that are below a certain threshold in size
    void ProcessMap() {
        List<List<Coord>> wallRegions = GetRegions(1);

        foreach (List<Coord> wallRegion in wallRegions) {
            if (wallRegion.Count < wallClusterThresholdSize) {
                foreach (Coord tile in wallRegion) {
                    map[tile.x, tile.y] = 0; // nuke the walls
                }
            }
        }

        List<List<Coord>> roomRegions = GetRegions(0);

        foreach (List<Coord> roomRegion in roomRegions) {
            if (roomRegion.Count < roomThresholdSize) {
                foreach (Coord tile in roomRegion) {
                    map[tile.x, tile.y] = 1; // nuke the floors
                }
            }
        }
    }
}