using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/*
 * Code in this class was in part found from videos in the following tutorial series
 * https://unity3d.com/learn/tutorials/s/procedural-cave-generation-tutorial
 * https://unity3d.com/learn/tutorials/s/2d-roguelike-tutorial
 */
public class RandomMapGenerator : MonoBehaviour {

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
    private int[,] map2; // used for cellular automota
    private Transform boardHolder;
    
    public GameObject[] floorTiles;     //Array of floor prefabs.
    public GameObject[] wallTiles;      //Array of wall prefabs.
    public GameObject[] outerWallTiles; //Array of outer tile prefabs.

    // Use this for initialization
    void Start() {
        GenerateMap();
    }

    private void Update() {
        if (Input.GetMouseButtonDown(0)) {
            GenerateMap();
        }
    }

    void GenerateMap() {
        map = new int[width, height];
        map2 = new int[width, height];
        if (boardHolder != null) {
            Destroy(boardHolder.gameObject);
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
            seed = DateTime.Now.Ticks.ToString();
        }

        // pseudo random number generator (17% is just a little Dota 2 joke)
        System.Random seventeenPercent = new System.Random(seed.GetHashCode());

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1) {
                    map[x, y] = 1;
                } else {
                    map[x, y] = seventeenPercent.Next(0, 100) < randomFillPercent ? 1 : 0;
                }
            }
        }
    }

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
                    if (neighbourX != x || neighbourY != y) {
                        wallCount += map[neighbourX, neighbourY];
                    }
                } else {
                    wallCount++;
                }
            }
        }

        return wallCount;
    }

    void BoardSetup() {
        boardHolder = new GameObject("Map").transform;

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                GameObject toInstantiate = floorTiles[UnityEngine.Random.Range(0, floorTiles.Length)];

                if (x == 0 || x == width - 1 || y == 0 || y == height - 1) {
                    toInstantiate = outerWallTiles[UnityEngine.Random.Range(0, outerWallTiles.Length)];
                } else if (map[x, y] == 1) {
                    toInstantiate = wallTiles[UnityEngine.Random.Range(0, wallTiles.Length)];
                }

                GameObject instance = Instantiate(toInstantiate, new Vector3(x, 0f, y), Quaternion.identity) as GameObject;

                instance.transform.SetParent(boardHolder);
                instance.transform.Rotate(new Vector3(90, 0, 0));
            }
        }
    }

    bool IsInMapRange(int x, int y) {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    List<Coord> GetRegionTiles(int startX, int startY) {
        List<Coord> tiles = new List<Coord>();
        int[,] mapFlags = new int[width, height];
        int tileType = map[startX, startY];

        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY));
        mapFlags[startX, startY] = 1;

        while (queue.Count > 0) {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

            for (int x = tile.x - 1; x <= tile.x + 1; x++) {
                for (int y = tile.y - 1; y <= tile.y + 1; y++) {
                    if (IsInMapRange(x, y) && (y == tile.y || x == tile.x)) {
                        if (mapFlags[x, y] == 0 && map[x, y] == tileType) {
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
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,] mapFlags = new int[width, height];

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (mapFlags[x, y] == 0 && map[x, y] == tileType) {
                    List<Coord> newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);

                    foreach (Coord tile in newRegion) {
                        mapFlags[tile.x, tile.y] = 1;
                    }
                }
            }
        }

        return regions;
    }

    void ProcessMap() {
        List<List<Coord>> wallRegions = GetRegions(1);

        foreach (List<Coord> wallRegion in wallRegions) {
            if (wallRegion.Count < wallClusterThresholdSize) {
                foreach (Coord tile in wallRegion) {
                    map[tile.x, tile.y] = 0;
                }
            }
        }

        List<List<Coord>> roomRegions = GetRegions(0);

        foreach (List<Coord> roomRegion in roomRegions) {
            if (roomRegion.Count < roomThresholdSize) {
                foreach (Coord tile in roomRegion) {
                    map[tile.x, tile.y] = 1;
                }
            }
        }
    }
}